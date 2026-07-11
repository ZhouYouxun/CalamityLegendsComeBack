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

        // Only 2 named P1 weapons — a third of the 6-slot floor — so the alternating pair repeats 3 times.
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.CosmicKunai, AttackState.Cosmilamp,
            AttackState.CosmicKunai, AttackState.Cosmilamp,
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

            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.CosmicKunai;
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
                tracker = 0;
                npc.netUpdate = true;
            }

            if (stunTimer > 0)
            {
                stunTimer--;
                npc.velocity = Vector2.Zero;
                npc.alpha = 0;
            }
            else
            {
                npc.alpha = coreExposed ? 0 : 230; // 90% transparent while cloaked
            }

            UpdateMineGrid(npc, target, currentPhase);
            if (arenaHurtCooldown > 0) arenaHurtCooldown--;

            float borderSize = currentPhase == 1 ? 1400f : 1000f;
            Vector2 dist = target.Center - npc.Center;
            if (dist.Length() > borderSize / 2f)
            {
                target.AddBuff(BuffID.ShadowFlame, 180);
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 20, 0);
                }
            }

            if (stunTimer == 0 && state != AttackState.Transition)
            {
                float baseSpeed = currentPhase == 1 ? 14f : 20f;
                float speed = baseSpeed + (1f - lifeRatio) * 6f;
                float turnSpeed = 0.05f + (1f - lifeRatio) * 0.03f;
                Vector2 desiredVel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * speed;
                npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, turnSpeed);
            }
            npc.rotation = npc.velocity.X * 0.05f;

            if (stunTimer == 0)
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

            // Edge-detect the start of a decloak: in P2, every exposure also fires 4 reflective kunai (design doc).
            if (coreExposed && !wasCoreExposed && !IsP1(state) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathHelper.TwoPi / 4f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, a.ToRotationVector2() * 9f, ModContent.ProjectileType<ReflectKunaiProj>(), npc.damage / 4, 0f, Main.myPlayer);
                }
            }
            wasCoreExposed = coreExposed;

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
                return;
            }

            if (damage > 80)
            {
                stunTimer = 180; // 3s interrupt stun (design doc)
                SoundEngine.PlaySound(SoundID.NPCHit53, npc.Center);
                SignusFx.Burst(npc.Center, 6f, 20);
            }
        }
        #endregion

        #region Helpers
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
                        Projectile.NewProjectile(npc.GetSource_FromAI(), minePositions[i], Vector2.Zero, ModContent.ProjectileType<CosmicMineProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
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
                        target.velocity += SafeNormalize(closest - target.Center, Vector2.Zero) * 0.6f;
                }
            }
            else
            {
                minesActive = false;
            }
        }

        // Sets coreExposed for the given timer window (30 frames = 0.5s in P1, 18 frames = 0.3s in P2 per doc).
        private void ExposeWindow(ref float timer, int start, bool isP2)
        {
            int window = isP2 ? 18 : 30;
            coreExposed = timer >= start && timer <= start + window;
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            coreExposed = false;
            if (currentPhase == 1)
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
        private void ExecuteCosmicKunai(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldCosmicKunai>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 50, false);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                for (int i = 0; i < 5; i++)
                {
                    float angle = MathHelper.Lerp(-0.3f, 0.3f, i / 4f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(angle) * 10f, ModContent.ProjectileType<SignusKunaiProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<SignusHeldCosmicKunai>(npc)?.Pulse(12f);
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.CosmicKunai);
        }

        private void ExecuteCosmilamp(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldCosmilamp>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 60, false);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = target.Center + new Vector2(i * 180f - 180f, -220f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<CosmilampLanternProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<SignusHeldCosmilamp>(npc)?.Pulse(10f);
            }

            if (timer >= 180)
                RotateAttack(npc, phase, AttackState.Cosmilamp);
        }
        #endregion

        #region P2 Attacks
        private void ExecuteAethersWhisper(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldAethersWhisper>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 50, true);

            if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.15f) * 12f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<WhisperBulletProj>(), npc.damage / 3, 0f, Main.myPlayer, 700f, npc.Center.X, npc.Center.Y);
                }
                FindHeldWeapon<SignusHeldAethersWhisper>(npc)?.Pulse(8f);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.AethersWhisper);
        }

        private void ExecuteDeathsAscension(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldDeathsAscension>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 45, true);

            if (timer == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 14f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<DeathsAscensionScytheProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.DeathsAscension);
        }

        private void ExecuteEmpyreanKnives(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldEmpyreanKnives>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathHelper.TwoPi / 8f;
                    Vector2 pos = target.Center + a.ToRotationVector2() * 200f + new Vector2(0f, -100f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<EmpyreanKnifeProj>(), npc.damage / 3, 0f, Main.myPlayer, 30f + i * 8f);
                }
            }
            ExposeWindow(ref timer, 30, true);

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.EmpyreanKnives);
        }

        private void ExecuteKingConstellations(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldKingConstellations>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<ConstellationGridProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<SignusHeldKingConstellations>(npc)?.Pulse(10f);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.KingConstellations);
        }

        private void ExecuteMagneticMeltdown(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldMagneticMeltdown>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 4f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<MagneticSphereProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<SignusHeldMagneticMeltdown>(npc)?.Pulse(-10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.MagneticMeltdown);
        }

        private void ExecuteNadir(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldNadir>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 40, true);

            if (timer >= 40 && timer <= 100 && timer % 10 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-20f, 20f), 200f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(Main.rand.NextFloat(-2f, 2f), -12f), ModContent.ProjectileType<NadirGearProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.Nadir);
        }

        private void ExecuteSevensStriker(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldSevensStriker>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 40, true);

            if (timer >= 40 && timer <= 130 && timer % 12 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<SevensStrikerBulletProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                FindHeldWeapon<SignusHeldSevensStriker>(npc)?.Pulse(6f);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.SevensStriker);
        }

        private void ExecuteVenusianTrident(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, SafeNormalize(target.Center - npc.Center, Vector2.UnitY), ModContent.ProjectileType<SignusHeldVenusianTrident>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            ExposeWindow(ref timer, 40, true);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy((i - 1) * 0.2f) * 13f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<VenusianTridentProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<SignusHeldVenusianTrident>(npc)?.Pulse(12f);
            }

            if (timer >= 170)
                RotateAttack(npc, phase, AttackState.VenusianTrident);
        }

        private void ExecuteRealityRupture(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<SignusHeldRealityRupture>(), 0, 0f, Main.myPlayer, npc.whoAmI);
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(220f, 0f), Vector2.Zero, ModContent.ProjectileType<SignusRiftProj>(), npc.damage / 3, 0f, Main.myPlayer);
            }
            ExposeWindow(ref timer, 40, true);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.RealityRupture);
        }

        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            coreExposed = false;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;
            transitionFlashAlpha = MathHelper.Clamp(1f - Math.Abs(timer - 22f) / 22f, 0f, 1f);

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 9f;
            }

            if (timer == 45)
                SignusFx.Burst(npc.Center, 7f, 30);

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

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (coreExposed && stunTimer == 0)
            {
                Texture2D glowTex = TextureAssets.Dust.Value;
                Rectangle sourceRect = new Rectangle(0, 0, 8, 8);
                spriteBatch.Draw(glowTex, npc.Center - screenPos, sourceRect, Color.Purple * 0.9f, ticksRunning * 0.05f, new Vector2(4f, 4f), 5f, SpriteEffects.None, 0f);
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
