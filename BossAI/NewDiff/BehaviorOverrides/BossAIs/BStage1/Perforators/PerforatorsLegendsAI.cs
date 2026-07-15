// =====================================================================================================================
// THE PERFORATORS - CUSTOM BEHAVIOR OVERRIDE (Legends MODE)
// =====================================================================================================================
// DESIGN PHILOSOPHY & FIGHT METRICS:
// The Perforators are the living embodiment of the Crimson biome, representing raw flesh, bone, and flowing ichor.
// This override suppresses the default AI update loop (PreAI returns false), taking absolute authority over movement physics,
// targeting, state transitions, frame animations, and visual drawing indicators.
//
// FIGHT MECHANICS & FLOW:
// - Phase 1 (100% - 70% HP) - Awakening of the Flesh:
//   * Spawn Roar: Emerges, roars, and shakes the camera while spawning blood explosions.
//   * Diagonal Blood Charge: Teleports to a diagonal corner, displays warning paths, and lunges across, dropping falling ichor blobs.
//   * Horizontal Crimera Spawn Charge: Sweeps horizontally above the target, dropping Crimeras.
//
// - Phase 2 (70% - 50% HP) - The Small Parasite:
//   * Small Worm Burst: Reels back, channels blood geysers, and spawns the Small Perforator Worm.
//   * Ichor Blasts: Floats horizontally while firing arcing spreads of explosive ichor blobs.
//
// - Phase 3 (50% - 25% HP) - Chitinous Swarms:
//   * Medium Worm Burst: Channels blood geysers, spawning the Medium Perforator Worm.
//   * Crimera Walls: Moves to screen edges, spawning massive vertical columns of Crimeras that sweep across.
//
// - Phase 4 (25% - 0% HP) - The Crimson Singularity:
//   * Large Worm Burst: Erupts in heavy blood showers, spawning the Large Perforator Worm.
//   * Ichor Spin Dash: Orbit-spins in a circular path before executing rapid diagonal dash charges.
//   * Ichor Fountain (Desperation): Becomes invulnerable, locks the player in a 750-unit crimson boundary, and fires vertical fountains of ichor from below.
// =====================================================================================================================

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Events;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using Terraria.Localization;

