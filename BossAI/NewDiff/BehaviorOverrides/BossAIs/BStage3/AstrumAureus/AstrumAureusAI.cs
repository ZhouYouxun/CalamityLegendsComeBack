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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.AstrumAureus
{
    internal sealed class AstrumAureusAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/AstrumAureus").Type;
        public override string BossName => "Astrum Aureus";
        public override Color DebugColor => new(230, 200, 60);

        // Design doc specifies a single 50% HP unseal, not a 3-phase ladder.
        public override int MaxPhaseCount => 2;
        public override float[] PhaseLifeRatios => new[] { 0.50f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 1.0f;
        #endregion

        #region Attack States
        public enum AttackState
        {
            Nebulash = 0,
            AuroraBlazer = 1,
            AlulaAustralis = 2,
            BorealisBomber = 3,
            AuroradicalThrow = 4,
            AstralScythe = 5,
            TitanArm = 6,
            StellarCannon = 7,
            StellarKnife = 8,
            AstralachneaStaff = 9,
            AbandonedSlime = 10,
            HivePod = 11,
            StateTransition = 12,
            DeathAnimation = 13
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;

        // Rotation slot persists across HP-threshold bookkeeping; only the one real P1->P2 unseal resets it.
        // P1 has only 5 weapons, one short of the "at least 6 rotation slots per phase" floor, so Nebulash
        // is deliberately given two slots in the cycle — its UseVariantB toggle then naturally alternates
        // A/B between those two appearances (and keeps alternating lap to lap), so the "extra slot" reads
        // as a genuinely different execution rather than a repeat.
        private int attackCycleIndex = 0;
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.Nebulash, AttackState.AuroraBlazer, AttackState.AlulaAustralis,
            AttackState.BorealisBomber, AttackState.AuroradicalThrow, AttackState.Nebulash,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.AstralScythe, AttackState.TitanArm, AttackState.StellarCannon, AttackState.StellarKnife,
            AttackState.AstralachneaStaff, AttackState.AbandonedSlime, AttackState.HivePod,
        };

        // Per-attack variant toggle: flips deterministically each time that attack slot comes up, no RNG.
        private readonly bool[] attackVariant = new bool[13];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }

        // Stellar Limbs Emitters (4x600HP leg shield)
        private bool shieldActive = true;
        private int shieldStunTimer = 0;
        private int shieldRegenTimer = 0;
        private int shieldFxCooldown = 0;

        // Stellar Gravity Anomaly Well
        private int gravityCycleTimer = 0;
        private bool superGravity = true;

        private float transitionFlashAlpha = 0f;
        private int arenaHurtCooldown = 0;

        // Per-attack scratch state
        private float nebulashDirX, nebulashDirY;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
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

            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            // Re-normalize phase/state
            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.Nebulash;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                npc.netUpdate = true;
            }

            // Single real transition at 50% HP.
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = lifeRatio <= PhaseLifeRatios[0] ? 2 : 1;

            if (nextPhase > currentPhase)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;
                state = AttackState.StateTransition;
                npc.ai[1] = (float)state;
                timer = 0;
                stateTracker = 0;
                npc.netUpdate = true;
            }

            // Gravity anomaly cycles — frequency doubles (2s instead of 5s/3s) once P2's reactor overload hits.
            UpdateGravityAnomaly(npc, target, currentPhase);

            // Bounding arena — tightens as the boss shrinks 15% and speeds up 50% in P2.
            float borderSize = currentPhase <= 1 ? 1200f : 900f;
            Vector2 dist = target.Center - npc.Center;
            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (dist.Length() > borderSize / 2f)
            {
                target.velocity += SafeNormalize(npc.Center - target.Center, Vector2.Zero) * 2f;
                target.AddBuff(BuffID.Cursed, 60);
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 10, 0);
                }
            }

            // Stellar Limbs Emitters — only defends P1; the reactor is exposed for the entire second phase.
            UpdateEmitterShield(npc, currentPhase);

            if (state != AttackState.DeathAnimation)
            {
                npc.rotation = npc.velocity.X * 0.03f;
                npc.scale = (currentPhase <= 1 ? 1.05f : 0.9f) + (float)Math.Sin(ticksRunning * 0.05f) * 0.02f;
            }

            if (shieldFxCooldown > 0)
                shieldFxCooldown--;

            switch (state)
            {
                case AttackState.Nebulash:
                    ExecuteNebulash(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AuroraBlazer:
                    ExecuteAuroraBlazer(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AlulaAustralis:
                    ExecuteAlulaAustralis(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BorealisBomber:
                    ExecuteBorealisBomber(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AuroradicalThrow:
                    ExecuteAuroradicalThrow(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AstralScythe:
                    ExecuteAstralScythe(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.TitanArm:
                    ExecuteTitanArm(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.StellarCannon:
                    ExecuteStellarCannon(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.StellarKnife:
                    ExecuteStellarKnife(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AstralachneaStaff:
                    ExecuteAstralachneaStaff(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.AbandonedSlime:
                    ExecuteAbandonedSlime(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.HivePod:
                    ExecuteHivePod(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.StateTransition:
                    ExecuteTransition(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteDeathAnimation(npc, target, ref timer);
                    break;
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) => ApplyDefenseModifiers(npc, ref modifiers, player);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) => ApplyDefenseModifiers(npc, ref modifiers, Main.player[projectile.owner]);

        private void ApplyDefenseModifiers(NPC npc, ref NPC.HitModifiers modifiers, Player target)
        {
            if (npc.ai[1] == (float)AttackState.StateTransition)
            {
                modifiers.FinalDamage *= 0f; // fully invulnerable during the reactor-overload cutscene
                return;
            }

            if (shieldActive && (int)npc.ai[0] <= 1)
            {
                modifiers.FinalDamage *= 0f; // deflector field reflects all frontal/side hits while any emitter survives
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 10;
                    SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.5f, Pitch = 0.3f }, npc.Center);
                }
                return;
            }
            else if (shieldStunTimer > 0)
            {
                modifiers.FinalDamage *= 1.5f; // reactor exposed — 150% damage taken
            }

            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, target));
        }
        #endregion

        #region Systems Helpers
        private void UpdateGravityAnomaly(NPC npc, Player target, int currentPhase)
        {
            gravityCycleTimer++;
            int highDuration = currentPhase <= 1 ? 300 : 120;
            int lowDuration = currentPhase <= 1 ? 180 : 120;
            int activeDuration = superGravity ? highDuration : lowDuration;

            // Ongoing mode indicator: faint astral motes drift the way gravity currently pulls the player
            if (Main.rand.NextBool(4))
            {
                Vector2 driftDir = superGravity ? Vector2.UnitY : -Vector2.UnitY;
                Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.PurpleTorch, driftDir * Main.rand.NextFloat(1.5f, 3f), 160, default, 0.85f);
                d.noGravity = true;
            }

            // Pre-flip warning: 40 frames out, gold motes stream the way gravity is ABOUT to pull — the flip announces itself
            if (gravityCycleTimer > activeDuration - 40 && Main.rand.NextBool(2))
            {
                Vector2 nextDir = superGravity ? -Vector2.UnitY : Vector2.UnitY;
                Dust warn = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(90f, 90f), DustID.GoldFlame, nextDir * Main.rand.NextFloat(2f, 4f), 100, default, 1.2f);
                warn.noGravity = true;
                warn.fadeIn = 1.1f;
            }

            if (superGravity)
            {
                target.gravity *= 2f;
                target.maxFallSpeed *= 1.5f;
                if (gravityCycleTimer >= highDuration)
                {
                    superGravity = false;
                    gravityCycleTimer = 0;
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.3f }, target.Center);
                }
            }
            else
            {
                target.gravity = -0.5f;
                if (gravityCycleTimer >= lowDuration)
                {
                    superGravity = true;
                    gravityCycleTimer = 0;
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.3f }, target.Center);
                }
            }
        }

        private void UpdateEmitterShield(NPC npc, int currentPhase)
        {
            if (currentPhase > 1)
            {
                shieldActive = false;
                return;
            }

            int emitterType = ModContent.Find<ModNPC>("CalamityMod/AureusSpawn").Type;

            if (shieldActive)
            {
                bool alive = false;
                Vector2[] legOffsets = { new(-60f, 40f), new(60f, 40f), new(-100f, 20f), new(100f, 20f) };
                int activeEmitterIndex = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC emitter = Main.npc[i];
                    if (emitter.active && emitter.type == emitterType && emitter.ai[0] == npc.whoAmI)
                    {
                        alive = true;
                        if (activeEmitterIndex < 4)
                        {
                            emitter.Center = npc.Center + legOffsets[activeEmitterIndex].RotatedBy(npc.rotation);
                            emitter.velocity = Vector2.Zero;
                            activeEmitterIndex++;
                        }
                    }
                }

                if (!alive)
                {
                    shieldActive = false;
                    shieldStunTimer = 420; // 7s stun, 150% damage taken (design doc)
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.NPCHit53 with { Pitch = -0.2f }, npc.Center);
                    Player t = Main.player[npc.target];
                    if (t.active) t.Calamity().GeneralScreenShakePower = 7f;
                    AureusFx.Burst(npc.Center, 6f, 30);
                }
            }
            else
            {
                if (shieldStunTimer > 0)
                {
                    shieldStunTimer--;
                    npc.defense = 0;
                    if (shieldStunTimer == 0)
                        shieldRegenTimer = 1200; // 20s weak period before regen
                }
                else if (shieldRegenTimer > 0)
                {
                    shieldRegenTimer--;
                    if (shieldRegenTimer == 0)
                    {
                        shieldActive = true;
                        AureusFx.Burst(npc.Center, 4f, 20);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                int emitter = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + Main.rand.Next(-80, 80), (int)npc.Center.Y + Main.rand.Next(-40, 40), emitterType);
                                if (emitter >= 0 && emitter < Main.maxNPCs)
                                {
                                    Main.npc[emitter].ai[0] = npc.whoAmI;
                                    Main.npc[emitter].netUpdate = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Anti-cheese hover spot — offsets to whichever side the boss is already on and leads the target's
        // movement, so it never camps directly overhead for an attack's full duration.
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }

        private static void SmoothHover(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.13f);
        }
        #endregion

        #region Attack Rotation
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            if (currentPhase <= 1)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                    npc.netUpdate = true;
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
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region P1 State Machine — Star-Cruiser Payload
        // NEBULASH — chain-delay whip crack. A: single lash toward the player. B: crossing double lash (an X).
        private void ExecuteNebulash(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.Nebulash) ? 1f : 0f;
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                nebulashDirX = dir.X; nebulashDirY = dir.Y;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<AureusHeldNebulash>(), 0, 0f, Main.myPlayer, npc.whoAmI, Math.Sign(dir.X));
            }
            bool variantB = tracker != 0f;

            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -220f, 8f);
            SmoothHover(npc, spot, 0.06f, timer < 30 ? 14f : 4f);

            if (timer == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = new(nebulashDirX, nebulashDirY);
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.5f }, npc.Center);
                if (!variantB)
                {
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<NebulashLashProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                    {
                        Main.projectile[idx].ai[0] = dir.X; Main.projectile[idx].ai[1] = dir.Y; Main.projectile[idx].ai[2] = 800f;
                    }
                }
                else
                {
                    foreach (float spread in new float[] { -0.45f, 0.45f })
                    {
                        Vector2 d = dir.RotatedBy(spread);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<NebulashLashProj>(), npc.damage / 3, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles)
                        {
                            Main.projectile[idx].ai[0] = d.X; Main.projectile[idx].ai[1] = d.Y; Main.projectile[idx].ai[2] = 700f;
                        }
                    }
                }
            }

            if (timer >= 130)
                RotateAttack(npc, phase, AttackState.Nebulash);
        }

        // AURORA BLAZER — A: alternating blue-slow/pink-fast fan (documented). B: a continuously rotating sweep.
        private void ExecuteAuroraBlazer(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.AuroraBlazer) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<AureusHeldAuroraBlazer>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 spot = DirectedHoverSpot(npc, target, 280f, -220f, 6f);
            SmoothHover(npc, spot, 0.06f, 10f);
            FindHeldWeapon<AureusHeldAuroraBlazer>(npc)?.SetAim((target.Center - npc.Center).ToRotation());

            if (timer >= 40 && timer <= 160 && timer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!variantB)
                {
                    bool blue = (timer / 6) % 2 == 0;
                    Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    if (!blue) dir = dir.RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f));
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * (blue ? 3f : 14f), ModContent.ProjectileType<AuroraBoltProj>(), npc.damage / 3, 0f, Main.myPlayer, blue ? 0f : 1f);
                }
                else
                {
                    float sweepAngle = (target.Center - npc.Center).ToRotation() + MathF.Sin(timer * 0.05f) * 0.9f;
                    Vector2 dir = sweepAngle.ToRotationVector2();
                    bool blue = (timer / 6) % 3 != 0;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * (blue ? 3.5f : 13f), ModContent.ProjectileType<AuroraBoltProj>(), npc.damage / 3, 0f, Main.myPlayer, blue ? 0f : 1f);
                }
                FindHeldWeapon<AureusHeldAuroraBlazer>(npc)?.Pulse(6f);
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.AuroraBlazer);
        }

        // ALULA AUSTRALIS — A: two-row alternating horizontal pierce (documented). B: converging spiral volley.
        private void ExecuteAlulaAustralis(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.AlulaAustralis) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldAlulaAustralis>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            Vector2 spot = DirectedHoverSpot(npc, target, 240f, -260f, 5f);
            SmoothHover(npc, spot, 0.06f, 9f);

            if (!variantB)
            {
                if (timer >= 50 && timer <= 158 && (timer - 50) % 12 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int row = ((int)(timer - 50) / 12) % 2;
                    float y = target.Center.Y + (row == 0 ? -60f : 60f);
                    float side = Math.Sign(npc.Center.X - target.Center.X);
                    Vector2 spawn = new(target.Center.X + side * 500f, y);
                    Vector2 vel = new(-side * 11f, 0f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel, ModContent.ProjectileType<AlulaFeatherProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    FindHeldWeapon<AureusHeldAlulaAustralis>(npc)?.Pulse(8f);
                }
            }
            else
            {
                if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 10f;
                        Vector2 spawn = npc.Center + ang.ToRotationVector2() * 140f;
                        Vector2 dir = SafeNormalize(target.Center - spawn, Vector2.UnitY);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, dir * 12f, ModContent.ProjectileType<AlulaFeatherProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    }
                    FindHeldWeapon<AureusHeldAlulaAustralis>(npc)?.Pulse(10f);
                }
            }

            if (timer >= 200)
                RotateAttack(npc, phase, AttackState.AlulaAustralis);
        }

        // BOREALIS BOMBER — A: single jump, 4-bomb horizontal spread (documented). B: pincer from both sides.
        private void ExecuteBorealisBomber(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                tracker = UseVariantB(AttackState.BorealisBomber) ? 1f : 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldBorealisBomber>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }
            bool variantB = tracker != 0f;

            if (timer < 40)
            {
                npc.velocity.Y = -22f;
            }
            else if (timer == 50 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float floorY = target.Center.Y;
                if (!variantB)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 spawn = target.Center + new Vector2(i * 220f - 330f, -600f);
                        int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, 11f), ModContent.ProjectileType<BorealisBombProj>(), npc.damage / 3, 0f, Main.myPlayer);
                        if (idx >= 0 && idx < Main.maxProjectiles)
                            Main.projectile[idx].ai[0] = floorY;
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawnL = target.Center + new Vector2(-500f + i * 60f, -600f - i * 60f);
                        Vector2 spawnR = target.Center + new Vector2(500f - i * 60f, -600f - i * 60f);
                        int idxL = Projectile.NewProjectile(npc.GetSource_FromAI(), spawnL, new Vector2(2f, 11f), ModContent.ProjectileType<BorealisBombProj>(), npc.damage / 3, 0f, Main.myPlayer);
                        int idxR = Projectile.NewProjectile(npc.GetSource_FromAI(), spawnR, new Vector2(-2f, 11f), ModContent.ProjectileType<BorealisBombProj>(), npc.damage / 3, 0f, Main.myPlayer);
                        if (idxL >= 0 && idxL < Main.maxProjectiles) Main.projectile[idxL].ai[0] = floorY;
                        if (idxR >= 0 && idxR < Main.maxProjectiles) Main.projectile[idxR].ai[0] = floorY;
                    }
                }
                FindHeldWeapon<AureusHeldBorealisBomber>(npc)?.Pulse(14f);
            }
            else if (timer >= 120)
            {
                Vector2 spot = DirectedHoverSpot(npc, target, 0f, -240f, 0f);
                SmoothHover(npc, spot, 0.05f, 12f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.BorealisBomber);
        }

        // AURORADICAL THROW — A: single boomerang, weak-out/strong-back (documented). B: twin crossing boomerangs.
        private void ExecuteAuroradicalThrow(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
                tracker = UseVariantB(AttackState.AuroradicalThrow) ? 1f : 0f;
            bool variantB = tracker != 0f;

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -200f, 8f);
            SmoothHover(npc, spot, 0.06f, 10f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<AureusHeldAuroradical>(), 0, 0f, Main.myPlayer, npc.whoAmI);

                if (!variantB)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 18f, ModContent.ProjectileType<AuroradicalBoomerangProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                else
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(0.5f) * 18f, ModContent.ProjectileType<AuroradicalBoomerangProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(-0.5f) * 18f, ModContent.ProjectileType<AuroradicalBoomerangProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<AureusHeldAuroradical>(npc)?.Pulse(16f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.AuroradicalThrow);
        }
        #endregion

        #region P2 State Machine — Star-Waste Arsenal
        private void ExecuteAstralScythe(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldAstralScythe>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 0f, -280f, 4f);
            SmoothHover(npc, spot, 0.06f, 12f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 mid = target.Center;
                Vector2[] spawns = { mid + new Vector2(-450f, -450f), mid + new Vector2(450f, -450f) };
                foreach (Vector2 spawn in spawns)
                {
                    Vector2 dir = (mid - spawn).SafeNormalize(Vector2.UnitY) * 9f;
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, dir, ModContent.ProjectileType<AstralScytheProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    if (idx >= 0 && idx < Main.maxProjectiles)
                    {
                        Main.projectile[idx].ai[0] = mid.X; Main.projectile[idx].ai[1] = mid.Y;
                    }
                }
                FindHeldWeapon<AureusHeldAstralScythe>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.AstralScythe);
        }

        private void ExecuteTitanArm(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldTitanArm>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 250f, -220f, 6f);
            SmoothHover(npc, spot, 0.06f, 11f);

            if ((timer == 30 || timer == 90) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawn = new(target.Center.X, target.Center.Y + 400f);
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<TitanFistProj>(), npc.damage / 3 + 5, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles)
                {
                    Main.projectile[idx].ai[0] = target.Center.X;
                    Main.projectile[idx].ai[1] = target.Center.Y + 400f;
                }
                FindHeldWeapon<AureusHeldTitanArm>(npc)?.Pulse(-10f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.TitanArm);
        }

        private void ExecuteStellarCannon(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<AureusHeldStellarCannon>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            SmoothHover(npc, spot, 0.05f, 8f);
            FindHeldWeapon<AureusHeldStellarCannon>(npc)?.SetAim((target.Center - npc.Center).ToRotation());

            if (timer == 10 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<StellarBeamProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<AureusHeldStellarCannon>(npc)?.Pulse(20f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.StellarCannon);
        }

        private void ExecuteStellarKnife(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldStellarKnife>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -240f, 7f);
            SmoothHover(npc, spot, 0.06f, 10f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawn = target.Center + new Vector2(i * 130f - 325f, -420f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, new Vector2(0f, -4f), ModContent.ProjectileType<StellarKnifeHoverProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                FindHeldWeapon<AureusHeldStellarKnife>(npc)?.Pulse(10f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.StellarKnife);
        }

        private void ExecuteAstralachneaStaff(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldAstralachnea>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 240f, -220f, 5f);
            SmoothHover(npc, spot, 0.06f, 9f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(-420f, Main.rand.NextFloat(-80f, 80f)), Vector2.Zero, ModContent.ProjectileType<AstralWebProj>(), npc.damage / 4, 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center + new Vector2(420f, Main.rand.NextFloat(-80f, 80f)), Vector2.Zero, ModContent.ProjectileType<AstralWebProj>(), npc.damage / 4, 0f, Main.myPlayer);
                FindHeldWeapon<AureusHeldAstralachnea>(npc)?.Pulse(8f);
            }

            if (timer >= 150)
                RotateAttack(npc, phase, AttackState.AstralachneaStaff);
        }

        private void ExecuteAbandonedSlime(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldAbandonedSlime>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -260f, 6f);
            SmoothHover(npc, spot, 0.06f, 10f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(Math.Sign(target.Center.X - npc.Center.X) * 3f, -6f), ModContent.ProjectileType<AbandonedSlimeCoreProj>(), npc.damage / 3, 0f, Main.myPlayer);
                if (idx >= 0 && idx < Main.maxProjectiles)
                    Main.projectile[idx].ai[1] = target.Center.Y;
                FindHeldWeapon<AureusHeldAbandonedSlime>(npc)?.Pulse(8f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.AbandonedSlime);
        }

        private void ExecuteHivePod(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<AureusHeldHivePod>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 240f, -240f, 6f);
            SmoothHover(npc, spot, 0.06f, 10f);

            if ((timer == 40 || timer == 100) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 9f, ModContent.ProjectileType<HivePodProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<AureusHeldHivePod>(npc)?.Pulse(12f);
            }

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.HivePod);
        }
        #endregion

        #region Transition
        private void ExecuteTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true;

            const int shellStrip = 45, coreReveal = 90;
            if (timer < shellStrip)
                transitionFlashAlpha = MathHelper.Clamp(timer / (float)shellStrip, 0f, 1f);
            else
                transitionFlashAlpha = MathHelper.Clamp(1f - (timer - shellStrip) / (float)(coreReveal - shellStrip), 0f, 1f);

            if (timer == 1)
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f }, npc.Center);

            if (timer == shellStrip)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath10, npc.Center);
                target.Calamity().GeneralScreenShakePower = 8f;
                AureusFx.Burst(npc.Center, 6f, 30);

                int emitterType = ModContent.Find<ModNPC>("CalamityMod/AureusSpawn").Type;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == emitterType && Main.npc[i].ai[0] == npc.whoAmI)
                        Main.npc[i].active = false;
                }
            }

            if (timer == coreReveal)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 12f;
                AureusFx.Burst(npc.Center, 8f, 40);
            }

            if (timer >= coreReveal + 10)
            {
                transitionFlashAlpha = 0f;
                npc.dontTakeDamage = false;

                attackCycleIndex = 0;
                npc.ai[1] = (float)P2Cycle[0];
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Death Animation
        // 反应核心崩溃 — 五段演出, 让护盾/重力异常/星核身份成为演出主角, 而不是通用爆炸:
        // 护盾余烬崩解 -> 重力异常紊乱(呼应引力阱) -> 星辉过载迸发 -> 核心过载上腾 -> 终末核爆.
        private void BeginDeathAnimation(NPC npc, Player target)
        {
            npc.ai[1] = (float)AttackState.DeathAnimation;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;
            shieldActive = false;
            shieldStunTimer = 0;
            npc.netUpdate = true;

            TriggerDeathCinematic(npc, target, focusStrength: 0.55f, holdFrames: 55, shakePower: 10f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1f, Pitch = -0.4f }, npc.Center);
        }

        private void ExecuteDeathAnimation(NPC npc, Player target, ref float timer)
        {
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (timer < 25f)
            {
                // 护盾余烬崩解 — the same shell-strip visual as the phase-transition reveal, this time for good
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 1.2f) * 0.1f;
                if ((int)timer % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60f, 60f), DustID.GoldFlame, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 70f)
            {
                // 重力异常紊乱 — its own gravity-well identity turns erratic on itself: alternating pull/push jitter
                float t = timer - 25f;
                bool pulling = (int)(t / 10f) % 2 == 0;
                npc.velocity += (pulling ? Vector2.UnitY : -Vector2.UnitY) * 0.3f;
                npc.velocity *= 0.94f;
                if ((int)t % 2 == 0)
                {
                    Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(90f, 90f);
                    Vector2 vel = pulling ? (npc.Center - around) * 0.06f : (around - npc.Center) * 0.06f;
                    Dust d = Dust.NewDustPerfect(around, DustID.PurpleTorch, vel, 100, default, 1.2f);
                    d.noGravity = true;
                }
            }
            else if (timer < 105f)
            {
                // 星辉过载迸发 — sparks jitter erratically off the reactor core
                float t = timer - 70f;
                npc.velocity += Main.rand.NextVector2Circular(0.6f, 0.6f);
                npc.velocity *= 0.9f;
                if ((int)t % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(5f, 5f), 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else if (timer < 140f)
            {
                // 核心过载上腾 — rises while the final overload builds, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -6f), 0.06f);
                float t = timer - 105f;
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.GoldFlame, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末核爆 — the actual kill fires once, everything after is the lingering burst
                if (timer == 140f)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = -0.2f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    AureusFx.Burst(npc.Center, 8f, 40);
                    AureusFx.Burst(npc.Center, 5f, 24, DustID.PurpleTorch);
                }

                if (timer >= 162f)
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
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            for (int i = 0; i < oldPositions.Length; i++)
            {
                int idx = (oldPositionsIndex - i - 1 + oldPositions.Length) % oldPositions.Length;
                if (oldPositions[idx] == Vector2.Zero) continue;
                float alpha = (1f - i / (float)oldPositions.Length) * 0.55f;
                Color trailColor = new Color(230, 200, 60, 0) * alpha;
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, trailColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * transitionFlashAlpha);

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            Color overlayColor = superGravity ? (Color.Gold * 0.08f) : (Color.Cyan * 0.08f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), overlayColor);

            int currentPhase = (int)npc.ai[0];
            float borderSize = currentPhase <= 1 ? 1200f : 900f;
            Vector2 tl = npc.Center + new Vector2(-borderSize / 2f, -borderSize / 2f);
            Vector2 tr = npc.Center + new Vector2(borderSize / 2f, -borderSize / 2f);
            Vector2 bl = npc.Center + new Vector2(-borderSize / 2f, borderSize / 2f);
            Vector2 br = npc.Center + new Vector2(borderSize / 2f, borderSize / 2f);

            LegendsWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.Orange * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.Orange * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.Orange * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.Orange * 0.7f, 4f);

            if (shieldActive && currentPhase == 1)
            {
                Vector2[] legOffsets = { new(-60f, 40f), new(60f, 40f), new(-100f, 20f), new(100f, 20f) };
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = npc.Center + legOffsets[i].RotatedBy(npc.rotation);
                    spriteBatch.Draw(TextureAssets.Dust.Value, pos - screenPos, new Rectangle(0, 0, 8, 8), Color.DeepSkyBlue * 0.9f, ticksRunning * 0.05f, new Vector2(4f, 4f), 5f, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Color glowColor = new Color(230, 200, 60, 0) * 0.35f;
            spriteBatch.Draw(tex, npc.Center - screenPos, frame, glowColor, npc.rotation, origin, npc.scale * 1.08f, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
        #endregion

        // Finds this boss's live held weapon of the given style, for driving Pulse/SetAim from the attack code.
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
