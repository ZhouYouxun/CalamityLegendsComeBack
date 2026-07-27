using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using CalamityMod.Graphics.Primitives;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    public class BrinyBaron_LeftClick_Swing : BaseCustomUseStyleProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();
        public override Vector2 SpriteOrigin => new(0f, 102f);
        public override float HitboxOutset => BB_Balance.LeftClickCoreHitboxOutset;
        public override Vector2 HitboxSize => new(BB_Balance.LeftClickCoreHitboxSize, BB_Balance.LeftClickCoreHitboxSize);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private const float BaseHitboxOutset = 90f;
        private static readonly Vector2 BaseHitboxSize = new(132f, 132f);
        private const float SwooshRadiusCorrection = 0.575f;
        private const float RightSpinSwooshScaleMultiplier = 0.95f;
        // Keep the blade disc unchanged; only spread the water/ice field outward.
        private const float RightSpinVisualReachScale = 1.005f;
        private int CurrentGrowthStage => BB_Balance.GetGrowthStage();
        private int ComboLength => BB_Balance.GetLeftClickComboSequence(CurrentGrowthStage).Length;
        private const int StandardGapFrames = 0;
        private const int RightSpinTransitionFrames = 14;
        private const float RightSpinMaxEmpowerment = 180f;

        private int comboIndex;
        private int onHitSpawnsCount;
        private int currentStage;
        private int stageTimer;
        private int gapTimer;
        private int stageDuration;
        private int lockedFacingDirection = 1;
        private bool stageActive;
        private bool releaseRequested;
        private bool stageEventFired;
        private bool swingSoundPlayed;
        private bool postSwing;
        private float fadeIn;
        private float thicknessScale = 1f;
        private Vector2 lockedMouseWorld;
        private Vector2 lockedAimDirection = Vector2.UnitX;

        private float lastSlashFlipSide = 0f;
        private bool rightSpinActive;
        private int rightSpinTimer;
        private int rightSpinOrbitDirection = 1;
        private int rightSpinFacingDirection = 1;
        private bool rightSpinSound = true;
        private float rightSpinEmpowerment;
        private float rightSpinOverEmpowerment;
        private int rightSpinTransitionTimer;
        private Vector2 rightSpinDirectionVector = Vector2.UnitX;
        private Particle rightSpinSmear;
        private int rightSpinOuterBubbleIndex = -1;
        // 椭圆旋转：rightSpinAngle 是椭圆参数方程的累积角度 θ
        private float rightSpinAngle;
        // 龙卷风特效：透明度（0→1 淡入，1→0 淡出）
        private float tornadoOpacity;
        // 龙卷风云朵自转计时器
        private int tornadoAiTracker;

        // EXO 弧形刀光（Stage 2+ 专用）
        private const int BladeTrailHistoryFrames = 16;
        private Vector2[] bladeTipHistory = new Vector2[BladeTrailHistoryFrames];
        private int bladeTipHistoryLength = 0;
        private float slashOpacity = 0f;
        private static Asset<Texture2D> exoLensFlare;
        private const float BladeLength = 70f;
        private const float BladeTrailOutwardMultiplier = 1.5f;

        private float SlashAngle => FinalRotation + MathHelper.ToRadians(-45f);
        private float RightSpinChargeRatio => MathHelper.Clamp(rightSpinEmpowerment / RightSpinMaxEmpowerment, 0f, 1f);
        private float ActiveHitboxScale => Projectile.scale;
        private float RightSpinBladeReach => (BaseHitboxOutset + BaseHitboxSize.X * 0.38f) * Projectile.scale;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 0;
            Projectile.scale = BB_Balance.GetLeftClickScale();
        }

        public override void WhenSpawned()
        {
            // 不再锁定帧率，让外部攻速修正能正常生效
            DrawUnconditionally = true;
            Projectile.timeLeft = 2;
            Projectile.knockBack = 0f;
            Projectile.ai[1] = -1f;
            thicknessScale = 1f;
            UpdateLockedAimFromMouse();
            Projectile.scale = BB_Balance.GetLeftClickScale();
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

            SyncCurrentWeaponDamage();

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;
            Animation++;
            UseStyle();
            Owner.heldProj = Projectile.whoAmI;
            if (rightSpinActive)
            {
                ApplyRightSpinArmRotation();
            }
            else
            {
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
                Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);
            }

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

            if (!rightSpinActive)
            {
                Projectile.Center = Owner.MountedCenter;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, BB_Balance.GetLeftClickScale(), 0.22f);
                // 退出旋转后龙卷风平滑淡出；计时器继续走以保持淡出动画流畅
                tornadoOpacity = MathHelper.Lerp(tornadoOpacity, 0f, 0.12f);
                if (tornadoOpacity > 0.01f)
                    tornadoAiTracker++;
            }

            if (CurrentGrowthStage >= 2)
                TrackBladeTip();

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
            onHitSpawnsCount = 0;
            StageProfile profile = GetStageProfile(comboIndex % ComboLength);
            stageActive = true;
            currentStage = comboIndex % ComboLength;
            int useAnim = Math.Max(1, Owner.itemAnimationMax);
            stageDuration = useAnim;
            stageTimer = 0;
            stageEventFired = false;
            swingSoundPlayed = false;
            postSwing = false;
            CanHit = false;
            thicknessScale = 1f;

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;

            Projectile.numHits = 0;
            UpdateLockedAimFromMouse();
            Projectile.ai[1] = -1f;
            Owner.direction = lockedFacingDirection;
            FlipAsSword = lockedFacingDirection < 0;
        }

        private void RunStage()
        {
            stageTimer++;
            StageProfile profile = GetStageProfile(currentStage);
            int impactFrame = Math.Max(3, stageDuration / 3);

            if (stageTimer < impactFrame)
            {
                UpdateLockedAimFromMouse();
                CanHit = false;
                postSwing = false;
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.32f);
                thicknessScale = 1f;
                Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.1f);
                RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(-45f * Projectile.ai[1] * lockedFacingDirection), 0.2f);

                if (stageTimer >= stageDuration)
                    EndStage(profile);

                return;
            }

            Owner.direction = lockedFacingDirection;
            FlipAsSword = lockedFacingDirection < 0;
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.1f);

            float swingTime = stageTimer - stageDuration / 8f;
            float swingTimeMax = Math.Max(1f, stageDuration - stageDuration / 8f);
            float swingProgress = MathHelper.Clamp(swingTime / swingTimeMax, 0f, 1f);
            float easedProgress = CalamityUtils.ExpInOutEasing(swingProgress, 1);
            thicknessScale = 1f;

            bool hitWindow = swingProgress > 0.25f && swingProgress < 0.85f;
            CanHit = hitWindow;
            postSwing = swingProgress < 0.85f;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.32f : 0.35f);

            if (swingProgress >= 0.3f && !swingSoundPlayed)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = Main.rand.NextFloat(0.08f, 0.22f) }, Owner.Center);
                SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.45f, Pitch = Main.rand.NextFloat(0.08f, 0.22f) }, Owner.Center);
                swingSoundPlayed = true;
            }

            if (!stageEventFired && swingProgress >= 0.3f)
            {
                FireStageProjectile(profile.Kind);
                stageEventFired = true;
            }

            RotationOffset = MathHelper.Lerp(
                RotationOffset,
                MathHelper.ToRadians(MathHelper.Lerp(
                    -45f * Projectile.ai[1] * lockedFacingDirection,
                    405f * -Projectile.ai[1] * lockedFacingDirection,
                    easedProgress)),
                0.2f);

            if (CanHit)
                SpawnSwingParticles(profile);

            if (swingTime >= swingTimeMax)
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
                case ComboKind.SeafoamBlade:
                    SpawnSeafoamBlade();
                    ApplyScreenShake(2.5f);
                    break;
                case ComboKind.FormalWave:
                    SpawnFormalWave(faster: false);
                    ApplyScreenShake(3.5f);
                    break;
                case ComboKind.FormalWaveFaster:
                    SpawnFormalWave(faster: true);
                    ApplyScreenShake(3.5f);
                    break;
                case ComboKind.SingleShuriken:
                    SpawnSingleShuriken();
                    ApplyScreenShake(2f);
                    break;
                case ComboKind.FiveShurikens:
                    SpawnFiveShurikens();
                    ApplyScreenShake(3f);
                    break;
                case ComboKind.EnhancedWave:
                    SpawnEnhancedWave();
                    ApplyScreenShake(4f);
                    break;
                case ComboKind.Tornado:
                    SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.68f, Pitch = -0.25f }, Owner.Center);
                    SpawnTornadoBolt();
                    break;
                case ComboKind.Stage2_ThreeSeafoam:
                    SpawnThreeScatteredSeafoams();
                    ApplyScreenShake(3f);
                    break;
                case ComboKind.Stage2_SixSeaSpirits:
                    SpawnSixStage2SeaSpirits();
                    ApplyScreenShake(2.5f);
                    break;
                case ComboKind.Stage3_FiveParallelSeafoam:
                    SpawnFiveParallelSeafoams();
                    SpawnFormalWave(faster: false);
                    ApplyScreenShake(3.5f);
                    break;
                case ComboKind.Stage3_TenSeaSpirits:
                    SpawnTenStage3SeaSpirits();
                    SpawnFormalWave(faster: false);
                    ApplyScreenShake(2.8f);
                    break;
                case ComboKind.Stage3_FiveShurikens:
                    SpawnFiveShurikens();
                    SpawnFormalWave(faster: false);
                    ApplyScreenShake(3f);
                    break;
                case ComboKind.Stage3_EnhancedWave:
                case ComboKind.Stage4_EnhancedWave:
                    SpawnEnhancedWave();
                    ApplyScreenShake(4f);
                    break;
                case ComboKind.Stage4_TenSeaSpirits:
                    SpawnTenStage4SeaSpirits();
                    SpawnFormalWave(faster: false);
                    ApplyScreenShake(3f);
                    break;
            }
        }

        private void SpawnSeafoamBlade()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float speed = 24f; // Stage 1 Seafoam is 2x speed (which makes it 24f)
            int p = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 40f,
                shootDirection * speed,
                ModContent.ProjectileType<BrinyBaron_SeafoamBlade>(),
                Math.Max(1, (int)(Projectile.damage * 0.6f)),
                Projectile.knockBack * 0.5f,
                Projectile.owner);
            
            if (p != Main.maxProjectiles)
            {
                Main.projectile[p].timeLeft = 60;
            }

            // Spawn 3 Sea Spirits with 5 degree spread (weak homing)
            for (int i = -1; i <= 1; i++)
            {
                float spread = MathHelper.ToRadians(i * 5f);
                Vector2 velocity = shootDirection.RotatedBy(spread) * 14f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 40f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_SeaSpirit>(),
                    Math.Max(1, (int)(Projectile.damage * 0.35f)),
                    Projectile.knockBack * 0.4f,
                    Projectile.owner,
                    1f); // ai[0] = 1f: Stage 1 Sea Spirit
            }
        }

        private void SpawnThreeScatteredSeafoams()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float speed = 12f * 1.7f; // 20.4f
            for (int i = -1; i <= 1; i++)
            {
                float spread = MathHelper.ToRadians(i * 10f); // 10 degrees spread
                Vector2 velocity = shootDirection.RotatedBy(spread) * speed;
                int proj = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 40f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_SeafoamBlade>(),
                    Math.Max(1, (int)(Projectile.damage * 0.55f)),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner);
                    
                if (proj != Main.maxProjectiles)
                {
                    Main.projectile[proj].scale = 1.5f;
                    Main.projectile[proj].penetrate = 6;
                    Main.projectile[proj].ai[0] = 2f; // Stage 2 tag
                    Main.projectile[proj].timeLeft = 80;
                }
            }
        }

        private void SpawnSixStage2SeaSpirits()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < 6; i++)
            {
                // Random angle and speed in 5 degrees
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-5f, 5f));
                float speed = Main.rand.NextFloat(16f, 22f);
                Vector2 velocity = shootDirection.RotatedBy(spread) * speed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 40f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_SeaSpirit>(),
                    Math.Max(1, (int)(Projectile.damage * 0.6f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    2f); // ai[0] = 2f: Stage 2 Sea Spirit
            }
        }

        private void SpawnFiveParallelSeafoams()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float speed = 18f * 2.5f;
            float spacing = 20f;
            Vector2 perp = shootDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = -2; i <= 2; i++)
            {
                Vector2 spawnPos = Owner.MountedCenter + shootDirection * 40f + perp * (i * spacing);
                int proj = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    shootDirection * speed,
                    ModContent.ProjectileType<BrinyBaron_SeafoamBlade>(),
                    Math.Max(1, (int)(Projectile.damage * 0.66f)),
                    Projectile.knockBack * 0.4f,
                    Projectile.owner);
                    
                if (proj != Main.maxProjectiles)
                {
                    Main.projectile[proj].scale = 1.2f;
                    Main.projectile[proj].penetrate = 8;
                    Main.projectile[proj].ai[0] = 3f; // Stage 3 tag
                    Main.projectile[proj].timeLeft = 90;
                }
            }
        }

        private void SpawnTenStage3SeaSpirits()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < 10; i++)
            {
                // Scattered in a wider angle, e.g. 30 degrees
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-15f, 15f));
                float speed = Main.rand.NextFloat(14f, 20f);
                Vector2 velocity = shootDirection.RotatedBy(spread) * speed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 40f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_SeaSpirit>(),
                    Math.Max(1, (int)(Projectile.damage * 0.3f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    3f); // ai[0] = 3f: Stage 3 Sea Spirit
            }
        }

        private void SpawnTenStage4SeaSpirits()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < 10; i++)
            {
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-15f, 15f));
                float speed = Main.rand.NextFloat(14f, 20f);
                Vector2 velocity = shootDirection.RotatedBy(spread) * speed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 40f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_SeaSpirit>(),
                    Math.Max(1, (int)(Projectile.damage * 0.45f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    4f); // ai[0] = 4f: Stage 4 Sea Spirit
            }
        }

        private void SpawnFormalWave(bool faster)
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            float speed = faster ? 18f : 13.2f;
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 44f,
                shootDirection * speed,
                ModContent.ProjectileType<BBSwing_Wave>(),
                Math.Max(1, (int)(Projectile.damage * 0.75f)),
                Projectile.knockBack,
                Projectile.owner,
                1.42f,
                1f);
            if (proj != Main.maxProjectiles)
            {
                Main.projectile[proj].localAI[0] = -1f; // Disable tracking bubbles
            }
        }

        private void SpawnSingleShuriken()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 28f,
                shootDirection * 14f,
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                Math.Max(1, (int)(Projectile.damage * 0.4f)),
                Projectile.knockBack * 0.5f,
                Projectile.owner,
                0f);
        }

        private void SpawnFiveShurikens()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            // Incoming balance keeps this third-swing fan at three projectiles.
            // Retain the centered fan math so the three blades do not skew to one side.
            const int shurikenCount = 3;
            int middle = shurikenCount / 2;
            for (int i = 0; i < shurikenCount; i++)
            {
                float spread = MathHelper.ToRadians((i - middle) * 10f);
                float speedMultiplier = 1f - Math.Abs(i - middle) * 0.15f;
                Vector2 velocity = shootDirection.RotatedBy(spread).RotatedByRandom(0.02f) * (15f * speedMultiplier);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.MountedCenter + shootDirection * 28f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                    Math.Max(1, (int)(Projectile.damage * 0.4f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    0f,
                    1f); // ai[1] = 1f enables releasing Seafoam Blade on hit
            }
        }

        private void SpawnEnhancedWave()
        {
            Vector2 shootDirection = lockedAimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + shootDirection * 44f,
                shootDirection * 13.2f,
                ModContent.ProjectileType<BBSwing_Wave>(),
                Math.Max(1, (int)(Projectile.damage * 2f)),
                Projectile.knockBack,
                Projectile.owner,
                2.35f,
                3f); // ai[1] = 3f sets SpawnStage to 3 (Post Moon Lord)
        }

        private void SpawnTornadoBolt()
        {
            // Deprecated, no longer used
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
            thicknessScale = MathHelper.Lerp(thicknessScale, 1f, 0.18f);
            Projectile.scale = 1f + RightSpinChargeRatio * 0.7f;

            rightSpinTransitionTimer = Math.Min(rightSpinTransitionTimer + 1, RightSpinTransitionFrames);
            float transitionProgress = rightSpinTransitionTimer / (float)RightSpinTransitionFrames;
            float transitionSpeed = MathHelper.SmoothStep(0.18f, 1f, transitionProgress);
            float spinSpeedMultiplier = MathHelper.Lerp(1.2f, 2f, RightSpinChargeRatio);
            float spinRate = spinSpeedMultiplier * MathHelper.PiOver4 * 0.2f * rightSpinOrbitDirection * transitionSpeed;
            // ── 正圆 360 度自转轨迹（回退至 07/23 稳定版本） ──
            rightSpinAngle += spinRate;
            rightSpinDirectionVector = rightSpinAngle.ToRotationVector2();
            Projectile.rotation = rightSpinAngle + MathHelper.PiOver4;
            Projectile.Center = Owner.Center + rightSpinDirectionVector * (RightSpinBladeReach * 0.5f);
            RotationOffset = 0f;
            postSwing = true;
            CanHit = rightSpinTransitionTimer >= RightSpinTransitionFrames && RightSpinChargeRatio >= 0.45f;
            fadeIn = MathHelper.Lerp(fadeIn, MathHelper.Clamp(RightSpinChargeRatio, 0f, 1f), 0.16f);

            ApplyRightSpinHeldPose();

            if (rightSpinTimer % 34 == 1 || (rightSpinSound && RightSpinChargeRatio > 0.72f))
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.66f, Pitch = Main.rand.NextFloat(-0.08f, 0.08f) }, Projectile.Center);
                rightSpinSound = false;
            }

            SpawnRightSpinParticles();
            UpdateRightSpinSmear();

            if (RightSpinChargeRatio >= 1f && (rightSpinTimer + (int)rightSpinOverEmpowerment) % BB_Balance.RightSpinShurikenInterval == 1)
                SpawnRightSpinShuriken();

            MaintainPostFishronOuterBubble();

            rightSpinEmpowerment++;
            if (rightSpinEmpowerment > RightSpinMaxEmpowerment)
            {
                rightSpinEmpowerment = RightSpinMaxEmpowerment;
                rightSpinOverEmpowerment++;
            }
        }

        private void StartRightSpin()
        {
            stageActive = false;
            CanHit = false;
            postSwing = true;
            rightSpinActive = true;
            rightSpinTimer = 0;
            rightSpinFacingDirection = Owner.direction == 0 ? lockedFacingDirection : Owner.direction;
            rightSpinOrbitDirection = rightSpinFacingDirection;
            rightSpinDirectionVector = (FinalRotation - MathHelper.PiOver4).ToRotationVector2();
            rightSpinDirectionVector.Normalize();
            RotationOffset = 0f;
            Projectile.rotation = rightSpinDirectionVector.ToRotation() + MathHelper.PiOver4;
            ApplyRightSpinHeldPose();
            rightSpinSound = true;
            rightSpinEmpowerment = 0f;
            rightSpinOverEmpowerment = 0f;
            rightSpinTransitionTimer = 0;
            thicknessScale = 1f;
            // 初始化椭圆角度：从当前方向向量的角度开始，确保刀片位置连续
            rightSpinAngle = rightSpinDirectionVector.ToRotation();
            tornadoOpacity = 0f;
            tornadoAiTracker = 0;
            rightSpinOuterBubbleIndex = -1;
            ClearBladeShaderTrail();

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
            rightSpinEmpowerment = 0f;
            rightSpinOverEmpowerment = 0f;
            rightSpinTransitionTimer = 0;
            rightSpinAngle = 0f;
            // tornadoOpacity 不在此清零，由 AI() 的淡出逻辑平滑处理
            rightSpinSmear?.Kill();
            rightSpinSmear = null;
            rightSpinOuterBubbleIndex = -1;
            ClearBladeShaderTrail();
            CanHit = false;
            postSwing = false;
            gapTimer = 0;

            if (releaseRequested)
                Projectile.Kill();
        }

        private void SpawnRightSpinShuriken()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 radialDirection = Main.rand.NextVector2CircularEdge(1f, 1f).SafeNormalize(Vector2.UnitX);
            Vector2 tangentDirection = radialDirection.RotatedBy(MathHelper.PiOver2 * rightSpinOrbitDirection);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center + radialDirection * Main.rand.NextFloat(18f, 43f),
                radialDirection * Main.rand.NextFloat(10.5f, 13.5f) + tangentDirection * Main.rand.NextFloat(1f, 3f),
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                Math.Max(1, (int)(Projectile.damage * 0.67f)),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                0f);
        }

        private void MaintainPostFishronOuterBubble()
        {
            if (!NPC.downedFishron || Main.myPlayer != Projectile.owner)
                return;

            int bubbleType = ModContent.ProjectileType<BrinyBaron_RightSpinOuterBubble>();
            if (!Main.projectile.IndexInRange(rightSpinOuterBubbleIndex) || !Main.projectile[rightSpinOuterBubbleIndex].active || Main.projectile[rightSpinOuterBubbleIndex].type != bubbleType)
            {
                rightSpinOuterBubbleIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.Center,
                    Vector2.Zero,
                    bubbleType,
                    Math.Max(1, (int)(Projectile.damage * 0.92f)),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    rightSpinAngle,
                    rightSpinOrbitDirection);
            }

            if (Main.projectile.IndexInRange(rightSpinOuterBubbleIndex))
                Main.projectile[rightSpinOuterBubbleIndex].timeLeft = 3;
        }

        private void SpawnRightSpinParticles()
        {
            float bladeReach = RightSpinBladeReach * RightSpinVisualReachScale;
            float ringPhase = rightSpinAngle;

            for (int i = 0; i < 3; i++)
            {
                float angle = ringPhase + MathHelper.TwoPi * i / 3f + Main.rand.NextFloat(-0.16f, 0.16f);
                float rFactor = Main.rand.NextFloat(0.54f, 1.03f);
                Vector2 orbitDirection = angle.ToRotationVector2();
                Vector2 spawnPosition = Owner.Center + orbitDirection * (bladeReach * rFactor);
                Vector2 tangentDirection = orbitDirection.RotatedBy(MathHelper.PiOver2 * rightSpinOrbitDirection);
                Vector2 velocity = tangentDirection * Main.rand.NextFloat(0.6f, 1.6f + RightSpinChargeRatio) + orbitDirection * Main.rand.NextFloat(0.2f, 0.9f);

                Dust dust = Dust.NewDustPerfect(spawnPosition, Main.rand.NextBool() ? DustID.Water : DustID.Frost, velocity, 100, new Color(90, 205, 255), Main.rand.NextFloat(0.85f, 1.22f) * MathHelper.Lerp(0.9f, 1.16f, RightSpinChargeRatio));
                dust.noGravity = true;
            }

            if (Main.dedServ || rightSpinTimer % 2 != 0)
                return;

            int orbCount = RightSpinChargeRatio >= 0.6f ? 2 : 1;
            for (int i = 0; i < orbCount; i++)
            {
                float angle = ringPhase + Main.rand.NextFloat(MathHelper.TwoPi);
                float rFactor = Main.rand.NextFloat(0.62f, 1.08f);
                Vector2 orbitDirection = angle.ToRotationVector2();
                Vector2 spawnPosition = Owner.Center + orbitDirection * (bladeReach * rFactor);
                Vector2 tangentDirection = orbitDirection.RotatedBy(MathHelper.PiOver2 * rightSpinOrbitDirection);
                Vector2 velocity =
                    tangentDirection * Main.rand.NextFloat(0.2f, 0.7f + RightSpinChargeRatio * 0.45f) -
                    orbitDirection * Main.rand.NextFloat(0.05f, 0.35f);
                Color color = Color.Lerp(new Color(80, 185, 255), new Color(220, 250, 255), Main.rand.NextFloat(0.2f, 0.65f));

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    spawnPosition,
                    velocity,
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.34f, 0.58f) * MathHelper.Lerp(0.85f, 1.2f, RightSpinChargeRatio),
                    color,
                    true,
                    false,
                    true));
            }
        }

        private void UpdateRightSpinSmear()
        {
            if (RightSpinChargeRatio < 0.72f || Main.dedServ)
                return;

            Color smearColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, RightSpinChargeRatio) * ((RightSpinChargeRatio - 0.72f) / 0.28f * 0.72f);
            float smearScale = GetRightSpinSmearScale();
            if (rightSpinSmear == null)
            {
                // 涓嶉渶瑕佽繖鐜╂剰鍎匡紝娌￠偅涔堢伒娲?                //rightSpinSmear = new CircularSmearSmokeyVFX(Owner.Center, smearColor, rightSpinDirectionVector.ToRotation(), smearScale);
                //GeneralParticleHandler.SpawnParticle(rightSpinSmear);
                return;
            }

            rightSpinSmear.Position = Owner.Center;
            rightSpinSmear.Rotation = rightSpinDirectionVector.ToRotation() + MathHelper.PiOver2;
            rightSpinSmear.Scale = smearScale;
            rightSpinSmear.Color = smearColor;
            rightSpinSmear.Time = 0;
        }

        private float GetRightSpinSmearScale()
        {
            Texture2D smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;

            float swordRadius = MathF.Max(swordTexture.Width, swordTexture.Height) * 0.5f * Projectile.scale;
            float smearDiameter = MathF.Max(smearTexture.Width, smearTexture.Height);
            float desiredDiameter = swordRadius * 2f * MathHelper.Lerp(0.84f, 0.92f, RightSpinChargeRatio);
            return desiredDiameter / smearDiameter;
        }

        private void SpawnSwingParticles(StageProfile profile)
        {
            Vector2 slashDirection = SlashAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            float swingSign = -Projectile.ai[1] * lockedFacingDirection;
            if (swingSign == 0f)
                swingSign = 1f;

            Vector2 tangentDirection = slashDirection.RotatedBy(MathHelper.PiOver2 * swingSign);
            float bladeReach = (BaseHitboxOutset + BaseHitboxSize.X * 0.38f) * Projectile.scale;
            int lineCount = profile.Tilted ? 2 : 3;
            int scaledLineCount = (int)Math.Max(1, lineCount * Projectile.scale);

            for (int i = 0; i < scaledLineCount; i++)
            {
                float radialDistance = Main.rand.NextFloat(32f * Projectile.scale, bladeReach);
                Vector2 position =
                    Owner.Center +
                    slashDirection * radialDistance +
                    tangentDirection * Main.rand.NextFloat(-10f * Projectile.scale, 18f * Projectile.scale);

                Vector2 velocity =
                    tangentDirection * Main.rand.NextFloat(3.2f, 8.2f) +
                    slashDirection * Main.rand.NextFloat(0.6f, 2.4f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(13, 22),
                    Main.rand.NextFloat(0.36f, 0.74f),
                    Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Cyan));
            }

            if (Main.rand.NextFloat() < 0.5f * Projectile.scale)
            {
                Vector2 starPosition =
                    Owner.Center +
                    slashDirection * Main.rand.NextFloat(bladeReach * 0.72f, bladeReach * 1.02f) +
                    tangentDirection * Main.rand.NextFloat(-8f * Projectile.scale, 16f * Projectile.scale);

                Vector2 starVelocity =
                    tangentDirection * Main.rand.NextFloat(2.4f, 5.6f) +
                    slashDirection * Main.rand.NextFloat(0.7f, 2.1f);

                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    starPosition,
                    starVelocity,
                    Color.White,
                    Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()) * 0.62f,
                    Main.rand.NextFloat(0.42f, 0.8f),
                    Main.rand.Next(11, 18),
                    0.14f,
                    1.15f));
            }

            if (Main.rand.NextFloat() < 0.5f * Projectile.scale)
            {
                Vector2 dustPosition =
                    Owner.Center +
                    slashDirection * bladeReach +
                    tangentDirection * Main.rand.NextFloat(-18f * Projectile.scale, 18f * Projectile.scale);

                Dust dust = Dust.NewDustPerfect(
                    dustPosition,
                    DustID.Water,
                    tangentDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(1.4f, 4.8f),
                    100,
                    Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                    Main.rand.NextFloat(0.72f, 1.08f));
                dust.noGravity = true;
            }
        }

        private void ApplyIdleRotation()
        {
            UpdateLockedAimFromMouse();
            Projectile.rotation = Projectile.rotation.AngleLerp(GetAimRotation(), 0.1f);
            RotationOffset = RotationOffset.AngleLerp(MathHelper.ToRadians(-45f * Projectile.ai[1] * lockedFacingDirection), 0.2f);
        }

        private void UpdateLockedAimFromMouse()
        {
            lockedMouseWorld = Owner.Calamity().mouseWorld;
            if (lockedMouseWorld == Vector2.Zero)
                lockedMouseWorld = Main.MouseWorld;

            lockedAimDirection = (lockedMouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            lockedFacingDirection = lockedAimDirection.X >= 0f ? 1 : -1;
            Owner.direction = lockedFacingDirection;
            FlipAsSword = lockedFacingDirection < 0;
        }

        private float GetAimRotation() =>
            Owner.AngleTo(lockedMouseWorld) + MathHelper.ToRadians(lockedFacingDirection < 0 ? 0f : 120f);

        private bool WantsRightSpin()
        {
            if (CurrentGrowthStage < 2)
                return false;

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
            // channel can briefly clear as the held item's automatic-use animation rolls over.
            // The owning client has the authoritative physical input, so use it directly to
            // avoid destroying and recreating the combo midway through a continuous left hold.
            bool leftInputHeld = Main.myPlayer == Projectile.owner ? Main.mouseLeft : Owner.channel;
            return leftInputHeld &&
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
            if (rightSpinActive)
            {
                ApplyRightSpinArmRotation();
                return;
            }

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        private void ApplyRightSpinArmRotation()
        {
            float aimAngle = rightSpinDirectionVector.ToRotation();
            float baseArmAngle = aimAngle - MathHelper.PiOver2;

            float shakeFreq = MathHelper.Lerp(0.9f, 1.6f, RightSpinChargeRatio);
            float shakeAmp = MathHelper.ToRadians(18f) * MathHelper.Lerp(0.85f, 1.25f, RightSpinChargeRatio);
            float sideFactor = lastSlashFlipSide > 0.5f ? 1f : -1f;

            float frontArmAngle = baseArmAngle + MathF.Sin(rightSpinTimer * shakeFreq * 1.8f) * shakeAmp + MathHelper.ToRadians(8f) * sideFactor;
            float backArmAngle = baseArmAngle + MathF.Cos(rightSpinTimer * shakeFreq * 1.8f + 0.7f) * shakeAmp - MathHelper.ToRadians(8f) * sideFactor;

            Player.CompositeArmStretchAmount frontStretch = (rightSpinTimer % 4 < 2) ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.ThreeQuarters;
            Player.CompositeArmStretchAmount backStretch = (rightSpinTimer % 4 >= 2) ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.ThreeQuarters;

            Owner.SetCompositeArmFront(true, frontStretch, frontArmAngle);
            Owner.SetCompositeArmBack(true, backStretch, backArmAngle);
        }

        private void ApplyRightSpinHeldPose()
        {
            int facing = rightSpinDirectionVector.X >= 0f ? 1 : -1;
            Owner.ChangeDir(facing);
            FlipAsSword = facing < 0;

            Owner.itemRotation = rightSpinDirectionVector.ToRotation();
            if (Owner.direction != 1)
                Owner.itemRotation -= MathHelper.Pi;

            Owner.itemRotation = MathHelper.WrapAngle(Owner.itemRotation);
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
        }

        public override void OnKill(int timeLeft)
        {
            rightSpinSmear?.Kill();
            rightSpinSmear = null;
        }

        private Vector2 GetBladeTip()
        {
            Vector2 slashDirection = SlashAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            float bladeReach = (BaseHitboxOutset + BaseHitboxSize.X * 0.38f) * Projectile.scale;
            return Owner.MountedCenter + slashDirection * bladeReach;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle swingHitbox = GetSwingDamageHitbox();
            bool boxHit = Collision.CheckAABBvAABBCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                swingHitbox.TopLeft(),
                swingHitbox.Size());

            if (boxHit)
                return null;

            Vector2 slashDirection = SlashAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 start = Owner.MountedCenter + slashDirection * (24f * Projectile.scale);
            Vector2 end = Owner.MountedCenter + slashDirection * ((BB_Balance.LeftClickCoreHitboxOutset + BB_Balance.LeftClickCoreHitboxSize * 0.55f) * Projectile.scale);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BB_Balance.LeftClickCoreHitboxSize * 0.36f * Projectile.scale, ref collisionPoint);
        }

        private Rectangle GetSwingDamageHitbox()
        {
            Vector2 slashDirection = SlashAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            float hitboxSize = BB_Balance.LeftClickCoreHitboxSize * Projectile.scale;
            Vector2 center = Owner.MountedCenter + slashDirection * BB_Balance.LeftClickCoreHitboxOutset * Projectile.scale;
            return new Rectangle(
                (int)(center.X - hitboxSize * 0.5f),
                (int)(center.Y - hitboxSize * 0.5f),
                (int)hitboxSize,
                (int)hitboxSize);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0, 10, 1f, 0.62f, true);
            if (rightSpinActive)
                modifiers.SourceDamage *= MathHelper.Lerp(0.72f, 1.05f, RightSpinChargeRatio) * damageMult;
            else
                modifiers.SourceDamage *= 1.35f * damageMult;

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBladeHitEffects(target);
            target.AddBuff(BuffID.Frostburn, 180);
            if (Main.myPlayer == Projectile.owner)
                Owner.GetModPlayer<BBTideValuePlayer>().TryAddTideFromBlade();

            if (!rightSpinActive)
            {
                int growthStage = CurrentGrowthStage;
                if (growthStage > 1 && onHitSpawnsCount < 3)
                {
                    bool spawnedSomething = false;

                    SpawnTrueMeleeTyphoon(target);
                    spawnedSomething = true;

                    if (growthStage >= 4)
                    {
                        // Release a burst of 8 Sea Spirits with sin-wave homing on melee hits
                        for (int i = 0; i < 8; i++)
                        {
                            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                            float speed = Main.rand.NextFloat(8f, 12f);
                            Vector2 velocity = angle.ToRotationVector2() * speed;

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                target.Center,
                                velocity,
                                ModContent.ProjectileType<BrinyBaron_SeaSpirit>(),
                                Math.Max(1, (int)(Projectile.damage * 0.35f)),
                                Projectile.knockBack * 0.3f,
                                Projectile.owner,
                                5f, // ai[0] = 5f: Stage 4/5 melee-spawned
                                1f  // ai[1] = 1f: enable sin wave movement
                            );
                        }
                    }

                    if (spawnedSomething)
                    {
                        onHitSpawnsCount++;
                    }
                }
            }
        }

        private void SpawnTrueMeleeTyphoon(NPC target)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            if (Owner.ownedProjectileCounts[ModContent.ProjectileType<BrinySpout>()] != 0)
                return;

            Projectile.NewProjectile(
                Owner.GetSource_ItemUse(Owner.HeldItem),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BrinyTyphoonBubble>(),
                CurrentProjectileDamage(),
                Owner.HeldItem.knockBack,
                Owner.whoAmI);
        }

        private void SyncCurrentWeaponDamage()
        {
            int currentDamage = CurrentProjectileDamage();
            if (Projectile.damage == currentDamage && Projectile.originalDamage == currentDamage)
                return;

            Projectile.damage = currentDamage;
            Projectile.originalDamage = currentDamage;
        }

        private int CurrentProjectileDamage()
        {
            if (Owner?.HeldItem == null || Owner.HeldItem.type != AssignedItemID)
                return Math.Max(1, Projectile.damage);

            return Math.Max(1, Owner.GetWeaponDamage(Owner.HeldItem));
        }

        private void TrackBladeTip()
        {
            if (!CanUseBladeShaderTrail())
            {
                ClearBladeShaderTrail();
                return;
            }

            if (CanHit || rightSpinActive)
            {
                slashOpacity = MathHelper.Lerp(slashOpacity, 1f, 0.4f);
                Vector2 currentTip = (rightSpinActive ? rightSpinDirectionVector : SlashAngle.ToRotationVector2()) * BladeLength * BladeTrailOutwardMultiplier * Projectile.scale;
                Array.Copy(bladeTipHistory, 0, bladeTipHistory, 1, bladeTipHistory.Length - 1);
                bladeTipHistory[0] = currentTip;
                if (bladeTipHistoryLength < bladeTipHistory.Length)
                    bladeTipHistoryLength++;
            }
            else
            {
                slashOpacity = MathHelper.Lerp(slashOpacity, 0f, 0.2f);
                if (slashOpacity < 0.01f)
                    bladeTipHistoryLength = 0;
            }
        }

        private bool CanUseBladeShaderTrail()
        {
            return CurrentGrowthStage >= 2;
        }

        private void ClearBladeShaderTrail()
        {
            slashOpacity = 0f;
            bladeTipHistoryLength = 0;
        }

        private void DrawSlash()
        {
            if (!CanUseBladeShaderTrail() || bladeTipHistoryLength < 2 || slashOpacity <= 0.01f || Main.dedServ)
                return;

            Main.spriteBatch.EnterShaderRegion();

            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(new Color(0, 180, 255));
            slashShader.UseSecondaryColor(new Color(0, 30, 80));
            slashShader.Shader.Parameters["fireColor"].SetValue(new Color(80, 240, 255).ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(Owner.direction == 1);
            slashShader.Apply();

            var points = new List<Vector2>();
            for (int i = 0; i < bladeTipHistoryLength; i++)
                points.Add(bladeTipHistory[i]);

            float SlashWidthFunction(float completionRatio, Vector2 _)
            {
                float fade = Utils.GetLerpValue(1f, 0f, completionRatio, true);
                return Projectile.scale * 27.5f * fade * slashOpacity;
            }

            Color SlashColorFunction(float completionRatio, Vector2 _)
            {
                float alpha = Utils.GetLerpValue(0.95f, 0.3f, completionRatio, true);
                return Color.White * alpha * slashOpacity;
            }

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(
                    SlashWidthFunction,
                    SlashColorFunction,
                    (_, _) => Owner.MountedCenter,
                    shader: slashShader
                ),
                BladeTrailHistoryFrames
            );

            Main.spriteBatch.ExitShaderRegion();
        }

        private void DrawLensFlare()
        {
            if (!CanUseBladeShaderTrail() || slashOpacity <= 0.01f || Main.dedServ)
                return;

            exoLensFlare ??= ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar");
            Texture2D shineTex = exoLensFlare.Value;
            Vector2 bladeTip = Owner.MountedCenter + SlashAngle.ToRotationVector2() * BladeLength * BladeTrailOutwardMultiplier * Projectile.scale;
            Color lensColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()) with { A = 0 };

            Main.EntitySpriteDraw(
                shineTex,
                bladeTip - Main.screenPosition,
                null,
                lensColor * slashOpacity * 0.7f,
                MathHelper.PiOver2,
                shineTex.Size() / 2f,
                new Vector2(1f, 3f) * Projectile.scale,
                SpriteEffects.None,
                0
            );
        }

        private void DrawRightSpinTornado()
        {
            // 龙卷风绘制逻辑已回退停用
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
                float baseSwooshScale = GetSwooshScale();
                Vector2 swooshDrawPosition = rightSpinActive ? Owner.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY) : drawPosition;
                Vector2 swooshScaleVector = new Vector2(baseSwooshScale);
                float swooshRotation = GetSwooshRotation();
                float swooshAlpha = fadeIn * (rightSpinActive ? 0.68f : 0.42f);

                Main.EntitySpriteDraw(
                    swoosh.Value,
                    swooshDrawPosition,
                    null,
                    Color.DeepSkyBlue with { A = 0 } * swooshAlpha,
                    swooshRotation,
                    swoosh.Size() * 0.5f,
                    swooshScaleVector,
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

            bool tideFull = Owner.GetModPlayer<BBTideValuePlayer>().TideFull;
            if (tideFull)
            {
                Color outlineColor = new Color(0, 191, 255) with { A = 0 } * 0.6f * fadeIn;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 6f * fadeIn;
                    Main.EntitySpriteDraw(
                        ghost.Value,
                        drawPosition + offset,
                        ghost.Value.Frame(1, FrameCount, 0, Frame),
                        outlineColor,
                        Projectile.rotation + RotationOffset + r,
                        origin,
                        bladeScale,
                        effects);
                }
            }

            if (rightSpinActive && RightSpinChargeRatio >= 1f)
                DrawMaxRightSpinWeaponAfterimages(texture.Value, drawPosition, origin, bladeScale, effects, r);

            Main.EntitySpriteDraw(
                texture.Value,
                drawPosition,
                texture.Value.Frame(1, FrameCount, 0, Frame),
                lightColor,
                Projectile.rotation + RotationOffset + r,
                origin,
                bladeScale,
                effects);

            if (CanUseBladeShaderTrail())
            {
                DrawSlash();
                DrawLensFlare();
            }

            // 龙卷风特效：在满速转盘时以玩家为中心生成螺旋龙卷风
            DrawRightSpinTornado();

            return false;
        }

        private void DrawMaxRightSpinWeaponAfterimages(Texture2D texture, Vector2 drawPosition, Vector2 origin, Vector2 bladeScale, SpriteEffects effects, float rotationOffset)
        {
            float opacity = MathHelper.Clamp(0.52f + rightSpinOverEmpowerment * 0.012f, 0.52f, 0.82f) * fadeIn;
            for (int i = 0; i < 20; i++)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 22f + i * 0.7f);
                Vector2 ringOffset = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 1.2f;
                Vector2 jitterOffset = Main.rand.NextVector2Circular(2.4f, 2.4f);
                Color auraColor = Color.Lerp(Color.DeepSkyBlue, Color.White, pulse) with { A = 0 };

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + ringOffset + jitterOffset,
                    texture.Frame(1, FrameCount, 0, Frame),
                    auraColor * 0.16f * opacity,
                    Projectile.rotation + RotationOffset + rotationOffset,
                    origin,
                    bladeScale,
                    effects);
            }
        }

        private void SpawnBladeHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            Vector2 slashDirection = SlashAngle.ToRotationVector2().SafeNormalize(Vector2.UnitX * Owner.direction);
            Color ringColor = rightSpinActive ? Color.Cyan : Color.DeepSkyBlue;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center, Vector2.Zero, ringColor, new Vector2(1.05f, 0.58f),
                slashDirection.ToRotation(), 0.12f, 0.76f, 14));

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = slashDirection.RotatedByRandom(0.7f) * Main.rand.NextFloat(2.8f, 8.8f);
                Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost, velocity, 100,
                    Main.rand.NextBool(3) ? Color.White : ringColor, Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        // abb abc 杩炴嫑妯℃澘锛歐ave, Shuriken, Shuriken, Wave, Shuriken, Tornado
        // 鎵€鏈夋敾鍑婚棿闅斿钩绛夊寲锛屼笉鍐嶆湁蹇參涔嬪垎
        private float GetSwooshRotation()
        {
            if (rightSpinActive)
                return rightSpinDirectionVector.ToRotation() + MathHelper.PiOver2 * rightSpinOrbitDirection;

            return (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1f ? -90f : 90f) * -Owner.direction;
        }

        private float GetSwooshScale()
        {
            float scale = Projectile.scale * SwooshRadiusCorrection;
            if (rightSpinActive)
                scale *= RightSpinSwooshScaleMultiplier;

            return scale;
        }

        private StageProfile GetStageProfile(int stage)
        {
            BBLeftClickComboType[] sequence = BB_Balance.GetLeftClickComboSequence(CurrentGrowthStage);
            if (sequence == null || sequence.Length == 0)
                return new StageProfile(ComboKind.FormalWave, StandardGapFrames, 1f, 1f, 1f, 1f, 1f, 1f, false);

            stage = Utils.Clamp(stage, 0, sequence.Length - 1);
            BBLeftClickComboType comboType = sequence[stage];

            ComboKind kind = comboType switch
            {
                BBLeftClickComboType.SeafoamBlade => ComboKind.SeafoamBlade,
                BBLeftClickComboType.FormalWave => ComboKind.FormalWave,
                BBLeftClickComboType.FormalWaveFaster => ComboKind.FormalWaveFaster,
                BBLeftClickComboType.EnhancedWave => ComboKind.EnhancedWave,
                BBLeftClickComboType.ThreeSeafoams => ComboKind.Stage2_ThreeSeafoam,
                BBLeftClickComboType.SixSeaSpirits => ComboKind.Stage2_SixSeaSpirits,
                BBLeftClickComboType.TenSeaSpirits => ComboKind.Stage4_TenSeaSpirits,
                BBLeftClickComboType.FiveShurikens => ComboKind.FiveShurikens,
                BBLeftClickComboType.FiveParallelSeafoams => ComboKind.Stage3_FiveParallelSeafoam,
                BBLeftClickComboType.Tornado => ComboKind.Tornado,
                _ => ComboKind.FormalWave
            };

            return new StageProfile(kind, StandardGapFrames, 1f, 1f, 1f, 1f, 1f, 1f, false);
        }

        private enum ComboKind
        {
            SeafoamBlade,
            FormalWave,
            FormalWaveFaster,
            SingleShuriken,
            FiveShurikens,
            EnhancedWave,
            Tornado,
            Stage2_ThreeSeafoam,
            Stage2_SixSeaSpirits,
            Stage3_FiveParallelSeafoam,
            Stage3_TenSeaSpirits,
            Stage3_FiveShurikens,
            Stage3_EnhancedWave,
            Stage4_TenSeaSpirits,
            Stage4_EnhancedWave
        }

        private readonly struct StageProfile
        {
            public readonly ComboKind Kind;
            public readonly int GapFrames;
            public readonly float DrawScale;
            public readonly float RangeScale;
            public readonly float MinLengthScale;
            public readonly float MaxLengthScale;
            public readonly float MinThicknessScale;
            public readonly float SwooshScale;
            public readonly bool Tilted;

            public StageProfile(ComboKind kind, int gapFrames, float drawScale, float rangeScale, float minLengthScale, float maxLengthScale, float minThicknessScale, float swooshScale, bool tilted)
            {
                Kind = kind;
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
