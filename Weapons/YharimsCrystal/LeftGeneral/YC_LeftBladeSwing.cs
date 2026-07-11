using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_LeftBladeSwing : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        private readonly BalanceYharimsCrystal balance = new();

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";
        public override int AssignedItemID => ModContent.ItemType<NewLegendYharimsCrystal>();
        public override Vector2 SpriteOrigin => new(0f, 186f);

        public int size = 186 + 102;
        public override float HitboxOutset => size * 0.85f;
        public override Vector2 HitboxSize => new Vector2(size, size);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private static readonly Color BladeGold = new(255, 214, 88);
        private static readonly Color BladeOrange = new(255, 111, 34);
        private static readonly Color BladeWhite = new(255, 246, 196);

        // The throw windup raises the arm the same way NeptunesBounty's pullback does:
        // a single PolyOut segment from 0 to -70 degrees of extra offset.
        private static readonly CalamityUtils.CurveSegment ChargeWindupCurve =
            new(CalamityUtils.EasingType.PolyOut, 0f, 0f, MathHelper.ToRadians(-70f), 2);

        private enum BladeMotionState
        {
            Idle,
            Swing,
            Recovery
        }

        // Holdout state
        private BladeMotionState motionState = BladeMotionState.Idle;
        private int postSwingCooldown = 0;
        private int PostSwingCooldownMax => (int)(useAnim * 0.65f);
        private int useAnim = 45;
        private int lastSwingId = 0;
        private int stateTimer = 0;
        private bool playSwingSound = true;
        private int bladeHitFireballsThisSpin = 0;
        private const int MaxBladeHitFireballs = 5;

        // Idle auto-dismiss (3 seconds of no swing input)
        private int idleTimer = 0;
        private const int IdleDismissFrames = 180;
        private bool willDie = false;
        private int dyingTimer = 0;
        private const int DyingFadeFrames = 24;
        private float dyingFade = 1f;

        // Right-click charge-to-throw (1 second hold)
        private int rightChargeTimer = 0;
        private const int RightChargeFrames = 60;
        private float chargeSpinAngle;
        private SlotId chargeSpinSoundSlot;

        // Rotation/positioning. bladeAngle is the single authority for the visible sword angle.
        private float bladeAngle;
        private float bladeAngularVelocity;
        private float swingStartAngle;
        private const float ChargeMaxTurnSpeed = 0.16f;
        private const float RecoveryMaxTurnSpeed = 0.055f;
        private float fadeIn = 0;
        private float bladeFade = 0;
        private float chargeBorderIntensity = 0;
        private bool postSwing = false;
        private bool auricJudgementQueued = false;
        private bool auricJudgementReleased = false;

        private Vector2 lockedMouseWorld;
        private Vector2 aimVel;
        private Vector2 mousePos;
        private int lockedFacingDirection = 1;
        private float smoothedMouseBladeAngle;
        private float smoothedMouseBladeVelocity;

        private static float SwordAngleOffset => MathHelper.PiOver4;
        private static float DownwardPrepBladeAngle => MathHelper.PiOver2 + SwordAngleOffset;

        private Vector2 CurrentAimDirection => (FinalRotation - SwordAngleOffset).ToRotationVector2();

        private Vector2 GetMouseWorld()
        {
            Vector2 mouseWorld = Owner.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }

        private float GetRawMouseTargetAngle()
        {
            Vector2 mouseWorld = GetMouseWorld();
            Vector2 aim = (mouseWorld - Owner.MountedCenter).SafeNormalize(CurrentAimDirection);
            return aim.ToRotation() + SwordAngleOffset;
        }

        private float GetMouseHoldTargetAngle()
        {
            float side = Projectile.ai[1] == 0f ? 1f : -Projectile.ai[1];
            int facing = lockedFacingDirection == 0 ? Owner.direction : lockedFacingDirection;
            return GetRawMouseTargetAngle() + MathHelper.ToRadians(120f * side * facing);
        }

        private float GetSmoothedMouseTargetAngle()
        {
            float rawTarget = GetMouseHoldTargetAngle();
            float delta = MathHelper.WrapAngle(rawTarget - smoothedMouseBladeAngle);
            float distance = MathHelper.Clamp(Math.Abs(delta) / MathHelper.Pi, 0f, 1f);
            float maxTurnSpeed = 0.045f;
            float desiredSpeed = Math.Sign(delta) * maxTurnSpeed * SmootherStep(distance);
            float maxAcceleration = maxTurnSpeed * 0.06f;

            smoothedMouseBladeVelocity += MathHelper.Clamp(desiredSpeed - smoothedMouseBladeVelocity, -maxAcceleration, maxAcceleration);
            smoothedMouseBladeVelocity *= 0.91f;
            smoothedMouseBladeAngle += smoothedMouseBladeVelocity;

            Vector2 smoothedAim = (smoothedMouseBladeAngle - SwordAngleOffset).ToRotationVector2();
            lockedMouseWorld = Owner.MountedCenter + smoothedAim * 160f;
            return smoothedMouseBladeAngle;
        }

        private void SmoothBladeToward(float targetAngle, float maxSpeed)
        {
            float delta = MathHelper.WrapAngle(targetAngle - bladeAngle);
            float distance = MathHelper.Clamp(Math.Abs(delta) / MathHelper.Pi, 0f, 1f);
            float desiredSpeed = Math.Sign(delta) * maxSpeed * SmootherStep(distance);
            float maxAcceleration = maxSpeed * 0.08f;

            bladeAngularVelocity += MathHelper.Clamp(desiredSpeed - bladeAngularVelocity, -maxAcceleration, maxAcceleration);
            bladeAngularVelocity *= 0.94f;
            bladeAngle += bladeAngularVelocity;
            SetFinalBladeAngle(bladeAngle);
        }

        private void TurnIdleBladeTowardMouse()
        {
            UpdateBaseBladeRotation();
            float idleStretch = 1f + Utils.GetLerpValue(useAnim * 0.7f, useAnim, Animation, true) * 0.35f;
            float targetOffset = MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * idleStretch);
            RotationOffset = MathHelper.Lerp(RotationOffset, targetOffset, 0.2f);
            bladeAngle = FinalRotation;
            bladeAngularVelocity = MathHelper.Lerp(bladeAngularVelocity, 0f, 0.28f);
            smoothedMouseBladeAngle = bladeAngle;
            smoothedMouseBladeVelocity = 0f;
            lockedMouseWorld = Owner.MountedCenter + CurrentAimDirection * 160f;
        }

        private void UpdateFacingFromBlade()
        {
            Vector2 aim = (Projectile.rotation - SwordAngleOffset).ToRotationVector2();
            if (Math.Abs(aim.X) > 0.08f)
                lockedFacingDirection = aim.X >= 0f ? 1 : -1;

            Owner.direction = lockedFacingDirection;
            FlipAsSword = Owner.direction == -1;
        }

        private void ApplyBladeAngle()
        {
            bladeAngle = FinalRotation;
        }

        private void SetFinalBladeAngle(float finalAngle)
        {
            Projectile.rotation = finalAngle;
            RotationOffset = 0f;
            bladeAngle = finalAngle;
        }

        private void UpdateBaseBladeRotation()
        {
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(mousePos) + SwordAngleOffset, 0.1f);
        }

        private static float SmootherStep(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            return progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 0;
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;
            Projectile.scale = balance.GetLeftBladeScale();
            Owner.GetModPlayer<YharimsCrystalStatePlayer>().SetLastWeapon(YCWeaponForm.Blade);
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);

            CanHit = false;
            Projectile.ai[1] = -1;
            motionState = BladeMotionState.Idle;
            stateTimer = 0;
            postSwingCooldown = 0;
            idleTimer = 0;
            willDie = false;
            rightChargeTimer = 0;
            chargeSpinAngle = 0f;
            auricJudgementQueued = false;
            auricJudgementReleased = false;

            Vector2 spawnAim = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            if (Main.myPlayer == Projectile.owner)
            {
                mousePos = GetMouseWorld();
                aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX * -Owner.direction) * 65f;
            }
            else
            {
                aimVel = -spawnAim * 65f;
                mousePos = Owner.Center - aimVel;
            }

            if (Math.Abs(spawnAim.X) > 0.08f)
                lockedFacingDirection = spawnAim.X >= 0f ? 1 : -1;
            Owner.direction = lockedFacingDirection;

            Projectile.rotation = spawnAim.ToRotation() + SwordAngleOffset;
            RotationOffset = MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction);
            bladeAngle = FinalRotation;
            bladeAngularVelocity = 0f;
            smoothedMouseBladeAngle = bladeAngle;
            smoothedMouseBladeVelocity = 0f;
            swingStartAngle = bladeAngle;
            lockedMouseWorld = Owner.MountedCenter + spawnAim * 160f;
            UpdateFacingFromBlade();
            ApplyBladeAngle();

            Projectile.ai[0] = 0;
            lastSwingId = 0;

            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.72f, Pitch = -0.18f }, Owner.Center);
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                WhenSpawned();
                whenSpawned = false;
                Projectile.netUpdate = true;
            }

            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            bool holdingAssignedItem = Owner.HeldItem.type == AssignedItemID;
            if (!holdingAssignedItem)
            {
                CancelBecauseWeaponChanged();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            bool shouldOccupyHands = !willDie;
            if (shouldOccupyHands)
            {
                Owner.heldProj = Projectile.whoAmI;
                Owner.itemTime = Math.Max(Owner.itemTime, 2);
                Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            }

            UseStyle();
            if (shouldOccupyHands)
                UpdateFacingFromBlade();
            ApplyBladeAngle();
            if (shouldOccupyHands)
            {
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
                Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);
            }

            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => CanHit ? null : false;

        public override void UseStyle()
        {
            UseStyleContinuous();
        }

        private void UseStyleContinuous()
        {
            bool isOwner = Main.myPlayer == Projectile.owner;
            Owner.Calamity().mouseWorldListener = true;

            float speedFactor = Owner.GetAttackSpeed(DamageClass.Magic);
            useAnim = speedFactor > 0f ? (int)Math.Max(1f, Owner.HeldItem.useAnimation / speedFactor) : Owner.HeldItem.useAnimation;

            bool leftHeld = isOwner &&
                Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;

            bool rightHeld = isOwner &&
                (Main.mouseRight || Owner.Calamity().mouseRight || Owner.controlUseTile) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;

            if (isOwner)
            {
                if (CanHit || postSwing)
                    mousePos = Owner.Center - aimVel;
                else
                {
                    mousePos = GetMouseWorld();
                    aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
                    Projectile.velocity = -aimVel.SafeNormalize(Vector2.UnitX * Owner.direction);
                }
            }
            else
                mousePos = Owner.Center - aimVel;

            if (rightHeld && !willDie)
            {
                rightChargeTimer = Math.Min(rightChargeTimer + 1, RightChargeFrames);
                idleTimer = 0;
                if (motionState == BladeMotionState.Swing)
                {
                    motionState = BladeMotionState.Idle;
                    stateTimer = 0;
                    CanHit = false;
                    postSwing = false;
                }
            }
            else if (rightChargeTimer > 0)
            {
                rightChargeTimer = 0;
                StopChargeSpinSound();
            }

            chargeBorderIntensity = MathHelper.Lerp(chargeBorderIntensity, rightChargeTimer / (float)RightChargeFrames, 0.3f);

            if (willDie)
            {
                RunDyingFade();
                return;
            }

            if (isOwner && rightChargeTimer >= RightChargeFrames)
            {
                ThrowBlade();
                return;
            }

            bool isCharging = rightChargeTimer > 0;
            if (isCharging)
            {
                RunChargeHold();
                return;
            }

            if (isOwner && leftHeld && motionState == BladeMotionState.Idle && postSwingCooldown <= 0)
            {
                mousePos = GetMouseWorld();
                aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
                Projectile.velocity = -aimVel.SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.ai[0] += 1f;
                Projectile.ai[2] = FinalRotation;
                Projectile.netUpdate = true;
            }

            int swingId = (int)Projectile.ai[0];
            if (swingId != lastSwingId && motionState == BladeMotionState.Idle && postSwingCooldown <= 0)
                BeginSwing(swingId);

            switch (motionState)
            {
                case BladeMotionState.Swing:
                    RunSwing();
                    break;

                case BladeMotionState.Recovery:
                    RunRecovery();
                    break;

                default:
                    RunIdle();
                    break;
            }

            if (Projectile.ai[1] == -1)
                bladeFade = MathHelper.Lerp(bladeFade, 1, 0.15f);
            else
                bladeFade = MathHelper.Lerp(bladeFade, 0, 0.045f);

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void RunDyingFade()
        {
            dyingTimer++;
            dyingFade = 1f - MathHelper.Clamp(dyingTimer / (float)DyingFadeFrames, 0f, 1f);
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.2f);
            postSwing = false;
            CanHit = false;
            SmoothBladeToward(GetSmoothedMouseTargetAngle(), RecoveryMaxTurnSpeed);
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);

            if (dyingTimer >= DyingFadeFrames)
            {
                DrawUnconditionally = false;
                Projectile.Kill();
            }
        }

        private void BeginDyingFade()
        {
            if (willDie)
                return;

            willDie = true;
            dyingTimer = 0;
            CanHit = false;
            postSwing = false;
            rightChargeTimer = 0;
            StopChargeSpinSound();
        }

        private void CancelBecauseWeaponChanged()
        {
            CanHit = false;
            postSwing = false;
            willDie = true;
            rightChargeTimer = 0;
            auricJudgementQueued = false;
            auricJudgementReleased = false;
            StopChargeSpinSound();
            DrawUnconditionally = false;

            if (Owner.heldProj == Projectile.whoAmI)
                Owner.heldProj = -1;

            Owner.itemTime = 0;
            Owner.itemAnimation = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
            Projectile.Kill();
        }

        private void RunChargeHold()
        {
            if (rightChargeTimer == 1)
            {
                chargeSpinAngle = 0f;
                chargeSpinSoundSlot = SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SpinningWoosh") { Volume = 0.01f, Pitch = -0.35f, IsLooped = true }, Projectile.Center);
            }

            CanHit = false;
            postSwing = false;
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.22f);
            SmoothBladeToward(DownwardPrepBladeAngle, ChargeMaxTurnSpeed);
            ApplyChargeArmPose();
            EmitChargeSpinFX();
        }

        private void StopChargeSpinSound()
        {
            if (SoundEngine.TryGetActiveSound(chargeSpinSoundSlot, out ActiveSound spin))
                spin.Stop();
        }

        // Rotating vortex telegraph for the 3-second right-click hold: a pair of
        // counter-spinning smears orbiting the blade's aim point, speeding up and
        // brightening from BladeOrange toward BladeGold as the throw nears.
        private void EmitChargeSpinFX()
        {
            float chargeProgress = rightChargeTimer / (float)RightChargeFrames;

            if (SoundEngine.TryGetActiveSound(chargeSpinSoundSlot, out ActiveSound spin) && spin.IsPlaying)
            {
                spin.Position = Projectile.Center;
                spin.Volume = MathHelper.Lerp(0.05f, 0.75f, chargeProgress);
                spin.Pitch = MathHelper.Lerp(-0.35f, 0.35f, chargeProgress);
            }

            if (Main.dedServ)
                return;

            chargeSpinAngle += MathHelper.Lerp(0.16f, 0.62f, chargeProgress);
            Vector2 vortexCenter = Owner.MountedCenter + CurrentAimDirection.SafeNormalize(Vector2.UnitX * lockedFacingDirection) * (60f + chargeProgress * 40f);

            GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                vortexCenter,
                Color.Lerp(BladeOrange, BladeGold, chargeProgress) with { A = 0 } * (0.35f + chargeProgress * 0.5f),
                chargeSpinAngle,
                (0.55f + chargeProgress * 0.85f) * Projectile.scale));

            GeneralParticleHandler.SpawnParticle(new CircularSmearVFX(
                vortexCenter,
                BladeWhite with { A = 0 } * (0.18f + chargeProgress * 0.3f),
                -chargeSpinAngle * 1.4f,
                (0.32f + chargeProgress * 0.5f) * Projectile.scale));

            if (Main.rand.NextBool(3))
            {
                Vector2 orbitOffset = chargeSpinAngle.ToRotationVector2() * (26f + chargeProgress * 30f);
                Vector2 tangent = orbitOffset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                Dust dust = Dust.NewDustPerfect(vortexCenter + orbitOffset, DustID.GoldFlame, tangent * Main.rand.NextFloat(2f, 5f + chargeProgress * 6f), 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }

            if (rightChargeTimer > 0 && rightChargeTimer % 60 == 0)
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = MathHelper.Lerp(-0.3f, 0.2f, chargeProgress) }, Projectile.Center);
        }

        private void RunIdle()
        {
            Animation--;
            CanHit = false;
            postSwing = false;
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.22f);
            TurnIdleBladeTowardMouse();

            idleTimer++;
            if (idleTimer >= IdleDismissFrames)
                BeginDyingFade();
        }

        private void RunRecovery()
        {
            CanHit = false;
            postSwing = false;
            fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.18f);
            Animation--;
            TurnIdleBladeTowardMouse();

            if (postSwingCooldown > 0)
                postSwingCooldown--;

            if (postSwingCooldown <= 0)
            {
                motionState = BladeMotionState.Idle;
                stateTimer = 0;
            }
        }

        private void BeginSwing(int swingId)
        {
            motionState = BladeMotionState.Swing;
            stateTimer = 0;
            Animation = (int)(useAnim * 0.7f);
            swingStartAngle = FinalRotation;
            bladeAngularVelocity = 0f;
            smoothedMouseBladeAngle = FinalRotation;
            smoothedMouseBladeVelocity = 0f;
            lastSwingId = swingId;
            playSwingSound = true;
            bladeHitFireballsThisSpin = 0;
            idleTimer = 0;
            rightChargeTimer = 0;
            postSwingCooldown = 0;
            postSwing = false;
            CanHit = false;
            auricJudgementQueued = Owner.GetModPlayer<YharimsCrystalStatePlayer>().HasAuricJudgement;
            auricJudgementReleased = false;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
        }

        private void RunSwing()
        {
            int swingDirection = Projectile.ai[1] >= 0f ? 1 : -1;
            UpdateBaseBladeRotation();

            AnimationProgress = Animation % useAnim;
            float time = AnimationProgress - useAnim / 3f;
            float timeMax = useAnim - useAnim / 3f;
            float swingCompletion = MathHelper.Clamp(time / timeMax, 0f, 1f);

            CanHit = time > (int)(timeMax * 0.2f) && time < (int)(timeMax * 0.9f);
            postSwing = time < (int)(timeMax * 0.75f);
            fadeIn = MathHelper.Lerp(fadeIn, CanHit ? 1f : 0f, CanHit ? 0.5f : 0.2f);

            if (time >= (int)(timeMax * 0.4f) && playSwingSound)
            {
                SoundStyle swing = new("CalamityMod/Sounds/Item/SwingMid");
                SoundEngine.PlaySound(swing with { Volume = 0.8f, Pitch = (Projectile.ai[1] == 1 ? -0.4f : -0.1f) }, Projectile.Center);
                SoundStyle swing2 = new("CalamityMod/Sounds/Item/HellkiteSwing", 2);
                SoundEngine.PlaySound(swing2 with { Volume = 0.8f, Pitch = (Projectile.ai[1] == 1 ? 0.4f : 0.7f) }, Projectile.Center);
                playSwingSound = false;
            }

            if (stateTimer % 2 == 0 && Projectile.ai[1] == 1 && !Main.dedServ)
            {
                SoundStyle swoosh = new("CalamityMod/Sounds/Item/SwooshMid");
                SoundEngine.PlaySound(swoosh with { Volume = 1f, Pitch = -0.4f, MaxInstances = -1 }, Projectile.Center);
            }

            float easedSwing = CalamityUtils.ExpInOutEasing(swingCompletion * 0.9f, 1);
            float targetOffset = MathHelper.ToRadians(MathHelper.Lerp(
                150f * Projectile.ai[1] * Owner.direction,
                120f * -Projectile.ai[1] * Owner.direction,
                easedSwing));
            RotationOffset = MathHelper.Lerp(RotationOffset, targetOffset, 0.2f);
            bladeAngle = FinalRotation;

            if (auricJudgementQueued && !auricJudgementReleased && swingCompletion >= 0.28f)
                ReleaseAuricJudgementWave(swingDirection);

            if (CanHit)
                SpawnSwingParticles(swingDirection);

            Animation++;
            stateTimer++;
            if (time >= timeMax * 0.9f)
                EndSwing();
        }

        private void EndSwing()
        {
            Projectile.ai[1] = -Projectile.ai[1];
            bladeAngle = MathHelper.WrapAngle(FinalRotation);
            swingStartAngle = bladeAngle;
            bladeAngularVelocity = 0f;
            smoothedMouseBladeAngle = FinalRotation;
            smoothedMouseBladeVelocity = 0f;
            motionState = BladeMotionState.Recovery;
            postSwingCooldown = PostSwingCooldownMax;
            stateTimer = 0;
            idleTimer = 0;
            CanHit = false;
            postSwing = false;
            auricJudgementQueued = false;
            auricJudgementReleased = false;
        }

        private void SpawnSwingParticles(int swingDirection)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 particleVel = new Vector2(0, 10 * -swingDirection * lockedFacingDirection).RotatedBy(FinalRotation + MathHelper.ToRadians(-45));
                Vector2 particlePos = Owner.Center + (new Vector2(Main.rand.Next(30, (int)(170 * (1 + bladeFade * 0.6f))), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45))) * Projectile.scale;
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(particlePos, -particleVel.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.75f, 0.9f), false, 14, Main.rand.NextFloat(0.06f, 0.03f) * Projectile.scale, Color.Lerp(BladeOrange, BladeGold, 0.5f), new Vector2(1.3f, 0.2f), true, false, 0.55f));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 particleVel = new Vector2(0, 10 * -swingDirection * lockedFacingDirection).RotatedBy(FinalRotation + MathHelper.ToRadians(-45));
                Vector2 particlePos = Owner.Center + (new Vector2(Main.rand.Next(30, (int)(270 * (1 + bladeFade * 0.6f))), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45))) * Projectile.scale;
                if (Main.rand.NextBool(3))
                {
                    Particle sparker = new CustomSpark(particlePos + Main.rand.NextVector2Circular(15, 15), -particleVel * Main.rand.NextFloat(0.4f, 0.8f), "CalamityMod/Particles/Sparkle", false, 30, Main.rand.NextFloat(1.2f, 2.2f) * Projectile.scale, BladeGold, new Vector2(0.4f, Main.rand.NextFloat(0.9f, 1.4f)), true, true);
                    GeneralParticleHandler.SpawnParticle(sparker);
                }
                else
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(particlePos, -particleVel.RotatedByRandom(0.2f) * 2, "CalamityMod/Particles/LargeBloom", false, Main.rand.Next(7, 9 + 1), Main.rand.NextFloat(0.3f, 0.35f) * Projectile.scale, BladeOrange * 0.65f, new Vector2(1f, 1.2f), true, false, 0, false, false, 0.45f));
            }

            for (int i = 0; i < 3; i++)
            {
                float randRot = Main.rand.NextFloat(-30, -60);
                Vector2 dustVel = new Vector2(0, 10 * -swingDirection * lockedFacingDirection).RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));
                GenericSparkle sparker = new GenericSparkle(Owner.Center + (new Vector2(270 * (1 + bladeFade * 0.6f), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45)).RotatedByRandom(0.3f)) * Projectile.scale, Vector2.Zero, Color.White, BladeGold, Main.rand.NextFloat(0.5f, 0.7f) * Projectile.scale, 11, Main.rand.NextFloat(-0.1f, 0.1f), 2.68f);
                GeneralParticleHandler.SpawnParticle(sparker);
                Dust dust2 = Dust.NewDustPerfect(Owner.Center + (new Vector2(270 * (1 + bladeFade * 0.6f), 0).RotatedBy(FinalRotation + MathHelper.ToRadians(-45)).RotatedByRandom(0.3f)) * Projectile.scale, DustID.GoldFlame, dustVel);
                dust2.scale = Main.rand.NextFloat(0.65f, 1.05f);
                dust2.noGravity = true;
                dust2.color = Color.Lerp(Color.White, BladeGold, 0.5f);
            }
        }

        private void ApplyChargeArmPose()
        {
            float chargeProgress = rightChargeTimer / (float)RightChargeFrames;
            float windup = CalamityUtils.PiecewiseAnimation(chargeProgress, ChargeWindupCurve);
            ArmRotationOffset = MathHelper.ToRadians(-140f) + windup;
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f) + windup;
        }

        private void ThrowBlade()
        {
            StopChargeSpinSound();

            Vector2 launchDirection = -Vector2.UnitY;
            Vector2 launchPosition = GetBladeReleasePosition();

            int thrown = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                launchPosition,
                Vector2.Zero,
                ModContent.ProjectileType<YC_ThrownBlade>(),
                (int)(Projectile.damage * 2.3f),
                Projectile.knockBack * 2f,
                Projectile.owner,
                3f,
                0f,
                bladeAngle); // ai[0] = StateRightThrow, ai[2] = carried visual angle

            if (Main.projectile.IndexInRange(thrown))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[thrown], YCWeaponForm.Blade);
                Main.projectile[thrown].CritChance = Projectile.CritChance;
                Main.projectile[thrown].scale = Projectile.scale;
                Main.projectile[thrown].rotation = bladeAngle;
            }

            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 10f);
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1f, Pitch = -0.35f }, Owner.Center);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.12f }, Owner.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(launchPosition, Vector2.Zero, BladeGold, Vector2.One, launchDirection.ToRotation(), 0.12f, 2.4f, 22));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(launchPosition, Vector2.Zero, BladeOrange, Vector2.One, launchDirection.ToRotation(), 0.07f, 1.7f, 16));
                for (int i = 0; i < 48; i++)
                {
                    Vector2 vel = launchDirection.RotatedByRandom(0.85f) * Main.rand.NextFloat(4f, 22f);
                    Dust d = Dust.NewDustPerfect(launchPosition + Main.rand.NextVector2Circular(18f, 18f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(1.0f, 1.6f));
                    d.noGravity = true;
                }

                for (int i = 0; i < 10; i++)
                {
                    float angle = MathHelper.TwoPi * i / 10;
                    Vector2 sparkVel = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 12f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(launchPosition, sparkVel, "CalamityMod/Particles/Sparkle", false, Main.rand.Next(16, 24), Main.rand.NextFloat(0.6f, 1.05f), BladeGold, new Vector2(0.3f, 1f), true, true, shrinkSpeed: 0.14f));
                }
            }

            DrawUnconditionally = false;
            Projectile.Kill();
        }

        private Vector2 GetBladeReleasePosition()
        {
            return Owner.MountedCenter + CurrentAimDirection.SafeNormalize(Vector2.UnitX * lockedFacingDirection) * (142f * Projectile.scale);
        }

        private void ReleaseAuricJudgementWave(int swingDirection)
        {
            auricJudgementReleased = true;
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!Owner.GetModPlayer<YharimsCrystalStatePlayer>().TryConsumeAuricJudgement())
                return;

            Vector2 waveDirection = (GetMouseWorld() - Owner.MountedCenter).SafeNormalize(CurrentAimDirection);
            Vector2 spawnPosition = Owner.MountedCenter + waveDirection * (120f * Projectile.scale);
            int wave = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                waveDirection * 24f,
                ModContent.ProjectileType<YC_AuricJudgementWave>(),
                Math.Max(1, (int)(Projectile.damage * 1.15f)),
                Projectile.knockBack * 0.9f,
                Projectile.owner,
                6f,
                swingDirection);

            if (Main.projectile.IndexInRange(wave))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[wave], YCWeaponForm.Blade);
                Main.projectile[wave].CritChance = Projectile.CritChance;
            }

            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.72f, Pitch = -0.16f }, spawnPosition);
            if (!Main.dedServ)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(spawnPosition, Vector2.Zero, BladeGold, Vector2.One, waveDirection.ToRotation(), 0.08f, 1.7f, 18));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.2f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FinalDawnSlash") { Volume = 0.62f, Pitch = Main.rand.NextFloat(0.08f, 0.24f) }, target.Center);

            if (Projectile.owner == Main.myPlayer &&
                bladeHitFireballsThisSpin < MaxBladeHitFireballs &&
                YC_BurningShard.CanSpawnFollowFireballFor(Owner))
            {
                int orbitSlot = YC_BurningShard.NextFollowOrbitSlotFor(Owner);
                int shard = Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<YC_BurningShard>(),
                    (int)(Projectile.damage * 0.85f),
                    Projectile.knockBack * 0.2f,
                    Projectile.owner,
                    2f,
                    orbitSlot);
                if (Main.projectile.IndexInRange(shard))
                {
                    bladeHitFireballsThisSpin++;
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[shard], YCWeaponForm.Crystal);
                    Main.projectile[shard].CritChance = Projectile.CritChance;
                }
            }

            SpawnHitEffects(target);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float falloff = Utils.Remap(Projectile.numHits, 0f, 7f, 1.18f, 0.64f, true);
            modifiers.SourceDamage *= falloff;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write7BitEncodedInt((int)motionState);
            writer.Write(willDie);
            writer.Write7BitEncodedInt(useAnim);
            writer.Write7BitEncodedInt(lastSwingId);
            writer.Write7BitEncodedInt(stateTimer);
            writer.Write7BitEncodedInt(postSwingCooldown);
            writer.Write7BitEncodedInt(idleTimer);
            writer.Write7BitEncodedInt(rightChargeTimer);
            writer.WriteVector2(aimVel);
            writer.WriteVector2(mousePos);
            writer.Write(Animation);
            writer.Write(RotationOffset);
            writer.Write(Projectile.rotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            motionState = (BladeMotionState)reader.Read7BitEncodedInt();
            willDie = reader.ReadBoolean();
            useAnim = reader.Read7BitEncodedInt();
            lastSwingId = reader.Read7BitEncodedInt();
            stateTimer = reader.Read7BitEncodedInt();
            postSwingCooldown = reader.Read7BitEncodedInt();
            idleTimer = reader.Read7BitEncodedInt();
            rightChargeTimer = reader.Read7BitEncodedInt();
            aimVel = reader.ReadVector2();
            mousePos = reader.ReadVector2();
            Animation = reader.ReadSingle();
            RotationOffset = reader.ReadSingle();
            Projectile.rotation = reader.ReadSingle();
            bladeAngle = FinalRotation;
        }

        private void SpawnHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            Vector2 lockedAimDirection = CurrentAimDirection.SafeNormalize(Vector2.UnitX * lockedFacingDirection);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = lockedAimDirection.RotatedByRandom(0.62f) * Main.rand.NextFloat(5f, 18f);
                Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? BladeWhite : BladeGold, Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, BladeGold * 0.8f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.12f, 0.72f, 16, true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> ghost = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/EarthGhost");
            Asset<Texture2D> glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/EarthGlow");
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");

            float swordRotation = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = FlipAsSword ? new Vector2(texture.Value.Width - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Color aura = Color.Lerp(BladeOrange, BladeGold, 0.55f) with { A = 0 };

            if (CanHit || postSwing)
            {
                Main.EntitySpriteDraw(
                    swoosh.Value, drawPosition, null,
                    aura * fadeIn * (CanHit ? 0.9f : 0.64f) * dyingFade,
                    FinalRotation + MathHelper.ToRadians(45f) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -82f : 82f) * -lockedFacingDirection,
                    swoosh.Size() * 0.5f,
                    Projectile.scale * (CanHit ? 1.08f : 0.82f),
                    SpriteEffects.None);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 4.8f * fadeIn;
                Main.EntitySpriteDraw(ghost.Value, drawPosition + offset, null, aura * 0.13f * fadeIn * dyingFade, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            }

            // Real silhouette outline: the blade sprite itself, redrawn in solid bright
            // yellow at a ring of offsets directly behind the main sprite. Unmistakable
            // the instant the right-click charge begins, unlike a soft glow blob.
            float chargeOutline = MathHelper.Clamp(chargeBorderIntensity, 0f, 1f);
            if (chargeOutline > 0.02f)
            {
                Color outlineColor = Color.Lerp(BladeOrange, Color.Yellow, chargeOutline) with { A = 0 };
                float outlineRadius = 3.5f + chargeOutline * 5.5f;
                int rays = 14;
                for (int i = 0; i < rays; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / rays).ToRotationVector2() * outlineRadius;
                    Main.EntitySpriteDraw(texture.Value, drawPosition + offset, null, outlineColor * (0.55f + chargeOutline * 0.4f), Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
                }

                float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f);
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(bloom.Value, drawPosition, null, Color.Yellow with { A = 0 } * 0.5f * chargeOutline * pulse, 0f, bloom.Size() * 0.5f, Projectile.scale * (0.6f + chargeOutline * 0.5f) * pulse, SpriteEffects.None);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offsetDir = Vector2.One.RotatedBy(Projectile.rotation + RotationOffset + MathHelper.ToRadians(90f));
                bool tip = i > 13;
                float tipScale = tip ? Utils.Remap(i, 13f, 18f, 0.85f, 0.34f) : 1f;
                Vector2 drawOffset = -offsetDir * 8f * i * bladeFade;
                Main.EntitySpriteDraw(
                    bloom.Value,
                    Projectile.Center - offsetDir * 68f - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
                    null,
                    Color.Lerp(BladeGold, BladeWhite, 0.28f) with { A = 0 } * 0.28f * bladeFade * dyingFade,
                    RotationOffset + Projectile.rotation + MathHelper.ToRadians(45f),
                    bloom.Size() * 0.5f,
                    new Vector2(0.58f * tipScale, 1f) * 0.42f * tipScale * Projectile.scale * bladeFade,
                    effects);
            }

            Color bodyColor = Color.Lerp(lightColor, Color.Yellow, chargeOutline) * dyingFade;
            Main.EntitySpriteDraw(texture.Value, drawPosition, null, bodyColor, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(glow.Value, drawPosition, null, BladeGold * 0.8f * dyingFade, Projectile.rotation + RotationOffset + swordRotation, origin, Projectile.scale, effects);
            return false;
        }
    }
}
