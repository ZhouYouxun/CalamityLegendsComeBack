using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    public class BrinyBaron_LeftClick_Swing : BaseCustomUseStyleProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();
        public override Vector2 SpriteOrigin => new(0f, 102f);
        public override float HitboxOutset => 118f;
        public override Vector2 HitboxSize => new Vector2(182f, 182f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private const int ComboLength = 8;
        private const int RightSpinTransitionFrames = 14;
        private const int RightSpinHitboxInterval = 14;
        private const float RightSpinMaxAngularVelocity = 6.5f * MathHelper.Pi / 180f;
        private const float RightSpinAngularAcceleration = 0.09f;

        private int comboIndex;
        private int currentStage;
        private int stageTimer;
        private int gapTimer;
        private int stageDuration;
        private int swingDirection = 1;
        private bool stageActive;
        private bool releaseRequested;
        private bool stageEventFired;
        private bool swingSoundPlayed;
        private bool postSwing;
        private float fadeIn;
        private float currentRangeScale = 1f;
        private float currentDrawScale = 1f;
        private float lengthScale = 1f;
        private float thicknessScale = 1f;
        private float currentTiltDegrees;
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection = Vector2.UnitX;

        private bool rightSpinActive;
        private int rightSpinTimer;
        private int rightSpinDirection = 1;
        private bool rightSpinSound = true;
        private float rightSpinAngularVelocity;
        private int rightSpinTransitionTimer;
        private float rightSpinTransitionStartOffset;

        private float SlashAngle => FinalRotation + MathHelper.ToRadians(-45f);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 0;
            Projectile.scale = 1f;
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;
            Projectile.ai[1] = -1f;
            currentDrawScale = 1f;
            currentRangeScale = 1f;
            lengthScale = 1f;
            thicknessScale = 1f;
            UpdateLockedAimFromMouse();
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                WhenSpawned();
                whenSpawned = false;
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                Projectile.netUpdate = true;
            }

            if (Owner.HeldItem.type != AssignedItemID || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Animation++;
            UseStyle();
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);

            int itemAnimationMax = Math.Max(1, Owner.itemAnimationMax);
            AnimationProgress = Animation % itemAnimationMax;

            if (AbsolutePosition == Vector2.Zero)
                Projectile.position = Owner.position + Owner.Size / 2f - Projectile.Size / 2f + Offset;
            else
            {
                AbsolutePosition += Projectile.velocity;
                Projectile.position = AbsolutePosition - Projectile.Size / 2f + Offset;
            }

            if (AnimationProgress == itemAnimationMax - 1)
            {
                OnEndUse();
                NumberOfAnimations++;
            }

            if (Owner.itemAnimation == itemAnimationMax - 1)
            {
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                OnBeginUse();
            }

            Projectile.Center = Owner.MountedCenter;
            Projectile.scale = 1f;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
        }

        public override bool? CanDamage() => CanHit;

        public override void ResetStyle()
        {
            CanHit = false;
        }

        public override void UseStyle()
        {
            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.Center = Owner.MountedCenter;

            if (!IsLeftHeld())
                releaseRequested = true;

            if (!releaseRequested && (rightSpinActive || WantsRightSpin()))
            {
                DoRightSpin();
                ApplyArmRotation();
                return;
            }

            if (rightSpinActive)
            {
                EndRightSpin();
                ApplyArmRotation();
                return;
            }

            if (!stageActive)
            {
                CanHit = false;
                postSwing = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.25f);

                if (releaseRequested)
                {
                    Projectile.Kill();
                    return;
                }

                if (gapTimer > 0)
                {
                    gapTimer--;
                    ApplyIdleRotation();
                    ApplyArmRotation();
                    return;
                }

                StartStage();
                ApplyArmRotation();
                return;
            }

            RunStage();
            ApplyArmRotation();
        }

        private void StartStage()
        {
            StageProfile profile = GetStageProfile(comboIndex % ComboLength);
            stageActive = true;
            currentStage = comboIndex % ComboLength;
            stageDuration = profile.Duration;
            stageTimer = 0;
            stageEventFired = false;
            swingSoundPlayed = false;
            postSwing = false;
            CanHit = false;
            currentDrawScale = 1f;
            currentRangeScale = 1f;
            lengthScale = 1f;
            thicknessScale = 1f;
            currentTiltDegrees = profile.Tilted ? (currentStage % 2 == 0 ? -32f : 32f) + Main.rand.NextFloat(-10f, 10f) : 0f;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            UpdateLockedAimFromMouse();
            swingDirection = comboIndex % 2 == 0 ? -1 : 1;
            Projectile.ai[1] = swingDirection;
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
        }

        private void RunStage()
        {
            stageTimer++;
            StageProfile profile = GetStageProfile(currentStage);
            int impactFrame = Math.Max(3, (int)(profile.Duration * 0.34f));

            if (stageTimer < impactFrame)
            {
                UpdateLockedAimFromMouse();
                CanHit = false;
                postSwing = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.32f);
                lengthScale = 1f;
                thicknessScale = 1f;
                currentRangeScale = 1f;
                Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.24f);
                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians((118f * swingDirection * Owner.direction) + currentTiltDegrees),
                    0.18f);

                if (stageTimer >= stageDuration)
                    EndStage(profile);

                return;
            }

            if (!postSwing)
                FlipAsSword = Owner.direction < 0;

            postSwing = true;
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.18f);

            float swingTime = stageTimer - impactFrame;
            float swingTimeMax = Math.Max(1f, stageDuration - impactFrame);
            float swingProgress = MathHelper.Clamp(swingTime / swingTimeMax, 0f, 1f);
            float easedProgress = CalamityUtils.ExpInOutEasing(swingProgress, 1);
            lengthScale = 1f;
            thicknessScale = 1f;
            currentRangeScale = 1f;
            currentDrawScale = 1f;

            bool hitWindow = swingProgress > 0.12f && swingProgress < 0.74f;
            CanHit = hitWindow;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.32f : 0.35f);

            if (swingProgress >= 0.1f && !swingSoundPlayed)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = profile.Kind == ComboKind.FinalWave ? 0.92f : 0.78f, Pitch = Main.rand.NextFloat(0.08f, 0.22f) }, Owner.Center);
                SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.45f, Pitch = Main.rand.NextFloat(0.08f, 0.22f) }, Owner.Center);
                swingSoundPlayed = true;
            }

            if (!stageEventFired && swingProgress >= 0.24f)
            {
                FireStageProjectile(profile.Kind);
                stageEventFired = true;
            }

            float swingFacing = swingDirection * Owner.direction;
            RotationOffset = MathHelper.Lerp(
                RotationOffset,
                MathHelper.ToRadians(MathHelper.Lerp(145f * swingFacing + currentTiltDegrees, 112f * -swingFacing + currentTiltDegrees, easedProgress)),
                0.28f);

            if (CanHit)
                SpawnSwingParticles(profile);

            if (stageTimer >= stageDuration)
                EndStage(profile);
        }

        private void EndStage(StageProfile profile)
        {
            stageActive = false;
            CanHit = false;
            postSwing = false;
            stageTimer = 0;
            comboIndex++;
            gapTimer = profile.GapFrames;

            if (releaseRequested)
                Projectile.Kill();
        }

        private void FireStageProjectile(ComboKind kind)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            switch (kind)
            {
                case ComboKind.OpeningShuriken:
                    SpawnOpeningShurikenVolley();
                    ApplyScreenShake(4.5f);
                    break;
                case ComboKind.MediumTornado:
                    SpawnTornadoBolt();
                    break;
                case ComboKind.SmallWater:
                    SpawnWaterStreams();
                    break;
                case ComboKind.FinalWave:
                    SpawnFinalWave();
                    ApplyScreenShake(6f);
                    break;
            }
        }

        private void SpawnOpeningShurikenVolley()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < 5; i++)
            {
                float progress = i / 4f;
                float spread = MathHelper.Lerp(-0.34f, 0.34f, progress);
                Vector2 velocity = shootDirection.RotatedBy(spread).RotatedByRandom(0.035f) * Main.rand.NextFloat(12.5f, 15.5f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 28f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                    Math.Max(1, (int)(Projectile.damage * 0.44f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    1f);
            }
        }

        private void SpawnTornadoBolt()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 34f,
                shootDirection.RotatedByRandom(0.075f) * 16.5f,
                ModContent.ProjectileType<BrinyBaron_TornadoBolt>(),
                Math.Max(1, (int)(Projectile.damage * 0.58f)),
                Projectile.knockBack * 0.65f,
                Projectile.owner);
        }

        private void SpawnWaterStreams()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            int count = Main.rand.Next(2, 4);

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = shootDirection.RotatedBy(Main.rand.NextFloat(-0.34f, 0.34f)) * Main.rand.NextFloat(12f, 15f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 30f + shootDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-16f, 16f),
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_WaterStream>(),
                    Math.Max(1, (int)(Projectile.damage * 0.34f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        private void SpawnFinalWave()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 44f,
                shootDirection * 12.4f,
                ModContent.ProjectileType<BBSwing_Wave>(),
                Math.Max(1, (int)(Projectile.damage * 0.86f)),
                Projectile.knockBack,
                Projectile.owner,
                2.42f,
                3f);
        }

        private void DoRightSpin()
        {
            if (!WantsRightSpin())
            {
                EndRightSpin();
                return;
            }

            if (!rightSpinActive)
                StartRightSpin();

            rightSpinTimer++;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            DrawUnconditionally = true;
            currentDrawScale = 1f;
            currentRangeScale = 1f;
            lengthScale = 1f;
            thicknessScale = MathHelper.Lerp(thicknessScale, 1f, 0.18f);
            UpdateLockedAimFromMouse();

            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            Projectile.rotation = Projectile.rotation.AngleLerp(lockedAimDirection.ToRotation() + MathHelper.PiOver4, 0.42f);

            if (rightSpinTransitionTimer < RightSpinTransitionFrames)
            {
                rightSpinTransitionTimer++;
                float transitionProgress = rightSpinTransitionTimer / (float)RightSpinTransitionFrames;
                float easedTransition = 1f - (float)Math.Pow(1f - transitionProgress, 3f);
                RotationOffset = Utils.AngleLerp(rightSpinTransitionStartOffset, 0f, easedTransition);
                rightSpinAngularVelocity = 0f;
                postSwing = true;
                CanHit = false;
                return;
            }

            float targetAngularVelocity = RightSpinMaxAngularVelocity * rightSpinDirection;
            rightSpinAngularVelocity = MathHelper.Lerp(rightSpinAngularVelocity, targetAngularVelocity, RightSpinAngularAcceleration);
            RotationOffset += rightSpinAngularVelocity;
            postSwing = true;
            CanHit = false;

            float spinSpeedRatio = MathHelper.Clamp(Math.Abs(rightSpinAngularVelocity) / RightSpinMaxAngularVelocity, 0f, 1f);
            if (rightSpinTimer % 34 == 1 || (rightSpinSound && spinSpeedRatio > 0.72f))
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.66f, Pitch = Main.rand.NextFloat(-0.08f, 0.08f) }, Projectile.Center);
                rightSpinSound = false;
            }

            SpawnRightSpinParticles();

            if (spinSpeedRatio > 0.42f && rightSpinTimer % RightSpinHitboxInterval == 1)
                SpawnRightSpinHitbox();
        }

        private void StartRightSpin()
        {
            stageActive = false;
            CanHit = false;
            postSwing = true;
            rightSpinActive = true;
            rightSpinTimer = 0;
            rightSpinDirection = comboIndex % 2 == 0 ? 1 : -1;
            rightSpinSound = true;
            rightSpinAngularVelocity = 0f;
            rightSpinTransitionTimer = 0;
            rightSpinTransitionStartOffset = RotationOffset;
            currentDrawScale = 1f;
            currentRangeScale = 1f;
            lengthScale = 1f;
            thicknessScale = 1f;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.52f, Pitch = -0.2f }, Owner.Center);
        }

        private void EndRightSpin()
        {
            rightSpinActive = false;
            rightSpinTimer = 0;
            rightSpinSound = true;
            rightSpinAngularVelocity = 0f;
            rightSpinTransitionTimer = 0;
            rightSpinTransitionStartOffset = 0f;
            CanHit = false;
            postSwing = false;
            gapTimer = 0;

            if (releaseRequested)
                Projectile.Kill();
        }

        private void SpawnRightSpinHitbox()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int squareSize = 260;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<BBSwing_INV>(),
                Math.Max(1, (int)(Projectile.damage * 0.48f)),
                Projectile.knockBack * 0.5f,
                Projectile.owner,
                squareSize,
                1f,
                SlashAngle);
        }

        private void SpawnRightSpinParticles()
        {
            Vector2 slashDirection = SlashAngle.ToRotationVector2();
            Vector2 tangentDirection = slashDirection.RotatedBy(MathHelper.PiOver2 * rightSpinDirection);
            float ringPhase = rightSpinTimer * 0.18f * rightSpinDirection;

            for (int i = 0; i < 3; i++)
            {
                float angle = ringPhase + MathHelper.TwoPi * i / 3f + Main.rand.NextFloat(-0.16f, 0.16f);
                Vector2 orbitDirection = angle.ToRotationVector2();
                Vector2 spawnPosition = Owner.Center + orbitDirection * Main.rand.NextFloat(72f, 142f);
                Vector2 velocity = tangentDirection * Main.rand.NextFloat(0.6f, 1.6f) + orbitDirection * Main.rand.NextFloat(0.2f, 0.9f);

                Dust dust = Dust.NewDustPerfect(spawnPosition, Main.rand.NextBool() ? DustID.Water : DustID.Frost, velocity, 100, new Color(90, 205, 255), Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
            }
        }

        private void SpawnSwingParticles(StageProfile profile)
        {
            Vector2 slashDirection = SlashAngle.ToRotationVector2();
            Vector2 right = slashDirection.RotatedBy(MathHelper.PiOver2);
            float distance = 132f;

            for (int i = 0; i < (profile.Tilted ? 2 : 3); i++)
            {
                Vector2 position = Owner.Center + slashDirection * Main.rand.NextFloat(40f, distance) + right * Main.rand.NextFloat(-18f, 18f);
                Vector2 velocity = -slashDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, 5f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.45f, 0.85f),
                    Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Cyan));
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Owner.Center + slashDirection * distance + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.Water,
                    -slashDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.2f, 4f),
                    100,
                    Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                    Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void ApplyIdleRotation()
        {
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(lockedMouseWorld) + MathHelper.PiOver4, 0.3f);
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(40f * swingDirection * Owner.direction), 0.18f);
        }

        private void UpdateLockedAimFromMouse()
        {
            lockedMouseWorld = Owner.Calamity().mouseWorld;
            if (lockedMouseWorld == Vector2.Zero)
                lockedMouseWorld = Main.MouseWorld;

            lockedAimDirection = (lockedMouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = lockedAimDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
        }

        private bool WantsRightSpin()
        {
            if (Owner.whoAmI != Main.myPlayer)
                return rightSpinActive;

            Owner.Calamity().rightClickListener = true;
            return Owner.Calamity().mouseRight &&
                   IsLeftHeld() &&
                   !Owner.noItems &&
                   !Owner.CCed &&
                   !Owner.mouseInterface &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private bool IsLeftHeld()
        {
            return Owner.channel &&
                   (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private void ApplyScreenShake(float power)
        {
            float distanceFactor = Utils.GetLerpValue(1200f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        private void ApplyArmRotation()
        {
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0, 10, 1f, 0.62f, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!DrawUnconditionally && Owner.itemAnimation <= 0)
                return false;

            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaronGoest");
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");

            float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            Vector2 origin = FlipAsSword ? new Vector2(texture.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 bladeScale = Vector2.One * Projectile.scale;

            if (CanHit || postSwing)
            {
                Main.EntitySpriteDraw(
                    swoosh.Value,
                    drawPosition,
                    null,
                    Color.DeepSkyBlue with { A = 0 } * fadeIn * 0.42f,
                    (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -90f : 90f) * -Owner.direction,
                    swoosh.Size() * 0.5f,
                    Projectile.scale,
                    SpriteEffects.None);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 4.2f * fadeIn;
                Main.EntitySpriteDraw(
                    ghost.Value,
                    drawPosition + offset,
                    ghost.Value.Frame(1, FrameCount, 0, Frame),
                    Color.Aqua with { A = 0 } * 0.12f * fadeIn,
                    Projectile.rotation + RotationOffset + r,
                    origin,
                    bladeScale,
                    effects);
            }

            Main.EntitySpriteDraw(
                texture.Value,
                drawPosition,
                texture.Value.Frame(1, FrameCount, 0, Frame),
                lightColor,
                Projectile.rotation + RotationOffset + r,
                origin,
                bladeScale,
                effects);

            return false;
        }

        private static StageProfile GetStageProfile(int stage)
        {
            return stage switch
            {
                0 => new StageProfile(ComboKind.OpeningShuriken, 46, 5, 1f, 1f, 1f, 1f, 1f, 1f, false),
                1 or 2 => new StageProfile(ComboKind.MediumTornado, 28, 3, 1f, 1f, 1f, 1f, 1f, 1f, false),
                >= 3 and <= 6 => new StageProfile(ComboKind.SmallWater, 18, 2, 1f, 1f, 1f, 1f, 1f, 1f, true),
                _ => new StageProfile(ComboKind.FinalWave, 50, 6, 1f, 1f, 1f, 1f, 1f, 1f, false),
            };
        }

        private enum ComboKind
        {
            OpeningShuriken,
            MediumTornado,
            SmallWater,
            FinalWave
        }

        private readonly struct StageProfile
        {
            public readonly ComboKind Kind;
            public readonly int Duration;
            public readonly int GapFrames;
            public readonly float DrawScale;
            public readonly float RangeScale;
            public readonly float MinLengthScale;
            public readonly float MaxLengthScale;
            public readonly float MinThicknessScale;
            public readonly float SwooshScale;
            public readonly bool Tilted;

            public StageProfile(ComboKind kind, int duration, int gapFrames, float drawScale, float rangeScale, float minLengthScale, float maxLengthScale, float minThicknessScale, float swooshScale, bool tilted)
            {
                Kind = kind;
                Duration = duration;
                GapFrames = gapFrames;
                DrawScale = drawScale;
                RangeScale = rangeScale;
                MinLengthScale = minLengthScale;
                MaxLengthScale = maxLengthScale;
                MinThicknessScale = minThicknessScale;
                SwooshScale = swooshScale;
                Tilted = tilted;
            }
        }
    }
}
