using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingHoldout : ModProjectile, ILocalizedModType
    {
        // ── Left-click cadence ───────────────────────────────────────────────
        private const int BurstShotSpacing  = 4;   // frames between shots (burst & full-auto)
        private const int BurstInterval     = 15;  // frames between bursts (phases 1-5)
        private const int NukeBurstCadence  = 3;   // phase 5: nuke on last shot of every Nth burst
        private const int UltimateChargeFrames = 60;

        // ── Right-click charge ──────────────────────────────────────────────
        private const int RightChargeFrames    = 55;  // hold-to-charge before the payload releases
        private const int RightCooldownFrames  = 22;  // lockout after a payload before re-charging
        private const int ShotgunPellets       = 12;  // pellets per shotgun volley (kept unchanged)
        private const int ShotgunVolleyCount   = 2;   // volleys fired for a two-burst (T2+) release
        private const int ShotgunVolleySpacing = 7;   // frames between the two T2+ volleys

        // Legacy right-click visual thresholds — retained so the preserved charge /
        // rupture / locked-orbit draw code keeps compiling even though the new flow
        // no longer enters the Locked / Vent / Rupture states.
        private const int ChargeToLockedFrames  = 60;
        private const int LockedToRuptureFrames = 90;
        private const int VentCooldownFrames    = 80;
        private const int AbyssalRuptureFrames  = 130;

        private const float HoldoutDistance = 34f; // 持枪前后距离：数值越小，枪越靠近玩家身体。

        // ── Right-click state ────────────────────────────────────────────────
        private enum RightState { Idle, Charging, Locked, VentCooldown, AbyssalRupture }
        private enum UltimateState { Idle, Charging, Ready }

        private RightState rightState;
        private int        rightStateTimer;
        private UltimateState ultimateState;
        private int           ultimateStateTimer;

        // ── Left-click timers ────────────────────────────────────────────────
        private int burstShotsRemaining;
        private int burstShotTimer;
        private int burstLockoutTimer;
        private int burstCompletedCount;
        private int heavyRotationIndex;
        private int autoShotTimer;
        private int autoRoundCount;

        // ── Right-click shotgun volleys ──────────────────────────────────────
        private int shotgunVolleysRemaining;
        private int shotgunTotalVolleys;
        private int shotgunVolleyTimer;
        private int rightCooldownTimer;

        // ── Visual timers ────────────────────────────────────────────────────
        private int   muzzleFlashTimer;
        private int   useAnimationTimer;
        private float recoilOffset;
        private float chargeGlow;
        private float resonanceGlow;
        private float ruptureHeat;
        private float orbitAngle;
        private float ultimateChargeVisual;

        private bool leftHeldLastFrame;
        private bool rightHeldLastFrame;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SeasSearing/NewLegendSeasSearing";

        private Player  Owner          => Main.player[Projectile.owner];
        private Vector2 AimDirection   => Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Max(Owner.direction, 1));
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 47f;

        public override void SetDefaults()
        {
            Projectile.width       = 74;
            Projectile.height      = 34;
            Projectile.friendly    = false;
            Projectile.penetrate   = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType  = DamageClass.Ranged;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)ultimateState);
            writer.Write((byte)ultimateStateTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ultimateState = (UltimateState)reader.ReadByte();
            ultimateStateTimer = reader.ReadByte();
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed ||
                Owner.HeldItem.type != ModContent.ItemType<SeasSearing>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage    = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft  = 2;

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            UpdatePose();
            UpdateTimersAndVisuals();

            if (Main.myPlayer == Projectile.owner)
                HandleInputs();
        }

        // ────────────────────────────────────────────────────────────────────
        // INPUT HANDLING
        // ────────────────────────────────────────────────────────────────────

        private void HandleInputs()
        {
            // Pending second shotgun volley for a two-burst (T2+) right-click release.
            if (shotgunVolleysRemaining > 0)
            {
                if (shotgunVolleyTimer > 0)
                    shotgunVolleyTimer--;

                if (shotgunVolleyTimer <= 0)
                    FirePendingShotgunVolley();
            }

            bool valid     = SeasSearing.CanUseWorldInput(Owner);
            bool leftHeld  = valid && Main.mouseLeft;
            bool rightHeld = valid && (Main.mouseRight || Owner.Calamity().mouseRight);

            HandleRightStateMachine(rightHeld);
            bool ultimateConsumesLeftClick = HandleUltimateInput(valid, leftHeld && !leftHeldLastFrame);
            HandleLeftClick(ultimateConsumesLeftClick ? false : leftHeld, rightHeld);

            leftHeldLastFrame = leftHeld;
            rightHeldLastFrame = rightHeld;
        }

        // ── Right-click state machine ────────────────────────────────────────

        private void HandleRightStateMachine(bool rightHeld)
        {
            if (rightCooldownTimer > 0) rightCooldownTimer--;

            bool justPressed = rightHeld && !rightHeldLastFrame;

            switch (rightState)
            {
                case RightState.Charging:
                    if (!rightHeld)   // released before full charge → cancel
                    {
                        rightState      = RightState.Idle;
                        rightStateTimer = 0;
                        chargeGlow      = 0f;
                        break;
                    }
                    rightStateTimer++;
                    UpdateChargeVisuals(MathHelper.Clamp(rightStateTimer / (float)RightChargeFrames, 0f, 1f));
                    if (rightStateTimer >= RightChargeFrames)
                        ReleaseRightPayload();
                    break;

                default:
                    // Idle (and any never-entered legacy state): begin charging on a fresh press.
                    if (rightState != RightState.Idle) { rightState = RightState.Idle; rightStateTimer = 0; }
                    if (justPressed && rightCooldownTimer <= 0)
                        EnterCharging();
                    break;
            }
        }

        private void EnterCharging()
        {
            rightState = RightState.Charging; rightStateTimer = 0; chargeGlow = 0.15f;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.48f, Pitch = -0.55f }, GunTipPosition);
        }

        // One charged release fires a payload scaled by the Acid Rain tier:
        //   T0  一轮子弹                     one shotgun volley
        //   T1  一轮子弹 + 鼠标三道光芒        + three geyser beams at the cursor
        //   T2  两连喷 + 三道光芒             two shotgun volleys + beams
        //   T3  两连喷 + 三道光芒 + 大型漩涡    + a large abyssal vortex at the cursor
        private void ReleaseRightPayload()
        {
            int tier = SS_Balance.GetAcidRainTier();

            shotgunTotalVolleys     = tier >= 2 ? ShotgunVolleyCount : 1;
            shotgunVolleysRemaining = shotgunTotalVolleys;
            shotgunVolleyTimer      = 0;
            FirePendingShotgunVolley();

            if (tier >= 1)
                FireAbyssalGeyserColumn();

            if (tier >= 3)
                FireRightClickVortex();

            // Abyssal fission: every charged release consumes all accumulated
            // pollution and detonates it by stack count / enemy class.
            int detonated = SeasSearingPollutionNPC.DetonateAll(Owner, Projectile.damage, GunTipPosition);

            ApplyRecoil(14f);
            TriggerMuzzleFlash(24);
            ruptureHeat   = 1f;
            resonanceGlow = Math.Max(resonanceGlow, 1.1f);
            float shake = MathHelper.Clamp((tier >= 3 ? 6f : 4f) + detonated * 0.85f, 4f, 15f);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, shake);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.7f,  Pitch = -0.4f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f,  Pitch = -0.3f }, GunTipPosition);

            rightState        = RightState.Idle;
            rightStateTimer   = 0;
            chargeGlow        = 0f;
            rightCooldownTimer = RightCooldownFrames;
        }

        // ── Left-click ───────────────────────────────────────────────────────

        private void HandleLeftClick(bool leftHeld, bool rightHeld)
        {
            if (ultimateState != UltimateState.Idle)
            {
                ResetBurst();
                return;
            }

            // Right-click charging suppresses the main fire.
            if (rightHeld || rightState == RightState.Charging)
            {
                ResetBurst();
                return;
            }

            int phase = SS_Balance.GetLeftClickPhase();

            if (phase >= 6)
            {
                HandleFullAuto(leftHeld, phase);
                return;
            }

            int burstCount = SS_Balance.GetPhaseBurstCount(phase);

            if (leftHeld && burstShotsRemaining <= 0 && burstLockoutTimer <= 0)
                StartBurst(burstCount, phase);

            if (burstShotsRemaining > 0)
                HandleBurstShots(phase, burstCount);
        }

        private void ResetBurst()
        {
            burstShotsRemaining = 0;
            burstShotTimer      = 0;
            autoShotTimer       = 0;
            autoRoundCount      = 0;
        }

        private void StartBurst(int count, int phase)
        {
            burstShotsRemaining = count;
            burstShotTimer      = 0;
            burstLockoutTimer   = (count - 1) * BurstShotSpacing + BurstInterval;
            useAnimationTimer   = Math.Max(useAnimationTimer, BurstShotSpacing * (count - 1) + 8);
        }

        private void HandleBurstShots(int phase, int totalCount)
        {
            if (burstShotTimer > 0) { burstShotTimer--; return; }

            int  index   = totalCount - burstShotsRemaining;   // 0-based
            bool isFirst = index == 0;
            bool isLast  = index == totalCount - 1;

            // Phase 5: the last shot of every Nth burst is nuke-eligible (fires the
            // small nuke on hit). Earlier phases never mark a round eligible.
            bool nukeEligible = phase >= 5 && isLast && (burstCompletedCount + 1) % NukeBurstCadence == 0;

            FirePollutionRound(index, phase, nukeEligible);

            // Phase 2+: a bile spray on both the first and the last shot of the burst.
            if (phase >= 2 && (isFirst || isLast))
                FirePollutionJuiceSpray(index);

            // Phases 1-3 keep a dedicated rocket on the last shot; from phase 4 the
            // rocket folds into the post-burst heavy-companion rotation instead.
            if (isLast && phase <= 3)
                FirePollutionRocket();

            burstShotsRemaining--;
            burstShotTimer = burstShotsRemaining > 0 ? BurstShotSpacing : 0;

            if (burstShotsRemaining == 0)
                OnBurstComplete(phase);
        }

        private void OnBurstComplete(int phase)
        {
            burstCompletedCount++;

            // Phases 4+ deploy a rotating heavy companion group after each burst.
            if (phase >= 4)
                DeployHeavyCompanion(phase);
        }

        // Full-auto (phase 6): the between-burst gap is removed; one round every
        // BurstShotSpacing frames while held, with rolling-count triggers.
        private void HandleFullAuto(bool leftHeld, int phase)
        {
            if (!leftHeld)
            {
                autoShotTimer  = 0;
                autoRoundCount = 0;
                return;
            }

            if (autoShotTimer > 0) { autoShotTimer--; return; }
            autoShotTimer = BurstShotSpacing;

            autoRoundCount++;
            int round = autoRoundCount;

            bool nukeEligible = round % 30 == 0;              // small nuke on every 30th round's hit
            FirePollutionRound(0, phase, nukeEligible);

            if (round % 5 == 0)                               // bile spray every 5 rounds
                FirePollutionJuiceSpray(0);

            if (round % 40 == 0)                              // twin homing missiles every 40 rounds
                FireMissile();
            else if (round % 20 == 0)                         // heavy companion group every 20 rounds
                DeployHeavyCompanion(phase);

            useAnimationTimer = Math.Max(useAnimationTimer, 6);
        }

        // Rotating heavy companion. Phase 4 cycles rocket / twin torpedo; phases
        // 5-6 also include the pressure depth charge.
        private void DeployHeavyCompanion(int phase)
        {
            int options = phase >= 5 ? 3 : 2;
            switch (heavyRotationIndex++ % options)
            {
                case 0:  FirePollutionRocket();     break;
                case 1:  FireSkyfinTorpedo();       break;   // deploys the twin-torpedo pair
                default: FirePressureDepthCharge(); break;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // PROJECTILE FIRE METHODS
        // ────────────────────────────────────────────────────────────────────

        private void FirePollutionRound(int burstIndex, int phase, bool nukeEligible)
        {
            // 标准深渊污染弹幕（主角）
            float   speed = (24f + Math.Clamp(phase, 1, 6) * 2f) * SS_Balance.PollutionRoundSpeedMultiplier;
            Vector2 dir   = AimDirection;
            Vector2 vel   = dir.RotatedByRandom(MathHelper.ToRadians(0.65f + burstIndex * 0.22f)) * speed;

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 8f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                vel,
                ModContent.ProjectileType<SeasSearingPollutionRound>(),
                Projectile.damage, Projectile.knockBack, Projectile.owner,
                burstIndex, phase, nukeEligible ? 1f : 0f);

            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].CritChance       = Owner.GetWeaponCrit(Owner.HeldItem);
                Main.projectile[idx].ArmorPenetration += 18;
            }

            // Each main round carries a recon soul that hunts the nearest straggler.
            FireReconSoul(dir, burstIndex, phase);
            ApplyRecoil(5.2f + burstIndex * 1.3f);
            TriggerMuzzleFlash(8);
            SpawnMuzzleBurst(dir, burstIndex, Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, 0.3f));
            SeasSearingVisualUtility.PlayDeepShot(GunTipPosition, burstIndex * 0.06f);
        }

        private void FirePollutionJuiceSpray(int burstIndex)
        {
            Vector2 direction = AimDirection;
            int damage = Math.Max(1, (int)(Projectile.damage * 0.28f));

            SeasSearingPollutionJuice.SpawnCone(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 10f,
                direction,
                Main.rand.Next(3, 6),
                8f,
                13f,
                MathHelper.ToRadians(22f),
                damage,
                Projectile.knockBack * 0.25f,
                Projectile.owner);

            TriggerMuzzleFlash(10);
            resonanceGlow = Math.Max(resonanceGlow, 0.75f);
            SpawnMuzzleBurst(direction, burstIndex, SeasSearingPalette.BiohazardLime);
            SoundEngine.PlaySound(SoundID.SplashWeak with
            {
                Volume = 0.42f,
                Pitch = 0.28f,
                PitchVariance = 0.12f,
                MaxInstances = 5
            }, GunTipPosition);
        }

        // Right-click shotgun volley. Same projectile family as a left-click round;
        // pellet count is kept unchanged. A T2+ release fires two of these in a row.
        private void FirePendingShotgunVolley()
        {
            if (shotgunVolleysRemaining <= 0)
                return;

            int volleyIndex = shotgunTotalVolleys - shotgunVolleysRemaining;
            FireShotgunVolley(volleyIndex);
            shotgunVolleysRemaining--;
            shotgunVolleyTimer = shotgunVolleysRemaining > 0 ? ShotgunVolleySpacing : 0;
        }

        private void FireShotgunVolley(int volleyIndex)
        {
            int phase = SS_Balance.GetLeftClickPhase();
            Vector2 dir = AimDirection;
            Vector2 perpendicular = dir.RotatedBy(MathHelper.PiOver2);
            float speed = (24f + Math.Clamp(phase, 1, 6) * 2f) * SS_Balance.PollutionRoundSpeedMultiplier;
            float arc = MathHelper.ToRadians(9.5f);
            float centerBias = volleyIndex == 0 ? -0.35f : 0.35f;

            for (int i = 0; i < ShotgunPellets; i++)
            {
                float completion = ShotgunPellets == 1 ? 0.5f : i / (float)(ShotgunPellets - 1);
                float angle = MathHelper.Lerp(-arc, arc, completion)
                    + MathHelper.ToRadians(Main.rand.NextFloat(-0.7f, 0.7f))
                    + MathHelper.ToRadians(centerBias);
                Vector2 shotDir = dir.RotatedBy(angle);
                Vector2 spawnOffset = perpendicular * ((i % 2 == 0 ? -1f : 1f) * (3.5f + volleyIndex * 1.2f))
                    + Main.rand.NextVector2Circular(1.6f, 1.6f);

                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition + dir * 9f + spawnOffset,
                    shotDir * speed * Main.rand.NextFloat(0.94f, 1.06f),
                    ModContent.ProjectileType<SeasSearingPollutionRound>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    phase,
                    0f);

                if (Main.projectile.IndexInRange(idx))
                {
                    Main.projectile[idx].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    Main.projectile[idx].ArmorPenetration += 18;
                }
            }

            ApplyRecoil(11.5f + volleyIndex * 3.2f);
            TriggerMuzzleFlash(18 + volleyIndex * 4);
            resonanceGlow = Math.Max(resonanceGlow, 1.1f);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.6f + volleyIndex * 1.1f);
            SpawnReleaseSprayVisuals(dir, volleyIndex);
            SeasSearingVisualUtility.PlayDeepShot(GunTipPosition, -0.18f + volleyIndex * 0.08f);
        }

        private void FireReconSoul(Vector2 baseDirection, int burstIndex, int stage)
        {
            float offset = MathHelper.ToRadians(Main.rand.NextBool() ? -1f : 1f);
            Vector2 direction = baseDirection.RotatedBy(offset).SafeNormalize(baseDirection);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            NPC target = FindReconSoulTarget(GunTipPosition);
            int damage = Math.Max(1, (int)(Projectile.damage * (stage >= 4 ? 0.42f : 0.36f)));

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 14f + side * Main.rand.NextFloat(-2.5f, 2.5f),
                direction * Main.rand.NextFloat(17.5f, 19.5f),
                ModContent.ProjectileType<SeasSearingReconSoul>(),
                damage,
                Projectile.knockBack * 0.25f,
                Projectile.owner,
                target?.whoAmI ?? -1f,
                burstIndex % 3);

            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                Main.projectile[idx].netUpdate = true;
            }
        }

        private NPC FindReconSoulTarget(Vector2 origin)
        {
            NPC best = null;
            float bestDistance = 920f * 920f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.DistanceSquared(origin, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }

            return best;
        }

        private void FireAbyssalGeyserColumn()
        {
            Vector2 target = GetMouseWorld();
            for (int i = 0; i < 3; i++)
            {
                float xOffset = (i - 1) * Main.rand.NextFloat(28f, 55f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target + new Vector2(xOffset, 0f), Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingAbyssalGeyser>(),
                    (int)(Projectile.damage * 1.85f), 7f, Projectile.owner,
                    i * 13f);
            }
        }

        // Large abyssal vortex dropped at the cursor on a T3 right-click release.
        private void FireRightClickVortex()
        {
            Vector2 target = GetMouseWorld();
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), target, Vector2.Zero,
                ModContent.ProjectileType<SSPollutionVortex>(),
                Math.Max(1, (int)(Projectile.damage * 1.4f)), 4f, Projectile.owner,
                40f, 5f);   // ai[0] pollution scaling, ai[1] grade → clamps to the max radius

            SeasSearingVisualUtility.SpawnPressureRing(target, 8f, 20f, 40, SeasSearingPalette.RadioactiveCyan);
            SeasSearingVisualUtility.SpawnAbyssDust(target, 30, 7f, 24f, 1.4f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.5f }, target);
        }

        private void FireSkyfinTorpedo()
        {
            Vector2 dir = AimDirection;

            // Two torpedoes per deployment: they are dropped sideways off the aim line, one to each side,
            // so the pair always straddles the player-to-cursor axis. Their AI commits to a target later.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 drop = dir.RotatedBy(MathHelper.PiOver2 * side);
                Vector2 vel  = drop.RotatedByRandom(MathHelper.ToRadians(5f)) * Main.rand.NextFloat(6.5f, 8.5f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + drop * 10f, vel,
                    ModContent.ProjectileType<SkyfinTorpedo>(),
                    Projectile.damage, Projectile.knockBack * 0.6f, Projectile.owner,
                    Main.rand.NextFloat(-1f, 1f), drop.ToRotation());
            }

            TriggerMuzzleFlash(10);
            SpawnMuzzleBurst(dir, 0, SeasSearingPalette.BiohazardLime);
            SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.44f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 6 }, GunTipPosition);
        }

        private void FirePollutionRocket()
        {
            Vector2 dir = AimDirection.RotatedByRandom(MathHelper.ToRadians(5f));
            Vector2 vel = dir * 27f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + AimDirection * 16f, vel,
                ModContent.ProjectileType<SeasSearingPollutionRocket>(),
                (int)(Projectile.damage * 0.85f), Projectile.knockBack * 0.5f, Projectile.owner,
                Main.rand.NextFloat(-1f, 1f));
        }

        private void FirePressureDepthCharge()
        {
            Vector2 dir = AimDirection.RotatedByRandom(MathHelper.ToRadians(6f));
            int damage = Math.Max(1, (int)(Projectile.damage * 1.15f));
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 14f, dir * Main.rand.NextFloat(14f, 17f),
                ModContent.ProjectileType<SeasSearingPressureDepthCharge>(),
                damage, Projectile.knockBack * 0.65f, Projectile.owner,
                Main.rand.NextFloat(-1f, 1f));

            TriggerMuzzleFlash(12);
            ApplyRecoil(7f);
            SpawnMuzzleBurst(dir, 1, SeasSearingPalette.PressureBlue);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.54f, Pitch = -0.46f, PitchVariance = 0.08f }, GunTipPosition);
        }

        private void FireMissile()
        {
            Vector2 dir = AimDirection;
            Vector2 perpendicular = dir.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector2 missileDirection = dir.RotatedBy(MathHelper.ToRadians(4.5f) * side);
                Vector2 spawnPosition = GunTipPosition + dir * 16f + perpendicular * side * 5f;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition, missileDirection * 23f,
                    ModContent.ProjectileType<SeasSearingMissile>(),
                    (int)(Projectile.damage * 2f), Projectile.knockBack, Projectile.owner);
            }

            ApplyRecoil(12f); TriggerMuzzleFlash(18);
            SpawnMuzzleBurst(dir, 3, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime, 0.5f));
            SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, 14, 5f, 16f, 1.3f);
            SeasSearingVisualUtility.ShakeAt(GunTipPosition, 6f, 1200f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.75f, Pitch = -0.18f }, GunTipPosition);
        }

        private bool HandleUltimateInput(bool validMouse, bool justLeftPressed)
        {
            if (ultimateState == UltimateState.Charging)
                return true;

            if (ultimateState == UltimateState.Ready)
            {
                if (validMouse && justLeftPressed)
                    FireUltimateDesignator();

                return true;
            }

            if (!validMouse || KeybindSystem.LegendarySkill?.JustPressed != true)
                return false;

            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            if (!ssPlayer.CanUseUltimate)
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.32f, Pitch = -0.25f }, Owner.Center);
                return false;
            }

            StartUltimateCharge();
            return true;
        }

        private void StartUltimateCharge()
        {
            ultimateState = UltimateState.Charging;
            ultimateStateTimer = 0;
            ultimateChargeVisual = 0f;
            burstShotsRemaining = 0;
            burstShotTimer = 0;
            useAnimationTimer = Math.Max(useAnimationTimer, UltimateChargeFrames + 4);
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.48f, Pitch = -0.58f }, GunTipPosition);
            SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 2.6f, 26f, 18, SeasSearingPalette.DeepBlue);
        }

        private void UpdateUltimateChargeState()
        {
            if (ultimateState == UltimateState.Idle)
            {
                ultimateChargeVisual = MathHelper.Clamp(ultimateChargeVisual - 0.06f, 0f, 1f);
                return;
            }

            if (ultimateState == UltimateState.Charging)
            {
                ultimateStateTimer++;
                float charge = MathHelper.Clamp(ultimateStateTimer / (float)UltimateChargeFrames, 0f, 1f);
                ultimateChargeVisual = charge;
                useAnimationTimer = Math.Max(useAnimationTimer, 2);
                UpdateUltimateChargeVisuals(charge);

                if (ultimateStateTimer >= UltimateChargeFrames)
                {
                    ultimateState = UltimateState.Ready;
                    ultimateStateTimer = 0;
                    ultimateChargeVisual = 1f;
                    resonanceGlow = Math.Max(resonanceGlow, 1.15f);
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.62f, Pitch = -0.28f }, GunTipPosition);
                    SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 5.4f, 20f, 36, SeasSearingPalette.RadioactiveCyan);
                }
                return;
            }

            ultimateChargeVisual = 1f;
            useAnimationTimer = Math.Max(useAnimationTimer, 2);
        }

        private void FireUltimateDesignator()
        {
            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            Vector2 direction = (GetMouseWorld() - GunTipPosition).SafeNormalize(AimDirection);
            int beamIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 16f, direction,
                ModContent.ProjectileType<SeasSearingDesignatorBeam>(),
                Math.Max(1, (int)(Projectile.damage * SS_Balance.GetUltimateDamageMultiplier() / 8f)), 0f, Projectile.owner,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(beamIndex))
                Main.projectile[beamIndex].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            ultimateState = UltimateState.Idle;
            ultimateStateTimer = 0;
            ultimateChargeVisual = 0.7f;
            Projectile.netUpdate = true;
            ssPlayer.StartUltimateCooldown();
            useAnimationTimer = Math.Max(useAnimationTimer, 16);
            ApplyRecoil(18f); TriggerMuzzleFlash(26);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.2f);
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.74f, Pitch = -0.42f }, GunTipPosition);
        }

        // ────────────────────────────────────────────────────────────────────
        // POSE & TIMER UPDATES
        // ────────────────────────────────────────────────────────────────────

        private void UpdatePose()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 desiredAim = (GetMouseWorld() - armPosition).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Vector2.Lerp(AimDirection, desiredAim, 0.44f).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int     dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = dir;
            Projectile.direction       = dir;
            Projectile.rotation        = aim.ToRotation();

            Vector2 vibOffset = Vector2.Zero;
            if (rightState == RightState.Locked && !Main.dedServ)
            {
                float vib = rightStateTimer / (float)LockedToRuptureFrames * 2.8f;
                vibOffset = Main.rand.NextVector2Circular(vib, vib * 0.5f);
            }
            else if (ultimateState == UltimateState.Charging && !Main.dedServ)
            {
                float charge = MathHelper.Clamp(ultimateStateTimer / (float)UltimateChargeFrames, 0f, 1f);
                float vib = 0.35f + MathHelper.SmoothStep(0f, 1f, charge) * 4.25f;
                vibOffset = Main.rand.NextVector2Circular(vib, vib * 0.55f);
            }

            Projectile.Center = armPosition + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir) + vibOffset;

            Owner.ChangeDir(dir);
            Owner.heldProj     = Projectile.whoAmI;
            Owner.itemRotation = (aim * dir).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            if (useAnimationTimer > 0 || rightState == RightState.Charging || rightState == RightState.Locked || ultimateState != UltimateState.Idle)
            {
                Owner.itemTime      = Math.Max(Owner.itemTime, 2);
                Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            }

            float armRot = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f) armRot += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot + MathHelper.ToRadians(5f) * dir);
        }

        private void UpdateTimersAndVisuals()
        {
            if (burstLockoutTimer > 0) burstLockoutTimer--;
            if (muzzleFlashTimer  > 0) muzzleFlashTimer--;
            if (useAnimationTimer > 0) useAnimationTimer--;

            UpdateUltimateChargeState();

            recoilOffset  = MathHelper.Lerp(recoilOffset, 0f, 0.26f);
            resonanceGlow = MathHelper.Clamp(resonanceGlow - 0.035f, 0f, 1.5f);
            orbitAngle   += 0.055f;

            float heatDecay = rightState == RightState.AbyssalRupture ? 0.005f : 0.04f;
            ruptureHeat = MathHelper.Clamp(ruptureHeat - heatDecay, 0f, 1f);

            float rightChargeGlow;
            switch (rightState)
            {
                case RightState.Charging:
                    rightChargeGlow = rightStateTimer / (float)RightChargeFrames;
                    break;
                case RightState.Locked:
                    rightChargeGlow = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.1f;
                    break;
                default:
                    rightChargeGlow = MathHelper.Clamp(chargeGlow - 0.03f, 0f, 1f);
                    break;
            }
            chargeGlow = Math.Max(rightChargeGlow, ultimateChargeVisual);

            if (!Main.dedServ)
                Lighting.AddLight(Owner.Center, new Vector3(0.02f, 0.12f, 0.18f) * Owner.GetModPlayer<SeasSearingPlayer>().PressureVisualPower);
        }

        private void UpdateChargeVisuals(float charge)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.42f + charge * 0.48f) return;

            Vector2 center = GunTipPosition + AimDirection * (12f + charge * 16f);
            float   radius = 30f + 44f * (1f - charge);
            float   ang    = Main.GlobalTimeWrappedHourly * 5.5f + Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 offset = ang.ToRotationVector2() * radius;

            Dust dust = Dust.NewDustPerfect(
                center + offset,
                Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f + charge * 3f, 4f + charge * 6f),
                100,
                Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, charge),
                Main.rand.NextFloat(0.55f, 0.9f) * (0.65f + charge));
            dust.noGravity = true;

            if ((int)(Main.GlobalTimeWrappedHourly * 60f) % 18 == 0 && charge > 0.3f)
                SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 2.8f, 20f + charge * 14f, 16,
                    Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.PressureBlue, charge));
        }

        private void UpdateUltimateChargeVisuals(float charge)
        {
            if (Main.dedServ) return;

            Vector2 center = GunTipPosition + AimDirection * (10f + charge * 14f);
            if (Main.GameUpdateCount % 2 == 0)
            {
                float angle = Main.GlobalTimeWrappedHourly * (4f + charge * 8f) + Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(58f, 12f, charge) + Main.rand.NextFloat(-5f, 5f);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Dust dust = Dust.NewDustPerfect(
                    center + offset,
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    -offset.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1.6f, 8.5f, charge),
                    105,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, charge),
                    MathHelper.Lerp(0.55f, 1.05f, charge));
                dust.noGravity = true;
            }

            if (Main.GameUpdateCount % 12 == 0)
            {
                float pitch = MathHelper.Lerp(-0.52f, 0.12f, charge);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = MathHelper.Lerp(0.18f, 0.46f, charge), Pitch = pitch }, GunTipPosition);
                SeasSearingVisualUtility.SpawnPressureRing(center, 2.3f + charge * 2.2f, 58f - charge * 32f, 18,
                    Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, charge));
            }

            Lighting.AddLight(center, SeasSearingPalette.RadioactiveCyan.ToVector3() * (0.12f + charge * 0.42f));
        }

        private void UpdateLockedVisuals()
        {
            if (Main.dedServ) return;
            float progress = rightStateTimer / (float)LockedToRuptureFrames;

            if (Main.GameUpdateCount % 3 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    float   a   = orbitAngle + MathHelper.TwoPi * (i * 3 + Main.GameUpdateCount / 3 % 3) / 6f;
                    Vector2 pos = GunTipPosition + a.ToRotationVector2() * (22f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 5f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GemDiamond, Vector2.Zero, 100, SeasSearingPalette.RadioactiveCyan, 0.85f);
                    d.noGravity = true;
                }
            }

            if (Main.GameUpdateCount % 12 == 0)
                SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 3.5f, 30f, 20,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.WarningOrange, progress));

            Lighting.AddLight(GunTipPosition, (SeasSearingPalette.PressureBlue * (0.5f + progress * 0.4f)).ToVector3());

            if (progress > 0.6f && Main.GameUpdateCount % 7 == 0)
                SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, (int)(5 + progress * 12f),
                    3f + progress * 5f, 14f + progress * 22f, 0.8f + progress * 0.5f);
        }

        private Vector2 GetMouseWorld() => SeasSearing.GetMouseWorld(Owner);

        private void ApplyRecoil(float amount)
        {
            recoilOffset = Math.Max(recoilOffset, amount);
            Owner.velocity -= AimDirection * amount * 0.014f;
        }

        private void TriggerMuzzleFlash(int frames) => muzzleFlashTimer = Math.Max(muzzleFlashTimer, frames);

        private void SpawnMuzzleBurst(Vector2 direction, int burstIndex, Color tint)
        {
            if (Main.dedServ) return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = direction.RotatedByRandom(0.44f) * Main.rand.NextFloat(1.5f, 5.8f)
                            - direction * burstIndex * 0.2f;
                Dust d = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool(2) ? DustID.Water : DustID.GemEmerald,
                    vel, 100,
                    Color.Lerp(tint, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat(0.1f, 0.7f)),
                    Main.rand.NextFloat(0.75f, 1.35f));
                d.noGravity = true;
            }
            for (int i = 0; i < 4; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    GunTipPosition - direction * Main.rand.NextFloat(2f, 14f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.7f, 2.4f),
                    155, Color.Lerp(SeasSearingPalette.AbyssBlack, SeasSearingPalette.DeepBlue, 0.4f),
                    Main.rand.NextFloat(0.5f, 0.9f));
                smoke.noGravity = true;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // DRAWING
        // ────────────────────────────────────────────────────────────────────

        private void SpawnReleaseSprayVisuals(Vector2 direction, int sprayIndex)
        {
            if (Main.dedServ) return;

            Color pressureTint = Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, 0.45f);
            Color toxinTint = Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.BiohazardLime, 0.35f);
            Vector2 muzzle = GunTipPosition;

            for (int i = 0; i < 24; i++)
            {
                float spread = MathHelper.Lerp(-0.36f, 0.36f, i / 23f) + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 vel = direction.RotatedBy(spread) * Main.rand.NextFloat(3.6f, 9.4f);
                Dust jet = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(4f, 4f),
                    i % 3 == 0 ? DustID.Water : DustID.GemEmerald,
                    vel,
                    90,
                    Color.Lerp(pressureTint, toxinTint, Main.rand.NextFloat(0.15f, 0.85f)),
                    Main.rand.NextFloat(0.8f, 1.45f));
                jet.noGravity = true;
                jet.fadeIn = 0.6f;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = -direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.0f, 3.6f);
                Dust smoke = Dust.NewDustPerfect(
                    muzzle - direction * Main.rand.NextFloat(3f, 18f),
                    DustID.Smoke,
                    vel,
                    150,
                    Color.Lerp(SeasSearingPalette.AbyssBlack, SeasSearingPalette.FalloutAsh, Main.rand.NextFloat(0.35f, 0.75f)),
                    Main.rand.NextFloat(0.65f, 1.15f));
                smoke.noGravity = true;
            }

            SeasSearingVisualUtility.SpawnPressureRing(muzzle + direction * 6f, 4.5f + sprayIndex * 0.8f, 15f + sprayIndex * 4f, 22, pressureTint);
            SeasSearingVisualUtility.SpawnAbyssDust(muzzle + direction * 12f, 14, 5.2f + sprayIndex, 10f, 0.95f);
            SeasSearingVisualUtility.ShakeAt(muzzle, 4.6f + sprayIndex * 1.4f, 1300f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawPressureField();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2   drawPos = Projectile.Center - Main.screenPosition;
            Vector2   origin  = texture.Size() * 0.5f;
            SpriteEffects fx  = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            float flash = muzzleFlashTimer / 24f;

            if (ruptureHeat > 0.05f)
                DrawRuptureHeatOverlay(texture, drawPos, origin, fx);

            if (chargeGlow > 0.02f || resonanceGlow > 0.02f || flash > 0.02f)
                DrawWeaponGlow(texture, drawPos, origin, fx, flash);

            Main.EntitySpriteDraw(texture, drawPos, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, fx, 0);
            DrawMuzzleGlow(flash);
            DrawLockedOrbitCrown();
            DrawUltimateChargeCrown();
            return false;
        }

        private void DrawRuptureHeatOverlay(Texture2D texture, Vector2 drawPos, Vector2 origin, SpriteEffects fx)
        {
            Color heatColor = (SeasSearingPalette.WarningOrange with { A = 0 }) * ruptureHeat * 0.5f;
            int   draws = 8;
            float rad   = 3f + ruptureHeat * 5.5f;
            for (int i = 0; i < draws; i++)
            {
                float ang = MathHelper.TwoPi * i / draws + Main.GlobalTimeWrappedHourly * 2.5f;
                Main.EntitySpriteDraw(texture, drawPos + ang.ToRotationVector2() * rad, null, heatColor, Projectile.rotation, origin, Projectile.scale, fx, 0);
            }
        }

        private void DrawWeaponGlow(Texture2D texture, Vector2 drawPos, Vector2 origin, SpriteEffects fx, float flash)
        {
            Color baseGlow = rightState == RightState.AbyssalRupture
                ? Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.WarningOrange, ruptureHeat * 0.65f)
                : Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, 0.7f + chargeGlow * 0.3f);

            float alpha = MathHelper.Clamp(chargeGlow * 0.5f + resonanceGlow * 0.45f + flash * 0.65f + ruptureHeat * 0.35f, 0f, 0.95f);
            Color color = (baseGlow with { A = 0 }) * alpha;

            int   draws = rightState == RightState.Locked || ultimateState == UltimateState.Ready ? 16 : 10;
            float rad   = 2.2f + chargeGlow * 5.5f + resonanceGlow * 4f + flash * 5.5f;
            for (int i = 0; i < draws; i++)
            {
                float ang = MathHelper.TwoPi * i / draws + Main.GlobalTimeWrappedHourly * 1.6f;
                Main.EntitySpriteDraw(texture, drawPos + ang.ToRotationVector2() * rad, null, color, Projectile.rotation, origin, Projectile.scale, fx, 0);
            }
        }

        private void DrawMuzzleGlow(float flash)
        {
            float power = Math.Max(flash, Math.Max(chargeGlow * 0.75f, Math.Max(resonanceGlow * 0.48f, ruptureHeat * 0.65f)));
            if (power <= 0.02f || Main.dedServ) return;

            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star   = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2   muzzle = GunTipPosition - Main.screenPosition;

            Color muzzleColor = rightState == RightState.AbyssalRupture
                ? Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.WarningOrange, ruptureHeat)
                : Color.Lerp(SeasSearingPalette.RadioactiveCyan, Color.White, 0.25f + power * 0.3f);

            Color c = (muzzleColor with { A = 0 }) * power;

            Main.EntitySpriteDraw(bloom, muzzle, null, c * 0.72f, Projectile.rotation, bloom.Size() * 0.5f,
                new Vector2(0.24f + power * 0.38f, 0.12f + power * 0.24f), SpriteEffects.None, 0);

            int starCount = rightState == RightState.Locked || ultimateState == UltimateState.Ready ? 5 : 3;
            for (int i = 0; i < starCount; i++)
            {
                float rot = Projectile.rotation + MathHelper.TwoPi * i / starCount + Main.GlobalTimeWrappedHourly * 2.2f;
                Main.EntitySpriteDraw(star, muzzle, null, c * 0.52f, rot, star.Size() * 0.5f,
                    new Vector2(0.12f, 1.0f + power * 1.35f), SpriteEffects.None, 0);
            }
        }

        private void DrawLockedOrbitCrown()
        {
            if (rightState != RightState.Locked || Main.dedServ) return;

            Texture2D bloom       = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float     progress    = MathHelper.Clamp(rightStateTimer / (float)LockedToRuptureFrames, 0f, 1f);
            int       dotCount    = 6;
            float     orbitRadius = 20f + progress * 14f;
            Color     dotColor    = (Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.WarningOrange, progress) with { A = 0 }) * (0.55f + progress * 0.35f);

            for (int i = 0; i < dotCount; i++)
            {
                float   ang   = orbitAngle + MathHelper.TwoPi * i / dotCount;
                Vector2 pos   = GunTipPosition - Main.screenPosition + ang.ToRotationVector2() * orbitRadius;
                float   scale = 0.08f + progress * 0.06f;
                Main.EntitySpriteDraw(bloom, pos, null, dotColor, 0f, bloom.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            if (progress > 0.7f)
            {
                Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
                float     pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (8f + progress * 12f));
                Color     warn  = (SeasSearingPalette.WarningOrange with { A = 0 }) * ((progress - 0.7f) / 0.3f * 0.7f);
                Main.EntitySpriteDraw(ring, GunTipPosition - Main.screenPosition, null, warn, Main.GlobalTimeWrappedHourly * 3f,
                    ring.Size() * 0.5f, (0.18f + progress * 0.24f) * pulse, SpriteEffects.None, 0);
            }
        }

        private void DrawUltimateChargeCrown()
        {
            if (ultimateState == UltimateState.Idle || Main.dedServ) return;

            float charge = ultimateState == UltimateState.Charging
                ? MathHelper.Clamp(ultimateStateTimer / (float)UltimateChargeFrames, 0f, 1f)
                : 1f;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 muzzle = GunTipPosition - Main.screenPosition;
            Color cyan = (Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, charge) with { A = 0 });

            float ringScale = MathHelper.Lerp(0.42f, 0.16f, charge);
            float spin = Main.GlobalTimeWrappedHourly * MathHelper.Lerp(1.5f, 7f, charge);
            Main.EntitySpriteDraw(ring, muzzle, null, cyan * (0.25f + charge * 0.38f), spin,
                ring.Size() * 0.5f, ringScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, muzzle, null, cyan * (0.16f + charge * 0.28f), -spin * 1.35f,
                ring.Size() * 0.5f, ringScale * 0.72f, SpriteEffects.None, 0);

            int coreCount = ultimateState == UltimateState.Ready ? 6 : 3;
            float coreRadius = ultimateState == UltimateState.Ready ? 18f : MathHelper.Lerp(46f, 16f, charge);
            for (int i = 0; i < coreCount; i++)
            {
                float angle = -spin * 1.7f + MathHelper.TwoPi * i / coreCount;
                Vector2 position = muzzle + angle.ToRotationVector2() * coreRadius;
                float scale = ultimateState == UltimateState.Ready ? 0.075f : 0.05f + charge * 0.025f;
                Main.EntitySpriteDraw(bloom, position, null, cyan * (0.45f + charge * 0.28f), 0f,
                    bloom.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
        }

        private void DrawPressureField()
        {
            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            float power = ssPlayer.PressureVisualPower;
            if (power <= 0.02f || Main.dedServ) return;

            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   center = Owner.Center - Main.screenPosition;

            Color fieldColor = rightState == RightState.AbyssalRupture
                ? Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, 1f - ruptureHeat * 0.6f)
                : rightState == RightState.Locked
                    ? SeasSearingPalette.PressureBlue
                    : SeasSearingPalette.RadioactiveCyan;

            const float fieldOpacity = 0.5f;
            Color cyan  = (fieldColor with { A = 0 }) * ((0.12f + power * 0.22f) * fieldOpacity);
            Color deep  = (SeasSearingPalette.DeepBlue with { A = 0 }) * ((0.1f + power * 0.18f) * fieldOpacity);

            Main.EntitySpriteDraw(bloom, center, null, deep * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.9f, 1.25f) * power * 0.72f, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float local    = i / 3f;
                float rotation = Main.GlobalTimeWrappedHourly * (0.28f + local * 0.14f);
                float scale    = 1.65f + local * 0.74f + power * 0.35f;
                Main.EntitySpriteDraw(ring, center, null, (i == 0 ? cyan : deep) * (1f - local * 0.22f),
                    rotation, ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
        }
    }
}
