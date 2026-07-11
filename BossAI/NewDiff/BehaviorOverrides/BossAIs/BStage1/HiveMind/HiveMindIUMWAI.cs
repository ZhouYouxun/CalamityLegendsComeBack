// =====================================================================================================================
// THE HIVE MIND - CUSTOM BEHAVIOR OVERRIDE (IUMW MODE)
// =====================================================================================================================
// DESIGN PHILOSOPHY & FIGHT METRICS:
// The Hive Mind is the collective consciousness of the Corrupt biome, a mass of rotting tissue, infected cells,
// and shadow energy. This override completely replaces the default Calamity and Vanilla AI loops by returning false
// in PreAI, controlling target selection, movement velocity, transition checkpoints, frame animation, and visual indicators.
//
// FIGHT PHASES & ATTACK CYCLE PATTERNS:
// - Phase 1 (100% - 68% HP) - Awakening of the Hive:
//   * Spawn Roar: Emerges from a dormant wisp, screaming and shaking the screen while releasing corruption dust rings.
//   * Suspension Drift: Drifts in place. When struck by player attacks, it undergoes elastic recoil, bouncing away and accelerating back.
//   * NPC Spawn Arc: Circles the target in an orbital trajectory, spawning Eater of Souls and Dark Hearts.
//   * Spin Lunge: Teleports to a circular coordinate, spins rapidly while spraying vile clots, and charges at high speed.
//
// - Phase 2 (68% - 42% HP) - Creeping Corruptions:
//   * Cloud Dash: Charges across the screen horizontally, leaving static nimbus clouds that rain toxic acid droplets.
//   * Eater of Souls Wall: Rises to the heavens, spawning massive walls of Eaters that charge across the screen horizontally.
//
// - Phase 3 (42% - 16% HP) - Devouring Shadows:
//   * Underground Flame Dash: Digs deep beneath the player and dashes horizontally, trigger vertical eruptions of cursed flame geysers.
//   * Cursed Rain Storm: Hovers above the player, spawning rain clouds and spraying homing vile clots.
//
// - Phase 4 (16% - 0% HP) - Desperation: The Rotting Core:
//   * Final Transition: Channels shadow energy, becoming invulnerable for 90 frames while releasing screen-distorting waves.
//   * Toxic Arena: Restricts the player inside a 700-unit circular toxic barrier. Crossing the border inflicts heavy damage and slows movement.
//   * Frantic Overdrive: Combines high-speed charges, rapid blob bursts, and expanding void pulses.
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

