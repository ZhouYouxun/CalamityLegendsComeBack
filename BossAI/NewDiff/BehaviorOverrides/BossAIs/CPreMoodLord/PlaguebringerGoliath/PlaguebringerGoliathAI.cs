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

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage3.PlaguebringerGoliath
{
    internal sealed class PlaguebringerGoliathAI : LegendsBossAI
    {
        #region Constants & Configurations
        public override int NPCType => ModContent.Find<ModNPC>("CalamityMod/PlaguebringerGoliath").Type;
        public override string BossName => "Plaguebringer Goliath";
        public override Color DebugColor => new(88, 210, 60);

        public override int MaxPhaseCount => 6;
        public override float[] PhaseLifeRatios => new[] { 0.90f, 0.70f, 0.50f, 0.30f, 0.12f };
        public override int AttackCycleLength => 120;
        public override float MotionIntensity => 0.9f;

        private static readonly SoundStyle ShieldRegenSound = new("CalamityMod/Sounds/Custom/CryogenShieldRegenerate");
        private static readonly SoundStyle ShieldBreakSound = SoundID.NPCDeath52;
        #endregion

        #region Attack States
        public enum AttackState
        {
            Virulence = 0,
            Malevolence = 1,
            PlagueStaff = 2,
            FuelCellBundle = 3,
            InfectedRemote = 4,
            TheSyringe = 5,
            TheHive = 6,
            PestilentDefiler = 7,
            Malachite = 8,
            BlightSpewer = 9,
            Pandemic = 10,
            PlagueTaintedSMG = 11,
            OverloadTransition = 12,
            DeathAnimation = 13
        }
        #endregion

        #region Fields
        private int ticksRunning = 0;
        private int currentRepetition = 0;
        private readonly Vector2[] oldPositions = new Vector2[14];
        private int oldPositionsIndex;

        // Attack rotation persists across HP-threshold checkpoints within the same form; only the P1->P2
        // unseal (the one real transition, at 50%) resets it. This is the same fix Cryogen needed: without
        // it, any threshold crossing was wiping progress and stranding the fight on the first 1-2 attacks.
        private int attackCycleIndex = 0;
        private static readonly AttackState[] P1Cycle =
        {
            AttackState.Virulence, AttackState.Malevolence, AttackState.PlagueStaff,
            AttackState.FuelCellBundle, AttackState.InfectedRemote, AttackState.TheSyringe,
        };
        private static readonly AttackState[] P2Cycle =
        {
            AttackState.TheHive, AttackState.PestilentDefiler, AttackState.Malachite,
            AttackState.BlightSpewer, AttackState.Pandemic, AttackState.PlagueTaintedSMG,
        };

        // Shield parameters
        private bool shieldActive = true;
        private int shieldStunTimer = 0;
        private int shieldRegenTimer = 0;
        private int shieldFxCooldown = 0;

        // Arena steam variables. Lane coordinates LOCK at warning start (design doc) — the old version read
        // the player's position again at detonation, which made the steam untellably track you to the last frame.
        private int steamWarnTimer = 0;
        private int activeSteamAxis = -1; // -1: none, 0: horizontal, 1: vertical, 2: both
        private float steamWarnOpacity = 0f;
        private float steamLaneX = 0f;
        private float steamLaneY = 0f;

        // The greenhouse cage is a PLACE (ToxicHeart anchor per design doc), not a halo glued to the boss.
        private Vector2 arenaCenter = Vector2.Zero;
        private bool centerSet = false;

        // Per-attack A/B variant toggle: flips deterministically each time that attack comes up (no RNG).
        private readonly bool[] attackVariant = new bool[13];
        private bool UseVariantB(AttackState state)
        {
            int i = (int)state;
            bool v = attackVariant[i];
            attackVariant[i] = !v;
            return v;
        }
        private bool currentVariantB = false;

        // Syringe 0.5s red lock-line (design doc requirement)
        private Vector2 syringeAimDir = Vector2.Zero;
        private float syringeLineBright = 0f;

        // Visual fields
        private float armorDither = 0f;
        private float transitionFlashAlpha = 0f;
        private int arenaHurtCooldown = 0;

        // Jungle-biome leash (same pattern as Cryogen's snow leash) — Plaguebringer Goliath's real home biome
        // in Calamity is the Jungle (see base PlaguebringerGoliath.cs: `!player.ZoneJungle` gates its own
        // biomeEnrageTimer). Ours had nothing; a player could drag the fight anywhere with zero penalty.
        private int outOfBiomeTimer = 0;
        private float enrageSpeedMultiplier = 1f;
        private bool wasEnraged = false;

        // Per-attack scratch state
        private int malevolenceSide = 1;
        private float staffTargetX, staffTargetY;
        private float syringeSwingDir = 1f;
        private Vector2 defilerAimDir = Vector2.UnitY;
        #endregion

        #region Core AI Hooks
        public override bool PreAI(NPC npc, LegendsGlobalNPC data)
        {
            if ((int)npc.ai[0] == 0)
                ResetFightState();

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

            // Initialize Phase
            if (currentPhase == 0)
            {
                currentPhase = 1;
                npc.ai[0] = 1f;
                state = AttackState.Virulence;
                npc.ai[1] = (float)state;
                currentRepetition = 0;
                attackCycleIndex = 0;
                currentVariantB = UseVariantB(state);
                npc.netUpdate = true;
            }

            if (!centerSet)
            {
                arenaCenter = target.Center;
                centerSet = true;
            }
            arenaCenter = Vector2.Lerp(arenaCenter, target.Center, 0.005f);

            // Only the ONE real transition matters: crossing from P1 (<=3) into P2 (>=4) at 50% HP.
            // The 90/70/30/12 thresholds are silent bookkeeping (used for VFX intensity only) — they must
            // NOT interrupt the attack cycle. The previous version fired the full transition performance
            // (and force-reset the rotation to TheHive) on EVERY threshold, which is why the fight almost
            // never played Malevolence/PlagueStaff/FuelCellBundle/InfectedRemote/TheSyringe at all.
            float lifeRatio = npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax;
            int nextPhase = 1;
            for (int i = 0; i < PhaseLifeRatios.Length; i++)
            {
                if (lifeRatio <= PhaseLifeRatios[i])
                    nextPhase = i + 2;
            }

            bool wasP1 = currentPhase <= 3;
            bool willBeP2 = nextPhase >= 4;
            if (nextPhase > currentPhase)
            {
                currentPhase = nextPhase;
                npc.ai[0] = currentPhase;

                if (wasP1 && willBeP2 && state != AttackState.OverloadTransition)
                {
                    state = AttackState.OverloadTransition;
                    npc.ai[1] = (float)state;
                    timer = 0;
                    stateTracker = 0;
                }
                npc.netUpdate = true;
            }

            // Jungle leash — must run before movement so the speed multiplier is current this frame
            UpdateBiomeEnrage(npc, target);

            // Greenhouse Boundary Arena (1400px in P1-P3, 1000px in P4-P6). Square check matching the drawn
            // frame, anchored to the ToxicHeart arena center (was a circle glued to the boss). Damage throttled.
            float borderSize = currentPhase <= 3 ? 1400f : 1000f;
            if (Main.netMode != NetmodeID.Server)
            {
                Player arenaPlayer = Main.LocalPlayer;
                Vector2 dist = arenaPlayer.Center - arenaCenter;
                if (arenaHurtCooldown > 0)
                    arenaHurtCooldown--;
                if (Math.Abs(dist.X) > borderSize / 2f || Math.Abs(dist.Y) > borderSize / 2f)
                {
                    arenaPlayer.velocity += SafeNormalize(arenaCenter - arenaPlayer.Center, Vector2.Zero) * 2f;
                    if (arenaHurtCooldown <= 0)
                    {
                        arenaHurtCooldown = 30;
                        arenaPlayer.AddBuff(BuffID.Poisoned, 180);
                        arenaPlayer.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 15, 0);
                    }
                }
            }

            // Update steam vents every 6 seconds (360 frames)
            UpdateGreenhouseSteam(npc, target, borderSize);

            // Update Nano-Drone Grid Shield
            UpdateDroneShield(npc, currentPhase);

            // Armor readability: a faint, intermittent green dither lets players see whether the
            // drone shield is still supplying armor without competing with attack telegraphs.
            armorDither = MathHelper.Lerp(armorDither, shieldActive ? 1f : 0f, 0.08f);
            if (Main.netMode != NetmodeID.Server && armorDither > 0.08f && Main.rand.NextFloat() < armorDither * 0.12f)
            {
                Dust armorSpark = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(78f, 62f), DustID.GreenTorch, Main.rand.NextVector2Circular(0.9f, 0.9f), 135, default, 0.8f + armorDither * 0.45f);
                armorSpark.noGravity = true;
            }

            // Visual oscillations and breathing
            if (state != AttackState.DeathAnimation)
            {
                npc.rotation = npc.velocity.X * 0.03f;
                npc.scale = 1f + (float)Math.Sin(ticksRunning * 0.05f) * 0.02f;
            }

            if (shieldFxCooldown > 0)
                shieldFxCooldown--;

            // Execute state machine
            switch (state)
            {
                case AttackState.Virulence:
                    ExecuteVirulence(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Malevolence:
                    ExecuteMalevolence(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.PlagueStaff:
                    ExecutePlagueStaff(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.FuelCellBundle:
                    ExecuteFuelCell(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.InfectedRemote:
                    ExecuteInfectedRemote(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.TheSyringe:
                    ExecuteTheSyringe(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.TheHive:
                    ExecuteTheHive(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.PestilentDefiler:
                    ExecutePestilentDefiler(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Malachite:
                    ExecuteMalachite(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.BlightSpewer:
                    ExecuteBlightSpewer(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.Pandemic:
                    ExecutePandemic(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.PlagueTaintedSMG:
                    ExecutePlagueTaintedSMG(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.OverloadTransition:
                    ExecuteOverloadTransition(npc, target, ref timer, ref stateTracker, currentPhase);
                    break;
                case AttackState.DeathAnimation:
                    ExecuteDeathAnimation(npc, target, ref timer);
                    break;
            }

            return false;
        }

        private void ResetFightState()
        {
            ticksRunning = 0;
            currentRepetition = 0;
            Array.Clear(oldPositions, 0, oldPositions.Length);
            oldPositionsIndex = 0;
            attackCycleIndex = 0;
            shieldActive = true;
            shieldStunTimer = 0;
            shieldRegenTimer = 0;
            shieldFxCooldown = 0;
            steamWarnTimer = 0;
            activeSteamAxis = -1;
            steamWarnOpacity = 0f;
            steamLaneX = 0f;
            steamLaneY = 0f;
            arenaCenter = Vector2.Zero;
            centerSet = false;
            Array.Clear(attackVariant, 0, attackVariant.Length);
            currentVariantB = false;
            syringeAimDir = Vector2.Zero;
            syringeLineBright = 0f;
            armorDither = 0f;
            transitionFlashAlpha = 0f;
            arenaHurtCooldown = 0;
            outOfBiomeTimer = 0;
            enrageSpeedMultiplier = 1f;
            wasEnraged = false;
            malevolenceSide = 1;
            staffTargetX = 0f;
            staffTargetY = 0f;
            syringeSwingDir = 1f;
            defilerAimDir = Vector2.UnitY;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, player));
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
            InterceptLethalHit(npc, ref modifiers, (int)AttackState.DeathAnimation, () => BeginDeathAnimation(npc, Main.player[projectile.owner]));
        }

        private void ApplyDefenseModifiers(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.ai[1] == (float)AttackState.OverloadTransition)
            {
                modifiers.FinalDamage *= 0f; // fully invulnerable during the transformation cutscene
                return;
            }

            if (shieldActive)
            {
                modifiers.FinalDamage *= 0.10f; // 90% DR
                if (shieldFxCooldown <= 0)
                {
                    shieldFxCooldown = 10;
                    SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.6f, Pitch = 0.2f }, npc.Center);
                }
            }
        }
        #endregion

        #region Jungle Enrage
        private void UpdateBiomeEnrage(NPC npc, Player target)
        {
            const int graceFrames = 300; // matches CalamityGlobalNPC.biomeEnrageTimerMax
            const float maxMultiplier = 1.4f;

            if (!target.ZoneJungle)
                outOfBiomeTimer = Math.Min(outOfBiomeTimer + 1, graceFrames + 120);
            else
                outOfBiomeTimer = Math.Max(outOfBiomeTimer - 3, 0);

            bool enraged = outOfBiomeTimer >= graceFrames;
            npc.Calamity().CurrentlyEnraged = enraged;
            enrageSpeedMultiplier = MathHelper.Lerp(enrageSpeedMultiplier, enraged ? maxMultiplier : 1f, 0.04f);

            if (enraged && !wasEnraged)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.3f, Volume = 0.6f }, npc.Center);
                PlagueFx.Burst(npc.Center, 5f, 30);
            }
            wasEnraged = enraged;
        }

        // Instance method (not static) so the enrage multiplier can scale every hover call through one choke point.
        private void SmoothHover(NPC npc, Vector2 desiredPosition, float acceleration, float maxSpeed)
        {
            maxSpeed *= enrageSpeedMultiplier;
            Vector2 desiredVelocity = (desiredPosition - npc.Center) * acceleration;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * maxSpeed;
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.13f);
        }

        // Anti-cheese hover spot: offsets to whichever side the boss is already on and leads the target's
        // movement a little, so it never just camps directly overhead for an entire attack's duration.
        private static Vector2 DirectedHoverSpot(NPC npc, Player target, float sideOffset, float heightOffset, float lead = 0f)
        {
            float side = Math.Sign(npc.Center.X - target.Center.X);
            if (side == 0f)
                side = Main.rand.NextBool() ? 1f : -1f;
            Vector2 predicted = target.Center + target.velocity * lead;
            return predicted + new Vector2(side * sideOffset, heightOffset);
        }
        #endregion

        #region Vents & Shield Helper Logic
        private void UpdateGreenhouseSteam(NPC npc, Player target, float borderSize)
        {
            steamWarnTimer++;
            if (steamWarnTimer >= 360)
            {
                steamWarnTimer = 0;
                activeSteamAxis = Main.rand.Next(3); // 0: horizontal, 1: vertical, 2: cross
                steamWarnOpacity = 1f;
                // LOCK the lanes now — the whole point of the 1.2s warning is that the player can leave them
                steamLaneX = target.Center.X;
                steamLaneY = target.Center.Y;
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.3f }, npc.Center);
            }

            // Vent hiss: dust seeps from the locked lanes while the warning flashes
            if (steamWarnOpacity > 0f && Main.rand.NextBool(2))
            {
                if (activeSteamAxis == 0 || activeSteamAxis == 2)
                {
                    Vector2 pos = new(arenaCenter.X + Main.rand.NextFloat(-borderSize, borderSize) / 2f, steamLaneY);
                    Dust d = Dust.NewDustPerfect(pos, DustID.CursedTorch, new Vector2(0f, Main.rand.NextFloat(-1f, 1f)), 120, default, 1f);
                    d.noGravity = true;
                }
                if (activeSteamAxis == 1 || activeSteamAxis == 2)
                {
                    Vector2 pos = new(steamLaneX, arenaCenter.Y + Main.rand.NextFloat(-borderSize, borderSize) / 2f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.CursedTorch, new Vector2(Main.rand.NextFloat(-1f, 1f), 0f), 120, default, 1f);
                    d.noGravity = true;
                }
            }

            if (steamWarnOpacity > 0f)
            {
                steamWarnOpacity -= 0.0139f; // warning lasts 1.2s (72 frames)
                if (steamWarnOpacity <= 0f)
                {
                    int dmg = npc.damage / 3;
                    SoundEngine.PlaySound(SoundID.Item34, target.Center);
                    target.Calamity().GeneralScreenShakePower = 5f;

                    if (activeSteamAxis == 0 || activeSteamAxis == 2)
                    {
                        for (float x = -borderSize / 2f; x < borderSize / 2f; x += 90f)
                        {
                            Vector2 pos = new(arenaCenter.X + x, steamLaneY);
                            SpawnHostile(npc, pos, new Vector2(0f, 0f), "Projectiles/Boss/PlagueCloud", dmg);
                        }
                    }
                    if (activeSteamAxis == 1 || activeSteamAxis == 2)
                    {
                        for (float y = -borderSize / 2f; y < borderSize / 2f; y += 90f)
                        {
                            Vector2 pos = new(steamLaneX, arenaCenter.Y + y);
                            SpawnHostile(npc, pos, new Vector2(0f, 0f), "Projectiles/Boss/PlagueCloud", dmg);
                        }
                    }
                    activeSteamAxis = -1;
                }
            }
        }

        private void UpdateDroneShield(NPC npc, int currentPhase)
        {
            if (currentPhase > 3)
            {
                shieldActive = false;
                return;
            }

            if (shieldActive)
            {
                bool droneAlive = false;
                int droneType = ModContent.Find<ModNPC>("CalamityMod/PlagueChargerLarge").Type;
                int activeDroneIndex = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == droneType && Main.npc[i].ai[0] == npc.whoAmI)
                    {
                        droneAlive = true;
                        float angle = activeDroneIndex * MathHelper.TwoPi / 6f + ticksRunning * 0.02f;
                        Main.npc[i].Center = npc.Center + angle.ToRotationVector2() * 160f;
                        Main.npc[i].velocity = Vector2.Zero;
                        activeDroneIndex++;
                    }
                }

                if (!droneAlive)
                {
                    shieldActive = false;
                    shieldStunTimer = 480; // 8 seconds stun
                    npc.velocity = new Vector2(0, 1.5f);
                    SoundEngine.PlaySound(ShieldBreakSound, npc.Center);
                    Player t = Main.player[npc.target];
                    if (t.active)
                        t.Calamity().GeneralScreenShakePower = 8f;
                    PlagueFx.Burst(npc.Center, 6f, 30);
                }
            }
            else
            {
                if (shieldStunTimer > 0)
                {
                    // 瘫痪易伤 — wings smoking, engine sputtering, sagging 100px (design doc)
                    shieldStunTimer--;
                    npc.velocity.X *= 0.95f;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.04f, -2f, 1.2f);
                    npc.defense = 0;
                    npc.rotation = npc.velocity.X * 0.03f + (float)Math.Sin(ticksRunning * 0.25f) * 0.06f;
                    if (Main.rand.NextBool(2))
                    {
                        Dust smoke = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(90f, 50f), DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1.5f, 3f)), 120, default, 1.6f);
                        smoke.noGravity = true;
                        Dust spark = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 40f), DustID.CursedTorch, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.1f);
                        spark.noGravity = true;
                    }
                    if (shieldStunTimer == 0)
                    {
                        shieldRegenTimer = 1500; // 25s weak period before regeneration
                        // 辐射针雨 on recovery (design doc): a full ring of radiation needles
                        SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.2f }, npc.Center);
                        PlagueFx.Burst(npc.Center, 7f, 30);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                Vector2 vel = (i * MathHelper.TwoPi / 16f).ToRotationVector2() * 9f;
                                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<PlagueTaintedBulletProj>(), npc.defDamage / 3, 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                else if (shieldRegenTimer > 0)
                {
                    shieldRegenTimer--;
                    if (shieldRegenTimer == 0)
                    {
                        shieldActive = true;
                        SoundEngine.PlaySound(ShieldRegenSound, npc.Center);
                        PlagueFx.Burst(npc.Center, 4f, 24);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.Find<ModNPC>("CalamityMod/PlagueChargerLarge").Type;
                            for (int i = 0; i < 6; i++)
                            {
                                int minion = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, type);
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

        #region Attack Rotation
        // Rotation persists within a form (P1 or P2); only crossing 50% resets it (new arsenal debuts in order).
        // P1 keeps the design doc's explicit "each weapon fires 3 full times before rotating" rule; P2 has none.
        private void RotateAttack(NPC npc, int currentPhase, AttackState current)
        {
            CleanupHeldWeapons(npc);
            syringeLineBright = 0f;
            if (currentPhase <= 3)
            {
                currentRepetition++;
                if (currentRepetition < 3)
                {
                    // Same weapon again, but the A/B read flips so 3 reps never feel like 3 copies
                    currentVariantB = UseVariantB(current);
                    npc.ai[2] = 0;
                    npc.ai[3] = 0;
                    npc.netUpdate = true;
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
            npc.ai[2] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;
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

        // Plague-charge shimmer: green nano-dust drawn into the machine before a volley — every attack telegraphs.
        private static void ChargeNanites(NPC npc, int density = 2)
        {
            if (!Main.rand.NextBool(density))
                return;
            Vector2 around = npc.Center + Main.rand.NextVector2CircularEdge(110f, 110f);
            Dust d = Dust.NewDustPerfect(around, DustID.CursedTorch, (npc.Center - around) * 0.08f, 100, default, 1.2f);
            d.fadeIn = 1.2f;
            d.noGravity = true;
        }
        #endregion

        #region State Machine Implementations

        // P1 Attack 1: Virulence — 挥砍分裂型 · 巨剑斩出缓慢毒波, 滑行120像素后裂变成6枚弱追踪微波.
        // 变体A: 单波正斩; 变体B: 双波±14°交叉, 两团裂变从两个方向合拢.
        private void ExecuteVirulence(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer < 40)
            {
                Vector2 spot = DirectedHoverSpot(npc, target, 260f, -280f, 8f);
                SmoothHover(npc, spot, 0.06f, 15f);
            }
            else if (timer < 60)
            {
                npc.velocity *= 0.94f; // settle: the greatsword heaves back in stillness
                ChargeNanites(npc);
            }

            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldVirulence>(), npc.damage / 2, 0f, Main.myPlayer, npc.whoAmI, Math.Sign((target.Center - npc.Center).X));

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (currentVariantB)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(-0.24f) * 6f, ModContent.ProjectileType<VirulentWaveProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir.RotatedBy(0.24f) * 6f, ModContent.ProjectileType<VirulentWaveProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                else
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 6f, ModContent.ProjectileType<VirulentWaveProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<PlagueHeldVirulence>(), npc.damage / 2, 0f, Main.myPlayer, npc.whoAmI, Math.Sign(dir.X));
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.15f }, npc.Center);
                PlagueFx.Burst(npc.Center + dir * 60f, 4f, 10);
                npc.velocity -= dir * 4f; // swing recoil
            }

            if (timer > 60)
                SmoothHover(npc, DirectedHoverSpot(npc, target, 280f, -260f, 8f), 0.05f, 10f);

            if (timer >= 160)
                RotateAttack(npc, phase, AttackState.Virulence);
        }

        // P1 Attack 2: Malevolence — 处刑箭雨型 · 8箭升空悬停, 再化作横向激光式行刑射击.
        private void ExecuteMalevolence(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                malevolenceSide = Math.Sign(npc.Center.X - target.Center.X);
                if (malevolenceSide == 0) malevolenceSide = 1;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<PlagueHeldMalevolence>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            Vector2 spot = DirectedHoverSpot(npc, target, 350f, -240f, 6f);
            SmoothHover(npc, spot, 0.06f, 14f);

            if (timer > 24 && timer < 40)
            {
                npc.velocity *= 0.94f; // bowstring hum — the volley is coming
                ChargeNanites(npc, 1);
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 8; i++)
                {
                    // 变体A: 全部从同一侧行刑; 变体B: 左右交替进场, 上下跳跃的节奏被打成之字
                    float side = currentVariantB ? (i % 2 == 0 ? -1f : 1f) : malevolenceSide;
                    Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-300f, 300f), -480f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawnPos, new Vector2(0f, -6f), ModContent.ProjectileType<PlagueArrowProj>(), npc.damage / 3, 0f, Main.myPlayer, side, i * 8f);
                }
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.5f }, npc.Center);
                PlagueFx.Burst(npc.Center, 4f, 8);
                FindHeldWeapon<PlagueHeldMalevolence>(npc)?.Pulse(-14f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Malevolence);
        }

        // P1 Attack 3: Plague Staff — 三角合围型 · 三点凝聚45帧后同时冲向中心, 撞击粉碎成12发散射.
        private void ExecutePlagueStaff(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -Vector2.UnitY, ModContent.ProjectileType<PlagueHeldStaff>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -260f, 6f);
            SmoothHover(npc, spot, 0.05f, 13f);

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // 变体A: 正三角锁当前位; 变体B: 倒三角锁预判位 — 站桩和无脑直线跑都会被咬到
                Vector2 anchor = currentVariantB ? target.Center + target.velocity * 20f : target.Center;
                staffTargetX = anchor.X;
                staffTargetY = anchor.Y;
                Vector2[] offsets = currentVariantB
                    ? new Vector2[] { new(-240f, -240f), new(240f, -240f), new(0f, 320f) }
                    : new Vector2[] { new(-240f, 240f), new(240f, 240f), new(0f, -320f) };
                foreach (Vector2 off in offsets)
                {
                    Vector2 spawn = anchor + off;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<PlagueFangProj>(), npc.damage / 3, 0f, Main.myPlayer, staffTargetX, staffTargetY);
                    // Sigil condensation flare at each anchor
                    for (int k = 0; k < 5; k++)
                    {
                        Dust d = Dust.NewDustPerfect(spawn, DustID.CursedTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 1.25f);
                        d.fadeIn = 1.2f;
                        d.noGravity = true;
                    }
                }
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, npc.Center);
                FindHeldWeapon<PlagueHeldStaff>(npc)?.Pulse(-12f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.PlagueStaff);
        }

        // P1 Attack 4: Fuel Cell Bundle — 沸腾酸池型 · 抛掷燃料瓶, 破裂形成持续冒泡的地面酸池.
        private void ExecuteFuelCell(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldFuelCell>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -340f, 8f);
            SmoothHover(npc, spot, 0.06f, 12f);

            if (timer == 50 || timer == 100 || timer == 150)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel;
                    if (currentVariantB)
                    {
                        // 变体B: 三瓶依次砸向玩家左/中/右, 酸池从两侧夹拢立足点
                        int flaskIndex = (int)(timer - 50) / 50;
                        Vector2 landing = target.Center + new Vector2((flaskIndex - 1) * 240f, 0f);
                        vel = SafeNormalize(landing - npc.Center, Vector2.UnitY) * 8f + new Vector2(0f, -4f);
                    }
                    else
                    {
                        vel = new(Main.rand.NextFloat(-6f, 6f), -6f);
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<FuelCellFlaskProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f }, npc.Center);
                PlagueFx.Burst(npc.Center, 3f, 6);
                npc.velocity += new Vector2(0f, -2.5f); // the toss kicks the frame upward
                FindHeldWeapon<PlagueHeldFuelCell>(npc)?.Pulse(20f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.FuelCellBundle);
        }

        // P1 Attack 5: Infected Remote — 女皇呼唤型 · 召唤 Virili 投影滑翔投掷幼虫.
        private void ExecuteInfectedRemote(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldRemote>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 400f, -260f, 6f);
            SmoothHover(npc, spot, 0.045f, 16f);

            if (timer == 40)
            {
                FindHeldWeapon<PlagueHeldRemote>(npc)?.Pulse(-10f);
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // 变体A: 女皇自左向右高空滑翔; 变体B: 自右向左且更低 — 投弹节奏同型但进场方向反转
                    float glideDir = currentVariantB ? -1f : 1f;
                    float altitude = currentVariantB ? -240f : -320f;
                    int virili = NPC.NewNPC(npc.GetSource_FromAI(), (int)(target.Center.X - glideDir * 500f), (int)(target.Center.Y + altitude), ModContent.Find<ModNPC>("CalamityMod/PlaguePrincess").Type);
                    if (virili >= 0 && virili < Main.maxNPCs)
                    {
                        Main.npc[virili].velocity = new Vector2(glideDir * 12f, 0f);
                        Main.npc[virili].ai[0] = npc.whoAmI;
                        Main.npc[virili].netUpdate = true;
                    }
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.InfectedRemote);
        }

        // P1 Attack 6: The Syringe — 强力穿刺型 · 0.5秒锁定后标枪投掷, 命中/落地即碎裂成玻璃散弹.
        private void ExecuteTheSyringe(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1)
            {
                syringeSwingDir = Math.Sign(target.Center.X - npc.Center.X);
                if (syringeSwingDir == 0f) syringeSwingDir = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldSyringe>(), 0, 0f, Main.myPlayer, npc.whoAmI);
            }

            if (timer < 20)
            {
                Vector2 spot = DirectedHoverSpot(npc, target, 320f, -200f, 6f);
                SmoothHover(npc, spot, 0.06f, 15f);
            }

            // 0.5s red lock line (design doc): tracking 20-36, frozen flare 36-50, throw at 50
            RunSyringeLock(npc, target, timer, lockStart: 20, fireTime: 50);
            if (currentVariantB)
            {
                if (timer > 60 && timer < 80)
                    SmoothHover(npc, DirectedHoverSpot(npc, target, -340f, -240f, 6f), 0.07f, 16f);
                RunSyringeLock(npc, target, timer, lockStart: 80, fireTime: 110);
            }

            if (timer > 50 && !(currentVariantB && timer > 60 && timer < 110))
                SmoothHover(npc, DirectedHoverSpot(npc, target, 340f, -220f, 6f), 0.05f, 11f);

            if (timer >= (currentVariantB ? 200 : 160))
                RotateAttack(npc, phase, AttackState.TheSyringe);
        }

        private void RunSyringeLock(NPC npc, Player target, float timer, int lockStart, int fireTime)
        {
            int freezeAt = fireTime - 14;
            if (timer >= lockStart && timer < freezeAt)
            {
                npc.velocity *= 0.93f; // the needle steadies — no drift while aiming
                syringeAimDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                syringeLineBright = MathHelper.Lerp(0.25f, 0.6f, (timer - lockStart) / (float)(freezeAt - lockStart));
            }
            else if (timer >= freezeAt && timer < fireTime)
            {
                npc.velocity *= 0.93f;
                syringeLineBright = 1f; // frozen and flaring — this is the dodge cue
            }
            else if (timer == fireTime)
            {
                syringeLineBright = 0f;
                FindHeldWeapon<PlagueHeldSyringe>(npc)?.Pulse(26f);
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.15f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, syringeAimDir * 22f, ModContent.ProjectileType<TheSyringeProj>(), npc.damage / 2, 0f, Main.myPlayer);
                PlagueFx.Burst(npc.Center + syringeAimDir * 50f, 4f, 8);
                npc.velocity -= syringeAimDir * 6f; // javelin recoil
            }
        }

        // P2 Attack 1: The Hive — 缓慢核弹型 · 极慢核弹长时间飘移, 引爆后放射24枚微型导弹.
        private void ExecuteTheHive(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldHive>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 280f, -280f, 8f);
            SmoothHover(npc, spot, 0.06f, 16f);

            if (timer > 40 && timer < 60)
            {
                npc.velocity *= 0.94f; // the heavy cannon braces
                ChargeNanites(npc);
            }

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (currentVariantB)
                {
                    // 变体B: 双核弹±25°扇出, 两团24向导弹雨的引爆点互成犄角
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY).RotatedBy(s * 0.44f) * 2.5f;
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HiveNukeProj>(), npc.damage / 2, 0f, Main.myPlayer);
                    }
                }
                else
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 2.5f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HiveNukeProj>(), npc.damage / 2, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.6f }, npc.Center);
                PlagueFx.Burst(npc.Center, 5f, 12);
                npc.velocity -= SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 7f; // nuke launch recoil
                FindHeldWeapon<PlagueHeldHive>(npc)?.Pulse(-16f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.TheHive);
        }

        // P2 Attack 2: Pestilent Defiler — 纳米正弦波型 · 三轮8发正弦曲线子弹扫射.
        private void ExecutePestilentDefiler(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldDefiler>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 280f, -240f, 8f);
            SmoothHover(npc, spot, 0.06f, 18f);

            // 变体A: 三轮8发宽扇; 变体B: 四轮5发窄束更快节奏 — 同一杆步枪的两种火控
            int[] volleyTimes = currentVariantB ? new[] { 40, 78, 116, 154 } : new[] { 40, 90, 140 };
            foreach (int vt in volleyTimes)
            {
                if (timer == vt)
                {
                    defilerAimDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                    int rounds = currentVariantB ? 5 : 8;
                    float halfSpread = currentVariantB ? 0.11f : 0.2f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < rounds; i++)
                        {
                            Vector2 dir = defilerAimDir.RotatedBy(MathHelper.Lerp(-halfSpread, halfSpread, i / (float)(rounds - 1)));
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<SicknessRoundProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.5f }, npc.Center);
                    // Muzzle flash + rifle pushback
                    for (int k = 0; k < 4; k++)
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center + defilerAimDir * 50f, DustID.CursedTorch, defilerAimDir.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f)) * Main.rand.NextFloat(3f, 6f), 100, default, 1.2f);
                        d.noGravity = true;
                    }
                    npc.velocity -= defilerAimDir * 2f;
                    FindHeldWeapon<PlagueHeldDefiler>(npc)?.Pulse(-8f);
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.PestilentDefiler);
        }

        // P2 Attack 3: Malachite — 悬停回声刃型 · 12枚匕首各自延迟10帧依次锁定并刺出.
        private void ExecuteMalachite(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldMalachite>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -220f, 6f);
            SmoothHover(npc, spot, 0.06f, 20f);

            if (timer > 14 && timer < 30)
            {
                npc.velocity *= 0.94f;
                ChargeNanites(npc, 1); // the daggers ring with metallic resonance as they condense
            }

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 pos;
                    if (currentVariantB)
                    {
                        // 变体B: 十二刃排成玩家头顶的弧幕, 依次垂落刺杀 — 空间题从"环内"变成"幕下"
                        float arc = MathHelper.Lerp(-MathHelper.Pi * 0.75f, -MathHelper.Pi * 0.25f, i / 11f);
                        pos = target.Center + arc.ToRotationVector2() * 320f;
                    }
                    else
                    {
                        float ang = MathHelper.TwoPi * i / 12f;
                        pos = npc.Center + ang.ToRotationVector2() * 90f;
                    }
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<MalachiteDaggerProj>(), npc.damage / 3, 0f, Main.myPlayer, i * 10f + 20f);
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f }, npc.Center);
                PlagueFx.Burst(npc.Center, 4f, 10);
            }

            if (timer >= 30 && (int)(timer - 30) % 10 == 0 && timer <= 150)
                FindHeldWeapon<PlagueHeldMalachite>(npc)?.Pulse(14f);

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Malachite);
        }

        // P2 Attack 4: Blight Spewer — 烈毒火风暴型 · 180°扇形横扫喷吐火舌.
        private void ExecuteBlightSpewer(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldBlightSpewer>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 0f, -320f, 0f);
            SmoothHover(npc, spot, 0.05f, 14f);

            if (timer > 30 && timer < 50 && Main.rand.NextBool(2))
            {
                // Pilot flame licks out of the nozzle before the sweep
                Vector2 fwd = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Dust d = Dust.NewDustPerfect(npc.Center + fwd * 60f, DustID.CursedTorch, fwd * Main.rand.NextFloat(2f, 4f), 100, default, 1.3f);
                d.noGravity = true;
            }

            if (timer >= 50 && timer <= 170 && timer % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // 变体A: 左→右 180° 横扫; 变体B: 右→左且中段留两拍缺口(可穿越的呼吸)
                bool inGap = currentVariantB && timer >= 100 && timer <= 118;
                if (!inGap)
                {
                    float sweep = (timer - 50f) / 120f;
                    float angle = currentVariantB
                        ? MathHelper.Lerp(MathHelper.PiOver2, -MathHelper.PiOver2, sweep)
                        : MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, sweep);
                    Vector2 vel = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 12f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BlightFlameProj>(), npc.damage / 3, 0f, Main.myPlayer);
                    FindHeldWeapon<PlagueHeldBlightSpewer>(npc)?.Pulse(6f);
                    npc.velocity -= vel.SafeNormalize(Vector2.Zero) * 0.3f; // the hose pushes back, slowly
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.BlightSpewer);
        }

        // P2 Attack 5: Pandemic — 死亡双重奏型 · 两枚悠悠球围绕玩家公转, 轨道半径逐渐收缩.
        private void ExecutePandemic(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PlagueHeldPandemic>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -280f, 6f);
            SmoothHover(npc, spot, 0.055f, 15f);

            // Materialization warning at the orbit ring before the yoyos arrive
            if (timer > 28 && timer < 40 && Main.rand.NextBool(2))
            {
                float warnAng = Main.rand.NextFloat(MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(target.Center + warnAng.ToRotationVector2() * (currentVariantB ? 200f : 160f), DustID.CursedTorch, Vector2.Zero, 100, default, 1.25f);
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // 变体A: 双星180°对角, 半径160收缩; 变体B: 三星120°等分, 半径200 — 缝隙更多但更窄
                int count = currentVariantB ? 3 : 2;
                float radius = currentVariantB ? 200f : 160f;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * MathHelper.TwoPi / count;
                    Vector2 pos = target.Center + angle.ToRotationVector2() * radius;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<PandemicYoyoProj>(), npc.damage / 3, 0f, Main.myPlayer, 0f, angle);
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f }, npc.Center);
                FindHeldWeapon<PlagueHeldPandemic>(npc)?.Pulse(-10f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.Pandemic);
        }

        // P2 Attack 6: Plague Tainted SMG — 无人机十字电网型 · 四角无人机架起电网, 本体高频扫射.
        private void ExecutePlagueTaintedSMG(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldSMG>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 300f, -240f, 8f);
            SmoothHover(npc, spot, 0.06f, 18f);

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int type = ModContent.Find<ModNPC>("CalamityMod/PlagueTaintedDrone").Type;
                // 变体A: 四角落位 → X形电网; 变体B: 四边中点落位 → 十字电网(文档的两种网型)
                Vector2[] anchors = currentVariantB
                    ? new Vector2[] { new(0f, -500f), new(500f, 0f), new(0f, 500f), new(-500f, 0f) }
                    : new Vector2[] { new(-500f, -500f), new(500f, -500f), new(-500f, 500f), new(500f, 500f) };
                foreach (Vector2 c in anchors)
                {
                    int minion = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X + (int)c.X, (int)target.Center.Y + (int)c.Y, type);
                    if (minion >= 0 && minion < Main.maxNPCs)
                    {
                        Main.npc[minion].ai[0] = npc.whoAmI;
                        Main.npc[minion].netUpdate = true;
                    }
                }
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f }, npc.Center);
            }

            if (timer >= 80 && timer <= 180 && timer % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<PlagueTaintedBulletProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<PlagueHeldSMG>(npc)?.Pulse(6f);
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.PlagueTaintedSMG);
        }

        private void ExecuteOverloadTransition(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            npc.velocity *= 0.9f;
            npc.dontTakeDamage = true; // untouchable for the whole cutscene — see ApplyDefenseModifiers too

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
                PlagueFx.Burst(npc.Center, 6f, 30);

                int droneType = ModContent.Find<ModNPC>("CalamityMod/PlagueChargerLarge").Type;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == droneType && Main.npc[i].ai[0] == npc.whoAmI)
                        Main.npc[i].active = false;
                }
            }

            if (timer == coreReveal)
            {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.7f }, npc.Center);
                target.Calamity().GeneralScreenShakePower = 12f;
                PlagueFx.Burst(npc.Center, 8f, 40);
            }

            if (timer >= coreReveal + 10)
            {
                transitionFlashAlpha = 0f;
                npc.dontTakeDamage = false;

                attackCycleIndex = -1; // new arsenal debuts in order
                npc.ai[1] = (float)P2Cycle[0];
                npc.ai[2] = 0;
                npc.ai[3] = 0;
                npc.netUpdate = true;
            }
        }
        #endregion

        #region Death Animation
        // 瘟疫终末 — 五段演出, 让护甲/毒气/温室结界身份成为演出主角, 而不是通用爆炸:
        // 护甲腐蚀崩解 -> 毒气失控喷发 -> 温室锚点回收 -> 瘟疫核心过载上腾 -> 终末瘟疫核爆.
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
                // 护甲腐蚀崩解 — the same shell-strip visual as the overload transition, this time for good
                npc.velocity *= 0.9f;
                npc.rotation += MathF.Sin(timer * 1.2f) * 0.1f;
                if ((int)timer % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.GreenFairy, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 70f)
            {
                // 毒气失控喷发 — the greenhouse-steam identity vents wildly instead of on a lane/warning cycle
                npc.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                npc.velocity *= 0.92f;
                if ((int)timer % 2 == 0)
                {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50f, 50f), DustID.PoisonStaff, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.5f)), 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else if (timer < 105f)
            {
                // 温室锚点回收 — the greenhouse arena border rushes fully inward onto the boss itself
                float t = timer - 70f;
                if ((int)t % 3 == 0)
                {
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = npc.Center + a.ToRotationVector2() * MathHelper.Lerp(500f, 20f, t / 35f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GreenFairy, (npc.Center - pos) * 0.06f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else if (timer < 140f)
            {
                // 瘟疫核心过载上腾 — rises while the final overload builds, the cinematic pull peaks here
                npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(0f, -6f), 0.06f);
                float t = timer - 105f;
                if ((int)t % 2 == 0)
                {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2CircularEdge(100f, 100f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.GreenFairy, (npc.Center - spawn) * 0.07f, 100, default, 1.3f);
                    d.noGravity = true;
                }
            }
            else
            {
                // 终末瘟疫核爆 — the actual kill fires once, everything after is the lingering burst
                if (timer == 140f)
                {
                    npc.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = -0.2f }, npc.Center);
                    SoundEngine.PlaySound(SoundID.NPCDeath4, npc.Center);
                    target.Calamity().GeneralScreenShakePower = 13f;
                    PlagueFx.Burst(npc.Center, 8f, 40);
                    PlagueFx.Burst(npc.Center, 5f, 24);
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
                Color trailColor = new Color(88, 210, 60, 0) * alpha;
                spriteBatch.Draw(tex, oldPositions[idx] - screenPos, frame, trailColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            }

            if (transitionFlashAlpha > 0f)
            {
                Color flashColor = Color.White * transitionFlashAlpha;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), flashColor);
            }

            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() / 2f;

            // Draw greenhouse border (anchored to the ToxicHeart arena center, matching the hurt check)
            int currentPhase = (int)npc.ai[0];
            float borderSize = currentPhase <= 3 ? 1400f : 1000f;
            Vector2 tl = arenaCenter + new Vector2(-borderSize / 2f, -borderSize / 2f);
            Vector2 tr = arenaCenter + new Vector2(borderSize / 2f, -borderSize / 2f);
            Vector2 bl = arenaCenter + new Vector2(-borderSize / 2f, borderSize / 2f);
            Vector2 br = arenaCenter + new Vector2(borderSize / 2f, borderSize / 2f);

            LegendsWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.LimeGreen * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.LimeGreen * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.LimeGreen * 0.7f, 4f);
            LegendsWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.LimeGreen * 0.7f, 4f);

            if (activeSteamAxis != -1 && steamWarnOpacity > 0f)
            {
                // Warning lines sit on the LOCKED lanes (they no longer chase the player)
                float pulse = 0.6f + 0.4f * (float)Math.Sin(ticksRunning * 0.45f);
                Color warnColor = Color.LimeGreen * steamWarnOpacity * pulse;
                if (activeSteamAxis == 0 || activeSteamAxis == 2)
                {
                    Vector2 start = new(arenaCenter.X - borderSize / 2f, steamLaneY);
                    Vector2 end = new(arenaCenter.X + borderSize / 2f, steamLaneY);
                    LegendsWeaponBossVisuals.DrawLine(spriteBatch, start, end, warnColor, 3f);
                }
                if (activeSteamAxis == 1 || activeSteamAxis == 2)
                {
                    Vector2 start = new(steamLaneX, arenaCenter.Y - borderSize / 2f);
                    Vector2 end = new(steamLaneX, arenaCenter.Y + borderSize / 2f);
                    LegendsWeaponBossVisuals.DrawLine(spriteBatch, start, end, warnColor, 3f);
                }
            }

            // Syringe lock-line: thin while tracking, flaring when frozen (design doc's 0.5s red lock)
            if (syringeLineBright > 0.05f && syringeAimDir != Vector2.Zero)
            {
                float width = syringeLineBright >= 1f ? 4f : 1.5f;
                Color lockColor = Color.Lerp(Color.Red, Color.White, syringeLineBright >= 1f ? 0.4f : 0f) * (0.35f + syringeLineBright * 0.5f);
                lockColor.A = 0;
                Vector2 lineEnd = npc.Center + syringeAimDir * 1300f;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, (npc.Center + lineEnd) * 0.5f - screenPos, new Rectangle(0, 0, 1, 1), lockColor, syringeAimDir.ToRotation(), new Vector2(0.5f), new Vector2(Vector2.Distance(npc.Center, lineEnd), width), SpriteEffects.None, 0f);
            }

            if (shieldActive)
            {
                int droneType = ModContent.Find<ModNPC>("CalamityMod/PlagueChargerLarge").Type;
                Vector2[] dronePositions = new Vector2[6];
                int droneCount = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == droneType && Main.npc[i].ai[0] == npc.whoAmI)
                    {
                        if (droneCount < 6)
                            dronePositions[droneCount++] = Main.npc[i].Center;
                    }
                }

                for (int i = 0; i < droneCount; i++)
                {
                    Vector2 start = dronePositions[i];
                    Vector2 end = dronePositions[(i + 1) % droneCount];
                    LegendsWeaponBossVisuals.DrawLine(spriteBatch, start, end, Color.LimeGreen * 0.8f, 3f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Color glowColor = new Color(88, 210, 60, 0) * 0.35f;
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
