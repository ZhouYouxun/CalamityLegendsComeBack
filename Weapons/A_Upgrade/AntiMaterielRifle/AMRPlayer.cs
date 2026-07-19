using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRPlayer : ModPlayer
    {
        private const int DoubleTapWindowFrames = 15;
        private const float GoldenAngle = 2.39996323f;

        private bool holdingWeapon;
        private bool scopeRequested;
        private float requestedScopeCompletion;

        private int slideTimer;
        private int slideCooldown;
        private int slideChainWindow;
        private int slidesRemaining;
        private int rotationRecoveryTimer;
        private int doubleTapWindow;
        private int lastTapDirection;
        private int slideVisualAge;
        private int slideSerial;
        private Vector2 slideDirection;
        private bool slideEmpoweredShot;

        private int calibrationTarget = -1;
        private int calibrationStacks;
        private int calibrationTimer;
        private bool nextOnyxRoundIsMarker = true;

        internal bool IsSliding => slideTimer > 0;
        internal int SlideCooldown => slideCooldown;

        public override void Initialize()
        {
            nextOnyxRoundIsMarker = true;
        }

        public override void ResetEffects()
        {
            holdingWeapon = false;
            scopeRequested = false;
            requestedScopeCompletion = 0f;
        }

        internal void SetHoldingWeapon() => holdingWeapon = true;

        internal void RequestScope(float completion, bool zoomEnabled)
        {
            if (!zoomEnabled)
                return;

            scopeRequested = true;
            requestedScopeCompletion = MathHelper.Clamp(completion, 0f, 1f);
            Player.scope = true;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            if (Player.HeldItem.ModItem is not NewLegendAntiMaterielRifle || !AMRBalance.SlideUnlocked)
            {
                doubleTapWindow = 0;
                lastTapDirection = 0;
                return;
            }

            if (doubleTapWindow > 0)
                doubleTapWindow--;

            int dashDirection = 0;
            if (CalamityKeybinds.DashHotkey?.JustPressed == true)
                dashDirection = ResolveManualDashDirection();
            else
                dashDirection = DetectDoubleTapDirection();

            if (dashDirection != 0)
                TryStartSlide(dashDirection);
        }

        private int ResolveManualDashDirection()
        {
            if (Player.controlRight && !Player.controlLeft)
                return 1;
            if (Player.controlLeft && !Player.controlRight)
                return -1;
            if (MathF.Abs(Player.velocity.X) > 0.01f)
                return Player.velocity.X > 0f ? 1 : -1;
            return Player.direction == 0 ? 1 : Player.direction;
        }

        private int DetectDoubleTapDirection()
        {
            int pressedDirection = 0;
            if (Player.controlLeft && Player.releaseLeft && !Player.controlRight)
                pressedDirection = -1;
            else if (Player.controlRight && Player.releaseRight && !Player.controlLeft)
                pressedDirection = 1;

            if (pressedDirection == 0)
                return 0;

            if (lastTapDirection == pressedDirection && doubleTapWindow > 0)
            {
                lastTapDirection = 0;
                doubleTapWindow = 0;
                return pressedDirection;
            }

            lastTapDirection = pressedDirection;
            doubleTapWindow = DoubleTapWindowFrames;
            return 0;
        }

        private void TryStartSlide(int direction)
        {
            if (slideTimer > 0)
                return;

            if (slideChainWindow > 0 && slidesRemaining > 0)
            {
                StartSlide(direction);
                return;
            }

            if (slideCooldown > 0)
                return;

            slidesRemaining = 2;
            slideChainWindow = AMRBalance.SlideChainWindowFrames;
            slideCooldown = AMRBalance.SlideCooldownFrames;
            StartSlide(direction);
        }

        private void StartSlide(int direction)
        {
            direction = direction < 0 ? -1 : 1;
            slideDirection = Vector2.UnitX * direction;
            slideTimer = AMRBalance.SlideFrames;
            slideVisualAge = 0;
            slideSerial++;
            slidesRemaining--;
            slideEmpoweredShot = true;
            rotationRecoveryTimer = 12;
            Player.ChangeDir(direction);
            Player.dashTime = 0;
            Player.dashDelay = 1;
            Player.Calamity().dashTimeMod = 0;
            Player.Calamity().GeneralScreenShakePower = 3f;
            SpawnSlideStartEffects();
        }

        public override void PostUpdate()
        {
            if (slideCooldown > 0)
                slideCooldown--;

            if (slideChainWindow > 0)
            {
                slideChainWindow--;
                if (slideChainWindow <= 0)
                    slidesRemaining = 0;
            }

            if (calibrationTimer > 0)
            {
                calibrationTimer--;
                if (calibrationTimer <= 0)
                    ResetCalibration();
            }

            if (slideTimer > 0)
            {
                float speed = AMRBalance.DimensionalSlideUnlocked ? 28f : 22f;
                Player.velocity = slideDirection * speed;
                Player.immune = true;
                Player.immuneTime = System.Math.Max(Player.immuneTime, 2);
                Player.fallStart = (int)(Player.position.Y / 16f);
                Player.dashTime = 0;
                Player.dashDelay = System.Math.Max(Player.dashDelay, 1);
                Player.Calamity().dashTimeMod = 0;
                Player.fullRotation = -0.22f * slideDirection.X * Player.gravDir;
                Player.fullRotationOrigin = Player.Size * 0.5f;
                slideVisualAge++;
                SpawnSlideTrailEffects();
                slideTimer--;

                if (slideTimer <= 0)
                    SpawnSlideEndEffects();
            }
            else if (rotationRecoveryTimer > 0)
            {
                Player.fullRotation = MathHelper.Lerp(Player.fullRotation, 0f, 0.35f);
                rotationRecoveryTimer--;
                if (rotationRecoveryTimer <= 0)
                    Player.fullRotation = 0f;
            }

            if (!holdingWeapon && calibrationTimer <= 0)
                ResetCalibration();
        }

        private void SpawnSlideStartEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = slideDirection;
            Vector2 normal = new(-forward.Y, forward.X);
            Vector2 center = Player.Center;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.38f }, center);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, -forward * 0.5f,
                new Color(73, 48, 12), new Vector2(0.55f, 1.45f), forward.ToRotation(), 0.08f, 1.25f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, -forward * 0.25f,
                new Color(255, 195, 58), new Vector2(0.42f, 1.18f), forward.ToRotation(), 0.04f, 0.88f, 15));

            const int vertexCount = 12;
            for (int i = 0; i < vertexCount; i++)
            {
                float theta = MathHelper.TwoPi * i / vertexCount;
                Vector2 ellipseOffset = -forward * MathF.Cos(theta) * 17f + normal * MathF.Sin(theta) * 30f;
                Vector2 velocity = -forward * (3.2f + MathF.Cos(theta) * 1.5f) + normal * MathF.Sin(theta) * 5f;
                Color color = i % 2 == 0 ? new Color(255, 210, 82) : new Color(128, 78, 14);
                GeneralParticleHandler.SpawnParticle(new LineParticle(center + ellipseOffset, velocity,
                    false, 15, 0.42f, color));
            }

            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(center - forward * 8f, -forward * 2.5f,
                new Color(15, 11, 7), 22, 0.72f, 0.82f, 0.04f * forward.X, false, required: true));
        }

        private void SpawnSlideTrailEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = slideDirection;
            Vector2 normal = new(-forward.Y, forward.X);
            float progress = MathHelper.Clamp(slideVisualAge / (float)AMRBalance.SlideFrames, 0f, 1f);
            float phase = (slideVisualAge + slideSerial * 0.5f) * GoldenAngle;
            float envelope = MathF.Sin(progress * MathHelper.Pi);
            float radius = 9f + envelope * 9f;

            for (int sign = -1; sign <= 1; sign += 2)
            {
                float axialWave = MathF.Cos(phase) * 5f;
                Vector2 position = Player.Center - forward * (12f + axialWave) + normal * radius * sign;
                Vector2 velocity = -forward * (5f + envelope * 2.5f) + normal * MathF.Sin(phase) * 2f * sign;
                Color color = sign < 0 ? new Color(255, 201, 62) : new Color(163, 100, 16);

                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity, false, 12, 0.38f, color));
                if (slideVisualAge % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(position, -forward * 1.8f,
                        false, 12, 0.32f, color, true, false, true));
                }
            }

            if (slideVisualAge % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Player.Center - forward * (20f + MathF.Cos(phase * 0.5f) * 4f),
                    -forward * 3.2f,
                    new Color(18, 13, 8),
                    19,
                    0.48f + envelope * 0.12f,
                    0.76f,
                    0.035f * (slideSerial % 2 == 0 ? 1f : -1f),
                    false,
                    required: true));
            }

            if (slideVisualAge % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Player.Center - forward * 28f,
                    -forward * 1.2f,
                    new Color(208, 137, 24),
                    new Vector2(0.32f, 0.92f),
                    forward.ToRotation(),
                    0.03f,
                    0.42f + envelope * 0.18f,
                    11));
            }
        }

        private void SpawnSlideEndEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = slideDirection;
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.36f, Pitch = -0.15f }, Player.Center);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center - forward * 8f,
                -forward * 1.5f, new Color(83, 53, 12), new Vector2(0.42f, 1.05f),
                forward.ToRotation(), 0.16f, 0.72f, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center - forward * 5f,
                -forward, new Color(255, 205, 76), new Vector2(0.3f, 0.82f),
                forward.ToRotation(), 0.08f, 0.48f, 11));
        }

        public override void ModifyZoom(ref float zoom)
        {
            if (!scopeRequested || zoom < 0f)
                return;

            // Terraria's native scope camera supplies 0.5 here for ordinary
            // sniper rifles. Scale that native pan by the aim charge so the
            // camera grows smoothly from no offset to the vanilla 50% limit.
            float completion = requestedScopeCompletion;
            float easedCompletion = completion * completion * (3f - 2f * completion);
            zoom *= easedCompletion;
        }

        public override void UpdateDead()
        {
            slideTimer = 0;
            slideChainWindow = 0;
            slidesRemaining = 0;
            doubleTapWindow = 0;
            lastTapDirection = 0;
            slideEmpoweredShot = false;
            ResetCalibration();
        }

        internal float ConsumeSlideEmpowerment()
        {
            if (!slideEmpoweredShot)
                return 1f;

            slideEmpoweredShot = false;
            return AMRBalance.DimensionalSlideUnlocked ? 1.65f : 1.35f;
        }

        internal float GetCalibrationMultiplier(int targetIndex)
        {
            if (!AMRBalance.CalibrationUnlocked || calibrationTarget != targetIndex || calibrationStacks < 2)
                return 1f;

            return 1.45f;
        }

        internal void RegisterCalibrationHit(int targetIndex)
        {
            if (!AMRBalance.CalibrationUnlocked)
                return;

            if (calibrationTarget == targetIndex)
            {
                if (calibrationStacks >= 2)
                    calibrationStacks = 0;
                else
                    calibrationStacks++;
            }
            else
            {
                calibrationTarget = targetIndex;
                calibrationStacks = 1;
            }

            calibrationTimer = 3 * 60;
        }

        internal void ResetCalibration()
        {
            calibrationTarget = -1;
            calibrationStacks = 0;
            calibrationTimer = 0;
        }

        internal bool ConsumeOnyxRoundType()
        {
            if (!AMRBalance.OnyxSequenceUnlocked)
                return false;

            bool marker = nextOnyxRoundIsMarker;
            nextOnyxRoundIsMarker = !nextOnyxRoundIsMarker;
            return marker;
        }
    }
}