using CalamityHiveMind = CalamityMod.NPCs.HiveMind.HiveMind;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage1.HiveMind
{
    internal sealed class HiveMindIUMWAI : IUMWBossAI
    {
        #region Constants & Configuration
        // NPC Identifiers
        public override int NPCType => ModContent.NPCType<CalamityHiveMind>();
        public override string BossName => "The Hive Mind";

        // Phase Thresholds
        public override float[] PhaseLifeRatios => new[] { 0.68f, 0.42f, 0.16f };
        public override int AttackCycleLength => 128;
        public override float MotionIntensity => 1f;
        public override Color DebugColor => new(146, 255, 150);

        // Sound Hooks
        public static readonly SoundStyle RoarSound = new("CalamityMod/Sounds/Custom/HiveMindRoar") { Volume = 1.0f, Pitch = 0.0f };
        public static readonly SoundStyle FastRoarSound = new("CalamityMod/Sounds/Custom/HiveMindRoar") { Volume = 1.0f, Pitch = 0.4f };
        public static readonly SoundStyle DashSound = SoundID.DoubleJump;
        public static readonly SoundStyle SpawnSound = SoundID.NPCDeath10;
        public static readonly SoundStyle EaterScreamSound = SoundID.NPCDeath4;

        // Math Constants
        private const float TwoPi = MathHelper.TwoPi;
        private const float Pi = MathHelper.Pi;
        private const float PiOver2 = MathHelper.PiOver2;
        private const float ArenaRadius = 700f;

        // Projectile Reference Keys
        private const string ProjClot = "VileClot";
        private const string ProjNimbus = "ShadeNimbusHostile";
        private const string ProjRain = "ShaderainHostile";
        private const string ProjBlob = "BlobProjectile";
        private const string ProjWave = "HiveMindWave";
        private const string ProjFire = "ShadeFire";
        private const string ProjEater = "EaterOfSouls";
        #endregion

        #region State Machine Enumeration
        public enum AttackState
        {
            SpawnRoar = 0,
            SuspensionDrift = 1,
            NPCSpawnArc = 2,
            SpinLunge = 3,
            CloudDash = 4,
            EaterOfSoulsWall = 5,
            UndergroundFlameDash = 6,
            CursedRain = 7,
            BlobBurst = 8,
            DesperationArena = 9,
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

        private struct ToxicNimbus
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Timer;
            public float Scale;

            public ToxicNimbus(Vector2 pos, Vector2 vel, int timer, float scale)
            {
                Position = pos;
                Velocity = vel;
                Timer = timer;
                Scale = scale;
            }
        }

        private struct ShadeGeyser
        {
            public Vector2 Position;
            public int Timer;
            public int MaxTimer;
            public float Height;
            public float Width;

            public ShadeGeyser(Vector2 pos, int timer, int maxTimer, float height, float width)
            {
                Position = pos;
                Timer = timer;
                MaxTimer = maxTimer;
                Height = height;
                Width = width;
            }
        }

        private struct GravityParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float Angle;
            public Color DrawColor;

            public GravityParticle(Vector2 pos, Vector2 vel, float scale, float angle, Color color)
            {
                Position = pos;
                Velocity = vel;
                Scale = scale;
                Angle = angle;
                DrawColor = color;
            }
        }
        #endregion

        #region Local Fields
        // Drawing & opacity variables
        private float arenaAlpha = 0f;
        private Vector2 desperationCenter = Vector2.Zero;

        // Custom Trajectory Prediction for lunges
        private readonly List<PredictionPoint> dashPredictionPath = new();
        private bool drawPredictionPath = false;

        // Teleportation coordinates
        private float teleportPositionX = 0f;
        private float teleportPositionY = 0f;

        // Active Custom Entities
        private readonly List<ToxicNimbus> activeNimbuses = new();
        private readonly List<ShadeGeyser> activeGeysers = new();
        private readonly List<GravityParticle> activeParticles = new();

        // Execution Timers and Counters
        private int ticksRunning = 0;
        private int dashCounter = 0;
        private int spawnCounter = 0;
        private int leapStage = 0;
        private int invincibilityTimer = 0;

        // Historic Position Cache (for Trails)
        private readonly Vector2[] oldPositions = new Vector2[12];
        private int oldPositionsIndex = 0;
        #endregion

        #region Core AI Hooks
        /// <summary>
        /// Main update override in PreAI. Suppresses default AI by returning false.
        /// Handles target validity, enrage calculations, phase thresholds, and delegates to custom states.
        /// </summary>
        public override bool PreAI(NPC npc, IUMWGlobalNPC data)
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

            // Clear harmful debuffs on players to manage Infernum balance
            if (target.HasBuff(BuffID.CursedInferno))
                target.ClearBuff(BuffID.CursedInferno);
            if (target.HasBuff(BuffID.ShadowFlame))
                target.ClearBuff(BuffID.ShadowFlame);

            // Record history positions for trail rendering
            oldPositions[oldPositionsIndex] = npc.Center;
            oldPositionsIndex = (oldPositionsIndex + 1) % oldPositions.Length;

            // Check for phase transitions (100% -> 68% -> 42% -> 16%)
            int nextPhase = CalculatePhase(npc);
            if (nextPhase > currentPhase)
            {
                npc.ai[0] = nextPhase;
                currentPhase = nextPhase;
                npc.netUpdate = true;
            }

            // Desperation Check (16% HP)
            if (currentPhase < 4 && npc.life <= npc.lifeMax * PhaseLifeRatios[2])
            {
                npc.ai[0] = 4f;
                currentPhase = 4;
                TransitionToAttack(npc, AttackState.DesperationArena);
                state = AttackState.DesperationArena;
                invincibilityTimer = 90; // Invulnerable state transition
                desperationCenter = target.Center;
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 15f;
            }

            // Update custom sub-entities
            UpdateToxicNimbuses(npc);
            UpdateShadeGeysers(npc);
            UpdateParticles();

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
                    EmitShadowFlameBurst(npc.Center, 20f, 15);
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
                case AttackState.SuspensionDrift:
                    ExecuteState_SuspensionDrift(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.NPCSpawnArc:
                    ExecuteState_NPCSpawnArc(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.SpinLunge:
                    ExecuteState_SpinLunge(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CloudDash:
                    ExecuteState_CloudDash(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.EaterOfSoulsWall:
                    ExecuteState_EaterOfSoulsWall(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.UndergroundFlameDash:
                    ExecuteState_UndergroundFlameDash(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.CursedRain:
                    ExecuteState_CursedRain(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BlobBurst:
                    ExecuteState_BlobBurst(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DesperationArena:
                    ExecuteState_DesperationArena(npc, target, ref timer, ref stateTracker, currentPhase);
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
            activeNimbuses.Clear();
            activeGeysers.Clear();
            activeParticles.Clear();

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
            AttackState nextState = AttackState.SuspensionDrift;
            AttackState currentAttack = (AttackState)(int)npc.ai[1];

            List<AttackState> allowed = new();

            switch (phase)
            {
                case 1:
                    allowed.Add(AttackState.SuspensionDrift);
                    allowed.Add(AttackState.NPCSpawnArc);
                    allowed.Add(AttackState.SpinLunge);
                    break;
                case 2:
                    allowed.Add(AttackState.SuspensionDrift);
                    allowed.Add(AttackState.CloudDash);
                    allowed.Add(AttackState.EaterOfSoulsWall);
                    allowed.Add(AttackState.SpinLunge);
                    break;
                case 3:
                    allowed.Add(AttackState.SuspensionDrift);
                    allowed.Add(AttackState.UndergroundFlameDash);
                    allowed.Add(AttackState.CursedRain);
                    allowed.Add(AttackState.CloudDash);
                    allowed.Add(AttackState.SpinLunge);
                    break;
                case 4:
                    allowed.Add(AttackState.DesperationArena);
                    allowed.Add(AttackState.BlobBurst);
                    allowed.Add(AttackState.SpinLunge);
                    allowed.Add(AttackState.CloudDash);
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
        /// Calculates the boss's current phase based on life ratios.
        /// </summary>
        private int CalculatePhase(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (lifeRatio <= PhaseLifeRatios[2]) // Under 16% HP
                return 4;
            if (lifeRatio <= PhaseLifeRatios[1]) // Under 42% HP
                return 3;
            if (lifeRatio <= PhaseLifeRatios[0]) // Under 68% HP
                return 2;
            return 1;
        }
        #endregion

        #region Phase 1 Attacks
        /// <summary>
        /// State 0: Emerges, roars, and releases expanding rings of corruption dust.
        /// </summary>
        private void ExecuteState_SpawnRoar(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.velocity *= 0.9f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.15f) * 0.08f;

            if (timer == 1)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 12f;
                EmitShadowFlameBurst(npc.Center, 15f, 32);
            }

            if (timer >= 60)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 1: Suspension Drift. Bounces away with elastic recoil when hit, then drifts back.
        /// </summary>
        private void ExecuteState_SuspensionDrift(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            // Drifts toward target smoothly
            Vector2 targetOffset = target.Center - npc.Center;
            float dist = targetOffset.Length();

            npc.rotation = (float)Math.Sin(ticksRunning * 0.08f) * 0.12f;

            // Handle hit-back elastic recoil
            if (npc.justHit)
            {
                SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);
                npc.velocity = SafeNormalize(-targetOffset, Vector2.UnitY) * 12f;
                timer = 0; // Reset drift duration on hit
                npc.netUpdate = true;
            }

            // Return to target coordinates
            if (timer > 20)
            {
                float returnSpeed = 6f + (dist * 0.015f);
                Vector2 returnVel = SafeNormalize(targetOffset, Vector2.UnitX) * returnSpeed;
                npc.velocity = Vector2.Lerp(npc.velocity, returnVel, 0.08f);
            }
            else
            {
                npc.velocity *= 0.94f; // Friction decay from recoil
            }

            // Shoot vile clots occasionally in drift
            if (timer > 30 && timer % 45 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projType = GetCalamityProjectile(ProjClot, 157);
                    int damage = ScaleDamage(npc, 75);
                    Vector2 shootVel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 7.5f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, shootVel, projType, damage, 1f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item20, npc.Center);
            }

            // End state and switch
            if (timer >= 150)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 2: Circles the player in an orbital path, spawning eaters and dark hearts.
        /// </summary>
        private void ExecuteState_NPCSpawnArc(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            float orbitRadius = 420f;
            float orbitSpeed = 0.05f;

            // Initialize orbit parameters
            if (timer == 1)
            {
                stateTracker = Main.rand.NextFloat(TwoPi); // Random start angle
                spawnCounter = 0;
                npc.netUpdate = true;
            }

            // Update orbital angle
            stateTracker += orbitSpeed;
            Vector2 desiredPos = target.Center + stateTracker.ToRotationVector2() * orbitRadius;
            npc.velocity = Vector2.Lerp(npc.velocity, (desiredPos - npc.Center) * 0.15f, 0.2f);
            npc.rotation = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).ToRotation() - PiOver2;

            // Spawning minions along the path
            if (timer % 40 == 0 && spawnCounter < 3)
            {
                spawnCounter++;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (spawnCounter == 2)
                    {
                        // Spawn a Dark Heart minion
                        int darkHeartType = GetCalamityNPC("DarkHeart", NPCID.Creeper);
                        int minionId = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, darkHeartType);
                        if (minionId < Main.maxNPCs)
                        {
                            Main.npc[minionId].velocity = SafeNormalize(npc.Center - target.Center, Vector2.UnitY).RotatedBy(PiOver2) * 5f;
                            Main.npc[minionId].netUpdate = true;
                        }
                    }
                    else
                    {
                        // Spawn an Eater of Souls
                        int minionId = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofSouls);
                        if (minionId < Main.maxNPCs)
                        {
                            Main.npc[minionId].velocity = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedByRandom(0.4f) * 6f;
                            Main.npc[minionId].netUpdate = true;
                        }
                    }
                }
                SoundEngine.PlaySound(EaterScreamSound, npc.Center);
                EmitShadowFlameBurst(npc.Center, 8f, 12);
            }

            if (timer >= 180)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 3: Teleports, orbits, and performs high-speed charges.
        /// </summary>
        private void ExecuteState_SpinLunge(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int windup = 50;
            int chargeDuration = 35;

            // stage:
            // 0 = Fade out and telegraph
            // 1 = Charge and spray
            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;
                    npc.velocity *= 0.88f;
                    npc.Opacity = MathHelper.Lerp(1f, 0.1f, timer / (float)windup);

                    if (timer == 1)
                    {
                        // Calculate teleport position relative to player
                        Vector2 offset = Main.rand.NextVector2CircularEdge(450f, 450f);
                        teleportPositionX = target.Center.X + offset.X;
                        teleportPositionY = target.Center.Y + offset.Y;
                        npc.netUpdate = true;
                    }

                    // Warning line preview
                    if (timer >= 15)
                    {
                        drawPredictionPath = true;
                        dashPredictionPath.Clear();
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX, teleportPositionY), 0.7f));
                        dashPredictionPath.Add(new PredictionPoint(target.Center, 0.7f));
                    }

                    // Spawn gather particles at teleport position
                    if (timer % 4 == 0)
                    {
                        Vector2 pVel = Main.rand.NextVector2Circular(3f, 3f);
                        EmitCursedDustPerfect(new Vector2(teleportPositionX, teleportPositionY), pVel, Color.MediumPurple, 1.3f);
                    }

                    if (timer >= windup)
                    {
                        npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                        npc.Opacity = 1f;

                        // Dash directly towards target
                        Vector2 dashDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitX);
                        npc.velocity = dashDir * 23f;
                        npc.rotation = dashDir.ToRotation() - PiOver2;
                        SoundEngine.PlaySound(RoarSound, npc.Center);
                        SoundEngine.PlaySound(DashSound, npc.Center);

                        leapStage = 1;
                        timer = 0;
                        drawPredictionPath = false;
                        npc.netUpdate = true;
                    }
                    break;

                case 1:
                    // Charge strike
                    npc.rotation = SafeNormalize(npc.velocity, Vector2.UnitY).ToRotation() - PiOver2;

                    // Release clots during charge
                    if (timer % 5 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projType = GetCalamityProjectile(ProjClot, 157);
                            int damage = ScaleDamage(npc, 78);
                            Vector2 clVel = SafeNormalize(npc.velocity, Vector2.UnitY).RotatedBy(PiOver2) * 5.5f;

                            // Perpendicular sparks
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, clVel, projType, damage, 1f, Main.myPlayer);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -clVel, projType, damage, 1f, Main.myPlayer);
                        }
                    }

                    if (timer >= chargeDuration)
                    {
                        dashCounter++;
                        if (dashCounter >= 2)
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
        #endregion

        #region Phase 2 Attacks
        /// <summary>
        /// State 4: Cloud Dash. Teleports to sides and dashes across, dropping toxic clouds.
        /// </summary>
        private void ExecuteState_CloudDash(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int windup = 45;
            int dashTime = 40;

            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;
                    npc.velocity *= 0.9f;
                    npc.Opacity = MathHelper.Lerp(1f, 0.15f, timer / (float)windup);

                    if (timer == 1)
                    {
                        // Position beside player
                        float side = (target.Center.X < npc.Center.X) ? 1f : -1f;
                        teleportPositionX = target.Center.X + side * 550f;
                        teleportPositionY = target.Center.Y - 100f;
                        npc.netUpdate = true;
                    }

                    // Render horizontal warning line
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

                        // Sweep across
                        float dir = (target.Center.X < npc.Center.X) ? -1f : 1f;
                        npc.velocity = new Vector2(dir * 20f, 0f);
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

                    // Release toxic nimbuses and rain
                    if (timer % 8 == 0)
                    {
                        activeNimbuses.Add(new ToxicNimbus(npc.Center, Vector2.Zero, 0, 1f));
                        SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.5f }, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projRainType = GetCalamityProjectile(ProjRain, ProjectileID.RainNimbus);
                            int damage = ScaleDamage(npc, 80);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, 6.5f), projRainType, damage, 0f, Main.myPlayer);
                        }
                    }

                    if (timer >= dashTime)
                    {
                        dashCounter++;
                        if (dashCounter >= 2)
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
        /// State 5: Eater of Souls Wall. Flies high and summons walls of Eaters sweeping horizontally.
        /// </summary>
        private void ExecuteState_EaterOfSoulsWall(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;

            // Stable hover high above player
            Vector2 hoverPos = target.Center + new Vector2(0f, -380f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hoverPos - npc.Center) * 0.08f, 0.15f);
            npc.rotation = npc.velocity.X * 0.03f;

            if (timer == 30)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                SoundEngine.PlaySound(EaterScreamSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 8f;
            }

            // Spawn Eater wall segments
            if (timer >= 45 && timer <= 125 && timer % 10 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int wallEater = GetCalamityProjectile(ProjEater, 157);
                    int damage = ScaleDamage(npc, 82);

                    float verticalOffset = (timer - 45) * 25f - 100f; // Stacked height offsets
                    Vector2 leftSpawn = target.Center + new Vector2(-950f, verticalOffset);
                    Vector2 rightSpawn = target.Center + new Vector2(950f, verticalOffset);

                    // Cross paths
                    Projectile.NewProjectile(npc.GetSource_FromAI(), leftSpawn, new Vector2(10.5f, 0f), wallEater, damage, 1f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), rightSpawn, new Vector2(-10.5f, 0f), wallEater, damage, 1f, Main.myPlayer);
                }

                // Play soft growls
                if (timer % 20 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Zombie8, npc.Center);
                }
            }

            if (timer >= 180)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Phase 3 Attacks
        /// <summary>
        /// State 6: Underground Flame Dash. Digs beneath and dashes horizontally, creating erupting geysers.
        /// </summary>
        private void ExecuteState_UndergroundFlameDash(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            int windup = 50;
            int dashDuration = 55;

            switch (leapStage)
            {
                case 0:
                    npc.damage = 0;
                    npc.velocity *= 0.86f;
                    npc.Opacity = MathHelper.Lerp(1f, 0.1f, timer / (float)windup);

                    if (timer == 1)
                    {
                        // Dig position: below player
                        float dir = Main.rand.NextBool() ? 1f : -1f;
                        teleportPositionX = target.Center.X - dir * 550f;
                        teleportPositionY = target.Center.Y + 320f;
                        npc.netUpdate = true;
                    }

                    // Indicator preview
                    if (timer >= 15)
                    {
                        drawPredictionPath = true;
                        dashPredictionPath.Clear();
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX, teleportPositionY), 0.7f));
                        dashPredictionPath.Add(new PredictionPoint(new Vector2(teleportPositionX + Math.Sign(target.Center.X - teleportPositionX) * 1200f, teleportPositionY), 0.7f));
                    }

                    if (timer >= windup)
                    {
                        npc.Center = new Vector2(teleportPositionX, teleportPositionY);
                        npc.Opacity = 1f;

                        float dir = (target.Center.X < npc.Center.X) ? -1f : 1f;
                        npc.velocity = new Vector2(dir * 18f, 0f);
                        npc.rotation = npc.velocity.X * 0.04f;
                        SoundEngine.PlaySound(DashSound, npc.Center);

                        leapStage = 1;
                        timer = 0;
                        drawPredictionPath = false;
                        npc.netUpdate = true;
                    }
                    break;

                case 1:
                    npc.rotation = npc.velocity.X * 0.02f;

                    // Release shade fire geysers upward
                    if (timer % 8 == 0)
                    {
                        Vector2 geyserPos = new(npc.Center.X, npc.Center.Y - 20f);
                        activeGeysers.Add(new ShadeGeyser(geyserPos, 0, 45, 750f, 32f));
                        SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f }, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projFire = GetCalamityProjectile(ProjFire, ProjectileID.ShadowFlame);
                            int damage = ScaleDamage(npc, 90);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, new Vector2(0f, -8f), projFire, damage, 1.5f, Main.myPlayer);
                        }
                    }

                    if (timer >= dashDuration)
                    {
                        dashCounter++;
                        if (dashCounter >= 2)
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
        /// State 7: Cursed Rain Storm. Hovers and spawns toxic rain clouds, clots, and flame pillars.
        /// </summary>
        private void ExecuteState_CursedRain(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.damage = 0;

            // Hover stable above target
            Vector2 hoverPos = target.Center + new Vector2(0f, -340f);
            npc.velocity = Vector2.Lerp(npc.velocity, (hoverPos - npc.Center) * 0.07f, 0.12f);
            npc.rotation = npc.velocity.X * 0.04f;

            // Rapid storm elements
            if (timer % 12 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projClot = GetCalamityProjectile(ProjClot, 157);
                    int damage = ScaleDamage(npc, 80);

                    // Shoot cloting sparks from sides
                    Vector2 leftPos = target.Center + new Vector2(-540f, Main.rand.NextFloat(-400f, -200f));
                    Vector2 rightPos = target.Center + new Vector2(540f, Main.rand.NextFloat(-400f, -200f));

                    Vector2 leftVel = SafeNormalize(target.Center - leftPos, Vector2.UnitX) * 8.5f;
                    Vector2 rightVel = SafeNormalize(target.Center - rightPos, -Vector2.UnitX) * 8.5f;

                    Projectile.NewProjectile(npc.GetSource_FromAI(), leftPos, leftVel.RotatedByRandom(0.12f), projClot, damage, 1f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), rightPos, rightVel.RotatedByRandom(0.12f), projClot, damage, 1f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.4f }, npc.Center);
            }

            // Occassionally drop toxic cloud rain
            if (timer % 40 == 0)
            {
                Vector2 cloudPos = target.Center + new Vector2(Main.rand.NextFloat(-300f, 300f), -450f);
                activeNimbuses.Add(new ToxicNimbus(cloudPos, Vector2.Zero, 0, 1.2f));

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projNimbus = GetCalamityProjectile(ProjNimbus, ProjectileID.RainNimbus);
                    int damage = ScaleDamage(npc, 80);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), cloudPos, Vector2.Zero, projNimbus, damage, 0f, Main.myPlayer);
                }
            }

            // End state
            if (timer >= 240)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Phase 4 Attacks & Desperation
        /// <summary>
        /// State 8: Blob Burst. Hovers, fires waves of curving blobs.
        /// </summary>
        private void ExecuteState_BlobBurst(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            npc.velocity *= 0.92f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.12f) * 0.1f;

            if (timer == 20)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 10f;
                EmitShadowFlameBurst(npc.Center, 12f, 24);
            }

            // Shoot blobs in fans
            if (timer > 30 && timer < 150 && timer % 30 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item62, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projBlob = GetCalamityProjectile(ProjBlob, ProjectileID.CursedFlameHostile);
                    int damage = ScaleDamage(npc, 86);
                    Vector2 baseDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);

                    int fanCount = (phase == 4) ? 12 : 7;
                    for (int i = 0; i < fanCount; i++)
                    {
                        float angleOffset = (TwoPi / fanCount) * i;
                        Vector2 shootVel = baseDir.RotatedBy(angleOffset) * 9.5f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, shootVel, projBlob, damage, 1.5f, Main.myPlayer);
                    }
                }
            }

            if (timer >= 180)
            {
                SelectNextAttack(npc, phase);
            }
        }

        /// <summary>
        /// State 9: Desperation Arena transition and lock.
        /// </summary>
        private void ExecuteState_DesperationArena(NPC npc, Player target, ref float timer, ref float stateTracker, int phase)
        {
            // Center the arena on target and hold position
            npc.velocity *= 0.85f;
            npc.rotation = (float)Math.Sin(ticksRunning * 0.2f) * 0.15f;

            // Fade in toxic arena boundary
            arenaAlpha = MathHelper.Clamp(arenaAlpha + 0.03f, 0f, 1f);

            // Restrict player movement within the boundary
            float dist = Vector2.Distance(target.Center, desperationCenter);
            if (dist > ArenaRadius)
            {
                // Penalize player
                target.AddBuff(BuffID.CursedInferno, 180);
                target.AddBuff(BuffID.Slow, 60);
                target.velocity *= 0.9f; // Pull drag

                // Drain health directly
                target.statLife = Math.Max(1, target.statLife - 3);

                // Warning feedback dust on player
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.CursedTorch, null, 100, default, 1.5f);
                    d.noGravity = true;
                }
            }

            // Draw gravity particle whirlpool inward to center
            if (timer % 3 == 0 && activeParticles.Count < 60)
            {
                Vector2 spawnPos = desperationCenter + Main.rand.NextVector2CircularEdge(ArenaRadius, ArenaRadius);
                Vector2 vel = SafeNormalize(desperationCenter - spawnPos, Vector2.Zero) * 4.5f;
                activeParticles.Add(new GravityParticle(spawnPos, vel, 1.2f, Main.rand.NextFloat(TwoPi), Color.MediumPurple));
            }

            // Spawn warning columns for geysers in random arena locations
            if (timer % 25 == 0)
            {
                Vector2 geyserPos = desperationCenter + Main.rand.NextVector2Circular(ArenaRadius - 100f, ArenaRadius - 100f);
                activeGeysers.Add(new ShadeGeyser(geyserPos, 0, 45, 800f, 36f));
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f }, geyserPos);
            }

            if (timer >= 200)
            {
                SelectNextAttack(npc, phase);
            }
        }
        #endregion

        #region Death & Despawn Logic
        /// <summary>
        /// State 10: Death animation. Channels shockwaves and explodes in gore.
        /// </summary>
        private void ExecuteState_DeathAnimation(NPC npc, ref float timer)
        {
            npc.damage = 0;
            npc.velocity *= 0.85f;
            npc.rotation += 0.15f * (timer / 60f);

            // Distort screen and flash
            if (timer == 1)
            {
                SoundEngine.PlaySound(RoarSound, npc.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 25f;
                EmitShadowFlameBurst(npc.Center, 22f, 48);
            }

            // Create heavy explosions of particles
            if (timer % 5 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                EmitShadowFlameBurst(npc.Center + Main.rand.NextVector2Circular(60f, 60f), 8f, 15);
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
        /// Despawns the boss by dropping down rapidly and deleting itself when off screen.
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
        /// Updates the timers and layout of custom toxic clouds.
        /// </summary>
        private void UpdateToxicNimbuses(NPC npc)
        {
            for (int i = activeNimbuses.Count - 1; i >= 0; i--)
            {
                ToxicNimbus cloud = activeNimbuses[i];
                cloud.Timer++;
                cloud.Position += cloud.Velocity;

                // Spawn acid droplets from active nimbuses
                if (cloud.Timer % 15 == 0 && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(cloud.Position + Main.rand.NextVector2Circular(24f, 8f), DustID.CursedTorch, Vector2.UnitY * 4f);
                    d.noGravity = true;
                    d.scale = 1.2f;
                }

                // Delete older clouds
                if (cloud.Timer >= 240)
                {
                    activeNimbuses.RemoveAt(i);
                }
                else
                {
                    activeNimbuses[i] = cloud;
                }
            }
        }

        /// <summary>
        /// Updates the timer and width of custom vertical geysers.
        /// </summary>
        private void UpdateShadeGeysers(NPC npc)
        {
            for (int i = activeGeysers.Count - 1; i >= 0; i--)
            {
                ShadeGeyser geyser = activeGeysers[i];
                geyser.Timer++;

                // Trigger geyser blast on count end
                if (geyser.Timer == geyser.MaxTimer)
                {
                    SoundEngine.PlaySound(SoundID.Item14, geyser.Position);
                    EmitShadowFlameBurst(geyser.Position, 10f, 18);

                    // Vertical eruption dust
                    for (int j = 0; j < 30; j++)
                    {
                        Vector2 dVel = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-14f, -4f));
                        Vector2 dPos = geyser.Position + new Vector2(Main.rand.NextFloat(-geyser.Width, geyser.Width), -Main.rand.NextFloat(geyser.Height));
                        Dust d = Dust.NewDustPerfect(dPos, DustID.CursedTorch, dVel, 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                        d.noGravity = true;
                    }
                }

                // Delete geysers that finished blasting
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
        /// Updates the movement and angles of custom particles.
        /// </summary>
        private void UpdateParticles()
        {
            for (int i = activeParticles.Count - 1; i >= 0; i--)
            {
                GravityParticle p = activeParticles[i];
                p.Position += p.Velocity;
                p.Angle += 0.05f;

                // Gravitational pull force scaling
                float distToCenter = Vector2.Distance(p.Position, desperationCenter);
                if (distToCenter < 30f)
                {
                    activeParticles.RemoveAt(i);
                }
                else
                {
                    // Swirl orbit force
                    Vector2 pullDir = SafeNormalize(desperationCenter - p.Position, Vector2.Zero);
                    Vector2 orbitDir = pullDir.RotatedBy(PiOver2);
                    p.Velocity = Vector2.Lerp(p.Velocity, pullDir * 5f + orbitDir * 3f, 0.08f);

                    activeParticles[i] = p;
                }
            }
        }
        #endregion

        #region Visual Overlay & Custom Drawing
        /// <summary>
        /// Draws trail afterimages, prediction paths, nimbuses, and the desperation toxic boundary ring.
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPos = npc.Center - Main.screenPosition;

            // Frame calculations matching default sprite layout
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
                Color trailColor = Color.Lerp(Color.MediumPurple, Color.LimeGreen, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.08f)) * opacity;
                spriteBatch.Draw(texture, histPos - Main.screenPosition, frameRectangle, npc.GetAlpha(trailColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            // 2. Draw telegraph line paths
            if (drawPredictionPath && dashPredictionPath.Count > 1)
            {
                for (int i = 0; i < dashPredictionPath.Count - 1; i++)
                {
                    PredictionPoint p1 = dashPredictionPath[i];
                    PredictionPoint p2 = dashPredictionPath[i + 1];
                    Color pathColor = Color.LimeGreen * p1.Alpha * 0.7f;
                    DrawLine(spriteBatch, p1.Position, p2.Position, pathColor, 4f);
                }
            }

            // 3. Draw shade geysers warnings and blast zones
            if (activeGeysers.Count > 0)
            {
                for (int i = 0; i < activeGeysers.Count; i++)
                {
                    ShadeGeyser geyser = activeGeysers[i];
                    float prog = (float)geyser.Timer / geyser.MaxTimer;

                    if (prog < 1f)
                    {
                        // Drawing warn telegraph column
                        Color c = (geyser.Timer % 8 < 4) ? Color.Purple * 0.6f : Color.Lime * 0.3f;
                        DrawLine(spriteBatch, geyser.Position, geyser.Position - Vector2.UnitY * geyser.Height, c, geyser.Width * 0.5f);
                    }
                    else
                    {
                        // Drawing blast geyser column
                        float blastFade = 1f - ((geyser.Timer - geyser.MaxTimer) / 15f);
                        Color c = Color.Lerp(Color.LimeGreen, Color.Purple, 0.5f) * blastFade * 0.9f;
                        DrawLine(spriteBatch, geyser.Position, geyser.Position - Vector2.UnitY * geyser.Height, c, geyser.Width * 1.5f);
                    }
                }
            }

            // 4. Draw toxic nimbuses
            if (activeNimbuses.Count > 0)
            {
                for (int i = 0; i < activeNimbuses.Count; i++)
                {
                    ToxicNimbus cloud = activeNimbuses[i];
                    float pulse = 1f + 0.1f * (float)Math.Sin(ticksRunning * 0.1f + i);
                    Color c = Color.Lerp(Color.Purple, Color.DarkSlateGray, 0.4f) * 0.65f;
                    spriteBatch.Draw(TextureAssets.BlackTile.Value, cloud.Position - Main.screenPosition - new Vector2(24f, 12f) * pulse, new Rectangle(0, 0, 1, 1), c, 0f, Vector2.Zero, new Vector2(48f, 24f) * pulse, SpriteEffects.None, 0f);
                }
            }

            // 5. Draw desperation arena toxic boundary
            if (arenaAlpha > 0f)
            {
                DrawToxicBoundary(spriteBatch, desperationCenter, ArenaRadius, arenaAlpha);
            }

            // 6. Draw gravity swirling particles
            if (activeParticles.Count > 0)
            {
                for (int i = 0; i < activeParticles.Count; i++)
                {
                    GravityParticle p = activeParticles[i];
                    spriteBatch.Draw(TextureAssets.BlackTile.Value, p.Position - Main.screenPosition - new Vector2(3f, 3f), new Rectangle(0, 0, 1, 1), p.DrawColor * 0.8f, p.Angle, Vector2.Zero, new Vector2(6f, 6f) * p.Scale, SpriteEffects.None, 0f);
                }
            }

            // 7. Draw core main boss texture
            spriteBatch.Draw(texture, drawPos, frameRectangle, npc.GetAlpha(drawColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            return false;
        }

        /// <summary>
        /// PostDraw overlay rendering (glowing core pulse and shadow flamed borders).
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
            Color glowColor = Color.Lerp(Color.Purple, Color.LimeGreen, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.06f)) * 0.5f * npc.Opacity;
            spriteBatch.Draw(texture, drawPos, frameRectangle, glowColor, npc.rotation, origin, npc.scale * pulseScale, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Draws the circular desperation toxic boundary ring.
        /// </summary>
        private void DrawToxicBoundary(SpriteBatch spriteBatch, Vector2 center, float radius, float alpha)
        {
            int segments = 64;
            float pulse = 1f + 0.05f * (float)Math.Sin(ticksRunning * 0.08f);
            Color boundaryColor = Color.Lerp(Color.Purple, Color.Lime, 0.5f + 0.5f * (float)Math.Sin(ticksRunning * 0.05f)) * alpha * 0.8f;

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
        /// Draws a vector line utilizing BlackTile asset.
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
        /// Animates frame offsets matching Calamity's 16-frame P2 layouts.
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
        /// Spawns a ring of shadowflame dust.
        /// </summary>
        private static void EmitShadowFlameBurst(Vector2 center, float speed, int count)
        {
            float step = TwoPi / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                Vector2 vel = angle.ToRotationVector2() * speed;
                Dust d = Dust.NewDustPerfect(center, DustID.Shadowflame, vel, 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
        }

        /// <summary>
        /// Spawns perfect indicator dust particles.
        /// </summary>
        private static void EmitCursedDustPerfect(Vector2 position, Vector2 velocity, Color color, float scale)
        {
            Dust d = Dust.NewDustPerfect(position, DustID.CursedTorch, velocity, 100, color, scale);
            d.noGravity = true;
        }

        /// <summary>
        /// Broadcasts messages to the game chat interface.
        /// </summary>
        private static void BroadcastAlert(string text)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(175, 75, 255));
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, new Color(175, 75, 255));
            }
        }
        #endregion

        #region Extended Theoretical Design Principles
        /*
         * HIVE MIND ULTIMATE MODE BEHAVIOR OVERRIDE DESIGN PRINCIPLES:
         * 
         * 1. The ElasticRecoil Suspension Model
         *    The SuspensionDrift state handles target following with an overlayed physical reaction system.
         *    When hit, we apply:
         *    - Vector2 recoil = -SafeNormalize(target.Center - npc.Center) * RecoilIntensity
         *    - velocity = recoil
         *    - On subsequent frames, we apply friction decay (0.94f) and a restorative spring force:
         *      restoringForce = (desiredPosition - Center) * stiffness
         *      velocity += restoringForce
         *    This renders a highly responsive, liquid-like movement profile matching the "collective hive tissues" lore.
         * 
         * 2. Multiphase Synchronous State Machine
         *    We use a unified double-array mapping or static routing logic inside SelectNextAttack:
         *    - P1 focuses on minion setups (NPCSpawnArc) and windup charges (SpinLunge).
         *    - P2 transitions to battlefield zoning (CloudDash with rain, and EaterOfSoulsWall).
         *    - P3 ramps up vertical hazard constraints (UndergroundFlameDash, CursedRainStorm).
         *    - P4 introduces structural confinement (DesperationArena) forcing the player to dodge highly accelerated
         *      fans and bursts in close proximity.
         * 
         * 3. Graphic Layering and Custom BlendStates
         *    In order to create the shadowflame ethereal aura, SpriteBatch is temporarily stopped and restarted with 
         *    BlendState.Additive. This allows the core aura pulse and segments to overlay their color values,
         *    rendering bright glowing borders rather than hard pixel edges.
         * 
         * 4. Desperation Constraints
         *    The desperation boundary restricts the arena to an 700-unit circle centered on the boss.
         *    Moving outside this zone subjects the challenger to instant frostbite, dealing percentage-based damage
         *    and slowing velocity. This forces close-quarters combat during the final phase.
         */
        #endregion
    }
}
