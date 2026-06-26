using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
        // ── Left-click burst ─────────────────────────────────────────────────
        private const int VentBurstCount    = 4;   // enhanced burst during VentCooldown
        private const int BurstShotSpacing  = 4;   // frames between shots in a burst
        private const int TorpedoDelay      = 4;   // frames after last burst shot → fire torpedo
        private const int VentLockoutExtra  = 24;  // extra lockout for VentCooldown bursts

        // ── Right-click state thresholds (frames) ───────────────────────────
        private const int ChargeToLockedFrames  = 60;
        private const int LockedToRuptureFrames = 90;
        private const int VentCooldownFrames    = 80;
        private const int AbyssalRuptureFrames  = 130;
        private const int RapidFireInterval     = 10;

        private const float HoldoutDistance = 44f;

        // ── Right-click state ────────────────────────────────────────────────
        private enum RightState { Idle, Charging, Locked, VentCooldown, AbyssalRupture }

        private RightState rightState;
        private int        rightStateTimer;

        // ── Left-click timers ────────────────────────────────────────────────
        private int burstShotsRemaining;
        private int burstShotTimer;
        private int burstLockoutTimer;
        private int rapidFireTimer;
        private int torpedoPendingTimer;
        private int cachedBurstStage;
        private int cachedBurstTotal;

        // ── Visual timers ────────────────────────────────────────────────────
        private int   muzzleFlashTimer;
        private int   useAnimationTimer;
        private float recoilOffset;
        private float chargeGlow;
        private float resonanceGlow;
        private float ruptureHeat;
        private float orbitAngle;

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
            // Post-burst torpedo/rocket delay countdown
            if (torpedoPendingTimer > 0)
            {
                torpedoPendingTimer--;
                if (torpedoPendingTimer == 0)
                {
                    int st = cachedBurstStage;
                    if (st == 3 || st == 4)
                        FirePostBurstWeapons(st);
                }
            }

            bool valid     = SeasSearing.CanUseWorldInput(Owner);
            bool leftHeld  = valid && Main.mouseLeft;
            bool rightHeld = valid && (Main.mouseRight || Owner.Calamity().mouseRight);

            HandleRightStateMachine(rightHeld);
            HandleLeftClick(leftHeld, rightHeld);
            HandleUltimateInput(valid);

            rightHeldLastFrame = rightHeld;
        }

        // ── Right-click state machine ────────────────────────────────────────

        private void HandleRightStateMachine(bool rightHeld)
        {
            bool justPressed       =  rightHeld && !rightHeldLastFrame;
            bool rightJustReleased = !rightHeld &&  rightHeldLastFrame;

            switch (rightState)
            {
                case RightState.Idle:
                    if (justPressed) EnterCharging();
                    break;

                case RightState.Charging:
                    if (!rightHeld)
                    {
                        if (rightStateTimer >= 20) FirePressureBolt(strong: false);
                        rightState      = RightState.Idle;
                        rightStateTimer = 0;
                        break;
                    }
                    rightStateTimer++;
                    UpdateChargeVisuals(rightStateTimer / (float)ChargeToLockedFrames);
                    if (rightStateTimer >= ChargeToLockedFrames) EnterLocked();
                    break;

                case RightState.Locked:
                    if (!rightHeld)
                    {
                        FirePressureBolt(strong: true);
                        EnterVentCooldown();
                        break;
                    }
                    rightStateTimer++;
                    UpdateLockedVisuals();
                    if (rightStateTimer >= LockedToRuptureFrames) TriggerAbyssalRupture();
                    break;

                case RightState.VentCooldown:
                    rightStateTimer--;
                    if (rightStateTimer <= 0) { rightState = RightState.Idle; rightStateTimer = 0; }
                    if (justPressed) EnterCharging();
                    break;

                case RightState.AbyssalRupture:
                    rightStateTimer--;
                    if (rightStateTimer <= 0) { rightState = RightState.Idle; rightStateTimer = 0; ruptureHeat = 0f; }
                    break;
            }
        }

        private void EnterCharging()
        {
            rightState = RightState.Charging; rightStateTimer = 0;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.48f, Pitch = -0.55f }, GunTipPosition);
        }

        private void EnterLocked()
        {
            rightState = RightState.Locked; rightStateTimer = 0; chargeGlow = 1f;
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.4f, Pitch = -0.35f }, GunTipPosition);
        }

        private void EnterVentCooldown()
        {
            rightState = RightState.VentCooldown; rightStateTimer = VentCooldownFrames; resonanceGlow = 1.2f;
        }

        private void TriggerAbyssalRupture()
        {
            FireAbyssalGeyserColumn();
            int detonated = SeasSearingPollutionNPC.DetonateAll(Owner, Projectile.damage, GunTipPosition);
            float shake = MathHelper.Clamp(4f + detonated * 0.85f, 4f, 15f);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, shake);
            ApplyRecoil(18f); TriggerMuzzleFlash(30);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.90f, Pitch = -0.58f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.65f, Pitch = -0.38f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.55f, Pitch = -0.5f  }, GunTipPosition);
            rightState = RightState.AbyssalRupture; rightStateTimer = AbyssalRuptureFrames; ruptureHeat = 1f;
            SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, 40, 9f, 22f, 1.6f);
            SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 7f, 16f, 36, SeasSearingPalette.WarningOrange);
        }

        // ── Left-click ───────────────────────────────────────────────────────

        private void HandleLeftClick(bool leftHeld, bool rightHeld)
        {
            if (rightHeld || rightState == RightState.Charging || rightState == RightState.Locked)
            {
                burstShotsRemaining = 0; burstShotTimer = 0;
                return;
            }

            if (rightState == RightState.AbyssalRupture)
            {
                if (leftHeld && burstLockoutTimer <= 0)
                {
                    rapidFireTimer++;
                    if (rapidFireTimer >= RapidFireInterval) { rapidFireTimer = 0; FireTorrentShot(); }
                }
                else
                    rapidFireTimer = 0;
                return;
            }

            bool enhanced   = rightState == RightState.VentCooldown;
            int  stage      = SS_Balance.GetLeftClickStage();
            int  burstCount = enhanced ? VentBurstCount : SS_Balance.GetBurstCount(stage);

            if (leftHeld && burstShotsRemaining <= 0 && burstLockoutTimer <= 0)
                StartBurst(burstCount, stage, enhanced);

            if (burstShotsRemaining > 0)
                HandleBurstShots(stage, enhanced, burstCount);
        }

        private void StartBurst(int count, int stage, bool enhanced)
        {
            cachedBurstStage   = stage;
            cachedBurstTotal   = count;
            burstShotsRemaining = count;
            burstShotTimer     = 0;
            burstLockoutTimer  = CalculateLockout(count, stage, enhanced);
            useAnimationTimer  = Math.Max(useAnimationTimer, BurstShotSpacing * (count - 1) + 8);
        }

        private int CalculateLockout(int count, int stage, bool enhanced)
        {
            if (enhanced) return (count - 1) * BurstShotSpacing + VentLockoutExtra;
            int burstDuration = (count - 1) * BurstShotSpacing;
            if (stage == 3 || stage == 4)
                return burstDuration + TorpedoDelay + 15;
            return burstDuration + 15;
        }

        private void HandleBurstShots(int stage, bool enhanced, int totalCount)
        {
            if (burstShotTimer > 0) { burstShotTimer--; return; }

            int index = totalCount - burstShotsRemaining;

            if (enhanced)
                FireVentShot(index);
            else
                FirePollutionRound(index, stage);

            // Stage 5: per-shot torpedoes; last shot fires missile instead
            if (stage == 5 && !enhanced)
            {
                bool isLastShot = burstShotsRemaining == 1;
                if (isLastShot)
                    FireMissile();
                else
                    FireSkyfinTorpedo();
            }

            burstShotsRemaining--;
            burstShotTimer = burstShotsRemaining > 0 ? BurstShotSpacing : 0;

            if (burstShotsRemaining == 0 && (stage == 3 || stage == 4))
                torpedoPendingTimer = TorpedoDelay;
        }

        // ────────────────────────────────────────────────────────────────────
        // PROJECTILE FIRE METHODS
        // ────────────────────────────────────────────────────────────────────

        private void FirePollutionRound(int burstIndex, int stage)
        {
            float   speed = 24f + Math.Clamp(stage, 0, 5) * 2f;
            Vector2 dir   = AimDirection;
            Vector2 vel   = dir.RotatedByRandom(MathHelper.ToRadians(0.65f + burstIndex * 0.22f)) * speed;

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 8f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                vel,
                ModContent.ProjectileType<SeasSearingPollutionRound>(),
                Projectile.damage, Projectile.knockBack, Projectile.owner,
                burstIndex, stage);

            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].CritChance       = Owner.GetWeaponCrit(Owner.HeldItem);
                Main.projectile[idx].ArmorPenetration += 18;
            }

            ApplyRecoil(5.2f + burstIndex * 1.3f);
            TriggerMuzzleFlash(8);
            SpawnMuzzleBurst(dir, burstIndex, Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, 0.3f));
            SeasSearingVisualUtility.PlayDeepShot(GunTipPosition, burstIndex * 0.06f);
        }

        private void FireVentShot(int burstIndex)
        {
            int     stage = SS_Balance.GetLeftClickStage();
            float   speed = 28f + Math.Clamp(stage, 0, 5) * 2f;
            Vector2 dir   = AimDirection;
            Vector2 vel   = dir.RotatedByRandom(MathHelper.ToRadians(0.5f + burstIndex * 0.18f)) * speed;
            int     damage = (int)(Projectile.damage * 1.22f);

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 8f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                vel,
                ModContent.ProjectileType<SeasSearingVentShot>(),
                damage, Projectile.knockBack, Projectile.owner,
                burstIndex, stage);

            if (Main.projectile.IndexInRange(idx))
                Main.projectile[idx].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            ApplyRecoil(4.8f + burstIndex * 1.1f);
            TriggerMuzzleFlash(13);
            SpawnMuzzleBurst(dir, burstIndex, SeasSearingPalette.PressureBlue);
            SeasSearingVisualUtility.PlayDeepShot(GunTipPosition, -0.08f + burstIndex * 0.06f);
            if (!Main.dedServ)
                SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, 6, 3.5f, 8f, 0.9f);
        }

        private void FireTorrentShot()
        {
            Vector2 dir = AimDirection;
            Vector2 vel = dir.RotatedByRandom(MathHelper.ToRadians(1.2f)) * 32f;
            int damage  = (int)(Projectile.damage * 0.82f);

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 8f, vel,
                ModContent.ProjectileType<SeasSearingTorrentShot>(),
                damage, Projectile.knockBack * 0.5f, Projectile.owner);

            if (Main.projectile.IndexInRange(idx))
                Main.projectile[idx].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            ApplyRecoil(3.5f); TriggerMuzzleFlash(7);
            SpawnMuzzleBurst(dir, 0, SeasSearingPalette.WarningOrange);
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.52f, Pitch = 0.18f, PitchVariance = 0.07f, MaxInstances = 8 }, GunTipPosition);
        }

        private void FirePressureBolt(bool strong)
        {
            Vector2 dir   = AimDirection;
            float   speed = strong ? 28f : 22f;
            int     damage = strong ? (int)(Projectile.damage * 2.5f) : (int)(Projectile.damage * 1.5f);

            int idx = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 14f, dir * speed,
                ModContent.ProjectileType<SeasSearingPressureBolt>(),
                damage, 8f, Projectile.owner, strong ? 1f : 0f);

            if (Main.projectile.IndexInRange(idx))
                Main.projectile[idx].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, strong ? 4f : 2f);
            ApplyRecoil(strong ? 16f : 10f); TriggerMuzzleFlash(strong ? 24 : 15);
            chargeGlow    = strong ? 0.85f : 0.5f;
            resonanceGlow = Math.Max(resonanceGlow, strong ? 1.0f : 0.6f);
            SeasSearingVisualUtility.SpawnPressureRing(GunTipPosition, 6f, 12f, 24, SeasSearingPalette.PressureBlue);
            SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, strong ? 22 : 12, 4f, 10f, 1.1f);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = strong ? 0.84f : 0.60f, Pitch = strong ? -0.44f : -0.22f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item36 with { Volume = strong ? 0.58f : 0.36f, Pitch = -0.3f }, GunTipPosition);
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

        // Fired after last burst shot in stage 3-4 (delayed by TorpedoPendingTimer)
        private void FirePostBurstWeapons(int stage)
        {
            FireSkyfinTorpedo();
            FirePollutionRocket();
        }

        private void FireSkyfinTorpedo()
        {
            Vector2 dir = AimDirection;
            Vector2 vel = dir.RotatedByRandom(MathHelper.ToRadians(3f)) * 16f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 16f, vel,
                ModContent.ProjectileType<SkyfinTorpedo>(),
                Projectile.damage, Projectile.knockBack * 0.6f, Projectile.owner);

            TriggerMuzzleFlash(10);
            SpawnMuzzleBurst(dir, 0, SeasSearingPalette.BiohazardLime);
            SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.44f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 6 }, GunTipPosition);
        }

        private void FirePollutionRocket()
        {
            Vector2 dir = AimDirection.RotatedByRandom(MathHelper.ToRadians(5f));
            Vector2 vel = dir * 22f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + AimDirection * 16f, vel,
                ModContent.ProjectileType<SeasSearingPollutionRocket>(),
                (int)(Projectile.damage * 0.85f), Projectile.knockBack * 0.5f, Projectile.owner);
        }

        private void FireMissile()
        {
            Vector2 dir = AimDirection;
            Vector2 vel = dir.RotatedByRandom(MathHelper.ToRadians(2f)) * 18f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + dir * 16f, vel,
                ModContent.ProjectileType<SeasSearingMissile>(),
                (int)(Projectile.damage * 2f), Projectile.knockBack, Projectile.owner);

            ApplyRecoil(12f); TriggerMuzzleFlash(18);
            SpawnMuzzleBurst(dir, 3, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.BiohazardLime, 0.5f));
            SeasSearingVisualUtility.SpawnAbyssDust(GunTipPosition, 14, 5f, 16f, 1.3f);
            SeasSearingVisualUtility.ShakeAt(GunTipPosition, 6f, 1200f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.75f, Pitch = -0.18f }, GunTipPosition);
        }

        private void HandleUltimateInput(bool validMouse)
        {
            if (!validMouse || KeybindSystem.LegendarySkill?.JustPressed != true) return;

            SeasSearingPlayer ssPlayer = Owner.GetModPlayer<SeasSearingPlayer>();
            if (!ssPlayer.CanUseUltimate)
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.32f, Pitch = -0.25f }, Owner.Center);
                return;
            }

            Vector2 direction = (GetMouseWorld() - GunTipPosition).SafeNormalize(AimDirection);
            int beamIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition + direction * 16f, direction,
                ModContent.ProjectileType<SeasSearingDesignatorBeam>(),
                Math.Max(1, Projectile.damage / 8), 0f, Projectile.owner,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(beamIndex))
                Main.projectile[beamIndex].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

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

            Projectile.Center = armPosition + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir) + vibOffset;

            Owner.ChangeDir(dir);
            Owner.heldProj     = Projectile.whoAmI;
            Owner.itemRotation = (aim * dir).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            if (useAnimationTimer > 0 || rightState == RightState.Charging || rightState == RightState.Locked)
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

            recoilOffset  = MathHelper.Lerp(recoilOffset, 0f, 0.26f);
            resonanceGlow = MathHelper.Clamp(resonanceGlow - 0.035f, 0f, 1.5f);
            orbitAngle   += 0.055f;

            float heatDecay = rightState == RightState.AbyssalRupture ? 0.005f : 0.04f;
            ruptureHeat = MathHelper.Clamp(ruptureHeat - heatDecay, 0f, 1f);

            switch (rightState)
            {
                case RightState.Charging:
                    chargeGlow = rightStateTimer / (float)ChargeToLockedFrames;
                    break;
                case RightState.Locked:
                    chargeGlow = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.1f;
                    break;
                default:
                    chargeGlow = MathHelper.Clamp(chargeGlow - 0.03f, 0f, 1f);
                    break;
            }

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

            int   draws = rightState == RightState.Locked ? 16 : 10;
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

            int starCount = rightState == RightState.Locked ? 5 : 3;
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

            Color cyan  = (fieldColor with { A = 0 }) * (0.12f + power * 0.22f);
            Color deep  = (SeasSearingPalette.DeepBlue with { A = 0 }) * (0.1f + power * 0.18f);
            float pulse = 0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f);

            Main.EntitySpriteDraw(bloom, center, null, deep * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.9f, 1.25f) * power * 0.72f, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float local    = i / 3f;
                float rotation = Main.GlobalTimeWrappedHourly * (0.28f + local * 0.14f);
                float scale    = (1.65f + local * 0.74f + power * 0.35f) * pulse;
                Main.EntitySpriteDraw(ring, center, null, (i == 0 ? cyan : deep) * (1f - local * 0.22f),
                    rotation, ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }
        }
    }
}
