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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.CeaselessVoid
{
    internal sealed class CeaselessVoidAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/CeaselessVoid").Type;
        public override string BossName => "Ceaseless Void";
        public override Color DebugColor => new(180, 100, 255);

        // Design doc specifies a single 50% HP unseal, not a 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 0.8f;

        // Dedicated sound identity — pitch-varied vanilla SoundIDs, matching the convention Cryogen/OldDuke/
        // Signus already use rather than bare unvaried SoundID calls.
        private static readonly SoundStyle VoidPulseSound = SoundID.Item103 with { Volume = 0.7f, Pitch = -0.35f };
        private static readonly SoundStyle OrbiterBreakSound = SoundID.NPCDeath4 with { Volume = 0.85f, Pitch = -0.1f };
        private static readonly SoundStyle StasisSound = SoundID.Item103 with { Volume = 0.7f, Pitch = -0.55f };
        #endregion

        #region Attack States
        public enum AttackState
        {
            MirrorBlade = 0,
            VoidConcentration = 1,
            DarkSpark = 2,
            EventHorizon = 3,
            Mistlestorm = 4,
            OntologicalDespoiler = 5,
            SealedSingularity = 6,
            TacticiansTrump = 7,
            Eternity = 8,
            PhantasmalFury = 9,
            RealityRupture = 10,
            Transition = 11,
            DeathAnimation = 12,
        }

        private static bool IsP1(AttackState s) => s == AttackState.MirrorBlade || s == AttackState.VoidConcentration;

        // Only 2 named P1 weapons — a third of the 6-slot floor — so the alternating pair repeats 3 times.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.MirrorBlade, AttackState.VoidConcentration,
            AttackState.MirrorBlade, AttackState.VoidConcentration,
            AttackState.MirrorBlade, AttackState.VoidConcentration,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.DarkSpark, AttackState.EventHorizon, AttackState.Mistlestorm, AttackState.OntologicalDespoiler,
            AttackState.SealedSingularity, AttackState.TacticiansTrump, AttackState.Eternity,
            AttackState.PhantasmalFury, AttackState.RealityRupture,
        };
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private int attackCycleIndex = 0;

        private readonly bool[] attackVariant = new bool[12];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Dark Energy Void Amplifiers
        private readonly float[] orbiterHPs = new float[6];
        private int stunTimer = 0;
        private int respawnOrbitersTimer = 0;

        // 六个环绕体的血量 gate 95% 减伤、stunTimer gate 150% 惩罚窗口 —— 30 倍的伤害差。
        protected override void DeclareSyncedFields(LegendsSyncedFields f) => f
            .FloatArray(orbiterHPs)
            .Int(() => stunTimer, v => stunTimer = v)
            .Int(() => respawnOrbitersTimer, v => respawnOrbitersTimer = v);
        private int orbiterFxCooldown = 0;

        private int arenaHurtCooldown = 0;
        private float transitionFlashAlpha = 0f;

        // Motion afterimages — mostly a stationary anchor, but the P3 charges and any positioning burst
        // still benefit from the same ghost-trail convention the rest of the roster uses.
        private readonly Vector2[] oldPos = new Vector2[9];
        private int oldPosIndex = 0;
        #endregion

        #region Per-fight State Reset
        // 跨场次状态清理（为什么需要见基类 LegendsBossAI.ResetFightState；调用时机由框架负责）。
        // 本 Boss 残留最要命的是 orbiterHPs —— 六个环绕体的血量若带着上一场的残值进场，
        // 玩家会发现有几个"一打就碎"甚至开局就是碎的，整个部位阶段被跳过。
        public override void ResetFightState(NPC npc, Player target)
        {
            ticksRunning = 0;
            currentRepetition = 0;
            attackCycleIndex = 0;
            Array.Clear(attackVariant, 0, attackVariant.Length);

            Array.Clear(orbiterHPs, 0, orbiterHPs.Length);
            stunTimer = 0;
            respawnOrbitersTimer = 0;
            orbiterFxCooldown = 0;

            arenaHurtCooldown = 0;
            transitionFlashAlpha = 0f;

            for (int i = 0; i < oldPos.Length; i++)
                oldPos[i] = npc.Center;
            oldPosIndex = 0;
        }
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
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
            ref float stateTracker = ref npc.ai[3];

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.MirrorBlade;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
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
                stateTracker = 0;
                npc.netUpdate = true;
            }

            if (orbiterHPs[0] == 0f && stunTimer == 0 && respawnOrbitersTimer == 0)
            {
                for (int i = 0; i < 6; i++) orbiterHPs[i] = 600f;
            }

            float borderSize = currentPhase == 1 ? 1300f : 900f;
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;
            Vector2 dist = target.Center - npc.Center;

            // The void boundary must be visible: dark motes trace the square edge
            {
                float half = borderSize / 2f;
                for (int i = 0; i < 3; i++)
                {
                    float t = Main.rand.NextFloat(4f);
                    Vector2 pos;
                    if (t < 1f) pos = npc.Center + new Vector2(MathHelper.Lerp(-half, half, t), -half);
                    else if (t < 2f) pos = npc.Center + new Vector2(half, MathHelper.Lerp(-half, half, t - 1f));
                    else if (t < 3f) pos = npc.Center + new Vector2(MathHelper.Lerp(half, -half, t - 2f), half);
                    else pos = npc.Center + new Vector2(-half, MathHelper.Lerp(half, -half, t - 3f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 160, default, 1.05f);
                    d.noGravity = true;
                    d.fadeIn = 1f;
                }
            }

            if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
            {
                target.AddBuff(BuffID.Darkness, 180);
                target.AddBuff(BuffID.Slow, 180); // Void Decay: -40% move speed for 3s
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                }
            }

            if (state != AttackState.Transition && state != AttackState.DeathAnimation)
                UpdateGravityBreathing(npc, target, currentPhase);
            UpdateOrbiterDeflection(npc, target);
            UpdateOrbitersRespawn();
            if (orbiterFxCooldown > 0) orbiterFxCooldown--;

            if (stunTimer > 0)
            {
                // Orbiters all shattered: the void core sputters, leaking dark matter
                stunTimer--;
                npc.velocity *= 0.85f;
                npc.damage = 0;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), DustID.PurpleTorch, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1f, 2.5f)), 100, default, 1.3f);
                    d.noGravity = true;
                }
                if (stunTimer == 0)
                {
                    SoundEngine.PlaySound(VoidPulseSound, npc.Center);
                    VoidFx.Burst(npc.Center, 6f, 24);
                }
            }
            else if (state != AttackState.Transition && state != AttackState.DeathAnimation)
            {
                npc.damage = npc.defDamage;
                // The Void keeps its distance — a higher, wider standoff arc instead of camping the player's head
                Vector2 desiredPos = target.Center + new Vector2((float)Math.Cos(ticksRunning * 0.02f) * 280f, -230f);
                Vector2 desiredVel = (desiredPos - npc.Center) * 0.03f;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, 0.1f);
            }
            if (state != AttackState.DeathAnimation)
                npc.rotation = ticksRunning * 0.05f;

            if (stunTimer == 0)
            {
                switch (state)
                {
                    case AttackState.MirrorBlade: ExecuteMirrorBlade(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.VoidConcentration: ExecuteVoidConcentration(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.DarkSpark: ExecuteDarkSpark(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.EventHorizon: ExecuteEventHorizon(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.Mistlestorm: ExecuteMistlestorm(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.OntologicalDespoiler: ExecuteOntologicalDespoiler(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.SealedSingularity: ExecuteSealedSingularity(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.TacticiansTrump: ExecuteTacticiansTrump(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.Eternity: ExecuteEternity(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.PhantasmalFury: ExecutePhantasmalFury(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.RealityRupture: ExecuteRealityRupture(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.Transition: ExecuteTransition(npc, target, ref timer, ref stateTracker); break;
                    case AttackState.DeathAnimation: ExecuteDeathAnimation(npc, target, ref timer); break;
                }
            }

            oldPos[oldPosIndex] = npc.Center;
            oldPosIndex = (oldPosIndex + 1) % oldPos.Length;

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ProcessOrbiterHits(npc, player.Center, ref modifiers, item.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ProcessOrbiterHits(npc, projectile.Center, ref modifiers, projectile.damage);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }
        #endregion

        #region Helpers
        private void UpdateGravityBreathing(NPC npc, Player target, int currentPhase)
        {
            int cycle = currentPhase == 1 ? 420 : 240;
            int siphonEnd = currentPhase == 1 ? 240 : 120;
            int holdEnd = currentPhase == 1 ? 300 : 160;

            int timer = ticksRunning % cycle;

            if (timer < siphonEnd)
            {
                Vector2 pullDir = SafeNormalize(npc.Center - target.Center, Vector2.Zero);
                float dist = Vector2.Distance(npc.Center, target.Center);
                target.velocity += pullDir * (60000f / (dist * dist + 1000f));

                // The siphon must be SEEN: void-dust streams from around the player toward the maw
                if (Main.rand.NextBool(2))
                {
                    Vector2 around = target.Center + Main.rand.NextVector2CircularEdge(120f, 120f);
                    Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, pullDir * Main.rand.NextFloat(2f, 5f), 120, default, 1.1f);
                    d.noGravity = true;
                }
                // Final second of the siphon: warning ring tightens — the stasis is coming
                if (timer > siphonEnd - 60 && Main.rand.NextBool(2))
                {
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust warn = Dust.NewDustPerfect(target.Center + a.ToRotationVector2() * MathHelper.Lerp(90f, 40f, (timer - (siphonEnd - 60)) / 60f), DustID.ShadowbeamStaff, Vector2.Zero, 100, default, 1.2f);
                    warn.noGravity = true;
                }
            }
            else if (timer < holdEnd)
            {
                target.velocity = Vector2.Zero;

                // Stasis shell: a visible cage of void-light holds the player — the freeze has a CAUSE
                if (timer == siphonEnd)
                    SoundEngine.PlaySound(StasisSound, target.Center);
                for (int i = 0; i < 2; i++)
                {
                    float a = ticksRunning * 0.2f + i * MathHelper.Pi;
                    Dust d = Dust.NewDustPerfect(target.Center + a.ToRotationVector2() * 46f, DustID.ShadowbeamStaff, Vector2.Zero, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                Vector2 pushDir = SafeNormalize(target.Center - npc.Center, Vector2.Zero);
                target.velocity += pushDir * 8f;
                // Expulsion wake trailing the thrown player
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(target.Center - pushDir * 30f, DustID.PurpleTorch, pushDir * 3f, 120, default, 1.15f);
                    d.noGravity = true;
                }

                if (timer == holdEnd && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dmg = npc.damage / 3;
                    SoundEngine.PlaySound(SoundID.Item62, npc.Center);
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = (i * MathHelper.TwoPi / 24f).ToRotationVector2() * 8f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<VoidOrbProj>(), dmg, 0f, Main.myPlayer);
                    }
                }
            }
        }

        private void UpdateOrbiterDeflection(NPC npc, Player target)
        {
            for (int i = 0; i < 6; i++)
            {
                if (orbiterHPs[i] <= 0f) continue;
                Vector2 orbiterPos = npc.Center + (ticksRunning * 0.02f + i * MathHelper.TwoPi / 6f).ToRotationVector2() * 140f;

                for (int p = 0; p < Main.maxProjectiles; p++)
                {
                    Projectile proj = Main.projectile[p];
                    if (proj.active && proj.hostile && proj.ModProjectile is VoidOrbProj && Vector2.Distance(proj.Center, orbiterPos) < 45f)
                    {
                        proj.Kill();
                        int dmg = npc.damage / 3;
                        Vector2 dir = SafeNormalize(target.Center - orbiterPos, Vector2.UnitY);
                        for (int s = -1; s <= 1; s++)
                            Projectile.NewProjectile(npc.GetSource_FromAI(), orbiterPos, dir.RotatedBy(s * 0.22f) * 12f, ModContent.ProjectileType<VoidSplitLaserProj>(), dmg, 0f, Main.myPlayer);
                    }
                }
            }
        }

        private void UpdateOrbitersRespawn()
        {
            bool allDead = true;
            for (int i = 0; i < 6; i++) if (orbiterHPs[i] > 0f) allDead = false;

            if (allDead && stunTimer == 0)
            {
                respawnOrbitersTimer++;
                if (respawnOrbitersTimer >= 1500) // 25s respawn
                {
                    for (int i = 0; i < 6; i++) orbiterHPs[i] = 600f;
                    respawnOrbitersTimer = 0;
                }
            }
        }

        private void ProcessOrbiterHits(NPC npc, Vector2 hitPos, ref NPC.HitModifiers modifiers, int damage)
        {
            if (npc.ai[1] == (float)AttackState.Transition)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }

            if (stunTimer > 0)
            {
                modifiers.FinalDamage *= 1.5f; // 150% damage during the all-orbiters-broken stun
                return;
            }

            int activeCount = 0;
            for (int i = 0; i < 6; i++) if (orbiterHPs[i] > 0f) activeCount++;
            if (activeCount > 0)
                modifiers.FinalDamage *= 0.05f; // 95% DR while orbiters are active

            for (int i = 0; i < 6; i++)
            {
                if (orbiterHPs[i] <= 0f) continue;
                Vector2 orbiterPos = npc.Center + (ticksRunning * 0.02f + i * MathHelper.TwoPi / 6f).ToRotationVector2() * 140f;
                if (Vector2.Distance(hitPos, orbiterPos) < 80f)
                {
                    orbiterHPs[i] -= damage;
                    if (orbiterFxCooldown <= 0) { orbiterFxCooldown = 8; SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.4f }, orbiterPos); }
                    if (orbiterHPs[i] <= 0f)
                    {
                        SoundEngine.PlaySound(OrbiterBreakSound, orbiterPos);
                        VoidFx.Burst(orbiterPos, 5f, 14);
                        CheckAllOrbitersBroken(npc);
                    }
                    break;
                }
            }
        }

        private void CheckAllOrbitersBroken(NPC npc)
        {
            bool allDead = true;
            for (int i = 0; i < 6; i++) if (orbiterHPs[i] > 0f) allDead = false;

            if (allDead)
            {
                stunTimer = 480; // 8s stun (design doc)
                npc.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                VoidFx.Burst(npc.Center, 7f, 30);
                if (Main.netMode != NetmodeID.Server)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 8f;
            }
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, AttackState current)
        {
            if (IsP1(current))
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
                    return;
                }
                currentRepetition = 0;
                attackCycleIndex++;
                npc.ai[1] = (float)P1Cycle[attackCycleIndex % P1Cycle.Length];
            }
            else
            {
                attackCycleIndex++;
                npc.ai[1] = (float)P2Cycle[attackCycleIndex % P2Cycle.Length];
            }
            npc.ai[2] = 0; npc.ai[3] = 0; npc.netUpdate = true;
        }
        #endregion

        #region P1 Attacks
        // MIRROR BLADE — a blade dashes straight and bounces off the arena boundary toward the player's back.
        private void ExecuteMirrorBlade(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.MirrorBlade) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<VoidHeldMirrorBlade>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 12f;
                if (!variantB)
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<MirrorBladeSwordProj>(), npc.damage / 3, 0f, Main.myPlayer, 2f, 650f, npc.Center.X);
                    if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[3] = npc.Center.Y;
                }
                else
                {
                    foreach (float spread in new float[] { -0.35f, 0.35f })
                    {
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel.RotatedBy(spread), ModContent.ProjectileType<MirrorBladeSwordProj>(), npc.damage / 4, 0f, Main.myPlayer, 1f, 650f, npc.Center.X);
                        if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[3] = npc.Center.Y;
                    }
                }
                FindHeldWeapon<VoidHeldMirrorBlade>(npc)?.Pulse(14f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.MirrorBlade);
        }

        // VOID CONCENTRATION — 3 mini singularities absorb bullets, then burst outward.
        private void ExecuteVoidConcentration(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldVoidConcentration>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i * 180f - 180f, -220f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<VoidAbsorbHoleProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<VoidHeldVoidConcentration>(npc)?.Pulse(10f);
            }

            if (timer >= 200)
                RotateAttack(npc, AttackState.VoidConcentration);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteDarkSpark(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldDarkSpark>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 6f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<VoidDarkSparkCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<VoidHeldDarkSpark>(npc)?.Pulse(10f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.DarkSpark);
        }

        private void ExecuteEventHorizon(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldEventHorizon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<VoidShrinkRingProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<VoidHeldEventHorizon>(npc)?.Pulse(10f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.EventHorizon);
        }

        private void ExecuteMistlestorm(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<VoidHeldMistlestorm>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 100 && timer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 11f, ModContent.ProjectileType<MistlestormLeafProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                if (idx >= 0 && idx < Main.maxProjectiles) Main.projectile[idx].ai[2] = timer * 0.1f;
                FindHeldWeapon<VoidHeldMistlestorm>(npc)?.Pulse(6f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.Mistlestorm);
        }

        private void ExecuteOntologicalDespoiler(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<VoidHeldOntologicalDespoiler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer >= 30 && timer <= 130 && timer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angle = MathF.Sin(timer * 0.08f) * 0.4f;
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(angle) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<OntologicalBulletProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<VoidHeldOntologicalDespoiler>(npc)?.Pulse(4f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.OntologicalDespoiler);
        }

        private void ExecuteSealedSingularity(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldSealedSingularity>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<SealedSingularityCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<VoidHeldSealedSingularity>(npc)?.Pulse(12f);
            }

            if (timer >= 180)
                RotateAttack(npc, AttackState.SealedSingularity);
        }

        private void ExecuteTacticiansTrump(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldTacticiansTrump>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 cardPos = target.Center + new Vector2(i * 200f - 300f, -400f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), cardPos, new Vector2(0f, 12f), ModContent.ProjectileType<TacticianCardLaserProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<VoidHeldTacticiansTrump>(npc)?.Pulse(10f);
            }

            if (timer >= 160)
                RotateAttack(npc, AttackState.TacticiansTrump);
        }

        private void ExecuteEternity(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldEternity>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float startAngle = (target.Center - npc.Center).ToRotation() - MathHelper.ToRadians(60f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<EternityBeamProj>(), npc.damage / 3, 0f, Main.myPlayer, startAngle);
                FindHeldWeapon<VoidHeldEternity>(npc)?.Pulse(14f);
            }

            if (timer >= 170)
                RotateAttack(npc, AttackState.Eternity);
        }

        private void ExecutePhantasmalFury(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldPhantasmalFury>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawn = npc.Center + (i * MathHelper.TwoPi / 6f).ToRotationVector2() * 80f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<PhantasmalPhantomProj>(), npc.damage / 4, 0f, Main.myPlayer);
                }
                FindHeldWeapon<VoidHeldPhantasmalFury>(npc)?.Pulse(10f);
            }

            if (timer >= 220)
                RotateAttack(npc, AttackState.PhantasmalFury);
        }

        private void ExecuteRealityRupture(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<VoidHeldRealityRupture>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (float side in new float[] { -1f, 1f })
                {
                    Vector2 spawn = target.Center + new Vector2(side * 900f, 0f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<VoidRiftProj>(), npc.damage / 3, 0f, Main.myPlayer, -side * 6f);
                }
                FindHeldWeapon<VoidHeldRealityRupture>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, AttackState.RealityRupture);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker)
        {
            timer++;
            npc.velocity *= 0.85f;
            npc.dontTakeDamage = true;
            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            if (timer == 45)
                VoidFx.Burst(npc.Center, 7f, 30);

            if (timer >= 90)
            {
                npc.dontTakeDamage = false;
                transitionFlashAlpha = 0f;
                attackCycleIndex = 0;
                npc.ai[1] = (float)P2Cycle[0];
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Death Animation
        // 奇点坍缩 — 五段演出, 把它的引力操控身份彻底反转到自己身上, 而不是通用爆炸:
        // 虚空震颤 -> 引力失稳反噬(呼吸节律紊乱) -> 镜像回收 -> 奇点坍缩(自吸) -> 终末爆发.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            stunTimer = 0;
            for (int i = 0; i < 6; i++) orbiterHPs[i] = 0f;
            npc.netUpdate = true;

            TriggerDeathCinematic(npc, target, focusStrength: 0.6f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Item103 with { Volume = 1f, Pitch = -0.5f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;
            npc.velocity *= 0.94f;

            if (timer < 30f)
            {
                // 虚空震颤 — spin destabilizes, faster and less even than its usual steady drift
                npc.rotation += 0.15f + MathF.Sin(timer * 0.5f) * 0.05f;
                if ((int)timer % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), DustID.PurpleTorch, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 75f)
            {
                // 引力失稳反噬 — the breathing rhythm it usually inflicts on the player turns erratic on itself:
                // short alternating in/out dust pulses instead of the clean siphon-hold-push cycle
                float t = timer - 30f;
                bool pulling = (int)(t / 8f) % 2 == 0;
                npc.rotation += pulling ? 0.03f : -0.06f;
                if ((int)t % 2 == 0)
                {
                    Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(90f, 90f);
                    Vector2 vel = pulling ? (npc.Center - around) * 0.06f : (around - npc.Center) * 0.06f;
                    Dust d = Dust.NewDustPerfect(around, DustID.ShadowbeamStaff, vel, 100, default, 1.2f);
                    d.noGravity = true;
                }
            }
            else if (timer < 110f)
            {
                // 镜像回收 — any lingering mirror-shard identity gets pulled back in and consumed
                float t = timer - 75f;
                if ((int)t % 3 == 0)
                {
                    float a = t * 0.4f;
                    Vector2 spawn = npc.Center + a.ToRotationVector2() * MathHelper.Lerp(160f, 30f, t / 35f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.PurpleCrystalShard, (npc.Center - spawn) * 0.08f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 150f)
            {
                // 奇点坍缩 — full self-implosion, the cinematic pull peaks as it shrinks toward a point
                float t = timer - 110f;
                npc.scale = MathHelper.Lerp(1f, 0.15f, Math.Min(1f, t / 40f));
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(220f, 220f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.PurpleTorch, (npc.Center - spawn) * 0.09f, 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末爆发 — the actual kill fires once, everything after is the lingering burst
                if (timer == 150f)
                {
                    npc.scale = 1f;
                    SoundEngine.PlaySound(SoundID.Item62, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    VoidFx.Burst(npc.Center, 8f, 40);
                    VoidFx.Burst(npc.Center, 5f, 24);
                }

                if (timer >= 172f)
                {
                    npc.dontTakeDamage = false;
                    npc.StrikeInstantKill();
                }
            }
        }
        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Motion afterimages — this boss mostly holds position, so the trail only shows up during the
            // rare real movement (P3 charges, positioning bursts), same convention as the rest of the roster.
            if (npc.velocity.Length() > 6f)
            {
                Texture2D tex = TextureAssets.Npc[npc.type].Value;
                Vector2 origin = npc.frame.Size() * 0.5f;
                for (int i = 1; i < oldPos.Length; i++)
                {
                    int idx = (oldPosIndex - i + oldPos.Length * 2) % oldPos.Length;
                    if (oldPos[idx] == Vector2.Zero) continue;
                    float fade = (1f - i / (float)oldPos.Length) * 0.35f * npc.Opacity;
                    Color ghost = new Color(180, 100, 255, 0) * fade;
                    spriteBatch.Draw(tex, oldPos[idx] - screenPos, npc.frame, ghost, npc.rotation, origin, npc.scale * (1f - i * 0.02f), SpriteEffects.None, 0f);
                }
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D glowTex = TextureAssets.Dust.Value;
            Rectangle sourceRect = new Rectangle(0, 0, 8, 8);

            for (int i = 0; i < 6; i++)
            {
                if (orbiterHPs[i] <= 0f) continue;
                Vector2 orbiterPos = npc.Center + (ticksRunning * 0.02f + i * MathHelper.TwoPi / 6f).ToRotationVector2() * 140f;
                spriteBatch.Draw(glowTex, orbiterPos - screenPos, sourceRect, Color.Purple * 0.7f, 0f, new Vector2(4f, 4f), 5f, SpriteEffects.None, 0f);
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
