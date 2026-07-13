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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.CalamitasClone
{
    // 灾厄克隆体 — 不稳定生化晶体核心. 设计文档: 大计划/C 灾厄克隆体/灾厄克隆体_重置版设计文档.md
    // 移动哲学(分寸感): 熔炉里的女巫不追人 — 她在方框内的侧翼火位之间用"硫火裂步"(带火尘汇聚预告的
    // 短瞬移)换位, 出手前落位、蓄力、再开火; 弹幕的反弹网和收缩的方框才是压力来源.
    internal sealed class CalamitasCloneAI : IUMWBossAI
    {
        #region Constants & Configuration
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.CalClone.CalamitasClone>();
        public override string BossName => "Calamitas Clone";
        public override Color DebugColor => new(220, 60, 60);

        public override int MaxPhaseCount => 4;
        public override float[] PhaseLifeRatios => new[] { 0.70f, 0.35f, 0.10f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.0f;

        private static readonly Color BrimRed = new(220, 60, 60);
        private static readonly Color BrimBright = new(255, 140, 90);
        #endregion

        #region Attack States
        public enum AttackState
        {
            Oblivion = 0,
            Animosity = 1,
            LashesOfChaos = 2,
            EntropysVigil = 3,
            CrushsawCrasher = 4,
            HavocsBreath = 5,
            DesperationOverload = 6,
            BrotherTransition = 7
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;
        private Vector2 arenaCenter = Vector2.Zero;
        private bool centerSet = false;
        private int arenaHurtCooldown = 0;

        // Shield status
        private bool shieldActive = true;
        private int shieldRegenTimer = 0;
        private int shieldStunTimer = 0;
        private int shieldFxCooldown = 0;

        // Per-attack A/B variant toggle: flips deterministically each time that attack comes up (no RNG).
        private readonly bool[] attackVariant = new bool[8];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }
        private bool currentVariantB = false;

        // 硫火裂步 — telegraphed brimstone flicker-blink
        private int blinkTimer = 0;
        private int blinkDuration = 0;
        private Vector2 blinkDestination = Vector2.Zero;

        // Animosity sniper lock (design doc: 0.6s bright-red aim line before the 40f bullet)
        private Vector2 animosityMuzzle = Vector2.Zero;
        private Vector2 animosityLockedDir = Vector2.Zero;
        private float animosityLineBright = 0f; // 0 = hidden, ramps while locking, flares when locked

        // Lashes of Chaos charging circles (design doc: three magic circles charge 45f before firing)
        private readonly Vector2[] lashesAnchors = new Vector2[3];
        private float lashesChargeT = 0f; // 0..1 charge progress, 0 = hidden
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
        {
            ticksRunning++;
            oldPositions[oldPositionsIndex] = npc.Center;
            oldPositionsIndex = (oldPositionsIndex + 1) % oldPositions.Length;

            if (!TryGetTarget(npc, out Player target))
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 60) npc.timeLeft = 60;
                return false;
            }

            if (!centerSet)
            {
                arenaCenter = npc.Center;
                centerSet = true;
            }

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.Oblivion;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                currentVariantB = UseVariantB(state);
                npc.netUpdate = true;
            }

            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = 1;
            foreach (float threshold in PhaseLifeRatios)
            {
                if (lifeRatio <= threshold)
                    nextPhase++;
            }

            if (nextPhase > currentPhase)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                if (currentPhase == 3)
                    state = AttackState.BrotherTransition;
                else if (currentPhase == 4)
                    state = AttackState.DesperationOverload;
                else
                    state = AttackState.Oblivion;
                npc.ai[1] = (float)state;
                timer = 0;
                stateTracker = 0;
                CleanupHeldWeapons(npc);
                animosityLineBright = 0f;
                lashesChargeT = 0f;
                npc.netUpdate = true;
            }

            float borderSize = 1400f;
            if (currentPhase == 2) borderSize = 1100f;
            else if (currentPhase == 3) borderSize = 900f;
            else if (currentPhase == 4) borderSize = 650f;

            // Boundary push + damage — throttled to one hit per half-second.
            Vector2 dist = target.Center - arenaCenter;
            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
            {
                if (Math.Abs(dist.X) > borderSize / 2f)
                    target.velocity.X = -Math.Sign(dist.X) * 5f;
                if (Math.Abs(dist.Y) > borderSize / 2f)
                    target.velocity.Y = -Math.Sign(dist.Y) * 5f;

                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.AddBuff(BuffID.OnFire, 180);
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 12, 0);
                }
            }

            UpdateProjectiles(borderSize);
            UpdateSoulSeekers(npc, currentPhase);

            npc.rotation = npc.velocity.X * 0.04f;
            npc.scale = 1f + (float)Math.Sin(ticksRunning * 0.06f) * 0.03f;
            npc.damage = npc.defDamage; // per-frame normalization; blink/transition re-zero it after this line

            if (shieldFxCooldown > 0)
                shieldFxCooldown--;

            // Ambient: brimstone cinders drift up inside the furnace box, thicker as the box tightens
            float cinderChance = 0.15f + (4 - Math.Min(currentPhase, 4)) * -0.02f + (1f - lifeRatio) * 0.25f;
            if (Main.rand.NextFloat() < cinderChance)
            {
                Vector2 spawnPos = arenaCenter + new Vector2(Main.rand.NextFloat(-borderSize, borderSize) / 2f, borderSize / 2f - 20f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(1.5f, 3.5f)), 150, default, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
                d.fadeIn = 1.1f;
            }

            if (blinkDuration <= 0)
            {
                switch (state)
                {
                    case AttackState.Oblivion:
                        ExecuteOblivion(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.Animosity:
                        ExecuteAnimosity(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.LashesOfChaos:
                        ExecuteLashes(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.EntropysVigil:
                        ExecuteVigil(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.CrushsawCrasher:
                        ExecuteCrushsaw(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.HavocsBreath:
                        ExecuteHavoc(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.DesperationOverload:
                        ExecuteDesperation(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                    case AttackState.BrotherTransition:
                        ExecuteBrotherTransition(npc, target, ref timer, ref stateTracker, currentPhase);
                        break;
                }
            }
            else
            {
                timer++; // attack timelines tick through the blink — blink windups belong to the attack rhythm
            }

            UpdateBlink(npc);

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ApplyDefense(npc, ref modifiers);

        private void ApplyDefense(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.ai[1] == (float)AttackState.BrotherTransition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }
            if (shieldActive && npc.ai[0] <= 2)
            {
                modifiers.FinalDamage *= 0.05f; // 95% DR
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 10;
                    SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.6f, Pitch = -0.1f }, npc.Center);
                }
            }
        }
        #endregion

        #region Movement & Blink Helpers
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        private void SmoothMove(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.14f);
        }

        // 硫火裂步: the witch dissolves into cinders while fire-dust converges on the destination, then reforms.
        private void BeginBlink(NPC npc, Vector2 destination, int windup = 18)
        {
            blinkDestination = destination;
            blinkDuration = windup;
            blinkTimer = 0;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.2f }, npc.Center);
            npc.netUpdate = true;
        }

        private void UpdateBlink(NPC npc)
        {
            if (blinkDuration <= 0)
                return;

            blinkTimer++;
            int half = Math.Max(1, blinkDuration / 2);
            npc.velocity *= 0.8f;
            npc.damage = 0; // never cheese contact damage mid-blink

            if (blinkTimer < half)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, blinkTimer / (float)half);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 around = blinkDestination + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(50f, 110f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Torch, (blinkDestination - around) * 0.08f, 100, BrimBright, Main.rand.NextFloat(1.1f, 1.4f));
                    d.fadeIn = 1.3f;
                    d.noGravity = true;
                }
            }
            else if (blinkTimer == half)
            {
                npc.Center = blinkDestination;
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.4f, Pitch = 0.3f }, npc.Center);
                BrimstoneFx.Burst(npc.Center, 4f, 14);
                for (int i = 0; i < oldPositions.Length; i++)
                    oldPositions[i] = npc.Center;
            }
            else
            {
                npc.Opacity = MathHelper.Lerp(0f, 1f, (blinkTimer - half) / (float)half);
            }

            if (blinkTimer >= blinkDuration)
            {
                blinkDuration = 0;
                npc.Opacity = 1f;
                npc.damage = npc.defDamage; // restore contact damage — without this the zero sticks forever
            }
        }

        private static void CleanupHeldWeapons(NPC npc)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is BossHeldWeaponBase && (int)p.ai[0] == npc.whoAmI)
                    p.Kill();
            }
        }

        // Charge-up: cinder-dust drawn into the witch before every volley — each attack telegraphs itself.
        private static void ChargeCinders(NPC npc, int density = 2)
        {
            if (!Main.rand.NextBool(density))
                return;
            Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
            Dust d = Dust.NewDustPerfect(around, DustID.Torch, (npc.Center - around) * 0.08f, 100, default, 1.2f);
            d.fadeIn = 1.2f;
            d.noGravity = true;
        }
        #endregion

        #region Bouncing & Orbiter Systems
        private void UpdateProjectiles(float borderSize)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile)
                {
                    Vector2 dist = proj.Center - arenaCenter;
                    bool bounced = false;

                    if (Math.Abs(dist.X) > borderSize / 2f)
                    {
                        proj.velocity.X = -proj.velocity.X;
                        bounced = true;
                    }
                    if (Math.Abs(dist.Y) > borderSize / 2f)
                    {
                        proj.velocity.Y = -proj.velocity.Y;
                        bounced = true;
                    }

                    if (bounced)
                    {
                        proj.localAI[0]++;
                        if (proj.localAI[0] >= 2)
                        {
                            proj.Kill();
                        }
                        else
                        {
                            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f }, proj.Center);
                            for (int k = 0; k < 8; k++)
                            {
                                Dust d = Dust.NewDustPerfect(proj.Center, DustID.Torch, Main.rand.NextVector2Circular(3f, 3f), 100, Color.Cyan, 1.3f);
                                d.fadeIn = 1.3f;
                                d.noGravity = true;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateSoulSeekers(NPC npc, int currentPhase)
        {
            if (currentPhase >= 3)
            {
                shieldActive = false;
                return;
            }

            int orbiterType = ModContent.NPCType<CalamityMod.NPCs.CalClone.SoulSeeker>();

            if (shieldActive)
            {
                bool alive = false;
                int activeSeekerIndex = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC m = Main.npc[i];
                    if (m.active && m.type == orbiterType && m.ai[0] == npc.whoAmI)
                    {
                        alive = true;
                        float angle = activeSeekerIndex * MathHelper.TwoPi / 6f + ticksRunning * 0.02f;
                        m.Center = npc.Center + angle.ToRotationVector2() * 120f;
                        m.velocity = Vector2.Zero;
                        activeSeekerIndex++;

                        for (int p = 0; p < Main.maxProjectiles; p++)
                        {
                            Projectile proj = Main.projectile[p];
                            if (proj.active && proj.hostile && (proj.ModProjectile?.Name == "BrimstoneBarrage" || proj.ModProjectile?.Name == "BrimstoneHellblast" || proj.ModProjectile?.Name == "BrimstoneGigablast"))
                            {
                                if (Vector2.Distance(proj.Center, m.Center) < 40f)
                                {
                                    proj.Kill();
                                    BrimstoneFx.Burst(m.Center, 5f, 16);
                                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f }, m.Center);
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        Vector2 dir = SafeNormalize(Main.player[npc.target].Center - m.Center, Vector2.UnitY);
                                        for (int s = -1; s <= 1; s++)
                                        {
                                            Projectile.NewProjectile(npc.GetSource_FromAI(), m.Center, dir.RotatedBy(s * 0.2f) * 12f, ModContent.ProjectileType<MiniAmplifiedLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!alive)
                {
                    shieldActive = false;
                    shieldStunTimer = 360;
                    npc.velocity = Vector2.Zero;
                    Player t = Main.player[npc.target];
                    if (t.active)
                        t.Calamity().GeneralScreenShakePower = 7f;
                    BrimstoneFx.Burst(npc.Center, 6f, 30);
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);
                }
            }
            else
            {
                if (shieldStunTimer > 0)
                {
                    shieldStunTimer--;
                    npc.defense = 0;
                    // Short-circuit sparks raining off the overloaded core
                    npc.rotation = MathF.Sin(ticksRunning * 0.3f) * 0.15f;
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), DustID.Torch, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(1f, 3f)), 100, default, 1.3f);
                        d.fadeIn = 1.2f;
                        d.noGravity = true;
                    }
                    if (shieldStunTimer == 0)
                        shieldRegenTimer = 720;
                }
                else if (shieldRegenTimer > 0)
                {
                    shieldRegenTimer--;
                    if (shieldRegenTimer == 0)
                    {
                        shieldActive = true;
                        BrimstoneFx.Burst(npc.Center, 4f, 24);
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                int minion = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, orbiterType);
                                if (minion >= 0 && minion < Main.maxNPCs)
                                {
                                    Main.npc[minion].ai[0] = npc.whoAmI;
                                    Main.npc[minion].netUpdate = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Attack Rotations
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            CleanupHeldWeapons(npc);
            animosityLineBright = 0f;
            lashesChargeT = 0f;
            currentRepetition++;
            if (currentPhase <= 2)
            {
                if (currentRepetition < 3)
                {
                    // Same weapon again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                }
                else
                {
                    currentRepetition = 0;
                    AttackState next = current switch
                    {
                        AttackState.Oblivion => AttackState.Animosity,
                        AttackState.Animosity => AttackState.LashesOfChaos,
                        AttackState.LashesOfChaos => AttackState.EntropysVigil,
                        _ => AttackState.Oblivion
                    };
                    currentVariantB = UseVariantB(next);
                    npc.ai[1] = (float)next;
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                }
            }
            else if (currentPhase == 3)
            {
                currentRepetition = 0;
                AttackState next = current switch
                {
                    AttackState.CrushsawCrasher => AttackState.HavocsBreath,
                    _ => AttackState.CrushsawCrasher
                };
                currentVariantB = UseVariantB(next);
                npc.ai[1] = (float)next;
                npc.ai[2] = 0;
                npc.ai[3] = 0;
            }
            npc.netUpdate = true;
        }
        #endregion

        #region Attack State Machine

        // 遗忘 · 轨道连线扫场 — 变体A: 悠悠球以玩家为圆心360°切割(文档原题);
        // 变体B: 悠悠球以方框中心为圆心沿大轨道扫外圈, 空间题反转 — 必须收进内圈.
        private void ExecuteOblivion(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 300f, -280f, 8f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldOblivion>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 16 && timer < 50)
            {
                npc.velocity *= 0.93f; // settle at the throwing perch, yoyo spinning up
                ChargeCinders(npc);
            }

            // Materialization warning at the yoyo's spawn point before it appears
            if (timer > 38 && timer < 50 && Main.rand.NextBool(2))
            {
                Vector2 warnPos = currentVariantB
                    ? arenaCenter + SafeNormalize(target.Center - arenaCenter, Vector2.UnitX) * 340f
                    : target.Center + new Vector2(Math.Sign(npc.Center.X - target.Center.X) * 210f, 0f);
                Dust d = Dust.NewDustPerfect(warnPos + Main.rand.NextVector2Circular(40f, 40f), DustID.Torch, Vector2.Zero, 100, BrimBright, 1.3f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    // Pivot preset to the arena center: the blade patrols the outer lane, the safe zone is the core
                    Vector2 spawn = arenaCenter + SafeNormalize(target.Center - arenaCenter, Vector2.UnitX) * 340f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<OblivionYoyoProj>(), npc.damage / 2, 0f, Main.myPlayer, 1f, arenaCenter.X, arenaCenter.Y);
                    if (idx >= 0) Main.projectile[idx].netUpdate = true;
                }
                else
                {
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    Vector2 spawn = target.Center + new Vector2(side * 210f, 0f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<OblivionYoyoProj>(), npc.damage / 2, 0f, Main.myPlayer);
                }
                FindHeldWeapon<CalHeldOblivion>(npc)?.Pulse(-14f);
                SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.5f }, npc.Center);
                BrimstoneFx.Burst(npc.Center, 4f, 10);
            }

            if (timer > 50)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 320f, -260f, 6f), 0.05f, 10f);

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Oblivion);
        }

        // 敌意 · 超视距阻击 — 0.6秒亮红锁定线(文档硬性要求)后40f弹穿刺, 弹道提前锁死可侧移躲开.
        // 变体A: 单发重狙; 变体B: 裂步换位双狙, 两条锁定线从不同角度到来.
        private void ExecuteAnimosity(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 420f, -180f, 6f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldAnimosity>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            // Lock window: the aim line tracks, then freezes 14 frames before the shot (0.6s total lock, per doc)
            RunSniperLock(npc, target, timer, lockStart: 18, fireTime: 54);

            if (currentVariantB)
            {
                if (timer == 66)
                {
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    BeginBlink(npc, target.Center + new Vector2(-side * 420f, -240f), 14);
                }
                RunSniperLock(npc, target, timer, lockStart: 84, fireTime: 120);
            }

            if (timer > 16 && blinkDuration <= 0)
                npc.velocity *= 0.93f; // a sniper does not drift while aiming

            int endTime = currentVariantB ? 175 : 150;
            if (timer >= endTime)
                RotateAttack(npc, phase, AttackState.Animosity);
        }

        private void RunSniperLock(NPC npc, Player target, float timer, int lockStart, int fireTime)
        {
            int freezeAt = fireTime - 14;
            if (timer >= lockStart && timer < freezeAt)
            {
                // Tracking: the thin red line follows the player
                animosityMuzzle = npc.Center;
                animosityLockedDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                animosityLineBright = MathHelper.Lerp(0.25f, 0.6f, (timer - lockStart) / (float)(freezeAt - lockStart));
            }
            else if (timer >= freezeAt && timer < fireTime)
            {
                // Locked: the line freezes and flares — this is the dodge cue
                animosityMuzzle = npc.Center;
                animosityLineBright = 1f;
                if (Main.rand.NextBool(2))
                {
                    float along = Main.rand.NextFloat(80f, 700f);
                    Dust d = Dust.NewDustPerfect(animosityMuzzle + animosityLockedDir * along, DustID.Torch, animosityLockedDir * 2f, 130, BrimRed, 0.9f);
                    d.noGravity = true;
                }
            }
            else if (timer == fireTime)
            {
                animosityLineBright = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, animosityLockedDir * 40f, ModContent.ProjectileType<AnimosityBulletProj>(), npc.damage / 2, 0f, Main.myPlayer);
                FindHeldWeapon<CalHeldAnimosity>(npc)?.Pulse(-18f);
                SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.7f, Pitch = -0.2f }, npc.Center);
                BrimstoneFx.Burst(npc.Center + animosityLockedDir * 50f, 5f, 10);
                npc.velocity -= animosityLockedDir * 7f; // recoil kick — the rifle has weight
            }
        }

        // 混乱鞭笞 · 吸力火球 — 三法阵蓄力45帧(可见的旋转法阵+汇聚火尘)后齐射.
        // 变体A: 扇形直取玩家; 变体B: 打向玩家身后的墙, 反弹网+漩涡封路(库内变轨).
        private void ExecuteLashes(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 320f, -260f, 6f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CalHeldLashes>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            const int chargeStart = 20;
            const int fireAt = 65; // 45 frames of visible charging, per doc

            if (timer >= chargeStart && timer < fireAt)
            {
                npc.velocity *= 0.94f;
                // The three circles hover in a row in front of the witch, spinning up
                Vector2 fwd = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Vector2 perp = new(-fwd.Y, fwd.X);
                for (int i = 0; i < 3; i++)
                    lashesAnchors[i] = npc.Center + fwd * 90f + perp * (i - 1) * 76f;
                lashesChargeT = (timer - chargeStart) / (float)(fireAt - chargeStart);

                if (Main.rand.NextBool(2))
                {
                    int c = Main.rand.Next(3);
                    Vector2 around = lashesAnchors[c] + Main.rand.NextVector2CircularEdge(40f, 40f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Torch, (lashesAnchors[c] - around) * 0.1f, 100, BrimRed, 1.1f);
                    d.fadeIn = 1.1f;
                    d.noGravity = true;
                }
            }

            if (timer == fireAt && Main.netMode != NetmodeID.MultiplayerClient)
            {
                lashesChargeT = 0f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 aim;
                    if (currentVariantB)
                    {
                        // Bank shots: aimed past the player at the far wall, so the vortices bloom BEHIND them
                        aim = SafeNormalize(target.Center + SafeNormalize(target.Center - npc.Center, Vector2.UnitX) * 400f - lashesAnchors[i], Vector2.UnitY);
                    }
                    else
                    {
                        aim = SafeNormalize(target.Center - lashesAnchors[i], Vector2.UnitY).RotatedBy((i - 1) * 0.15f);
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), lashesAnchors[i], aim * 8f, ModContent.ProjectileType<BrimstoneHellfireballProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<CalHeldLashes>(npc)?.Pulse(-12f);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f }, npc.Center);
                BrimstoneFx.Burst(npc.Center, 5f, 12);
                npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 4f; // volley pushback
            }

            if (timer > fireAt)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 340f, -240f, 6f), 0.05f, 10f);

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.LashesOfChaos);
        }

        // 熵之守望 · 俯冲爪击 — 变体A: 顶部两角X形下劈(文档原题); 变体B: 底部两角倒X上突, 逼顶部站位.
        // 出击角落提前20帧点起警示火苗.
        private void ExecuteVigil(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 60f, -300f, 0f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<CalHeldVigil>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            float cornerY = currentVariantB ? 400f : -400f;

            // Corner flares: the launch corners burn before the minis appear
            if (timer > 20 && timer < 40 && Main.rand.NextBool(2))
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 corner = arenaCenter + new Vector2(s * 400f, cornerY);
                    Dust d = Dust.NewDustPerfect(corner + Main.rand.NextVector2Circular(40f, 40f), DustID.Torch, -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.5f) * (currentVariantB ? -1f : 1f), 100, BrimBright, 1.35f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (timer == 40)
            {
                FindHeldWeapon<CalHeldVigil>(npc)?.Pulse(-10f);
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float diveY = currentVariantB ? -10f : 10f;
                    int c1 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(arenaCenter.X - 400), (int)(arenaCenter.Y + cornerY), ModContent.Find<ModNPC>("CalamityMod/Catastromini").Type);
                    int c2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)(arenaCenter.X + 400), (int)(arenaCenter.Y + cornerY), ModContent.Find<ModNPC>("CalamityMod/Cataclymini").Type);
                    if (c1 >= 0 && c1 < Main.maxNPCs)
                    {
                        Main.npc[c1].velocity = new Vector2(10f, diveY);
                        Main.npc[c1].ai[0] = npc.whoAmI;
                        Main.npc[c1].netUpdate = true;
                    }
                    if (c2 >= 0 && c2 < Main.maxNPCs)
                    {
                        Main.npc[c2].velocity = new Vector2(-10f, diveY);
                        Main.npc[c2].ai[0] = npc.whoAmI;
                        Main.npc[c2].netUpdate = true;
                    }
                }
            }

            if (timer > 16)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 60f, -300f, 0f), 0.05f, 11f);

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.EntropysVigil);
        }

        // 碎锯冲击者 · 贴边旋转轮 — 变体A: 单锯掷向玩家方向的墙; 变体B: 双锯分掷地板与天花板, 两圈对滚.
        private void ExecuteCrushsaw(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 320f, -240f, 6f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldCrushsaw>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 16 && timer < 50)
            {
                npc.velocity *= 0.94f;
                // Saw spin-up: grinding sparks
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.Torch, Main.rand.NextVector2CircularEdge(3f, 3f), 100, default, 1.1f);
                    d.noGravity = true;
                }
            }

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    // Floor & ceiling saws: two opposite wall-crawlers, jump timing doubles up
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, 14f), ModContent.ProjectileType<CrushaxProj>(), npc.damage / 2, 0f, Main.myPlayer, arenaCenter.X, arenaCenter.Y);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, -14f), ModContent.ProjectileType<CrushaxProj>(), npc.damage / 2, 0f, Main.myPlayer, arenaCenter.X, arenaCenter.Y);
                }
                else
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 14f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<CrushaxProj>(), npc.damage / 2, 0f, Main.myPlayer, arenaCenter.X, arenaCenter.Y);
                }
                FindHeldWeapon<CalHeldCrushsaw>(npc)?.Pulse(20f);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.3f }, npc.Center);
                BrimstoneFx.Burst(npc.Center, 4f, 10);
                npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 5f;
            }

            if (timer > 50)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 340f, -220f, 6f), 0.05f, 11f);

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.CrushsawCrasher);
        }

        // 浩劫之息 · 燃烧边界 — 变体A: 左→右扇形火舌; 变体B: 右→左且中段留两拍缺口(可穿越的呼吸).
        private void ExecuteHavoc(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                BeginBlink(npc, DirectedHoverSpot(npc, target, 320f, -260f, 6f), 16);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CalHeldHavoc>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer > 30 && timer < 50 && Main.rand.NextBool(2))
            {
                // Pilot flame licking out of the nozzle before the sweep
                Vector2 fwd = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Dust d = Dust.NewDustPerfect(npc.Center + fwd * 60f, DustID.Torch, fwd * Main.rand.NextFloat(2f, 4f), 100, default, 1.3f);
                d.noGravity = true;
            }

            if (timer >= 50 && timer <= 170 && timer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool inGap = currentVariantB && timer >= 100 && timer <= 118;
                if (!inGap)
                {
                    float sweep = (timer - 50f) / 120f;
                    float angle = currentVariantB ? MathHelper.Lerp(0.6f, -0.6f, sweep) : MathHelper.Lerp(-0.6f, 0.6f, sweep);
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angle) * 12f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BrimstoneFireFriendlyProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    FindHeldWeapon<CalHeldHavoc>(npc)?.Pulse(6f);
                    // The hose pushes the wielder back, slowly
                    npc.velocity -= vel.SafeNormalize(Vector2.Zero) * 0.35f;
                }
            }
            else if (timer > 16 && timer < 50)
            {
                npc.velocity *= 0.94f;
            }

            if (timer > 170)
                SmoothMove(npc, DirectedHoverSpot(npc, target, 320f, -250f, 6f), 0.05f, 10f);

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.HavocsBreath);
        }

        // 终局绝杀: 混乱反应堆过载 — 裂步锁定方框最中心, 紫色溢出光芒, 十字激光缓转 + 天降爆炸火星.
        private void ExecuteDesperation(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;

            if (timer == 1)
            {
                BeginBlink(npc, arenaCenter, 20);
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 10f;
                BrimstoneFx.Burst(npc.Center, 7f, 40);
            }

            if (timer > 20)
            {
                npc.Center = Vector2.Lerp(npc.Center, arenaCenter, 0.2f);
                npc.velocity = Vector2.Zero;

                // 紫色溢出光芒: the overloaded reactor bleeds violet
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.PurpleTorch, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), 100, default, 1.3f);
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int s = 0; s < 4; s++)
                {
                    float a = s * MathHelper.PiOver2;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, a.ToRotationVector2(), ModContent.ProjectileType<RotatingBrimstoneLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
            }

            if (timer >= 40 && timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 fallPos = arenaCenter + new Vector2(Main.rand.NextFloat(-300f, 300f), -300f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), fallPos, new Vector2(0f, 6f), ModContent.ProjectileType<HellfireStarExplosionProj>(), npc.damage / 2, 0f, Main.myPlayer);
                // Spawn twinkle so the drop lane reads a beat early
                for (int i = 0; i < 5; i++)
                {
                    Dust d = Dust.NewDustPerfect(fallPos, DustID.Torch, Main.rand.NextVector2Circular(2f, 2f), 100, BrimBright, 1.2f);
                    d.noGravity = true;
                }
            }
        }

        // 兄弟连战转场 — 左右汇聚一橙一蓝粒子流, 兄弟现身瞬间交叉发射4发斜向电离弹幕(文档要求).
        private void ExecuteBrotherTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            npc.damage = 0; // an invisible body must never body-check
            npc.alpha = (int)MathHelper.Lerp(0f, 255f, Math.Min(timer / 90f, 1f));

            Vector2 leftGather = arenaCenter + new Vector2(-250f, 0f);
            Vector2 rightGather = arenaCenter + new Vector2(250f, 0f);

            // 残影凝聚: orange stream condenses left, blue stream condenses right
            if (timer < 90)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 aroundL = leftGather + Main.rand.NextVector2CircularEdge(120f, 120f);
                    Dust dl = Dust.NewDustPerfect(aroundL, DustID.Torch, (leftGather - aroundL) * 0.07f, 100, new Color(255, 150, 60), 1.3f);
                    dl.fadeIn = 1.2f;
                    dl.noGravity = true;

                    Vector2 aroundR = rightGather + Main.rand.NextVector2CircularEdge(120f, 120f);
                    Dust dr = Dust.NewDustPerfect(aroundR, DustID.IceTorch, (rightGather - aroundR) * 0.07f, 100, new Color(90, 160, 255), 1.3f);
                    dr.fadeIn = 1.2f;
                    dr.noGravity = true;
                }
            }

            if (timer == 45)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 8f;
                BrimstoneFx.Burst(npc.Center, 6f, 30);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int c1 = NPC.NewNPC(npc.GetSource_FromAI(), (int)leftGather.X, (int)leftGather.Y, ModContent.Find<ModNPC>("CalamityMod/Cataclysm").Type);
                    int c2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)rightGather.X, (int)rightGather.Y, ModContent.Find<ModNPC>("CalamityMod/Catastrophe").Type);
                    if (c1 >= 0 && c1 < Main.maxNPCs)
                    {
                        Main.npc[c1].ai[0] = npc.whoAmI;
                        Main.npc[c1].netUpdate = true;
                    }
                    if (c2 >= 0 && c2 < Main.maxNPCs)
                    {
                        Main.npc[c2].ai[0] = npc.whoAmI;
                        Main.npc[c2].netUpdate = true;
                    }
                }
            }

            // 电离爆炸: the instant the brothers land, 4 diagonal bolts cross toward the player (design doc)
            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (Vector2 origin in new[] { leftGather, rightGather })
                {
                    Vector2 baseDir = SafeNormalize(target.Center - origin, Vector2.UnitY);
                    for (int s = -1; s <= 1; s += 2)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), origin, baseDir.RotatedBy(s * 0.3f) * 11f, ModContent.ProjectileType<MiniAmplifiedLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f }, arenaCenter);
            }

            bool brothersAlive = false;
            int cataclysm = ModContent.Find<ModNPC>("CalamityMod/Cataclysm").Type;
            int catastrophe = ModContent.Find<ModNPC>("CalamityMod/Catastrophe").Type;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && (Main.npc[i].type == cataclysm || Main.npc[i].type == catastrophe))
                {
                    brothersAlive = true;
                    break;
                }
            }

            if (!brothersAlive && timer >= 90)
            {
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.damage = npc.defDamage;
                AttackState next = AttackState.CrushsawCrasher;
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
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            // Trail only when moving with intent
            if (npc.velocity.Length() > 5f)
            {
                for (int i = 0; i < oldPositions.Length; i++)
                {
                    int idx = (oldPositionsIndex - i - 1 + oldPositions.Length) % oldPositions.Length;
                    if (oldPositions[idx] == Vector2.Zero) continue;
                    float alpha = (1f - i / (float)oldPositions.Length) * 0.55f;
                    Color trailColor = new Color(220, 60, 60, 0) * alpha;
                    spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, trailColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            int currentPhase = (int)npc.ai[0];
            float borderSize = 1400f;
            if (currentPhase == 2) borderSize = 1100f;
            else if (currentPhase == 3) borderSize = 900f;
            else if (currentPhase == 4) borderSize = 650f;

            Vector2 tl = arenaCenter + new Vector2(-borderSize / 2f, -borderSize / 2f);
            Vector2 tr = arenaCenter + new Vector2(borderSize / 2f, -borderSize / 2f);
            Vector2 bl = arenaCenter + new Vector2(-borderSize / 2f, borderSize / 2f);
            Vector2 br = arenaCenter + new Vector2(borderSize / 2f, borderSize / 2f);

            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.Red * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.Red * 0.7f, 4f);

            // Sniper lock line: thin while tracking, flaring when locked
            if (animosityLineBright > 0.05f && animosityLockedDir != Vector2.Zero)
            {
                float width = animosityLineBright >= 1f ? 4f : 1.5f;
                Color lineColor = Color.Lerp(BrimRed, Color.White, animosityLineBright >= 1f ? 0.5f : 0f) * (0.4f + animosityLineBright * 0.5f);
                lineColor.A = 0;
                Vector2 lineEnd = animosityMuzzle + animosityLockedDir * 1400f;
                float rot = animosityLockedDir.ToRotation();
                spriteBatch.Draw(pixel, (animosityMuzzle + lineEnd) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lineColor, rot, new Vector2(0.5f), new Vector2(Vector2.Distance(animosityMuzzle, lineEnd), width), SpriteEffects.None, 0f);
            }

            // Lashes charging circles: three spinning diamonds swelling toward the release
            if (lashesChargeT > 0.01f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float spin = ticksRunning * 0.12f + i * 2.1f;
                    float size = MathHelper.Lerp(10f, 30f, lashesChargeT);
                    Color circleColor = Color.Lerp(BrimRed, BrimBright, lashesChargeT);
                    circleColor.A = 0;
                    spriteBatch.Draw(pixel, lashesAnchors[i] - screenPos, new Rectangle(0, 0, 1, 1), circleColor * 0.8f, spin, new Vector2(0.5f), new Vector2(size, size), SpriteEffects.None, 0f);
                    spriteBatch.Draw(pixel, lashesAnchors[i] - screenPos, new Rectangle(0, 0, 1, 1), circleColor * 0.5f, -spin * 0.7f, new Vector2(0.5f), new Vector2(size * 1.5f, size * 0.5f), SpriteEffects.None, 0f);
                }
            }

            if (shieldActive)
            {
                int orbiterType = ModContent.NPCType<CalamityMod.NPCs.CalClone.SoulSeeker>();
                Vector2[] seekerPositions = new Vector2[6];
                int seekerCount = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == orbiterType && Main.npc[i].ai[0] == npc.whoAmI)
                    {
                        if (seekerCount < 6)
                            seekerPositions[seekerCount++] = Main.npc[i].Center;
                    }
                }

                for (int i = 0; i < seekerCount; i++)
                {
                    Vector2 start = seekerPositions[i];
                    Vector2 end = seekerPositions[(i + 1) % seekerCount];
                    IUMWWeaponBossVisuals.DrawLine(spriteBatch, start, end, Color.Red * 0.8f, 3f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            // Overload pulse in the final stand; steady ember glow otherwise
            float glowScale = currentPhase == 4 ? 1.08f + 0.06f * (float)Math.Sin(ticksRunning * 0.2f) : 1.08f;
            Color glowColor = currentPhase == 4
                ? Color.Lerp(new Color(220, 60, 60, 0), new Color(180, 60, 220, 0), 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.1f)) * 0.4f
                : new Color(220, 60, 60, 0) * 0.35f;
            spriteBatch.Draw(tex, npc.Center - screenPos, frame, glowColor, npc.rotation, origin, npc.scale * glowScale, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
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
