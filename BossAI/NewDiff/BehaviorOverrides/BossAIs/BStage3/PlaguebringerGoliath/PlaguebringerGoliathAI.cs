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
    internal sealed class PlaguebringerGoliathAI : IUMWBossAI
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
            OverloadTransition = 12
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

        // Arena steam variables
        private int steamWarnTimer = 0;
        private int activeSteamAxis = -1; // -1: none, 0: horizontal, 1: vertical, 2: both
        private float steamWarnOpacity = 0f;

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
                npc.netUpdate = true;
            }

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

            // Greenhouse Boundary Arena (1400px in P1-P3, 1000px in P4-P6). Damage is throttled — the old
            // version called target.Hurt() every single frame the player was outside, which is 60 hits/sec.
            float borderSize = currentPhase <= 3 ? 1400f : 1000f;
            Vector2 dist = target.Center - npc.Center;
            if (arenaHurtCooldown > 0)
                arenaHurtCooldown--;
            if (dist.Length() > borderSize / 2f)
            {
                target.velocity += SafeNormalize(npc.Center - target.Center, Vector2.Zero) * 2f;
                if (arenaHurtCooldown <= 0)
                {
                    arenaHurtCooldown = 30;
                    target.AddBuff(BuffID.Poisoned, 180);
                    target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(npc.whoAmI), 15, 0);
                }
            }

            // Update steam vents every 6 seconds (360 frames)
            UpdateGreenhouseSteam(npc, target, borderSize);

            // Update Nano-Drone Grid Shield
            UpdateDroneShield(npc, currentPhase);

            // Visual oscillations and breathing
            npc.rotation = npc.velocity.X * 0.03f;
            npc.scale = 1f + (float)Math.Sin(ticksRunning * 0.05f) * 0.02f;

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
            }

            return false;
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            ApplyDefenseModifiers(npc, ref modifiers);
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
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.3f }, npc.Center);
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
                            Vector2 pos = npc.Center + new Vector2(x, target.Center.Y - npc.Center.Y);
                            SpawnHostile(npc, pos, new Vector2(0f, 0f), "Projectiles/Boss/PlagueCloud", dmg);
                        }
                    }
                    if (activeSteamAxis == 1 || activeSteamAxis == 2)
                    {
                        for (float y = -borderSize / 2f; y < borderSize / 2f; y += 90f)
                        {
                            Vector2 pos = npc.Center + new Vector2(target.Center.X - npc.Center.X, y);
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
                    shieldStunTimer--;
                    npc.velocity *= 0.95f;
                    npc.defense = 0;
                    if (shieldStunTimer == 0)
                        shieldRegenTimer = 1500; // 25s weak period before regeneration
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
            if (currentPhase <= 3)
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

        #region State Machine Implementations

        // P1 Attack 1: Virulence — 挥砍分裂型 · 巨剑斩出缓慢毒波, 滑行120像素后裂变成6枚弱追踪微波.
        private void ExecuteVirulence(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            Vector2 spot = DirectedHoverSpot(npc, target, 260f, -280f, 8f);
            SmoothHover(npc, spot, 0.06f, timer < 40 ? 15f : 4f);

            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldVirulence>(), npc.damage / 2, 0f, Main.myPlayer, npc.whoAmI, Math.Sign((target.Center - npc.Center).X));

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 dir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 6f, ModContent.ProjectileType<VirulentWaveProj>(), npc.damage / 3, 0f, Main.myPlayer);
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir, ModContent.ProjectileType<PlagueHeldVirulence>(), npc.damage / 2, 0f, Main.myPlayer, npc.whoAmI, Math.Sign(dir.X));
            }

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

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-300f, 300f), -480f);
                    int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), spawnPos, new Vector2(0f, -6f), ModContent.ProjectileType<PlagueArrowProj>(), npc.damage / 3, 0f, Main.myPlayer, malevolenceSide, i * 8f);
                }
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.5f }, npc.Center);
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
                staffTargetX = target.Center.X;
                staffTargetY = target.Center.Y;
                Vector2[] offsets = { new(-240f, 240f), new(240f, 240f), new(0f, -320f) };
                foreach (Vector2 off in offsets)
                {
                    Vector2 spawn = target.Center + off;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, Vector2.Zero, ModContent.ProjectileType<PlagueFangProj>(), npc.damage / 3, 0f, Main.myPlayer, staffTargetX, staffTargetY);
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
                    Vector2 vel = new(Main.rand.NextFloat(-6f, 6f), -6f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<FuelCellFlaskProj>(), npc.damage / 3, 0f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f }, npc.Center);
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
                    int virili = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X - 500, (int)target.Center.Y - 320, ModContent.Find<ModNPC>("CalamityMod/PlaguePrincess").Type);
                    if (virili >= 0 && virili < Main.maxNPCs)
                    {
                        Main.npc[virili].velocity = new Vector2(12f, 0f);
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

            Vector2 spot = DirectedHoverSpot(npc, target, 320f, -200f, 6f);
            SmoothHover(npc, spot, 0.06f, 15f);

            if (timer == 50)
            {
                FindHeldWeapon<PlagueHeldSyringe>(npc)?.Pulse(26f);
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.15f }, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 22f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<TheSyringeProj>(), npc.damage / 2, 0f, Main.myPlayer);
                }
            }

            if (timer >= 220)
                RotateAttack(npc, phase, AttackState.TheSyringe);
        }

        // P2 Attack 1: The Hive — 缓慢核弹型 · 极慢核弹长时间飘移, 引爆后放射24枚微型导弹.
        private void ExecuteTheHive(NPC npc, Player target, ref float timer, ref float tracker, int phase)
        {
            timer++;
            if (timer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (target.Center - npc.Center).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<PlagueHeldHive>(), 0, 0f, Main.myPlayer, npc.whoAmI);

            Vector2 spot = DirectedHoverSpot(npc, target, 280f, -280f, 8f);
            SmoothHover(npc, spot, 0.06f, 16f);

            if (timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = SafeNormalize(target.Center - npc.Center, Vector2.UnitY) * 2.5f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<HiveNukeProj>(), npc.damage / 2, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.6f }, npc.Center);
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

            if (timer == 40 || timer == 90 || timer == 140)
            {
                defilerAimDir = SafeNormalize(target.Center - npc.Center, Vector2.UnitY);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 dir = defilerAimDir.RotatedBy(MathHelper.Lerp(-0.2f, 0.2f, i / 7f));
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 13f, ModContent.ProjectileType<SicknessRoundProj>(), npc.damage / 3, 0f, Main.myPlayer, dir.X, dir.Y);
                    }
                }
                SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.5f }, npc.Center);
                FindHeldWeapon<PlagueHeldDefiler>(npc)?.Pulse(-8f);
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

            if (timer == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 12; i++)
                {
                    float ang = MathHelper.TwoPi * i / 12f;
                    Vector2 pos = npc.Center + ang.ToRotationVector2() * 90f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<MalachiteDaggerProj>(), npc.damage / 3, 0f, Main.myPlayer, i * 10f + 20f);
                }
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f }, npc.Center);
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

            if (timer >= 50 && timer <= 170 && timer % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angle = MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, (timer - 50f) / 120f);
                Vector2 vel = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 12f;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, vel, ModContent.ProjectileType<BlightFlameProj>(), npc.damage / 3, 0f, Main.myPlayer);
                FindHeldWeapon<PlagueHeldBlightSpewer>(npc)?.Pulse(6f);
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

            if (timer == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    float angle = i * MathHelper.Pi;
                    Vector2 pos = target.Center + angle.ToRotationVector2() * 160f;
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
                Vector2[] corners = { new(-500f, -500f), new(500f, -500f), new(-500f, 500f), new(500f, 500f) };
                foreach (Vector2 c in corners)
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

            // Draw greenhouse border
            int currentPhase = (int)npc.ai[0];
            float borderSize = currentPhase <= 3 ? 1400f : 1000f;
            Vector2 tl = npc.Center + new Vector2(-borderSize / 2f, -borderSize / 2f);
            Vector2 tr = npc.Center + new Vector2(borderSize / 2f, -borderSize / 2f);
            Vector2 bl = npc.Center + new Vector2(-borderSize / 2f, borderSize / 2f);
            Vector2 br = npc.Center + new Vector2(borderSize / 2f, borderSize / 2f);

            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tl, tr, Color.LimeGreen * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, tr, br, Color.LimeGreen * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, br, bl, Color.LimeGreen * 0.7f, 4f);
            IUMWWeaponBossVisuals.DrawLine(spriteBatch, bl, tl, Color.LimeGreen * 0.7f, 4f);

            if (activeSteamAxis != -1 && steamWarnOpacity > 0f)
            {
                Player target = Main.player[npc.target];
                Color warnColor = Color.LimeGreen * steamWarnOpacity;
                if (activeSteamAxis == 0 || activeSteamAxis == 2)
                {
                    Vector2 start = npc.Center + new Vector2(-borderSize / 2f, target.Center.Y - npc.Center.Y);
                    Vector2 end = npc.Center + new Vector2(borderSize / 2f, target.Center.Y - npc.Center.Y);
                    IUMWWeaponBossVisuals.DrawLine(spriteBatch, start, end, warnColor, 3f);
                }
                if (activeSteamAxis == 1 || activeSteamAxis == 2)
                {
                    Vector2 start = npc.Center + new Vector2(target.Center.X - npc.Center.X, -borderSize / 2f);
                    Vector2 end = npc.Center + new Vector2(target.Center.X - npc.Center.X, borderSize / 2f);
                    IUMWWeaponBossVisuals.DrawLine(spriteBatch, start, end, warnColor, 3f);
                }
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
                    IUMWWeaponBossVisuals.DrawLine(spriteBatch, start, end, Color.LimeGreen * 0.8f, 3f);
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
