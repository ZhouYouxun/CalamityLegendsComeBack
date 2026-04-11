using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    public class BrinyBaron_LeftClick_Swing : BaseCustomUseStyleProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();

        // =========================
        // Exposed Tuning
        // =========================
        private const float BaseHitboxOutset = 125f;
        private static readonly Vector2 BaseHitboxSize = new Vector2(190f, 190f);
        private const float NormalSwooshScale = 0.6f;
        private const float StageFourSwooshScale = 0.75f;
        private const float GiantSwooshScale = 1.35f;

        // 0/1/2 = 前三刀普通攻击，3 = 第四刀普通攻击+手里剑，4 = 第五刀巨大化攻击
        private int CurrentComboStage => swingCount % 5;
        private bool IsGiantComboStage => CurrentComboStage == 4;
        private float CurrentVisualScale => currentScale;
        private float CurrentSwooshScaleMultiplier => IsGiantComboStage ? GiantSwooshScale : (CurrentComboStage == 3 ? StageFourSwooshScale : NormalSwooshScale);
        private float SlashAngle => FinalRotation + MathHelper.ToRadians(-45f);

        public override float HitboxOutset
        {
            get
            {
                return BaseHitboxOutset * CurrentVisualScale;
            }
        }

        public override Vector2 HitboxSize
        {
            get
            {
                return BaseHitboxSize * CurrentVisualScale;
            }
        }

        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        // 雄祖之护 贴图：124×124（正方形）
        // 雄祖之护 原点：(0, 124)
        // 月炎之锋 贴图：100×118（非正方形）
        // 月炎之锋 原点：(0, 118) 那我这个100×102
        // 我们的贴图是100×102，因此：
        public override Vector2 SpriteOrigin => new Vector2(0f, 102f);

        public Vector2 mousePos;
        public Vector2 aimVel;
        public bool doSwing = true;
        public bool postSwing = false;
        public float fadeIn = 0f;
        public int useAnim;
        public int swingCount;
        public bool finalFlip = false;
        public bool swingSound = true;
        public int armoredHits = 0;

        // 第四刀手里剑
        private bool spawnedStage4Projectiles = false;
        private bool spawnedNormalSwingWave = false;

        // 第五刀巨大化专属流程
        private bool giantGrowing = false;
        private bool giantSlashing = false;
        private bool giantShrinking = false;
        private int giantTimer = 0;
        private float normalModeScale = 1f;
        private float giantModeScale = 1.2f;
        private int giantGrowFrames = 15;
        private int giantShrinkFrames = 15;
        private int legendaryGrowthTier = 0;
        private float currentScale = 0.5f;
        private bool spawnedInvisibleSwingHitbox = false;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 0;
        }

        public override void WhenSpawned()
        {
            InitializeLegendaryGrowthValues();

            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.knockBack = 0f;
            SetCurrentScale(normalModeScale);
            Projectile.ai[1] = -1f;
            spawnedInvisibleSwingHitbox = false;

            mousePos = Main.MouseWorld;
            aimVel = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
            useAnim = Owner.itemAnimationMax;

            if (mousePos.X < Owner.Center.X)
                Owner.direction = -1;
            else
                Owner.direction = 1;

            FlipAsSword = Owner.direction == -1;
        }

        public override void ResetStyle()
        {
            CanHit = false;
        }

        public override bool? CanDamage() => false;

        // =========================
        // Scale And Placement Helpers
        // =========================
        private void InitializeLegendaryGrowthValues()
        {
            BB_Balance.BladeGrowthProfile growthProfile = ResolveBladeGrowthProfile();

            legendaryGrowthTier = growthProfile.GrowthTier;
            normalModeScale = growthProfile.BladeScale;
            giantModeScale = growthProfile.BladeScale * growthProfile.GiantScaleFactor;
            giantGrowFrames = growthProfile.GiantGrowFrames;
            giantShrinkFrames = growthProfile.GiantShrinkFrames;
        }

        private BB_Balance.BladeGrowthProfile ResolveBladeGrowthProfile()
        {
            return BB_Balance.GetBladeGrowthProfile();
        }

        private void SetCurrentScale(float scale)
        {
            currentScale = scale;
            Projectile.scale = scale;
        }

        private float ScaleDistance(float value) => value * CurrentVisualScale;

        private int GetInvisibleHitboxSize()
        {
            float squareSize = Math.Max(HitboxSize.X, HitboxSize.Y) + HitboxOutset * 2f;
            return Math.Max(1, (int)Math.Round(squareSize));
        }

        private void TrySpawnInvisibleSwingHitbox(float progress)
        {
            if (spawnedInvisibleSwingHitbox || progress < 0.5f || Main.myPlayer != Projectile.owner)
                return;

            int squareSize = GetInvisibleHitboxSize();
            float encodedScale = IsGiantComboStage ? -CurrentVisualScale : CurrentVisualScale;
            Vector2 spawnCenter = Owner.Center;

            Vector2 spawnDirection = (Main.MouseWorld - Projectile.Center).SafeNormalize(SlashAngle.ToRotationVector2());
            spawnCenter = Projectile.Center + spawnDirection * (squareSize * 0.5f);

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnCenter,
                Vector2.Zero,
                ModContent.ProjectileType<BBSwing_INV>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                squareSize,
                encodedScale,
                SlashAngle);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Projectile hitboxProjectile = Main.projectile[projectileIndex];
                hitboxProjectile.Center = spawnCenter;
                hitboxProjectile.velocity = Vector2.Zero;
            }

            spawnedInvisibleSwingHitbox = true;
        }

        private Vector2 GetSlashOffset(float distance)
        {
            return new Vector2(ScaleDistance(distance), 0f).RotatedBy(SlashAngle);
        }

        private Vector2 GetRandomSlashPosition(float minDistance, float maxDistance)
        {
            return Owner.Center + GetSlashOffset(Main.rand.NextFloat(minDistance, maxDistance));
        }

        private static float EvaluateEarthStyleSwingProgress(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float easedProgress = CalamityUtils.ExpInOutEasing(progress, 1);
            return MathHelper.Lerp(easedProgress, progress, 0.12f);
        }

        private static float EvaluateEarthStyleTurnLerp(float progress, float minLerp, float maxLerp)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float easedProgress = CalamityUtils.ExpInOutEasing(progress, 1);
            return MathHelper.Lerp(minLerp * 2f, maxLerp, easedProgress);
        }

        private void ApplyEarthStyleRotation(float targetDegrees, float progress, float minLerp, float maxLerp, bool forceAngleLerp = false)
        {
            float targetRotation = MathHelper.ToRadians(targetDegrees);
            float rotationLerp = EvaluateEarthStyleTurnLerp(progress, minLerp, maxLerp);

            if (forceAngleLerp)
            {
                RotationOffset = Utils.AngleLerp(RotationOffset, targetRotation, rotationLerp);
                return;
            }

            RotationOffset = MathHelper.Lerp(RotationOffset, targetRotation, rotationLerp);
        }

        private void ResetSwingState()
        {
            giantGrowing = false;
            giantSlashing = false;
            giantShrinking = false;
            giantTimer = 0;
            spawnedNormalSwingWave = false;
            spawnedInvisibleSwingHitbox = false;
            SetCurrentScale(normalModeScale);
        }

        private void SpawnNormalSwingWave()
        {
            Projectile.Center = Owner.Center;

            if (Main.myPlayer != Projectile.owner || CurrentComboStage > 2 || spawnedNormalSwingWave)
                return;

            Vector2 shootDirection = (Main.MouseWorld - Owner.Center).SafeNormalize(SlashAngle.ToRotationVector2());
            Vector2 spawnPosition = Owner.Center + shootDirection * ScaleDistance(34f);
            Vector2 waveVelocity = shootDirection * 11.5f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                waveVelocity,
                ModContent.ProjectileType<BBSwing_Wave>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );

            spawnedNormalSwingWave = true;
        }

        private void SpawnSwingScaleAccent(float distance, float intensity)
        {
            if (!CanHit)
                return;

            float wave = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f + AnimationProgress * 0.35f);
            Vector2 slashDirection = SlashAngle.ToRotationVector2();
            Vector2 perpendicular = slashDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 accentCenter = Owner.Center + GetSlashOffset(distance);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 accentPosition = accentCenter + perpendicular * side * ScaleDistance(8f + 4f * wave);
                Vector2 accentVelocity = slashDirection * (1.2f + 0.25f * intensity) + perpendicular * side * 0.25f;

                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        accentPosition,
                        accentVelocity,
                        false,
                        12,
                        CurrentVisualScale * (0.35f + 0.1f * intensity),
                        side < 0 ? Color.DeepSkyBlue : Color.Cyan,
                        true,
                        false,
                        true
                    )
                );
            }
        }

        public override void UseStyle()
        {
            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = false;

            if (CanHit || postSwing)
                mousePos = Owner.Center - aimVel;
            else
                mousePos = Main.MouseWorld;

            if (CanHit)
                fadeIn = MathHelper.Lerp(fadeIn, 1f, 0.3f);
            else
                fadeIn = MathHelper.Lerp(fadeIn, 0f, 0.35f);

            if (!doSwing)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                    Projectile.localNPCImmunity[i] = 0;

                Projectile.numHits = 0;
                mousePos = Main.MouseWorld;
                aimVel = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
                CanHit = false;

                if (mousePos.X < Owner.Center.X)
                    Owner.direction = -1;
                else
                    Owner.direction = 1;

                FlipAsSword = Owner.direction == -1;

                doSwing = true;
                swingCount++;
                finalFlip = false;
                swingSound = true;
                armoredHits = 0;
                spawnedStage4Projectiles = false;

                // 第五刀状态重置
                ResetSwingState();
            }
            else
            {
                if (!CanHit && !postSwing)
                {
                    if (mousePos.X < Owner.Center.X)
                        Owner.direction = -1;
                    else
                        Owner.direction = 1;
                }
                else
                {
                    if ((Owner.Center - aimVel).X < Owner.Center.X)
                        Owner.direction = -1;
                    else
                        Owner.direction = 1;
                }

                // 转动速度的核心
                Projectile.rotation = Projectile.rotation.AngleLerp(
                    Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f),
                    0.5f // 更改转动速度的快慢
                );

                // =========================
                // 第五刀：放大 → 挥砍 → 缩小
                // =========================
                if (IsGiantComboStage)
                {
                    DoGiantSwing();
                }
                else
                {
                    DoNormalSwing();
                }
            }




            // =========================
            // 手臂抓取角度（贴手校准）
            // =========================
            // 雄祖之护（GrandDad）：
            // - 贴图：124×124（正方形）
            // - 原点：(0, 124)
            // - ArmRotationOffset：-140°（完全贴手，无偏移）
            //
            // 月炎之锋（StellarStriker）：
            // - 贴图：100×118（非正方形）
            // - 原点：(0, 118)
            // - ArmRotationOffset：-140°（依然贴手，说明该值是“标准模板角度”）
            //
            // 我们当前武器：
            // - 贴图：100×102（非正方形）
            // - 原点：(0, 102)
            // - 当前问题：
            //   已经通过 SpriteOrigin 修正了“旋转轴”
            //   但由于贴图高度缩短，刀柄在贴图中的相对位置发生变化
            //   → 导致玩家手抓点与刀柄存在固定偏移
            //
            // 结论：
            // - ArmRotationOffset = -140° 是“标准武器模板值”
            // - 但当贴图高度变化（尤其变短）时：
            //   需要微调这个角度，让“手抓刀柄”重新对齐
            //
            // 调整方向：
            // - 如果刀“离手偏远” → 提高角度（-140 → -130）
            // - 如果刀“压进身体” → 降低角度（-140 → -150）
            //
            // 当前武器建议微调范围：
            // -135° ~ -145°
            //
            // ⚠️ 注意：
            // - 这里不是控制挥舞轨迹（RotationOffset）
            // - 这里只控制“手抓刀的位置”
            //
            // =========================
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        // =========================
        // Normal Swing
        // =========================
        private void DoNormalSwing()
        {
            SetCurrentScale(normalModeScale);

            if (AnimationProgress < (useAnim / 1.5f))
            {
                aimVel = (Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
                CanHit = false;
                postSwing = false;

                if (AnimationProgress == 0)
                {
                    doSwing = false;
                    Projectile.ai[1] = -Projectile.ai[1];
                }



                // 这里决定的是整体的挥动角度，角度越大，挥动的角度越大
                // GrandDad：ToRadians(120f
                // StellarStriker：ToRadians(-45f
                float windupProgress = Utils.GetLerpValue(0f, useAnim / 1.5f, AnimationProgress, true);
                float windupTargetAngle = 118f * Projectile.ai[1] * Owner.direction *
                    (1f + Utils.GetLerpValue(useAnim * 0.35f, useAnim * 0.6f, Animation, true) * 0.22f);

                ApplyEarthStyleRotation(windupTargetAngle, windupProgress, 0.18f, 0.2f);
            }
            else
            {
                if (!finalFlip)
                    FlipAsSword = Owner.direction < 0;

                float time = AnimationProgress - (useAnim / 3f);
                float timeMax = useAnim - (useAnim / 3f);
                float linearSlashProgress = Utils.GetLerpValue(0f, timeMax, time, true);
                float slashProgress = EvaluateEarthStyleSwingProgress(linearSlashProgress);
                TrySpawnInvisibleSwingHitbox(linearSlashProgress);

                if (time >= (int)(timeMax * 0.4f) && swingSound)
                {
                    SoundEngine.PlaySound(
                        SoundID.Item71 with
                        {
                            Volume = 0.8f,
                            Pitch = Main.rand.NextFloat(0.12f, 0.22f)
                        },
                        Projectile.Center
                    );

                    SoundEngine.PlaySound(
                        SoundID.Splash with
                        {
                            Volume = 0.45f,
                            Pitch = Main.rand.NextFloat(0.15f, 0.25f)
                        },
                        Projectile.Center
                    );

                    swingSound = false;
                }

                float hitStart = timeMax * 0.28f;
                float hitEnd = timeMax * 0.78f;

                if (time >= hitStart && time <= hitEnd)
                {
                    CanHit = true;
                    SpawnNormalSwingWave();

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 particleVel = new Vector2(0f, 10f * -Projectile.ai[1] * Owner.direction)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        Vector2 particlePos = GetRandomSlashPosition(30f, CurrentComboStage == 3 ? 210f : 165f);

                        GeneralParticleHandler.SpawnParticle(
                            new LineParticle(
                                particlePos,
                                -particleVel.RotatedByRandom(0.2f) * 2f,
                                false,
                                19,
                                Main.rand.NextFloat(0.5f, 1f) * CurrentVisualScale,
                                Main.rand.NextBool(4) ? Color.DeepSkyBlue : Color.Cyan
                            )
                        );

                        GeneralParticleHandler.SpawnParticle(
                            new HeavySmokeParticle(
                                particlePos,
                                -particleVel.RotatedByRandom(0.2f) * 2f,
                                Main.rand.NextBool(4) ? Color.MediumBlue : Color.Teal,
                                23,
                                Main.rand.NextFloat(0.5f, 1f) * CurrentVisualScale,
                                0.65f
                            )
                        );
                    }

                    // 第四刀：正常大小攻击 + 放手里剑
                    if (CurrentComboStage == 3 && !spawnedStage4Projectiles && Main.myPlayer == Projectile.owner)
                    {
                        Vector2 shootDir = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX);
                        int tideValue = Owner.GetModPlayer<BBEXPlayer>().TideValue;
                        int shurikenCount = BB_Balance.GetShurikenVolleyCount(tideValue);

                        for (int i = 0; i < shurikenCount; i++)
                        {
                            float progress = shurikenCount <= 1 ? 0.5f : i / (float)(shurikenCount - 1);
                            float spread = MathHelper.Lerp(-0.2f, 0.2f, progress);
                            float speed = 12f + i * 1.8f;
                            Vector2 velocity = shootDir.RotatedBy(spread).RotatedByRandom(0.04f) * speed;

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Owner.Center + shootDir * ScaleDistance(24f),
                                velocity,
                                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                                Projectile.damage,
                                Projectile.knockBack,
                                Projectile.owner,
                                2f
                            );
                        }

                        spawnedStage4Projectiles = true;
                    }
                }
                else
                {
                    CanHit = false;
                }

                ApplyEarthStyleRotation(
                    MathHelper.Lerp(
                        150f * Projectile.ai[1] * Owner.direction,
                        120f * -Projectile.ai[1] * Owner.direction,
                        slashProgress
                    ),
                    linearSlashProgress,
                    0.2f,
                    0.2f,
                    linearSlashProgress > 0.8f
                );

                if (time >= timeMax)
                    doSwing = false;

                if (time < (int)(timeMax * 0.7f))
                    postSwing = true;

                if (CanHit)
                {
                    float dustDistance = CurrentComboStage == 3 ? 225f : 180f;

                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 spawnPos = Owner.Center + GetSlashOffset(dustDistance).RotatedByRandom(0.3f);

                        if (Main.rand.NextBool(3))
                        {
                            Dust dust = Dust.NewDustPerfect(
                                spawnPos,
                                DustID.Water,
                                Vector2.Zero,
                                0,
                                default,
                                Main.rand.NextFloat(1.15f, 1.5f) * CurrentVisualScale
                            );
                            dust.noGravity = true;
                            dust.color = Color.DeepSkyBlue;

                            GeneralParticleHandler.SpawnParticle(
                                new GlowOrbParticle(
                                    spawnPos,
                                    Vector2.Zero,
                                    false,
                                    23,
                                    Main.rand.NextFloat(0.5f, 1f) * CurrentVisualScale,
                                    Color.DarkBlue,
                                    false,
                                    false,
                                    false
                                )
                            );
                        }
                        else
                        {
                            Dust dust = Dust.NewDustPerfect(
                                spawnPos,
                                DustID.Frost,
                                Vector2.Zero,
                                0,
                                default,
                                Main.rand.NextFloat(1.15f, 1.5f) * CurrentVisualScale
                            );
                            dust.noGravity = true;
                            dust.color = Color.Cyan;

                            GeneralParticleHandler.SpawnParticle(
                                new GlowOrbParticle(
                                    spawnPos,
                                    Vector2.Zero,
                                    false,
                                    23,
                                    Main.rand.NextFloat(0.5f, 1f) * CurrentVisualScale,
                                    Main.rand.NextBool(4) ? Color.DeepSkyBlue : Color.Cyan
                                )
                            );
                        }
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        float randRot = Main.rand.NextFloat(-30f, -60f);
                        Vector2 dustVel = new Vector2(0f, 15f * -Projectile.ai[1] * Owner.direction)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));

                        Dust dust2 = Dust.NewDustPerfect(
                            Owner.Center + GetSlashOffset(dustDistance + 5f).RotatedByRandom(0.3f),
                            DustID.FireworksRGB,
                            dustVel * Main.rand.NextFloat(0.1f, 0.5f)
                        );

                        dust2.scale = Main.rand.NextFloat(0.55f, 1.05f) * CurrentVisualScale;
                        dust2.noGravity = true;
                        dust2.color = Main.rand.NextBool(3) ? Color.LightSeaGreen : Color.Aqua;
                    }

                    SpawnSwingScaleAccent(dustDistance, CurrentComboStage == 3 ? 1.25f : 1f);
                }
            }
        }

        // =========================
        // Giant Swing
        // =========================
        private void DoGiantSwing()
        {
            if (!finalFlip)
                FlipAsSword = Owner.direction < 0;

            // 进入第五刀时，先卡在后方角度并开始放大
            if (!giantGrowing && !giantSlashing && !giantShrinking)
            {
                giantGrowing = true;
                giantTimer = 0;
                SetCurrentScale(normalModeScale);
                CanHit = false;
                postSwing = false;
                swingSound = true;
            }

            // 放大阶段
            if (giantGrowing)
            {
                giantTimer++;
                float growProgress = EvaluateEarthStyleSwingProgress(Utils.GetLerpValue(0f, giantGrowFrames, giantTimer, true));

                SetCurrentScale(MathHelper.Lerp(normalModeScale, giantModeScale, growProgress));
                CanHit = false;
                postSwing = false;

                ApplyEarthStyleRotation(
                    MathHelper.Lerp(
                        24f * Projectile.ai[1] * Owner.direction,
                        118f * Projectile.ai[1] * Owner.direction,
                        growProgress
                    ),
                    growProgress,
                    0.18f,
                    0.2f
                );

                SpawnGiantChargeParticles(growProgress);

                if (giantTimer >= giantGrowFrames)
                {
                    giantGrowing = false;
                    giantSlashing = true;
                    giantTimer = 0;
                }

                return;
            }

            // 挥砍阶段：速度和普通挥砍一致
            if (giantSlashing)
            {
                giantTimer++;

                {
                    // =========================
                    // 巨大化挥砍：主喷射尾迹 + 两侧45°辅喷流
                    // =========================
                    Vector2 slashDirection = (FinalRotation + MathHelper.ToRadians(-45f)).ToRotationVector2();
                    Vector2 backwardDirection = (Projectile.rotation + RotationOffset + MathHelper.ToRadians(90f)).ToRotationVector2();

                    // 主喷射尾迹：更密、更跟手、更像沿刀路拖出来的高能尾流
                    Vector2 trailStart = Owner.Center + slashDirection * ScaleDistance(78f);
                    for (int i = 0; i < 14; i++)
                    {
                        // 沿着刀路依次排开，而不是完全随机散点
                        float step = ScaleDistance(12f + i * 12f);
                        Vector2 spawnPos =
                            trailStart +
                            slashDirection * step -
                            backwardDirection * ScaleDistance(i * 6f) +
                            Main.rand.NextVector2Circular(7f, 7f) * CurrentVisualScale;

                        // 主尾流：扩散更小，速度更收敛，但数量更多
                        Vector2 vel =
                            -backwardDirection.RotatedByRandom(0.11f) * Main.rand.NextFloat(8f, 15f) -
                            Owner.velocity * 0.88f;

                        GeneralParticleHandler.SpawnParticle(
                            new CustomSpark(
                                spawnPos,
                                vel,
                                "CalamityMod/Particles/BloomCircle",
                                false,
                                24,
                                Main.rand.NextFloat(0.4f, 0.65f) * CurrentVisualScale,
                                Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Cyan,
                                new Vector2(1.2f, 0.58f),
                                shrinkSpeed: 0.92f
                            )
                        );
                    }

                    // 两侧45°辅喷流：很小、很短，只做点缀
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 sideDirection = (-backwardDirection).RotatedBy(MathHelper.ToRadians(45f) * side);
                        Vector2 sideStart =
                            Owner.Center +
                            slashDirection * ScaleDistance(132f) +
                            sideDirection * ScaleDistance(10f);

                        for (int j = 0; j < 4; j++)
                        {
                            Vector2 sideSpawnPos =
                                sideStart +
                                sideDirection * ScaleDistance(j * 11f) +
                                Main.rand.NextVector2Circular(4f, 4f) * CurrentVisualScale;

                            Vector2 sideVel =
                                sideDirection.RotatedByRandom(0.08f) * Main.rand.NextFloat(4.6f, 7.4f) -
                                Owner.velocity * 0.4f;

                            GeneralParticleHandler.SpawnParticle(
                                new CustomSpark(
                                    sideSpawnPos,
                                    sideVel,
                                    "CalamityMod/Particles/BloomCircle",
                                    false,
                                    16,
                                    Main.rand.NextFloat(0.18f, 0.3f) * CurrentVisualScale,
                                    side > 0 ? Color.Cyan : Color.DeepSkyBlue,
                                    new Vector2(0.82f, 0.38f),
                                    shrinkSpeed: 0.955f
                                )
                            );
                        }
                    }
                }



                float time = giantTimer;
                float timeMax = Owner.itemAnimationMax * 1.2f; // Earth 第1下：useAnim=2倍，挥砍段=0.6useAnim，所以最终=1.2倍 itemAnimationMax


                float linearSlashProgress = Utils.GetLerpValue(0f, timeMax, time, true);

                float slashProgress = CalamityUtils.ExpInOutEasing(time / timeMax, 1);
                
                TrySpawnInvisibleSwingHitbox(linearSlashProgress);

                float start = 150f * Projectile.ai[1] * Owner.direction;
                float end = 120f * -Projectile.ai[1] * Owner.direction;

                if (time >= (int)(timeMax * 0.4f) && swingSound)
                {
                    SoundEngine.PlaySound(
                        SoundID.Item71 with
                        {
                            Volume = 0.95f,
                            Pitch = Main.rand.NextFloat(0.05f, 0.15f)
                        },
                        Projectile.Center
                    );

                    SoundEngine.PlaySound(
                        SoundID.Item84 with
                        {
                            Volume = 0.7f,
                            Pitch = -0.1f
                        },
                        Projectile.Center
                    );

                    swingSound = false;
                }


                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(MathHelper.Lerp(start, end, slashProgress)),
                    0.2f
                );

                if (time > timeMax * 0.8f)
                {
                    RotationOffset = Utils.AngleLerp(
                        RotationOffset,
                        MathHelper.ToRadians(MathHelper.Lerp(start, end, slashProgress)),
                        0.2f
                    );
                }

                if (time > (int)(timeMax * 0.45f) && time < (int)(timeMax * 0.9f))
                {
                    CanHit = true;
                    postSwing = true;
                    SpawnGiantSlashParticles();
                    SpawnSwingScaleAccent(220f, 1.5f);
                }
                else
                    CanHit = false;

                if (time < (int)(timeMax * 0.7f))
                    postSwing = true;

                if (time >= timeMax)
                {
                    giantSlashing = false;
                    giantShrinking = true;
                    giantTimer = 0;
                    CanHit = false;
                    postSwing = false;
                }

                return;
            }

            // 缩小阶段
            if (giantShrinking)
            {
                giantTimer++;
                float shrinkProgress = EvaluateEarthStyleSwingProgress(Utils.GetLerpValue(0f, giantShrinkFrames, giantTimer, true));

                SetCurrentScale(MathHelper.Lerp(giantModeScale, normalModeScale, shrinkProgress));
                CanHit = false;
                postSwing = false;

                ApplyEarthStyleRotation(
                    MathHelper.Lerp(
                        120f * -Projectile.ai[1] * Owner.direction,
                        28f * -Projectile.ai[1] * Owner.direction,
                        shrinkProgress
                    ),
                    shrinkProgress,
                    0.18f,
                    0.2f
                );

                SpawnGiantShrinkParticles(shrinkProgress);

                if (giantTimer >= giantShrinkFrames)
                {
                    giantShrinking = false;
                    SetCurrentScale(normalModeScale);
                    doSwing = false;
                }
            }
        }

        // =========================
        // Effects
        // =========================
        private void SpawnGiantChargeParticles(float growProgress)
        {
            float scaledDistance = ScaleDistance(MathHelper.Lerp(120f, 260f, growProgress));

            for (int i = 0; i < 5; i++)
            {
                Vector2 particleVel = new Vector2(0f, 8f * -Projectile.ai[1] * Owner.direction)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                Vector2 particlePos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(30f * CurrentVisualScale, scaledDistance), 0f)
                    .RotatedBy(SlashAngle);

                GeneralParticleHandler.SpawnParticle(
                    new LineParticle(
                        particlePos,
                        -particleVel.RotatedByRandom(0.2f) * MathHelper.Lerp(1.2f, 2.2f, growProgress),
                        false,
                        19,
                        Main.rand.NextFloat(0.6f, 1.2f) * CurrentVisualScale * 0.6f,
                        Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan
                    )
                );

                Dust frost = Dust.NewDustPerfect(
                    particlePos,
                    DustID.Frost,
                    -particleVel.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.4f, 1.1f)
                );
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1.0f, 1.6f) * CurrentVisualScale * 0.5f;
                frost.color = Color.Aqua;
            }
        }

        private void SpawnGiantSlashParticles()
        {
            float dustDistance = ScaleDistance(180f);

            for (int i = 0; i < 12; i++)
            {
                Vector2 spawnPos = Owner.Center +
                    new Vector2(dustDistance, 0f)
                    .RotatedBy(SlashAngle)
                    .RotatedByRandom(0.3f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool() ? DustID.GemSapphire : DustID.Frost,
                    Vector2.One.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(0.3f, 1.1f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.3f, 2.4f) * CurrentVisualScale * 0.5f;
                dust.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
            }

            for (int i = 0; i < 6; i++)
            {
                float randRot = Main.rand.NextFloat(-20f, -100f);
                Vector2 dustVel = new Vector2(0f, 11f * -Projectile.ai[1] * Owner.direction)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));

                Vector2 placement = Owner.Center +
                    new Vector2(Main.rand.NextFloat(120f * CurrentVisualScale, 520f * CurrentVisualScale), 0f)
                    .RotatedBy(SlashAngle);

                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        placement,
                        dustVel,
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        33,
                        Main.rand.NextFloat(0.45f, 0.7f) * CurrentVisualScale * 0.5f,
                        Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                        new Vector2(1f, 1f),
                        shrinkSpeed: 0.1f
                    )
                );
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 beamPos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(160f * CurrentVisualScale, 560f * CurrentVisualScale), 0f)
                    .RotatedBy(SlashAngle);

                Vector2 beamVel = -Vector2.UnitY
                    .RotatedBy(FinalRotation)
                    .RotatedByRandom(0.3f)
                    * Main.rand.NextFloat(10f, 20f);

                Dust gem = Dust.NewDustPerfect(beamPos, DustID.GemSapphire, beamVel);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(1.5f, 2.6f) * CurrentVisualScale * 0.45f;
                gem.color = Color.DeepSkyBlue;
            }
        }

        private void SpawnGiantShrinkParticles(float shrinkProgress)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 particlePos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(40f * CurrentVisualScale, 180f * CurrentVisualScale), 0f)
                    .RotatedBy(SlashAngle);

                Dust dust = Dust.NewDustPerfect(
                    particlePos,
                    DustID.Water,
                    Main.rand.NextVector2Circular(1.5f, 1.5f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.9f, 1.5f) * MathHelper.Lerp(1.2f, 0.6f, shrinkProgress) * CurrentVisualScale;
                dust.color = Color.Cyan;
            }
        }

        private static float ResolveGiantSlashRotationDegrees(float startAngle, float loopEndAngle, float slashEndAngle, float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);

            const float fullSpinPortion = 0.8f;
            if (progress <= fullSpinPortion)
            {
                float spinProgress = EvaluateEarthStyleSwingProgress(progress / fullSpinPortion);
                return MathHelper.Lerp(startAngle, loopEndAngle, spinProgress);
            }

            float slashProgress = EvaluateEarthStyleSwingProgress((progress - fullSpinPortion) / (1f - fullSpinPortion));
            return MathHelper.Lerp(loopEndAngle, slashEndAngle, slashProgress);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float minMult = 0.5f;
            int hitsToMinMult = 15;
            float damageMult = Utils.Remap(Projectile.numHits - armoredHits, 0, hitsToMinMult, 1f, minMult, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if ((useAnim > 0 || DrawUnconditionally) && Owner.ItemAnimationActive)
            {
                Asset<Texture2D> tex = ModContent.Request<Texture2D>(Texture);
                Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");
                Asset<Texture2D> earthGhost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaronGoest");


                float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
                float drawScale = CurrentVisualScale;
                SpriteEffects drawEffects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

                if (Animation > useAnim * 0.2f || giantGrowing || giantSlashing || giantShrinking)
                {
                    Main.EntitySpriteDraw(
                        swoosh.Value,
                        Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
                        null,
                        Color.DeepSkyBlue with { A = 0 } * fadeIn * (IsGiantComboStage ? 0.4f : 0.65f),
                        (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1 ? -90f : 90f) * -Owner.direction,
                        swoosh.Size() * 0.5f,
                        drawScale * CurrentSwooshScaleMultiplier * (IsGiantComboStage ? 0.5f : 1f),
                        SpriteEffects.None
                    );
                }

                if (giantGrowing || giantSlashing || giantShrinking || CurrentComboStage == 4)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        float ringRotation = MathHelper.TwoPi * i / 20f;
                        Vector2 drawOffset = ringRotation.ToRotationVector2() * 6.5f * drawScale * fadeIn;
                        Color auraColor = Color.Lerp(Color.DeepSkyBlue, Color.White, 0.18f) with { A = 0 } * 0.17f * fadeIn;

                        Main.EntitySpriteDraw(
                            earthGhost.Value,
                            Projectile.Center - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
                            earthGhost.Value.Frame(1, FrameCount, 0, Frame),
                            auraColor,
                            Projectile.rotation + RotationOffset + r,
                            FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                            Projectile.scale,
                            drawEffects
                        );
                    }

                    for (int i = 0; i < 12; i++)
                    {
                        float ringRotation = MathHelper.TwoPi * i / 12f;
                        Vector2 drawOffset = ringRotation.ToRotationVector2() * 11f * drawScale * fadeIn;
                        Color auraColor = Color.Lerp(Color.Cyan, Color.White, 0.32f) with { A = 0 } * 0.11f * fadeIn;

                        Main.EntitySpriteDraw(
                            earthGhost.Value,
                            Projectile.Center - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
                            earthGhost.Value.Frame(1, FrameCount, 0, Frame),
                            auraColor,
                            Projectile.rotation + RotationOffset + r,
                            FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                            Projectile.scale * 1.04f,
                            drawEffects
                        );
                    }
                }

                for (int i = 0; i < 20; i++)
                {
                    Color auraColor = Color.Aqua with { A = 0 } * 0.12f * fadeIn;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 4f * drawScale * fadeIn;

                    Main.EntitySpriteDraw(
                        tex.Value,
                        Projectile.Center - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
                        tex.Frame(1, FrameCount, 0, Frame),
                        auraColor,
                        Projectile.rotation + RotationOffset + r,
                        FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                        Projectile.scale,
                        drawEffects
                    );
                }

                Main.EntitySpriteDraw(
                    tex.Value,
                    Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
                    tex.Frame(1, FrameCount, 0, Frame),
                    lightColor,
                    Projectile.rotation + RotationOffset + r,
                    FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                    Projectile.scale,
                    drawEffects
                );
            }

            return false;
        }

    }
}