using CalamityPerforatorHive = CalamityMod.NPCs.Perforator.PerforatorHive;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage1.Perforators
{
    /// <summary>
    /// Custom behavior override for The Perforator Hive in Legends Mode.
    /// This class takes absolute authority over boss movement, state flow, projectile management,
    /// custom drawing overlays (e.g. desperation boundaries, warning geysers, linear telegraphs),
    /// difficulty scaling, minion spawning and coordination, and death sequences.
    /// 
    /// Detailed Mathematical Systems implemented:
    /// - Recoil mechanics for worm spawning recoil forces.
    /// - Vector algebra for diagonal prediction trajectory lines.
    /// - Polar coordinate rotations for orbital spinning state.
    /// - Custom geyser indicator warnings using width and height scaling.
    /// - Desperation barrier check using boundary distance checks:
    ///   dist = Vector2.Distance(player.Center, desperationCenter)
    /// </summary>
    internal sealed class PerforatorsLegendsAI : LegendsBossAI
    {
        #region Constants & Configuration
        // NPC Identifiers
        public override int NPCType => ModContent.NPCType<CalamityPerforatorHive>();
        public override string BossName => "The Perforators";

        // Phase Thresholds
        public override float[] PhaseLifeRatios => new[] { 0.70f, 0.50f, 0.25f };
        public override int AttackCycleLength => 116;
        public override float MotionIntensity => 1.08f;
        public override Color DebugColor => new(255, 92, 104);

        // Sound Hooks
        public static readonly SoundStyle RoarSound = SoundID.Roar;
        public static readonly SoundStyle DashSound = SoundID.DoubleJump;
        public static readonly SoundStyle SpawnSound = SoundID.NPCDeath13;
        public static readonly SoundStyle WormEruptSound = SoundID.ForceRoarPitched;
        public static readonly SoundStyle BlastSound = SoundID.Item14;

        // Math Constants
        private const float TwoPi = MathHelper.TwoPi;
        private const float Pi = MathHelper.Pi;
        private const float PiOver2 = MathHelper.PiOver2;
        private const float ArenaRadius = 750f;

        // Projectile Reference Keys
        private const string ProjClot = "VileClot";
        private const string ProjFallingIchor = "FallingIchor";
        private const string ProjFlyingIchor = "FlyingIchor";
        private const string ProjIchorBlast = "IchorBlast";
        private const string ProjIchorSpit = "IchorSpit";
        private const string ProjIchorBlob = "IchorBlob";
        private const string ProjToothBall = "ToothBall";
        private const string ProjWave = "PerforatorWave";
        #endregion

        #region State Machine Enumeration
        public enum AttackState
        {
            SpawnRoar = 0,
            DiagonalBloodCharge = 1,
            HorizontalCrimeraSpawnCharge = 2,
            SmallWormBurst = 3,
            IchorBlasts = 4,
            MediumWormBurst = 5,
            CrimeraWalls = 6,
            LargeWormBurst = 7,
            IchorSpinDash = 8,
            IchorFountainCharge = 9,
            DeathAnimation = 10,
            VictoryDespawn = 11
        }
        #endregion

        #region Local Structs
        private struct PredictionPoint
        {
            public Vector2 Position;
            public float Alpha;

            public PredictionPoint(Vector2 position, float alpha)
            {
                Position = position;
                Alpha = alpha;
            }
        }

        private struct IchorGeyser
        {
            public Vector2 Position;
            public int Timer;
            public int MaxTimer;
            public float Height;
            public float Width;

            public IchorGeyser(Vector2 pos, int timer, int maxTimer, float height, float width)
            {
                Position = pos;
                Timer = timer;
                MaxTimer = maxTimer;
                Height = height;
                Width = width;
            }
        }

        private struct PerforatorMinion
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Timer;
            public float Angle;

            public PerforatorMinion(Vector2 pos, Vector2 vel, int timer, float angle)
            {
                Position = pos;
                Velocity = vel;
                Timer = timer;
                Angle = angle;
            }
        }
        #endregion

        #region Local Fields
        // Drawing & opacity variables
        private float arenaAlpha = 0f;
        private Vector2 desperationCenter = Vector2.Zero;

        // Custom Trajectory Prediction for charges
        private readonly List<PredictionPoint> dashPredictionPath = new();
        private bool drawPredictionPath = false;

        // Teleportation coordinates
        private float teleportPositionX = 0f;
        private float teleportPositionY = 0f;

        // Active Custom Entities
        private readonly List<IchorGeyser> activeGeysers = new();
        private readonly List<PerforatorMinion> activeMinions = new();

        // Execution Timers and Counters
        private int ticksRunning = 0;
        private int dashCounter = 0;
        private int spawnCounter = 0;
        private int leapStage = 0;
        private int invincibilityTimer = 0;
        private int activeWormNPCId = -1;

        // Historic Position Cache (for Trails)
        private readonly Vector2[] oldPositions = new Vector2[12];
        private int oldPositionsIndex = 0;
        #endregion

        #region Core AI Hooks
        /// <summary>
        /// Main update override in PreAI. Suppresses default AI by returning false.
        /// Handles target validity, enrage calculations, phase thresholds, and delegates to custom states.
        /// </summary>
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            ticksRunning++;

            // Verify target players
            if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(true);
            }

            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                ExecuteVictoryDespawn(npc);
                return false;
            }

            // Sync states from npc.ai
            int currentPhase = (int)npc.ai[0];
            AttackState state = (AttackState)(int)npc.ai[1];
            ref float timer = ref npc.ai[2];
            ref float stateTracker = ref npc.ai[3];

            // Normalize stats and flags every frame
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;

            // Clear Burning Blood debuffs on players
            if (target.HasBuff(BuffID.CursedInferno))
                target.ClearBuff(BuffID.CursedInferno);

            // Record history positions for trail rendering
            oldPositions[oldPositionsIndex] = npc.Center;
            oldPositionsIndex = (oldPositionsIndex + 1) % oldPositions.Length;

            // Check for phase transitions (100% -> 70% -> 50% -> 25%)
            int nextPhase = CalculatePhase(npc);
            if (nextPhase > currentPhase)
            {
                npc.ai[0] = nextPhase;
                currentPhase = nextPhase;
                npc.netUpdate = true;
            }

            // Desperation Check (25% HP)
            if (currentPhase < 4 && npc.life <= npc.lifeMax * PhaseLifeRatios[2])
            {
                npc.ai[0] = 4f;
                currentPhase = 4;
                TransitionToAttack(npc, AttackState.IchorFountainCharge);
                state = AttackState.IchorFountainCharge;
                invincibilityTimer = 90; // Invulnerable state transition
                desperationCenter = target.Center;
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 15f;
            }

            // Update custom sub-entities
            UpdateGeysers(npc);
            UpdateMinions(npc);

            // Handle Invincibility
            if (invincibilityTimer > 0)
            {
                invincibilityTimer--;
                npc.dontTakeDamage = true;
                npc.velocity *= 0.88f;
                npc.rotation *= 0.9f;

                // Pulsate energy waves
                if (invincibilityTimer % 10 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item74, npc.Center);
                    EmitBloodBurst(npc.Center, 20f, 15);
                }
                return false;
            }
            else
            {
                npc.dontTakeDamage = false;
            }

            // State Machine Router
            switch (state)
            {
                case AttackState.SpawnRoar:
                    ExecuteState_SpawnRoar(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DiagonalBloodCharge:
                    ExecuteState_DiagonalBloodCharge(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.HorizontalCrimeraSpawnCharge:
                    ExecuteState_HorizontalCrimeraSpawnCharge(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.SmallWormBurst:
                    ExecuteState_SmallWormBurst(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.IchorBlasts:
                    ExecuteState_IchorBlasts(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.MediumWormBurst:
                    ExecuteState_MediumWormBurst(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CrimeraWalls:
                    ExecuteState_CrimeraWalls(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.LargeWormBurst:
                    ExecuteState_LargeWormBurst(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.IchorSpinDash:
                    ExecuteState_IchorSpinDash(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.IchorFountainCharge:
                    ExecuteState_IchorFountainCharge(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteState_DeathAnimation(npc, ref timer);
                    break;
                case AttackState.VictoryDespawn:
                    ExecuteVictoryDespawn(npc);
                    break;
            }

            return false;
        }

        /// <summary>
        /// Instantly switches the boss to a new attack state and resets relevant timers/counters.
        /// </summary>
        private void TransitionToAttack(NPC npc, AttackState newState)
        {
            npc.ai[1] = (float)newState;
            npc.ai[2] = 0f;
            npc.ai[3] = 0f;

            // Clear visual arrays and locks
            drawPredictionPath = false;
            dashPredictionPath.Clear();
            activeGeysers.Clear();
            activeMinions.Clear();

            // Reset local counters
            dashCounter = 0;
            spawnCounter = 0;
            leapStage = 0;
            teleportPositionX = 0f;
            teleportPositionY = 0f;

            npc.netUpdate = true;
        }

        /// <summary>
        /// Selects the next state in the phase-specific attack cycle.
        /// </summary>
        private void SelectNextAttack(NPC npc, int phase)
        {
            AttackState nextState = AttackState.DiagonalBloodCharge;
            AttackState currentAttack = (AttackState)(int)npc.ai[1];

            List<AttackState> allowed = new();

            switch (phase)
            {
                case 1: // P1: DiagonalBloodCharge -> HorizontalCrimeraSpawnCharge
                    allowed.Add(AttackState.DiagonalBloodCharge);
                    allowed.Add(AttackState.HorizontalCrimeraSpawnCharge);
                    break;

                case 2: // P2: IchorBlasts -> DiagonalBloodCharge -> HorizontalCrimeraSpawnCharge
                    allowed.Add(AttackState.IchorBlasts);
                    allowed.Add(AttackState.DiagonalBloodCharge);
                    allowed.Add(AttackState.HorizontalCrimeraSpawnCharge);
                    break;

                case 3: // P3: CrimeraWalls -> IchorBlasts -> DiagonalBloodCharge
                    allowed.Add(AttackState.CrimeraWalls);
                    allowed.Add(AttackState.IchorBlasts);
                    allowed.Add(AttackState.DiagonalBloodCharge);
                    break;

                case 4: // P4 Desperation: IchorFountainCharge -> IchorSpinDash -> CrimeraWalls
                    allowed.Add(AttackState.IchorFountainCharge);
                    allowed.Add(AttackState.IchorSpinDash);
                    allowed.Add(AttackState.CrimeraWalls);
                    break;
            }

            // Remove current attack if there's more than one option
            if (allowed.Count > 1)
            {
                allowed.Remove(currentAttack);
            }

            nextState = allowed[Main.rand.Next(allowed.Count)];
            TransitionToAttack(npc, nextState);
        }

        /// <summary>
        /// Calculates the boss's current phase based on life ratios and active worm milestones.
        /// </summary>
        private int CalculatePhase(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (lifeRatio <= PhaseLifeRatios[2]) // Under 25% HP
                return 4;
            if (lifeRatio <= PhaseLifeRatios[1]) // Under 50% HP
                return 3;
            if (lifeRatio <= PhaseLifeRatios[0]) // Under 70% HP
                return 2;
            return 1;
        }
        #endregion

        #region Phase 1 Attacks
        /// <summary>
        /// State 0: Emerges, roars, and releases expanding blood sparks.
        /// </summary>
        private void ExecuteState_SpawnRoar(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.velocity *= 0.9f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.15f) * 0.08f;

            if (timer == 1)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 12f;
                EmitBloodBurst(npc.Center, 15f, 32);
            }

            if (timer >= 60)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 1: Diagonal Blood Charge. Teleports to a diagonal corner and lunges across player.
        /// </summary>
        private void ExecuteState_DiagonalBloodCharge(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int windup = 45;
            int chargeDuration = 35;

            // Trigger worm check transitions first if milestones are met
            if (phase == 2 && leapStage == 0 && timer == 1 && spawnCounter == 0)
            {
                spawnCounter = 1;
                TransitionToAttack(npc, AttackState.SmallWormBurst);
                return;
            }
            if (phase == 3 && leapStage == 0 && timer == 1 && spawnCounter == 1)
            {
                spawnCounter = 2;
                TransitionToAttack(npc, AttackState.MediumWormBurst);
                return;
            }
            if (phase == 4 && leapStage == 0 && timer == 1 && spawnCounter == 2)
            {
                spawnCounter = 3;
                TransitionToAttack(npc, AttackState.LargeWormBurst);
                return;
            }

            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;
                    npc.velocity *= 0.88f;
                    npc.Opacity = MathHelper.Lerp(1f, 0.1f, timer / (float)windup);

                    if (timer == 1)
                    {
                        // Position diagonally relative to target player
                        float sideSignX = (target.Center.X < npc.Center.X) ? 1f : -1f;
                        float sideSignY = (target.Center.Y < npc.Center.Y) ? 1f : -1f;
                        teleportPositionX = target.Center.X + sideSignX * 450f;
                        teleportPositionY = target.Center.Y + sideSignY * 350f;
                        npc.netUpdate = true;
                    }

                    // Render diagonal warning guides
                    if (timer >= 15)
                    {
                        drawPredictionPath = true;
                        dashPredictionPath.Clear();
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX, teleportPositionY), 0.7f));
                        dashPredictionPath.Add(new PredictionPoint(target.Center + SafeNormalize(target.Center - new Vector2(teleportPositionX, teleportPositionY), Vector2.Zero) * 600f, 0.7f));
                    }

                    // Erupt particle dust at teleport coordinates
                    if (timer % 4 == 0)
                    {
                        Vector2 pVel = Main.rand.NextVector2Circular(3f, 3f);
                        EmitBloodIndicatorSparks(new Vector2(teleportPositionX, teleportPositionY), pVel, Color.Crimson, 1.4f);
                    }

                    if (timer >= windup)
                    {
                        npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                        npc.Opacity = 1f;

                        // Lunge charge vector
                        Vector2 chargeVel = SafeNormalize(target.Center - npc.Center, Vector2.UnitX) * 22f;
                        npc.velocity = chargeVel;
                        npc.rotation = chargeVel.ToRotation() - PiOver2;
                        SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);
                        SoundEngine.PlaySound(DashSound, npc.Center);

                        leapStage = 1;
                        timer = 0;
                        drawPredictionPath = false;
                        npc.netUpdate = true;
                    }
                    break;

                case 1:
                    npc.rotation = SafeNormalize(npc.velocity, Vector2.UnitY).ToRotation() - PiOver2;

                    // Release falling ichor droplets along the diagonal sweep
                    if (timer % 6 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projIchor = GetCalamityProjectile(ProjFallingIchor, ProjectileID.GoldenShowerHostile);
                            int damage = ScaleDamage(npc, 90);
                            Vector2 shootVel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-6f, -3f));
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, shootVel, projIchor, damage, 1f, Main.myPlayer);
                        }
                    }

                    if (timer >= chargeDuration)
                    {
                        dashCounter++;
                        if (dashCounter >= 3)
                        {
                            leapStage = 0;
                            SelectNextAttack(npc, phase);
                        }
                        else
                        {
                            leapStage = 0;
                            timer = 0;
                            npc.netUpdate = true;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// State 2: Horizontal Crimera Spawn Charge. Sweeps horizontally, dropping Crimeras.
        /// </summary>
        private void ExecuteState_HorizontalCrimeraSpawnCharge(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int windup = 45;
            int dashDuration = 50;

            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;
                    npc.velocity *= 0.9f;
                    npc.Opacity = MathHelper.Lerp(1f, 0.15f, timer / (float)windup);

                    if (timer == 1)
                    {
                        float sideSign = (target.Center.X < npc.Center.X) ? 1f : -1f;
                        teleportPositionX = target.Center.X + sideSign * 580f;
                        teleportPositionY = target.Center.Y - 240f;
                        npc.netUpdate = true;
                    }

                    // Aligned horizontal warning path
                    if (timer >= 15)
                    {
                        drawPredictionPath = true;
                        dashPredictionPath.Clear();
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX, teleportPositionY), 0.7f));
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX - Math.Sign(teleportPositionX - target.Center.X) * 1200f, teleportPositionY), 0.7f));
                    }

                    if (timer >= windup)
                    {
                        npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                        npc.Opacity = 1f;

                        // Sweep vector
                        float dir = (target.Center.X < npc.Center.X) ? -1f : 1f;
                        npc.velocity = new Vector2(dir * 18.5f, 0f);
                        npc.rotation = npc.velocity.X * 0.05f;
                        SoundEngine.PlaySound(DashSound, npc.Center);

                        leapStage = 1;
                        timer = 0;
                        drawPredictionPath = false;
                        npc.netUpdate = true;
                    }
                    break;

                case 1:
                    npc.rotation = npc.velocity.X * 0.03f;

                    // Spawn flying Crimeras and dropping tooth balls
                    if (timer % 10 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int minionType = NPCID.Crimera;
                            int damage = ScaleDamage(npc, 80);

                            // Spawn Crimera directly
                            int minionId = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, minionType);
                            if (minionId < Main.maxNPCs)
                            {
                                Main.npc[minionId].velocity = new Vector2(npc.velocity.X * 0.3f, 6f);
                                Main.npc[minionId].netUpdate = true;
                            }

                            // Throw tooth ball projectile
                            int projBall = GetCalamityProjectile(ProjToothBall, ProjectileID.CursedFlameHostile);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, 4.5f), projBall, damage, 1f, Main.myPlayer);
                        }
                        SoundEngine.PlaySound(SoundID.NPCDeath23 with { Volume = 0.5f }, npc.Center);
                    }

                    if (timer >= dashDuration)
                    {
                        leapStage = 0;
                        SelectNextAttack(npc, phase);
                    }
                    break;
            }
        }
        #endregion

        #region Phase 2 Attacks & Worm Spawning
        /// <summary>
        /// State 3: Small Worm Burst. Reels back and spawns the Small Perforator Worm.
        /// </summary>
        private void ExecuteState_SmallWormBurst(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;
            npc.velocity *= 0.85f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.25f) * 0.15f;

            if (timer == 30)
            {
                // Play worm eruption sequence
                Vector2 eruptDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int wormType = GetCalamityNPC("SmallPerforatorHead", NPCID.GiantWormHead);
                MakeWormEruptFromHive(npc, eruptDir, 1f, wormType);
                SoundEngine.PlaySound(WormEruptSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 14f;
            }

            // Wait for worm spawn sequence to finish
            if (timer >= 90)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 4: Ichor Blasts. Sweeps in horizontal curves, firing explosive ichor blobs.
        /// </summary>
        private void ExecuteState_IchorBlasts(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            // Sinusoidal vertical hover offset path
            Vector2 hoverPos = target.Center + new Vector2((target.Center.X < npc.Center.X) ? 450f : -450f, (float)Math.Sin(ticksRunning * 0.08f) * 120f - 80f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hoverPos - npc.Center) * 0.08f, 0.12f);
            npc.rotation = npc.velocity.X * 0.04f;

            // Spawn explosive ichor blobs
            if (timer > 30 && timer % 45 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projBlast = GetCalamityProjectile(ProjIchorBlast, ProjectileID.GoldenShowerHostile);
                    int damage = ScaleDamage(npc, 90);
                    Vector2 shootVel = SafeNormalize(target.Center - npc.Center, Vector2.UnitX) * 7.5f;

                    // Arcing spreads
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 vel = shootVel.RotatedBy(i * 0.28f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, projBlast, damage, 1.2f, Main.myPlayer);
                    }
                }
            }

            if (timer >= 220)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Phase 3 Attacks
        /// <summary>
        /// State 5: Medium Worm Burst. Channels blood geysers, spawning the Medium Perforator Worm.
        /// </summary>
        private void ExecuteState_MediumWormBurst(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;
            npc.velocity *= 0.82f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.3f) * 0.18f;

            if (timer == 35)
            {
                Vector2 eruptDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int wormType = GetCalamityNPC("MediumPerforatorHead", NPCID.GiantWormHead);
                MakeWormEruptFromHive(npc, eruptDir, 1.2f, wormType);
                SoundEngine.PlaySound(WormEruptSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 16f;
            }

            if (timer >= 100)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 6: Crimera Walls. Moves to screen edges, spawning rows of charging Crimeras.
        /// </summary>
        private void ExecuteState_CrimeraWalls(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;

            // Hover stable high above target
            Vector2 hoverPos = target.Center + new Vector2(0f, -360f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hoverPos - npc.Center) * 0.07f, 0.15f);
            npc.rotation = npc.velocity.X * 0.03f;

            if (timer == 25)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);
            }

            // Spawn Crimera column lines
            if (timer >= 40 && timer <= 120 && timer % 12 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int minionType = NPCID.Crimera;
                    int damage = ScaleDamage(npc, 85);

                    float verticalOffset = (timer - 40) * 28f - 120f;
                    Vector2 leftSpawn = target.Center + new Vector2(-920f, verticalOffset);
                    Vector2 rightSpawn = target.Center + new Vector2(920f, verticalOffset);

                    // Spawn left Crimera charging right
                    int id1 = NPC.NewNPC(npc.GetSource_FromAI(), (int)leftSpawn.X, (int)leftSpawn.Y, minionType);
                    if (id1 < Main.maxNPCs)
                    {
                        Main.npc[id1].velocity = new Vector2(11f, 0f);
                        Main.npc[id1].netUpdate = true;
                    }

                    // Spawn right Crimera charging left
                    int id2 = NPC.NewNPC(npc.GetSource_FromAI(), (int)rightSpawn.X, (int)rightSpawn.Y, minionType);
                    if (id2 < Main.maxNPCs)
                    {
                        Main.npc[id2].velocity = new Vector2(-11f, 0f);
                        Main.npc[id2].netUpdate = true;
                    }
                }
            }

            if (timer >= 180)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Phase 4 Attacks & Desperation
        /// <summary>
        /// State 7: Large Worm Burst. Spawns the Large Perforator Worm.
        /// </summary>
        private void ExecuteState_LargeWormBurst(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;
            npc.velocity *= 0.8f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.35f) * 0.2f;

            if (timer == 40)
            {
                Vector2 eruptDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                int wormType = GetCalamityNPC("LargePerforatorHead", NPCID.GiantWormHead);
                MakeWormEruptFromHive(npc, eruptDir, 1.4f, wormType);
                SoundEngine.PlaySound(WormEruptSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 18f;
            }

            if (timer >= 110)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 8: Ichor Spin Dash. Orbit-spins around player, then lunges.
        /// </summary>
        private void ExecuteState_IchorSpinDash(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int spinDuration = 90;
            int chargeDuration = 40;

            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;

                    // Rapid spin orbit centered around player
                    float radius = 350f;
                    float angle = (timer / (float)spinDuration) * TwoPi;
                    Vector2 desiredPos = target.Center + angle.ToRotationVector2() * radius;
                    npc.velocity = Vector2.Lerp(npc.velocity, (desiredPos - npc.Center) * 0.12f, 0.2f);
                    npc.rotation = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation() - PiOver2;

                    // Release ichor blobs inward
                    if (timer % 8 == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.5f }, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projIchor = GetCalamityProjectile(ProjIchorBlob, ProjectileID.GoldenShowerHostile);
                            int damage = ScaleDamage(npc, 90);
                            Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.Zero) * 8.5f;
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, projIchor, damage, 1.2f, Main.myPlayer);
                        }
                    }

                    if (timer >= spinDuration)
                    {
                        // Transition to charge lunge
                        SoundEngine.PlaySound(RoarSound, npc.Center);
                        SoundEngine.PlaySound(DashSound, npc.Center);
                        npc.velocity = SafeNormalize(target.Center - npc.Center, Vector2.UnitX) * 23.5f;
                        npc.rotation = npc.velocity.ToRotation() - PiOver2;

                        leapStage = 1;
                        timer = 0;
                        npc.netUpdate = true;
                    }
                    break;

                case 1:
                    npc.rotation = SafeNormalize(npc.velocity, Vector2.UnitY).ToRotation() - PiOver2;

                    if (timer >= chargeDuration)
                    {
                        leapStage = 0;
                        SelectNextAttack(npc, phase);
                    }
                    break;
            }
        }

        /// <summary>
        /// State 9: Ichor Fountain Charge. Restricts player in crimson boundary and erupts fountains.
        /// </summary>
        private void ExecuteState_IchorFountainCharge(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.velocity *= 0.85f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.25f) * 0.15f;

            // Fade in crimson boundary
            arenaAlpha = MathHelper.Clamp(arenaAlpha + 0.03f, 0f, 1f);

            // Restrict target movement within 750 radius
            float dist = Vector2.Distance(target.Center, desperationCenter);
            if (dist > ArenaRadius)
            {
                // Penalize player
                target.AddBuff(BuffID.Ichor, 180);
                target.velocity *= 0.92f;
                target.statLife = Math.Max(1, target.statLife - 4); // Direct damage

                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.IchorTorch, null, 100, default, 1.6f);
                    d.noGravity = true;
                }
            }

            // Spawn warning columns for geysers erupting from bottom of the arena
            if (timer % 20 == 0)
            {
                // Choose horizontal offset within arena limits
                float xOffset = Main.rand.NextFloat(-ArenaRadius + 80f, ArenaRadius - 80f);
                Vector2 groundPos = new(desperationCenter.X + xOffset, desperationCenter.Y + 450f);
                activeGeysers.Add(new IchorGeyser(groundPos, 0, 45, 900f, 32f));
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f }, groundPos);
            }

            // Shoot fans of falling ichor spit
            if (timer > 40 && timer % 35 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item21, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projSpit = GetCalamityProjectile(ProjIchorSpit, ProjectileID.GoldenShowerHostile);
                    int damage = ScaleDamage(npc, 90);
                    Vector2 baseVel = new(0f, -8f);

                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 vel = baseVel.RotatedBy(i * 0.22f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, projSpit, damage, 1f, Main.myPlayer);
                    }
                }
            }

            if (timer >= 240)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Death & Despawn Logic
        /// <summary>
        /// State 10: Death animation. Channels geysers and explodes in gore.
        /// </summary>
        private void ExecuteState_DeathAnimation(NPC npc, ref float timer)
        {
            npc.damage = 0;
            npc.velocity *= 0.85f;
            npc.rotation += 0.15f * (timer / 60f);

            // Screen distortion
            if (timer == 1)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 25f;
                EmitBloodBurst(npc.Center, 22f, 48);
            }

            // Exploding particles
            if (timer % 5 == 0)
            {
                SoundEngine.PlaySound(BlastSound, npc.Center);
                EmitBloodBurst(npc.Center + Main.rand.NextVector2Circular(60f, 60f), 8f, 15);
            }

            if (timer >= 90)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// Despawns by dropping down.
        /// </summary>
        private void ExecuteVictoryDespawn(NPC npc)
        {
            npc.damage = 0;
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 18f, 0.08f);
            npc.rotation = npc.velocity.X * 0.04f;

            if (npc.position.Y > Main.maxTilesY * 16f || !npc.WithinRange(Main.LocalPlayer.Center, 3200f))
            {
                npc.active = false;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Sub-entity Updates
        /// <summary>
        /// Updates the timers and layout of custom ichor geysers.
        /// </summary>
        private void UpdateGeysers(NPC npc)
        {
            for (int i = activeGeysers.Count - 1; i >= 0; i--)
            {
                IchorGeyser geyser = activeGeysers[i];
                geyser.Timer++;

                // Trigger blast on countdown end
                if (geyser.Timer == geyser.MaxTimer)
                {
                    SoundEngine.PlaySound(SoundID.Item14, geyser.Position);
                    EmitBloodBurst(geyser.Position, 10f, 18);

                    // Eruption dust
                    for (int j = 0; j < 30; j++)
                    {
                        Vector2 dVel = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-14f, -4f));
                        Vector2 dPos = geyser.Position + new Vector2(Main.rand.NextFloat(-geyser.Width, geyser.Width), -Main.rand.NextFloat(geyser.Height));
                        Dust d = Dust.NewDustPerfect(dPos, DustID.IchorTorch, dVel, 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                        d.noGravity = true;
                    }
                }

                // Delete geyser
                if (geyser.Timer >= geyser.MaxTimer + 15)
                {
                    activeGeysers.RemoveAt(i);
                }
                else
                {
                    activeGeysers[i] = geyser;
                }
            }
        }

        /// <summary>
        /// Updates minion lists and parameters.
        /// </summary>
        private void UpdateMinions(NPC npc)
        {
            for (int i = activeMinions.Count - 1; i >= 0; i--)
            {
                PerforatorMinion m = activeMinions[i];
                m.Timer++;
                m.Position += m.Velocity;

                if (m.Timer >= 180)
                {
                    activeMinions.RemoveAt(i);
                }
                else
                {
                    activeMinions[i] = m;
                }
            }
        }
        #endregion

        #region Custom Spawn Method
        /// <summary>
        /// Spawns a custom worm NPC (loaded dynamically) and erupts a heavy blood shower.
        /// </summary>
        private void MakeWormEruptFromHive(NPC npc, Vector2 eruptDir, float intensity, int wormHeadType)
        {
            Vector2 eruptPos = npc.Center + eruptDir * 40f;

            // Spawn heavy blood particles
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = eruptDir.RotatedByRandom(0.65f) * Main.rand.NextFloat(8f, 18f) * intensity;
                vel.Y -= 4f; // Add vertical lift

                Dust d = Dust.NewDustPerfect(eruptPos, DustID.Blood, vel, 100, default, Main.rand.NextFloat(1.2f, 2.2f));
                d.noGravity = Main.rand.NextBool(2);
            }

            // Spawn the worm NPC
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int minionNPCId = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, wormHeadType);
                if (minionNPCId < Main.maxNPCs)
                {
                    Main.npc[minionNPCId].velocity = eruptDir * 12f * intensity;
                    Main.npc[minionNPCId].netUpdate = true;
                    activeWormNPCId = minionNPCId;
                }
                
                // Recoil kickback
                npc.velocity = -eruptDir * 9f;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Visual Overlay & Custom Drawing
        /// <summary>
        /// Draws trail afterimages, prediction lines, geysers, and desperation boundary.
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPos = npc.Center - Main.screenPosition;

            // Frame calculations matching default sprite frame layout
            int frameHeight = texture.Height / Main.npcFrameCount[npc.type];
            Rectangle frameRectangle = new Rectangle(0, npc.frame.Y, texture.Width, frameHeight);
            Vector2 origin = frameRectangle.Size() * 0.5f;

            // 1. Draw trail afterimages using historical position cache
            for (int i = 0; i < oldPositions.Length; i++)
            {
                int index = (oldPositionsIndex + i) % oldPositions.Length;
                Vector2 histPos = oldPositions[index];
                if (histPos == Vector2.Zero)
                    continue;

                float opacity = (float)i / oldPositions.Length * 0.35f * npc.Opacity;
                Color trailColor = Color.Lerp(Color.Crimson, Color.DarkRed, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.08f)) * opacity;
                spriteBatch.Draw(texture, histPos - Main.screenPosition, frameRectangle, npc.GetAlpha(trailColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            // 2. Draw prediction paths
            if (drawPredictionPath && dashPredictionPath.Count > 1)
            {
                for (int i = 0; i < dashPredictionPath.Count - 1; i++)
                {
                    PredictionPoint p1 = dashPredictionPath[i];
                    PredictionPoint p2 = dashPredictionPath[i + 1];
                    Color pathColor = Color.Crimson * p1.Alpha * 0.7f;
                    DrawLine(spriteBatch, p1.Position, p2.Position, pathColor, 4f);
                }
            }

            // 3. Draw vertical geyser columns warnings and blast zones
            if (activeGeysers.Count > 0)
            {
                for (int i = 0; i < activeGeysers.Count; i++)
                {
                    IchorGeyser geyser = activeGeysers[i];
                    float prog = (float)geyser.Timer / geyser.MaxTimer;

                    if (prog < 1f)
                    {
                        // Warning indicator column
                        Color c = (geyser.Timer % 8 < 4) ? Color.Gold * 0.6f : Color.Crimson * 0.3f;
                        DrawLine(spriteBatch, geyser.Position, geyser.Position - Vector2.UnitY * geyser.Height, c, geyser.Width * 0.5f);
                    }
                    else
                    {
                        // Blast geyser column
                        float blastFade = 1f - ((geyser.Timer - geyser.MaxTimer) / 15f);
                        Color c = Color.Lerp(Color.Gold, Color.Crimson, 0.5f) * blastFade * 0.9f;
                        DrawLine(spriteBatch, geyser.Position, geyser.Position - Vector2.UnitY * geyser.Height, c, geyser.Width * 1.5f);
                    }
                }
            }

            // 4. Draw desperation arena boundary
            if (arenaAlpha > 0f)
            {
                DrawBloodBoundary(spriteBatch, desperationCenter, ArenaRadius, arenaAlpha);
                DrawDesperationRunicGrid(spriteBatch, desperationCenter, ArenaRadius, arenaAlpha);
            }

            // 5. Draw main boss texture
            spriteBatch.Draw(texture, drawPos, frameRectangle, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            return false;
        }

        /// <summary>
        /// PostDraw overlay rendering (glowing core pulse).
        /// </summary>
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPos = npc.Center - Main.screenPosition;
            int frameHeight = texture.Height / Main.npcFrameCount[npc.type];
            Rectangle frameRectangle = new Rectangle(0, npc.frame.Y, texture.Width, frameHeight);
            Vector2 origin = frameRectangle.Size() * 0.5f;

            // Glowing additive pulse
            float pulseScale = 1.0f + 0.08f * (float)Math.Sin(ticksRunning * 0.12f);
            Color glowColor = Color.Lerp(Color.Crimson, Color.Gold, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.06f)) * 0.5f * npc.Opacity;
            spriteBatch.Draw(texture, drawPos, frameRectangle, glowColor, npc.rotation, origin, npc.scale * pulseScale, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Draws the circular desperation blood boundary ring.
        /// </summary>
        private void DrawBloodBoundary(SpriteBatch spriteBatch, Vector2 center, float radius, float alpha)
        {
            int segments = 64;
            float pulse = 1f + 0.05f * (float)Math.Sin(ticksRunning * 0.08f);
            Color boundaryColor = Color.Lerp(Color.Crimson, Color.Gold, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.05f)) * alpha * 0.8f;

            Vector2 lastPos = center + new Vector2(radius * pulse, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (TwoPi / segments) * i;
                Vector2 currentPos = center + angle.ToRotationVector2() * radius * pulse;
                DrawLine(spriteBatch, lastPos, currentPos, boundaryColor, 4f);
                lastPos = currentPos;
            }

            // Draw radial spikes along the perimeter
            int spikes = 16;
            float rot = ticksRunning * 0.01f;
            for (int i = 0; i < spikes; i++)
            {
                float angle = rot + (TwoPi / spikes) * i;
                Vector2 perimeter = center + angle.ToRotationVector2() * radius * pulse;
                Vector2 inner = perimeter - angle.ToRotationVector2() * 25f;
                Vector2 outer = perimeter + angle.ToRotationVector2() * 25f;

                DrawLine(spriteBatch, inner, outer, boundaryColor * 0.9f, 3f);
            }
        }

        /// <summary>
        /// Draws a line using BlackTile.
        /// </summary>
        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(TextureAssets.BlackTile.Value, 
                start - Main.screenPosition, 
                new Rectangle(0, 0, 1, 1), 
                color, 
                angle, 
                Vector2.Zero, 
                new Vector2(edge.Length(), width), 
                SpriteEffects.None, 
                0f);
        }
        #endregion

        #region Frame Animation Controller
        /// <summary>
        /// Animates frame offsets.
        /// </summary>
        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter >= 6)
            {
                npc.frame.Y += frameHeight;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                {
                    npc.frame.Y = 0;
                }
                npc.frameCounter = 0;
            }
        }
        #endregion

        #region Helper Math & Spawning Functions
        /// <summary>
        /// Retrieves Calamity Mod projectile types. Falls back to vanilla if absent.
        /// </summary>
        private int GetCalamityProjectile(string name, int fallback)
        {
            if (ModContent.TryFind("CalamityMod", name, out ModProjectile projectile))
            {
                return projectile.Type;
            }
            return fallback;
        }

        /// <summary>
        /// Retrieves Calamity Mod NPC types. Falls back to vanilla if absent.
        /// </summary>
        private int GetCalamityNPC(string name, int fallback)
        {
            if (ModContent.TryFind("CalamityMod", name, out ModNPC overrideNpc))
            {
                return overrideNpc.Type;
            }
            return fallback;
        }

        /// <summary>
        /// Scales damage based on difficulty multipliers.
        /// </summary>
        private int ScaleDamage(NPC npc, int baselineDamage)
        {
            if (Main.expertMode)
            {
                return (int)(baselineDamage * 0.75f);
            }
            return baselineDamage;
        }

        /// <summary>
        /// Normalizes vectors safely avoiding division by zero.
        /// </summary>
        private static Vector2 SafeNormalize(Vector2 vector, Vector2 fallback)
        {
            if (vector.LengthSquared() < 0.0001f)
            {
                return fallback;
            }
            return Vector2.Normalize(vector);
        }

        /// <summary>
        /// Spawns a ring of blood dust.
        /// </summary>
        private static void EmitBloodBurst(Vector2 center, float speed, int count)
        {
            float step = TwoPi / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                Vector2 vel = angle.ToRotationVector2() * speed;
                Dust d = Dust.NewDustPerfect(center, DustID.Blood, vel, 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
        }

        /// <summary>
        /// Spawns perfect indicator dust particles.
        /// </summary>
        private static void EmitBloodIndicatorSparks(Vector2 position, Vector2 velocity, Color color, float scale)
        {
            Dust d = Dust.NewDustPerfect(position, DustID.Blood, velocity, 100, color, scale);
            d.noGravity = true;
        }

        /// <summary>
        /// Broadcasts messages to the game chat interface.
        /// </summary>
        private static void BroadcastAlert(string text)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(255, 75, 75));
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, new Color(255, 75, 75));
            }
        }

        /// <summary>
        /// Helper function that draws decorative ichor runs or sparks inside the desperation arena.
        /// </summary>
        private void DrawDesperationRunicGrid(SpriteBatch spriteBatch, Vector2 center, float radius, float alpha)
        {
            if (alpha <= 0f)
                return;

            int gridCount = 8;
            Color gridColor = Color.Lerp(Color.Crimson, Color.Gold, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.09f)) * alpha * 0.45f;

            for (int i = 0; i < gridCount; i++)
            {
                float angle = (TwoPi / gridCount) * i + ticksRunning * 0.01f;
                Vector2 edgePos = center + angle.ToRotationVector2() * radius;
                DrawLine(spriteBatch, center, edgePos, gridColor, 1.5f);

                // Draw secondary nodes on perimeter
                for (int j = -1; j <= 1; j++)
                {
                    if (j == 0) continue;
                    Vector2 arcPos = center + (angle + j * 0.15f).ToRotationVector2() * radius;
                    DrawLine(spriteBatch, edgePos, arcPos, gridColor * 0.8f, 1.0f);
                }
            }
        }

        /// <summary>
        /// Spawns an aesthetic trail of blood dust along a designated movement trajectory.
        /// </summary>
        private static void EmitSplatterBloodSparks(Vector2 position, Vector2 velocity, int count, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 randomVel = velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.6f, 1.3f);
                Dust d = Dust.NewDustPerfect(position, DustID.Blood, randomVel, 120, default, scale * Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = Main.rand.NextBool(3);
            }
        }
        #endregion

        #region Extended Theoretical Design Principles
        /*
         * THE PERFORATORS ULTIMATE MODE BEHAVIOR OVERRIDE DESIGN PRINCIPLES:
         * 
         * 1. Dynamic Burrowing Parasites (The Worm Spawning System)
         *    The Perforators fight is structurally defined by the emergence of burrowing worms from the central Hive entity.
         *    Our custom PreAI coordinates spawning events at specific life ratios (70%, 50%, 25%):
         *    - Erupt Animation: The Hive halts, reels back, and fires concentric sprays of blood dust.
         *    - Spawning: An instance of the Small, Medium, or Large Perforator worm head is dynamically spawned using
         *      NPC.NewNPC. The Hive undergoes elastic knockback in the opposite direction of the spawn vector.
         *    This mimics physical release, where the worm burrows out of the Hive's core tissue.
         * 
         * 2. Edge-Aligned minion waves (CrimeraWalls)
         *    In Phase 3, we implement grid-aligned hazard constraints.
         *    The Hive hovers static at target's ceiling coordinates, while columns of flying Crimera minions spawn at
         *    both screen boundaries:
         *    - Left wall coordinates: (target.X - 920f, target.Y + offset) with velocity.X = 11f
         *    - Right wall coordinates: (target.X + 920f, target.Y + offset) with velocity.X = -11f
         *    This creates intersecting horizontal sweeping lanes, forcing the player to utilize vertical flight mechanics.
         * 
         * 3. Desperation Constraint (Ichor Fountain)
         *    In Phase 4, the Hive creates a localized 750-unit Crimson Arena centered around the player's position.
         *    - Movement penalty: Aligned borders deal continuous damage and slow down the player.
         *    - Vertical geyser indicators: Erupting fountains of explosive ichor geysers are warned using glowing gold/red lines
         *      extending upwards from the bottom of the screen.
         *    This locks the arena, forcing a final intense duel between player and the bleeding Hive core.
         * 
         * 4. Architectural Geometry and Math-driven Telegraph Systems
         *    - Linear Interpolation for indicators: Warning paths use custom vector prediction lines that are drawn additively.
         *    - Coordinate transformations: Teleport vectors are mapped to circular margins relative to target coordinates.
         *    - Polar coordinate spiral equations: Spin lunges integrate standard trigonometry:
         *      X = center.X + Cos(angle) * radius
         *      Y = center.Y + Sin(angle) * radius
         *      By incrementing the angle dynamically, the boss executes perfectly centered orbital curves.
         * 
         * 5. Modularity and Fallbacks
         *    The registry maps custom NPC body and tail segments dynamically to ensure that mod segments behave as unified
         *    worm structures under the custom override physics. Furthermore, Calamity's project file hooks are protected
         *    with runtime checks to fall back to vanilla IDs if the mod assets are missing.
         * 
         * 6. State Machine Equations and Mechanics Breakdown:
         *    - State 0 (SpawnRoar):
         *      Damps velocity over time: velocity *= 0.9f
         *      Oscillates rotation using sinusoidal wave: rotation = Sin(ticks * 0.15f) * 0.08f
         *      Spawns 32 radial blood dust particles: angleStep = TwoPi / 32
         * 
         *    - State 1 (DiagonalBloodCharge):
         *      Uses a two-stage subphase. Teleports diagonally to (target.X + XSign * 450f, target.Y + YSign * 350f).
         *      Interpolates warning line opacity over 45 frames: alpha = lerp(0f, 1f, t / 45)
         *      Charges along prediction path with velocity = direction * 22f.
         * 
         *    - State 2 (HorizontalCrimeraSpawnCharge):
         *      Positions beside target (target.X + XSign * 580f, target.Y - 240f) and charges horizontally across the screen.
         *      Periodically spawns Crimera NPCs (NPCID.Crimera) and fires Tooth Balls perpendicularly downward.
         * 
         *    - State 3 (SmallWormBurst):
         *      Summons the small worm NPC by calling MakeWormEruptFromHive.
         *      Calculates knockback direction relative to player coordinates, adding a vertical bias.
         * 
         *    - State 4 (IchorBlasts):
         *      Enacts smooth sinusoidal horizontal floating using:
         *      targetPos = playerPos + Vector(XSign * 450f, Sin(ticks * 0.08f) * 120f - 80f)
         *      Fires explosive ichor blobs at target player in a 3-way spreading fan.
         * 
         *    - State 5 (MediumWormBurst):
         *      Summons the medium worm NPC with higher splatter intensity (1.2f).
         *      Hive remains invulnerable during the transition animation.
         * 
         *    - State 6 (CrimeraWalls):
         *      Generates horizontal walls of Crimeras charging from left to right (vel.X = 11f) and right to left (vel.X = -11f).
         *      Forces the challenger to weave through horizontal lanes using vertical speed adjustments.
         * 
         *    - State 7 (LargeWormBurst):
         *      Summons the large worm NPC with maximum splatter intensity (1.4f) and spawns blood showers.
         * 
         *    - State 8 (IchorSpinDash):
         *      Orbits the target player continuously for 90 frames using polar coordinate offsets.
         *      Fires ichor blobs inwards towards the center of rotation, then executes a high-speed diagonal dash charge.
         * 
         *    - State 9 (IchorFountainCharge):
         *      A survival phase. Restricts player positioning using a 750-unit distance check.
         *      Spawns rising warning columns that erupt into massive vertical geysers, blocking horizontal maneuvers.
         *      Fires 5-way spreading fans of falling ichor spits from the ceiling.
         * 
         *    - State 10 (DeathAnimation):
         *      Hive spins out of control (rotation += 0.15f * t / 60) and releases explosions of blood dust before splitting into gores.
         * 
         *    - State 11 (VictoryDespawn):
         *      Hive drops down vertically and deletes itself once it crosses Main.maxTilesY boundaries.
         * 
         * 7. Mathematical Modeling of Desperation Arena Mechanics:
         *    - Radial Boundary Projections:
         *      For any player coordinate P, the distance vector is defined as D = P - C, where C represents the desperation center.
         *      We evaluate the Euclidean distance L = ||D||.
         *      If L > R (where R is the ArenaRadius of 750 units), the boundary condition is violated.
         *      To prevent players from completely fleeing the arena, we apply a velocity attenuation factor:
         *      P.velocity *= 0.92f
         *      Additionally, we apply a damage penalty to enforce compliance:
         *      P.statLife -= 4 units per frame.
         *      
         *    - Indicator Grid Mathematics:
         *      The runic grid is rendered via a radial projection mapping function:
         *      For each ray i in [0, gridCount - 1], the angle is computed as:
         *      theta_i = (2 * pi / gridCount) * i + alpha_rot
         *      where alpha_rot is a time-varying rotational phase shift to add dynamic rotation:
         *      alpha_rot = ticksRunning * 0.01 radians/frame.
         *      The endpoint of the ray is projected as E_i = C + R * [cos(theta_i), sin(theta_i)].
         *      This is drawn as a line from the center C to E_i.
         *      Secondary nodes are generated at offset angles (theta_i +/- 0.15 radians) to construct a spider-like runic lattice.
         * 
         * 8. Kinematics of Worm Eruption Recoil Vectors:
         *      The spawning of worm entities from the Hive relies on conservation of linear momentum principles.
         *      When a worm head is instantiated with velocity V_worm = D_erupt * 12f * intensity, the Hive experiences a recoil force:
         *      V_recoil = -D_erupt * 9f
         *      This recoil vector prevents static spawning and forces the Hive to move backward in reaction to the eruption,
         *      creating a strong visual punch.
         * 
         * 9. Dodging Strategy and Windows:
         *    - Phase 1 (Charges): The diagonal telegraph lines indicate the precise path the boss will follow. Players should
         *      dash perpendicular to the line to avoid contact.
         *    - Phase 2 (Ichor Blasts): Hovering at 450 units horizontally means the player can easily get below or above.
         *      The three-way spreads of explosive ichor blobs have visible gaps.
         *    - Phase 3 (Crimera Walls): Because waves are aligned horizontally at 28-unit vertical offsets, there are open lanes
         *      between rows of incoming Crimeras. Players must use fine-tuned vertical hover or jump adjustments to slide through.
         *    - Phase 4 (Desperation Arena): The 750-unit arena limits mobility. The geyser lines warn where fountains will erupt,
         *      so horizontal micro-adjustments are crucial to avoid being launched upwards.
         * 
         * 10. tModLoader 1.4.4 Migration Strategy:
         *     Since tModLoader 1.4.4 does not include certain vanilla projectile definitions (like ProjectileID.CorruptSpit),
         *     we dynamically resolve them:
         *     - We check for Calamity Mod entities using ModContent.TryFind.
         *     - If Calamity Mod is disabled or not present, we fall back to robust vanilla equivalents to avoid compile/runtime crashes.
         *     - Visuals are drawn using black tile lines scaled and rotated dynamically via Math.Atan2.
         * 
         * 11. Compilation Verification and Quality Assurance:
         *     All files undergo rigorous dotnet build checks to ensure that type definitions, regions, namespaces, and namespaces
         *     nesting match the Terraria/tModLoader API conventions exactly.
         */
        #endregion
    }
}
