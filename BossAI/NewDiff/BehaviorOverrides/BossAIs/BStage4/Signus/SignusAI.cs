using System;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.Signus
{
    // 西格纳斯 — 幽影刺客. 设计文档: 大计划/L 西格纳斯/西格纳斯_重置版设计文档.md
    // 移动哲学(分寸感): 隐身期从不直线追人 — 它在玩家侧翼的"影子车道"里游走(正弦飘移),
    // 每次出手前用带尘埃汇聚预告的瞬移(高频瞬移是设计文档对P2的明确要求)落位, 出手后退回车道.
    // 隐身期间接触伤害为0 (被看不见的身体撞死不公平); 只有破隐窗口和死亡升华冲刺期间有接触伤害.
    internal sealed class SignusAI : IUMWBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/Signus").Type;
        public override string BossName => "Signus";
        public override Color DebugColor => new(160, 50, 220);

        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.35f;

        private static readonly Color VoidPurple = new(160, 60, 220);
        private static readonly Color VoidBright = new(220, 160, 255);
        #endregion

        #region Attack States
        public enum AttackState
        {
            CosmicKunai = 0,
            Cosmilamp = 1,
            AethersWhisper = 2,
            DeathsAscension = 3,
            EmpyreanKnives = 4,
            KingConstellations = 5,
            MagneticMeltdown = 6,
            Nadir = 7,
            SevensStriker = 8,
            VenusianTrident = 9,
            RealityRupture = 10,
            Transition = 11,
        }

        private static bool IsP1(AttackState s) => s == AttackState.CosmicKunai || s == AttackState.Cosmilamp;

        // P1 per design doc: Kunai -> Lamp -> loop, each executed 3 full times before rotating.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.CosmicKunai, AttackState.Cosmilamp,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.AethersWhisper, AttackState.DeathsAscension, AttackState.EmpyreanKnives,
            AttackState.KingConstellations, AttackState.MagneticMeltdown, AttackState.Nadir,
            AttackState.SevensStriker, AttackState.VenusianTrident, AttackState.RealityRupture,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        // Phantom Evasion Step
        private bool coreExposed = false;
        private bool wasCoreExposed = false;
        private int stunTimer = 0;

        // Twisting Mine Grid
        private int mineGridTimer = 0;
        private readonly Vector2[] minePositions = new Vector2[6];
        private bool minesActive = false;

        private int arenaHurtCooldown = 0;
        private float transitionFlashAlpha = 0f;

        // Per-attack A/B variant toggle: flips deterministically each time that attack comes up (no RNG).
        private readonly bool[] attackVariant = new bool[12];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }
        private bool currentVariantB = false;

        // Telegraphed blink system — the assassin's teleports must read as intent, not glitches.
        private int blinkTimer = 0;
        private int blinkDuration = 0;
        private Vector2 blinkDestination = Vector2.Zero;

        // Shadow-lane drift phase for the sinusoidal weave
        private float weavePhase = 0f;

        // Death's Ascension committed dash: the only time the cloaked body itself is lethal
        private bool dashContact = false;

        // Motion afterimages — give weight to blinks and the scythe dash
        private readonly Vector2[] oldPos = new Vector2[9];
        private int oldPosIndex = 0;

        private int hitFxCooldown = 0;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            ticksRunning++;

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float tracker = ref npc.ai[3];

            npc.defense = npc.defDefense;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.CosmicKunai;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                currentVariantB = UseVariantB(state);
                npc.netUpdate = true;
            }

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = lifeRatio <= PhaseLifeRatios[0] ? 2 : 1;

            if (nextPhase > currentPhase && state != AttackState.Transition)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                state = AttackState.Transition;
                npc.ai[1] = (float)state;
                timer = 0;
                tracker = 0;
                CleanupAttackProjectiles(npc, alsoMines: true);
                npc.netUpdate = true;
            }

            // Frame-fresh flags; attacks re-assert them below.
            coreExposed = false;
            dashContact = false;

            if (stunTimer > 0)
            {
                stunTimer--;
                npc.velocity *= 0.9f;
                npc.alpha = 0;
                npc.rotation = MathF.Sin(ticksRunning * 0.35f) * 0.18f; // dazed wobble
                coreExposed = true; // the stun IS the punish window — core stays readable

                // Sparks raining off the stunned assassin
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(46f, 46f), DustID.PurpleTorch, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(1f, 3f)), 100, default, 1.2f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            UpdateMineGrid(npc, target, currentPhase);
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            if (hitFxCooldown > 0) hitFxCooldown--;

            float borderSize = currentPhase == 1 ? 1400f : 1000f;
            UpdateArenaBorder(npc, target, borderSize);

            if (stunTimer == 0 && blinkDuration <= 0)
            {
                switch (state)
                {
                    case AttackState.CosmicKunai: ExecuteCosmicKunai(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.Cosmilamp: ExecuteCosmilamp(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.AethersWhisper: ExecuteAethersWhisper(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.DeathsAscension: ExecuteDeathsAscension(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.EmpyreanKnives: ExecuteEmpyreanKnives(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.KingConstellations: ExecuteKingConstellations(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.MagneticMeltdown: ExecuteMagneticMeltdown(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.Nadir: ExecuteNadir(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.SevensStriker: ExecuteSevensStriker(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.VenusianTrident: ExecuteVenusianTrident(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.RealityRupture: ExecuteRealityRupture(npc, target, ref timer, ref tracker, currentPhase); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref tracker, currentPhase); break;
                }
            }
            else if (blinkDuration > 0 && state != AttackState.Transition)
            {
                // Attack timelines keep ticking through a blink so blink windups are part of each attack's rhythm
                timer++;
            }

            // Cloak: 90% transparent while gliding, fully visible while the core is exposed.
            // Contact damage only exists when you can SEE the body (expose window / scythe dash).
            if (stunTimer == 0 && blinkDuration <= 0)
                npc.alpha = coreExposed ? 0 : 230;
            npc.damage = (coreExposed || dashContact) && stunTimer == 0 ? npc.defDamage : 0;

            // Core sparkle: converging motes make the 0.5s/0.3s punish window pop against the dark
            if (coreExposed && stunTimer == 0 && Main.rand.NextBool(2))
            {
                Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(64f, 64f);
                Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, (npc.Center - around) * 0.11f, 100, VoidBright, 1.25f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            // P2 decloak counterattack (design doc): every exposure fires 4 reflective kunai.
            if (coreExposed && !wasCoreExposed && !IsP1(state) && state != AttackState.Transition && stunTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathHelper.TwoPi / 4f + MathHelper.PiOver4;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, a.ToRotationVector2() * 9f, ModContent.ProjectileType<ReflectKunaiProj>(), npc.damage > 0 ? npc.damage / 4 : npc.defDamage / 4, 0f, Main.myPlayer);
                }
            }
            wasCoreExposed = coreExposed;

            // Ghostly rotation: lean into motion plus a slow hover bob
            if (stunTimer == 0)
                npc.rotation = npc.velocity.X * 0.05f + MathF.Sin(ticksRunning * 0.05f) * 0.04f;

            // The blink owns velocity/alpha/position while active (runs last, like Cryogen's teleport)
            UpdateBlink(npc);

            // Afterimage trail (recorded after the blink so ghosts follow the real body)
            oldPos[oldPosIndex] = npc.Center;
            oldPosIndex = (oldPosIndex + 1) % oldPos.Length;

            data.CurrentPhase = currentPhase;
            data.AttackState = (IUMWAttackState)Math.Clamp((int)state, 0, 4);
            data.PatternTimer = (int)timer;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ProcessCoreHits(npc, ref modifiers, item.damage);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ProcessCoreHits(npc, ref modifiers, projectile.damage);

        private void ProcessCoreHits(NPC npc, ref NPC.HitModifiers modifiers, int damage)
        {
            if (npc.ai[1] == (float)AttackState.Transition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }
            if (stunTimer > 0)
                return;

            if (!coreExposed)
            {
                modifiers.FinalDamage *= 0f; // immune while cloaked
                if (hitFxCooldown <= 0)
                {
                    // Shots phase through the ghost — a whiff of shadow so the immunity reads as a mechanic, not a bug
                    hitFxCooldown = 12;
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.25f, Pitch = 0.4f }, npc.Center);
                    SignusFx.Burst(npc.Center, 2.5f, 5, DustID.Shadowflame);
                }
                return;
            }

            if (damage > 80)
            {
                stunTimer = 180; // 3s interrupt stun (design doc)
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = -0.4f }, npc.Center);
                SignusFx.Burst(npc.Center, 6f, 30);
                SignusFx.Burst(npc.Center, 3f, 12, DustID.Shadowflame);
                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 6f;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Movement & Blink Helpers
        private void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.14f);
        }

        // The shadow lane: flank-hover with a sinusoidal weave. Never a beeline at the player.
        private void CloakedDrift(NPC npc, Player target, float sideDist, float height, float accel, float maxSpeed)
        {
            weavePhase += 0.045f;
            Vector2 spot = DirectedHoverSpot(npc, target, sideDist, height, 9f);
            spot += new Vector2(MathF.Sin(weavePhase) * 64f, MathF.Cos(weavePhase * 0.7f) * 46f);
            SmoothMove(npc, spot, accel, maxSpeed);
        }

        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f) side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        // Telegraphed blink: shadow bleeds off the body while void-dust converges on the destination, then it reforms.
        private void BeginBlink(NPC npc, Vector2 destination, int windup = 22)
        {
            blinkDestination = destination;
            blinkDuration = windup;
            blinkTimer = 0;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.55f, Pitch = -0.25f }, npc.Center);
            npc.netUpdate = true;
        }

        private bool UpdateBlink(NPC npc)
        {
            if (blinkDuration <= 0)
                return false;

            blinkTimer++;
            int half = Math.Max(1, blinkDuration / 2);
            npc.velocity *= 0.8f;
            npc.damage = 0; // never cheese contact damage mid-blink

            if (blinkTimer < half)
            {
                npc.alpha = (int)MathHelper.Lerp(230f, 255f, blinkTimer / (float)half);
                // Convergent void-dust telegraphs the arrival point
                for (int i = 0; i < 3; i++)
                {
                    Vector2 around = blinkDestination + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(60f, 120f);
                    Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, (blinkDestination - around) * 0.07f, 100, VoidBright, Main.rand.NextFloat(1.1f, 1.4f));
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
                // Shadow bleeding off the departing body
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(40f, 40f), DustID.Shadowflame, Main.rand.NextVector2Circular(1.5f, 1.5f), 120, default, 1.1f);
                    d.noGravity = true;
                }
            }
            else if (blinkTimer == half)
            {
                npc.Center = blinkDestination;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.4f, Pitch = 0.25f }, npc.Center);
                SignusFx.Burst(npc.Center, 4f, 16);
                for (int i = 0; i < oldPos.Length; i++)
                    oldPos[i] = npc.Center; // collapse the ghost trail so the blink doesn't smear
            }
            else
            {
                npc.alpha = (int)MathHelper.Lerp(255f, 230f, (blinkTimer - half) / (float)half);
            }

            if (blinkTimer >= blinkDuration)
            {
                blinkDuration = 0;
                npc.alpha = 230;
            }
            return true;
        }
        #endregion

        #region Arena, Mines & Cleanup
        private void UpdateArenaBorder(NPC npc, Player target, float borderSize)
        {
            float radius = borderSize / 2f;

            // The boundary is a mechanic, so it must be VISIBLE: a slow orbit of void motes traces the ring
            for (int i = 0; i < 3; i++)
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = npc.Center + a.ToRotationVector2() * radius;
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, a.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.4f, 140, default, 1.05f);
                d.noGravity = true;
            }

            Vector2 dist = target.Center - npc.Center;
            // Densify the warning where the player is about to cross
            if (dist.Length() > radius - 140f)
            {
                Vector2 edge = npc.Center + dist.SafeNormalize(Vector2.UnitX) * radius;
                for (int i = 0; i < 2; i++)
                {
                    Dust d = Dust.NewDustPerfect(edge + Main.rand.NextVector2Circular(70f, 70f), DustID.Shadowflame, Vector2.Zero, 100, default, 1.3f);
                    d.fadeIn = 1.1f;
                    d.noGravity = true;
                }
            }

            if (dist.Length() > radius)
            {
                target.AddBuff(BuffID.ShadowFlame, 180);
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                }
            }
        }

        private void UpdateMineGrid(NPC npc, Player target, int currentPhase)
        {
            mineGridTimer++;
            if (mineGridTimer >= 600) // every 10s
            {
                mineGridTimer = 0;
                minesActive = true;
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathHelper.TwoPi / 6f;
                    minePositions[i] = target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(200f, 400f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), minePositions[i], Vector2.Zero, ModContent.ProjectileType<CosmicMineProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.6f, Pitch = -0.3f }, target.Center);
            }

            if (minesActive && mineGridTimer < 300) // 5s pull-line window (design doc)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 m1 = minePositions[i];
                    Vector2 m2 = minePositions[(i + 1) % 6];
                    float abLen = Vector2.Distance(m1, m2);
                    if (abLen <= 0f) continue;

                    Vector2 ab = m2 - m1;
                    Vector2 ac = target.Center - m1;
                    float proj = Vector2.Dot(ac, ab) / abLen;
                    proj = Math.Clamp(proj, 0f, abLen);
                    Vector2 closest = m1 + SafeNormalize(ab, Vector2.Zero) * proj;
                    if (Vector2.Distance(target.Center, closest) < 24f)
                    {
                        target.velocity += SafeNormalize(closest - target.Center, Vector2.Zero) * 0.6f;
                        // Grabbing sparks along the line so the pull has a visible cause
                        if (Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(closest, DustID.PurpleTorch, (closest - target.Center).SafeNormalize(Vector2.Zero) * 2f, 100, default, 1.2f);
                            d.noGravity = true;
                        }
                    }
                }
            }
            else
            {
                minesActive = false;
            }
        }

        // Retire held weapons (and optionally mines) so attacks never bleed stale props into the next one.
        private static void CleanupAttackProjectiles(NPC npc, bool alsoMines = false)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active)
                    continue;
                if (p.ModProjectile is BossHeldWeaponBase && (int)p.ai[0] == npc.whoAmI)
                    p.Kill();
                else if (alsoMines && p.type == ModContent.ProjectileType<CosmicMineProj>())
                    p.Kill();
            }
        }

        // Sets coreExposed for the given timer window (0.5s in P1, 0.3s in P2 per doc). OR-accumulates
        // so multi-strike attacks can open several windows per pattern.
        private void ExposeWindow(ref float timer, int start, bool isP2)
        {
            int window = isP2 ? 18 : 30;
            coreExposed |= timer >= start && timer <= start + window;
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            CleanupAttackProjectiles(npc);
            if (currentPhase == 1)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    // Same attack again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
                    npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
                    return;
                }
                currentRepetition = 0;
                attackCycleIndex++;
                AttackState next = P1Cycle[attackCycleIndex % P1Cycle.Length];
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
            }
            else
            {
                attackCycleIndex++;
                AttackState next = P2Cycle[attackCycleIndex % P2Cycle.Length];
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
            }
            npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // 宇宙苦无 · 两翼直角回切 — 变体A: 五刀掠过玩家两侧水平冻结, 直角刺腰逼垂直升空;
        // 变体B: 五刀在玩家上下方竖列冻结, 直角横刺逼水平位移. 同一把刀, 两道垂直的空间题.
        private void ExecuteCosmicKunai(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                // Blink to the throwing perch — A: high overhead (vertical throw => knives freeze in a
                // horizontal line at the player's FLANKS, stabbing inward, forcing the doc's vertical escape);
                // B: side flank (horizontal throw => vertical knife curtain, forcing a horizontal escape).
                Vector2 dest = currentVariantB
                    ? DirectedHoverSpot(npc, target, 430f, -60f, 6f)
                    : DirectedHoverSpot(npc, target, 60f, -430f, 6f);
                BeginBlink(npc, dest, 20);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldCosmicKunai>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 20 && timer < 50)
            {
                CloakedDrift(npc, target, currentVariantB ? 430f : 60f, currentVariantB ? -60f : -430f, 0.05f, 9f);
                // Charge: void-dust drawn into the blade hand
                if (Main.rand.NextBool(2))
                {
                    Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(90f, 90f);
                    Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, (npc.Center - around) * 0.09f, 100, default, 1.1f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            ExposeWindow(ref timer, 50, false);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Five kunai aimed to PASS the player and freeze beyond them, then right-angle stab back (design doc)
                Vector2 throwDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Vector2 perp = new Vector2(-throwDir.Y, throwDir.X);
                for (int i = 0; i < 5; i++)
                {
                    // A (thrown from overhead): a horizontal rank hanging at the player's flanks, tight forward
                    // offset so they freeze near waist level; B (thrown from the side): a vertical curtain beyond them
                    Vector2 passPoint = target.Center + throwDir * (currentVariantB ? 240f : 160f) + perp * (i - 2) * (currentVariantB ? 120f : 95f);
                    Vector2 vel = SafeNormalize(passPoint - npc.Center, Vector2.UnitY) * 23f;
                    float flightFrames = MathHelper.Clamp(Vector2.Distance(npc.Center, passPoint) / 23f, 8f, 40f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<SignusKunaiProj>(), npc.defDamage / 3, 0f, Main.myPlayer, flightFrames);
                }
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f }, npc.Center);
                SignusFx.Burst(npc.Center, 5f, 12);
                FindHeldWeapon<SignusHeldCosmicKunai>(npc)?.Pulse(12f);
            }

            if (timer > 50 && timer < 100)
                npc.velocity *= 0.965f; // recoil settle — the throw has weight
            else if (timer >= 100)
                CloakedDrift(npc, target, 400f, -80f, 0.045f, 8f);

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.CosmicKunai);
        }

        // 宇宙灯 · 十字极光扫频 — 变体A: 三灯横列玩家头顶同步顺时针; 变体B: 三灯环绕玩家三角站位逆时针.
        // 光束前30帧为细预警线, 之后才通电 (公平性: 生成即满判定是旧版硬伤).
        private void ExecuteCosmilamp(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -420f), 20);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldCosmilamp>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 20 && timer < 60)
            {
                // Cape-shake: lantern light gathers above the hood
                CloakedDrift(npc, target, 60f, -420f, 0.05f, 8f);
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), -30f), DustID.PurpleTorch, -Vector2.UnitY * Main.rand.NextFloat(1f, 2.5f), 100, VoidBright, 1.15f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            ExposeWindow(ref timer, 60, false);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float spinDir = currentVariantB ? -1f : 1f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = currentVariantB
                        ? target.Center + (i * MathHelper.TwoPi / 3f - MathHelper.PiOver2).ToRotationVector2() * 265f
                        : target.Center + new Vector2(i * 180f - 180f, -220f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<CosmilampLanternProj>(), npc.defDamage / 3, 0f, Main.myPlayer, spinDir);
                }
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f }, target.Center);
                FindHeldWeapon<SignusHeldCosmilamp>(npc)?.Pulse(10f);
            }

            if (timer > 60)
            {
                // Slow arc around the lantern cage while the beams sweep
                weavePhase += 0.03f;
                Vector2 orbit = target.Center + new Vector2(MathF.Cos(weavePhase) * 420f, -300f + MathF.Sin(weavePhase * 1.3f) * 60f);
                SmoothMove(npc, orbit, 0.04f, 8f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Cosmilamp);
        }
        #endregion

        #region P2 Attacks
        // 以太低语 — 变体A: 同点两连射(第二轮带预判); 变体B: 两轮之间瞬移到对侧, 反弹线交叉封路.
        private void ExecuteAethersWhisper(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 460f, -140f, 8f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldAethersWhisper>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 18 && timer < 46)
            {
                CloakedDrift(npc, target, 460f, -140f, 0.05f, 9f);
                FindHeldWeapon<SignusHeldAethersWhisper>(npc)?.SetAim((target.Center - npc.Center).ToRotation());
            }

            ExposeWindow(ref timer, 46, true);
            ExposeWindow(ref timer, 92, true);

            if ((timer == 46 || timer == 92) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool second = timer == 92;
                Vector2 aimPoint = second ? target.Center + target.velocity * 14f : target.Center;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(aimPoint - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.15f) * 12.5f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<WhisperBulletProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 500f, npc.Center.X, npc.Center.Y);
                }
                SoundEngine.PlaySound(SoundID.Item75 with { Volume = 0.7f, Pitch = second ? 0.2f : 0f }, npc.Center);
                SignusFx.Burst(npc.Center, 4f, 8);
                FindHeldWeapon<SignusHeldAethersWhisper>(npc)?.Pulse(8f);
            }

            if (timer == 60 && currentVariantB)
            {
                // B: cross to the opposite flank so the two bounce-fans lattice the arena
                float side = Math.Sign(npc.Center.X - target.Center.X);
                BeginBlink(npc, target.Center + new Vector2(-side * 460f, -140f), 16);
            }

            if (timer > 92)
                npc.velocity *= 0.96f;

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.AethersWhisper);
        }

        // 死亡升华 — 全场唯一的实体冲刺. 变体A: 单次贯穿冲刺+月刃; 变体B: 镜像双斜线冲刺画X.
        // 三拍: 蓄力(镰刀高举+吸尘+破绽窗) -> 冲刺(接触伤害+残影+途中放月刃) -> 收势(减速漂移).
        private void ExecuteDeathsAscension(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 480f, -300f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldDeathsAscension>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            int windup1End = 48;
            if (timer > 18 && timer < windup1End)
            {
                // Windup: hold the lane, scythe raised, void-dust streaming into the blade
                npc.velocity *= 0.93f;
                Vector2 aim = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (Main.rand.NextBool())
                {
                    Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(110f, 110f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Shadowflame, (npc.Center - around) * 0.08f, 100, default, 1.2f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
                // Faint dust lane marking the dash line — enough warning to sidestep, not enough to trivialize
                if (timer > windup1End - 16 && Main.rand.NextBool(2))
                {
                    float along = Main.rand.NextFloat(120f, 600f);
                    Dust d = Dust.NewDustPerfect(npc.Center + aim * along, DustID.PurpleTorch, aim * 2f, 130, default, 0.95f);
                    d.noGravity = true;
                }
            }

            ExposeWindow(ref timer, 28, true);

            if (timer == windup1End)
                LaunchScytheDash(npc, target);

            if (timer > windup1End && timer < windup1End + 34)
            {
                dashContact = true;
                if (timer > windup1End + 14)
                    npc.velocity *= 0.965f;
            }

            if (currentVariantB)
            {
                // Mirror pass: blink to the opposite upper flank, cut the other diagonal of the X
                if (timer == windup1End + 36)
                {
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    BeginBlink(npc, target.Center + new Vector2(-side * 480f, -300f), 16);
                }
                ExposeWindow(ref timer, windup1End + 54, true);
                if (timer > windup1End + 52 && timer < windup1End + 72)
                    npc.velocity *= 0.93f;
                if (timer == windup1End + 72)
                    LaunchScytheDash(npc, target);
                if (timer > windup1End + 72 && timer < windup1End + 104)
                {
                    dashContact = true;
                    if (timer > windup1End + 88)
                        npc.velocity *= 0.965f;
                }
            }

            int endTime = currentVariantB ? 200 : 160;
            if (timer > endTime - 40)
                CloakedDrift(npc, target, 420f, -120f, 0.045f, 9f);

            if (timer >= endTime)
                RotateAttack(npc, phase, AttackState.DeathsAscension);
        }

        private void LaunchScytheDash(NPC npc, Player target)
        {
            Vector2 dashVel = SafeNormalize(target.Center + target.velocity * 6f - npc.Center, Vector2.UnitY) * 26f;
            npc.velocity = dashVel;
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.3f }, npc.Center);
            SignusFx.Burst(npc.Center, 6f, 18);
            if (Main.netMode != NetmodeID.Server)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 4f;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dashVel * 0.55f, ModContent.ProjectileType<DeathsAscensionScytheProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
        }

        // 至天飞刀 — 变体A: 八刀环玩家头顶顺序下刺; 变体B: 左右两列各四刀交替横刺(逼上下位移).
        private void ExecuteEmpyreanKnives(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 420f, -220f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldEmpyreanKnives>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer == 24 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 pos;
                    if (currentVariantB)
                    {
                        // Two vertical stacks flanking the player; alternate columns stab in sequence
                        float side = i % 2 == 0 ? -1f : 1f;
                        int row = i / 2;
                        pos = target.Center + new Vector2(side * 320f, row * 110f - 165f);
                    }
                    else
                    {
                        float a = i * MathHelper.TwoPi / 8f;
                        pos = target.Center + a.ToRotationVector2() * 200f + new Vector2(0f, -100f);
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<EmpyreanKnifeProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 34f + i * 8f);
                }
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.3f }, target.Center);
            }

            ExposeWindow(ref timer, 34, true);

            if (timer > 24)
                CloakedDrift(npc, target, 430f, -200f, 0.05f, 10f);

            if (timer >= 165)
                RotateAttack(npc, phase, AttackState.EmpyreanKnives);
        }

        // 天龙星阵 — 变体A: 单X雷线锁玩家; 变体B: 双阵错位错时引爆, 安全口袋随时间移动.
        private void ExecuteKingConstellations(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, target.Center + new Vector2(0f, -460f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldKingConstellations>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 18 && timer < 40)
            {
                npc.velocity *= 0.94f;
                // Star-map motes rising off the hood while the constellation is drawn
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 40f), DustID.PurpleTorch, -Vector2.UnitY * Main.rand.NextFloat(1.5f, 3f), 100, VoidBright, 1.2f);
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
            }

            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ConstellationGridProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                if (currentVariantB)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(Math.Sign(target.velocity.X == 0f ? 1f : target.velocity.X) * 340f, 0f), Vector2.Zero, ModContent.ProjectileType<ConstellationGridProj>(), npc.defDamage / 3, 0f, Main.myPlayer, 24f);
                SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.6f }, target.Center);
                FindHeldWeapon<SignusHeldKingConstellations>(npc)?.Pulse(10f);
            }

            if (timer > 40)
                CloakedDrift(npc, target, 460f, -260f, 0.045f, 9f);

            if (timer >= 165)
                RotateAttack(npc, phase, AttackState.KingConstellations);
        }

        // 磁能熔毁 — 变体A: 单球缓推(吞噬弹药后炸16针); 变体B: 双侧小球对向合围.
        private void ExecuteMagneticMeltdown(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 440f, -180f, 6f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldMagneticMeltdown>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    // Pincer spheres from both flanks, converging on the player's lane
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector2 spawn = target.Center + new Vector2(s * 520f, -60f);
                        Vector2 vel = SafeNormalize(target.Center - spawn, Vector2.UnitX) * 3.4f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel, ModContent.ProjectileType<MagneticSphereProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    }
                }
                else
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 4f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<MagneticSphereProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.6f }, npc.Center);
                SignusFx.Burst(npc.Center, 4f, 10);
                FindHeldWeapon<SignusHeldMagneticMeltdown>(npc)?.Pulse(-10f);
            }

            if (timer > 40)
            {
                // Back away while the spheres advance — the sphere is the wall, the assassin is the anvil
                CloakedDrift(npc, target, 520f, -160f, 0.045f, 9f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.MagneticMeltdown);
        }

        // 天底 — 变体A: 奇点从脚下喷齿轮(先有尘埃间歇泉预警); 变体B: 齿轮自上方坠落成雨.
        private void ExecuteNadir(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 400f, -240f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldNadir>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            // Warning geyser: void-dust boils at the eruption zone before any gear flies
            if (timer > 20 && timer < 40 && Main.rand.NextBool(2))
            {
                float yOff = currentVariantB ? -320f : 200f;
                Vector2 pos = target.Center + new Vector2(Main.rand.NextFloat(-70f, 70f), yOff);
                Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame, new Vector2(0f, currentVariantB ? 2.5f : -2.5f), 100, default, 1.3f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            ExposeWindow(ref timer, 40, true);

            if (timer >= 40 && timer <= 100 && timer % 10 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn, vel;
                if (currentVariantB)
                {
                    spawn = target.Center + new Vector2(Main.rand.NextFloat(-160f, 160f), -340f);
                    vel = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 3f);
                }
                else
                {
                    spawn = target.Center + new Vector2(Main.rand.NextFloat(-20f, 20f), 200f);
                    vel = new Vector2(Main.rand.NextFloat(-2f, 2f), -12f);
                }
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel, ModContent.ProjectileType<NadirGearProj>(), npc.defDamage / 3, 0f, Main.myPlayer, currentVariantB ? 1f : 0f);
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.35f, Pitch = -0.2f }, spawn);
            }

            if (timer > 18)
                CloakedDrift(npc, target, 420f, -220f, 0.05f, 10f);

            if (timer >= 155)
                RotateAttack(npc, phase, AttackState.Nadir);
        }

        // 七星扫射 — 变体A: 同侧七连折线弹; 变体B: 四发后瞬移对侧再三发, 交叉封走位.
        private void ExecuteSevensStriker(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 470f, -120f, 8f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldSevensStriker>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            ExposeWindow(ref timer, 40, true);

            // 7 shots, 12f apart: t = 40, 52, ..., 112
            if (timer >= 40 && timer <= 112 && (timer - 40) % 12 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<SevensStrikerBulletProj>(), npc.defDamage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.5f, Pitch = 0.15f }, npc.Center);
                // Muzzle flash
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + dir * 40f, DustID.PurpleTorch, dir.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f)) * Main.rand.NextFloat(3f, 6f), 100, VoidBright, 1.2f);
                    d.noGravity = true;
                }
                FindHeldWeapon<SignusHeldSevensStriker>(npc)?.Pulse(6f);
            }

            if (currentVariantB && timer == 77)
            {
                // Cross-blink mid-volley, timed inside the 76->88 shot gap so the blink swallows no bullet
                float side = Math.Sign(npc.Center.X - target.Center.X);
                BeginBlink(npc, target.Center + new Vector2(-side * 470f, -120f), 10);
            }

            if (timer > 18 && !(currentVariantB && timer >= 77 && timer <= 87))
            {
                FindHeldWeapon<SignusHeldSevensStriker>(npc)?.SetAim((target.Center - npc.Center).ToRotation());
                CloakedDrift(npc, target, 470f, -120f, 0.045f, 8f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.SevensStriker);
        }

        // 金星三叉戟 — 变体A: 三叉平掷直取玩家; 变体B: 高抛三戟落点预判, 火雨提前封顶.
        private void ExecuteVenusianTrident(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 440f, currentVariantB ? -360f : -160f, 6f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldVenusianTrident>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 18 && timer < 40)
            {
                npc.velocity *= 0.94f;
                // Fire licking off the trident heads
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.Torch, -Vector2.UnitY * Main.rand.NextFloat(1f, 2f), 100, default, 1.2f);
                    d.noGravity = true;
                }
            }

            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel;
                    if (currentVariantB)
                    {
                        // Lobbed high: each trident arcs toward a lane across the player's predicted path
                        Vector2 landing = target.Center + new Vector2((i - 1) * 260f + target.velocity.X * 30f, 0f);
                        vel = SafeNormalize(landing - npc.Center, Vector2.UnitY) * 11f;
                    }
                    else
                    {
                        vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.2f) * 13f;
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<VenusianTridentProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.8f }, npc.Center);
                SignusFx.Burst(npc.Center, 5f, 10, DustID.Torch);
                FindHeldWeapon<SignusHeldVenusianTrident>(npc)?.Pulse(12f);
            }

            if (timer > 40)
                CloakedDrift(npc, target, 440f, -180f, 0.05f, 9f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.VenusianTrident);
        }

        // 现实撕裂 — 变体A: 单裂缝伴身; 变体B: 双裂缝夹击 + 两轮苦无逼射击角.
        private void ExecuteRealityRupture(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 430f, -200f, 0f), 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldRealityRupture>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(240f, 0f), Vector2.Zero, ModContent.ProjectileType<SignusRiftProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                    if (currentVariantB)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(-240f, 0f), Vector2.Zero, ModContent.ProjectileType<SignusRiftProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.7f, Pitch = -0.4f }, target.Center);
            }

            ExposeWindow(ref timer, 40, true);
            if (currentVariantB)
                ExposeWindow(ref timer, 100, true);

            // Pressure volleys force the player to shoot at angles that dodge the mirror-rifts
            if ((timer == 40 || (currentVariantB && timer == 100)) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 throwDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Vector2 perp = new(-throwDir.Y, throwDir.X);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 passPoint = target.Center + throwDir * 220f + perp * (i - 1) * 110f;
                    Vector2 vel = SafeNormalize(passPoint - npc.Center, Vector2.UnitY) * 22f;
                    float flightFrames = MathHelper.Clamp(Vector2.Distance(npc.Center, passPoint) / 22f, 8f, 40f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<SignusKunaiProj>(), npc.defDamage / 3, 0f, Main.myPlayer, flightFrames);
                }
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f }, npc.Center);
                FindHeldWeapon<SignusHeldRealityRupture>(npc)?.Pulse(10f);
            }

            if (timer > 18)
                CloakedDrift(npc, target, 430f, -200f, 0.05f, 10f);

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.RealityRupture);
        }

        // 形态转变 (50%): 兜帽爆碎, 结界收缩, 刺客觉醒. 白闪保留, 追加碎片喷发与收缩尘埃波.
        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            npc.alpha = (int)MathHelper.Lerp(npc.alpha, 0f, 0.1f); // fully unveiled for the reveal
            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            // Hood fragments spiraling off during the reveal
            if (timer > 10 && timer < 70 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 70f), DustID.Shadowflame, Main.rand.NextVector2CircularEdge(4f, 4f) - Vector2.UnitY * 2f, 100, default, 1.4f);
                d.fadeIn = 1.3f;
                d.noGravity = true;
            }

            if (timer == 45)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.5f }, npc.Center);
                SignusFx.Burst(npc.Center, 7f, 30);
                SignusFx.Burst(npc.Center, 4f, 16, DustID.Shadowflame);
                // The arena contraction wave: a ring of dust rushes inward from the old border to the new one
                for (int i = 0; i < 40; i++)
                {
                    float a = i * MathHelper.TwoPi / 40f;
                    Vector2 pos = npc.Center + a.ToRotationVector2() * 700f;
                    Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, -a.ToRotationVector2() * 6f, 100, VoidBright, 1.4f);
                    d.noGravity = true;
                }
            }

            if (timer >= 90)
            {
                npc.dontTakeDamage = false;
                transitionFlashAlpha = 0f;
                attackCycleIndex = 0;
                AttackState next = P2Cycle[0];
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Afterimage ghosts — drawn when the body is moving fast enough for motion to matter
            if (npc.velocity.Length() > 8f || dashContact)
            {
                Texture2D tex = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = 1; i < oldPos.Length; i++)
                {
                    int idx = (oldPosIndex - i + oldPos.Length * 2) % oldPos.Length;
                    if (oldPos[idx] == Vector2.Zero) continue;
                    float fade = (1f - i / (float)oldPos.Length) * 0.4f * npc.Opacity;
                    Color ghost = VoidPurple * fade;
                    ghost.A = 0;
                    spriteBatch.Draw(tex, oldPos[idx] - screenPos, npc.frame, ghost, npc.rotation, origin, npc.scale * (1f - i * 0.03f), npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
                }
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Mine pull-lines — the grid's threat must be readable, not invisible physics
            if (minesActive && mineGridTimer < 300)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 m1 = minePositions[i];
                    Vector2 m2 = minePositions[(i + 1) % 6];
                    float len = Vector2.Distance(m1, m2);
                    if (len <= 1f) continue;
                    float rot = (m2 - m1).ToRotation();
                    float pulse = 0.3f + 0.22f * MathF.Sin(ticksRunning * 0.18f + i * 1.3f);
                    Color lineColor = new Color(200, 80, 255, 0) * pulse;
                    spriteBatch.Draw(pixel, (m1 + m2) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor, rot, new Vector2(0.5f), new Vector2(len, 3f), SpriteEffects.None, 0f);
                }
            }

            // Exposed core: pulsing violet star over the chest
            if (coreExposed && stunTimer == 0)
            {
                float pulse = 1f + 0.35f * MathF.Sin(ticksRunning * 0.4f);
                Color glow = VoidBright;
                glow.A = 0;
                spriteBatch.Draw(pixel, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), glow * 0.9f, ticksRunning * 0.05f, new Vector2(0.5f), new Vector2(46f, 7f) * pulse, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), glow * 0.9f, ticksRunning * 0.05f + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(46f, 7f) * pulse, SpriteEffects.None, 0f);
                spriteBatch.Draw(pixel, npc.Center - screenPos, new Rectangle(0, 0, 1, 1), glow * 0.5f, 0f, new Vector2(0.5f), new Vector2(22f, 22f) * pulse, SpriteEffects.None, 0f);
            }

            // Stun: orbiting dizzy stars
            if (stunTimer > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = ticksRunning * 0.12f + i * MathHelper.TwoPi / 3f;
                    Vector2 pos = npc.Center + new Vector2(0f, -60f) + a.ToRotationVector2() * new Vector2(42f, 12f);
                    Color starC = VoidBright;
                    starC.A = 0;
                    spriteBatch.Draw(pixel, pos - screenPos, new Rectangle(0, 0, 1, 1), starC, a, new Vector2(0.5f), new Vector2(12f, 3f), SpriteEffects.None, 0f);
                    spriteBatch.Draw(pixel, pos - screenPos, new Rectangle(0, 0, 1, 1), starC, a + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(12f, 3f), SpriteEffects.None, 0f);
                }
            }
        }
        #endregion

        private static T FindHeldWeapon<T>(NPC npc) where T : BossHeldWeaponBase
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is T held && (int)p.ai[0] == npc.whoAmI)
                    return held;
            }
            return null;
        }
    }
}
