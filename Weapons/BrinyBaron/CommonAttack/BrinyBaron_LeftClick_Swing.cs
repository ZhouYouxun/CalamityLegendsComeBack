using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityLegendsComeBack.Weapons.BrinyBaron.LeftClick;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
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

        // 0/1/2 = 前三刀普通攻击，3 = 第四刀普通攻击+手里剑，4 = 第五刀巨大化攻击
        private int CurrentComboStage => swingCount % 5;

        public override float HitboxOutset
        {
            get
            {
                if (CurrentComboStage == 4)
                    return 125f * currentScale;
                if (CurrentComboStage == 3)
                    return 125f;
                return 125f;
            }
        }

        public override Vector2 HitboxSize
        {
            get
            {
                if (CurrentComboStage == 4)
                    return new Vector2(190f, 190f) * currentScale;
                if (CurrentComboStage == 3)
                    return new Vector2(190f, 190f);
                return new Vector2(190f, 190f);
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

        // 第五刀巨大化专属流程
        private const int GiantGrowFrames = 15;
        private const int GiantShrinkFrames = 15;
        private bool giantGrowing = false;
        private bool giantSlashing = false;
        private bool giantShrinking = false;
        private int giantTimer = 0;
        private float currentScale = 1f;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void WhenSpawned()
        {
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.knockBack = 0f;
            Projectile.scale = 1f;
            Projectile.ai[1] = -1f;

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
                giantGrowing = false;
                giantSlashing = false;
                giantShrinking = false;
                giantTimer = 0;
                currentScale = 1f;
                Projectile.scale = 1f;
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
                    0.35f // 更改转动速度的快慢
                );

                // =========================
                // 第五刀：放大 → 挥砍 → 缩小
                // =========================
                if (CurrentComboStage == 4)
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

        private void DoNormalSwing()
        {
            Projectile.scale = 1f;
            currentScale = 1f;

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
                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * (1f + (Utils.GetLerpValue(useAnim * 0.8f, useAnim, Animation, true) * 0.35f))),
                    0.2f
                );
            }
            else
            {
                if (!finalFlip)
                    FlipAsSword = Owner.direction < 0;

                float time = AnimationProgress - (useAnim / 3f);
                float timeMax = useAnim - (useAnim / 3f);

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

                if (time > (int)(timeMax * 0.4f) && time < (int)(timeMax * 0.7f))
                {
                    CanHit = true;

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 particleVel = new Vector2(0f, 10f * -Projectile.ai[1] * Owner.direction)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        Vector2 particlePos = Owner.Center +
                            new Vector2(Main.rand.Next(30, CurrentComboStage == 3 ? 210 : 165), 0f)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        GeneralParticleHandler.SpawnParticle(
                            new LineParticle(
                                particlePos,
                                -particleVel.RotatedByRandom(0.2f) * 2f,
                                false,
                                19,
                                Main.rand.NextFloat(0.5f, 1f),
                                Main.rand.NextBool(4) ? Color.DeepSkyBlue : Color.Cyan
                            )
                        );

                        GeneralParticleHandler.SpawnParticle(
                            new HeavySmokeParticle(
                                particlePos,
                                -particleVel.RotatedByRandom(0.2f) * 2f,
                                Main.rand.NextBool(4) ? Color.MediumBlue : Color.Teal,
                                23,
                                Main.rand.NextFloat(0.5f, 1f),
                                0.65f
                            )
                        );
                    }

                    // 第四刀：正常大小攻击 + 放手里剑
                    if (CurrentComboStage == 3 && !spawnedStage4Projectiles && Main.myPlayer == Projectile.owner)
                    {
                        Vector2 shootDir = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX);

                        for (int i = 0; i < 3; i++)
                        {
                            float speed = 12f + i * 3.5f;
                            Vector2 velocity = shootDir.RotatedByRandom(0.08f) * speed;

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Owner.Center + shootDir * 24f,
                                velocity,
                                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                                Projectile.damage,
                                Projectile.knockBack,
                                Projectile.owner
                            );
                        }

                        spawnedStage4Projectiles = true;
                    }
                }
                else
                {
                    CanHit = false;
                }

                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(
                        MathHelper.Lerp(
                            150f * Projectile.ai[1] * Owner.direction,
                            120f * -Projectile.ai[1] * Owner.direction,
                            Utils.GetLerpValue(0f, 1f, time / timeMax, true)
                        )
                    ),
                    0.2f
                );

                if (time >= timeMax)
                    doSwing = false;

                if (time < (int)(timeMax * 0.7f))
                    postSwing = true;

                if (CanHit)
                {
                    int dustDistance = CurrentComboStage == 3 ? 225 : 180;

                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 spawnPos = Owner.Center +
                            new Vector2(dustDistance, 0f)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f))
                            .RotatedByRandom(0.3f);

                        if (Main.rand.NextBool(3))
                        {
                            Dust dust = Dust.NewDustPerfect(
                                spawnPos,
                                DustID.Water,
                                Vector2.Zero,
                                0,
                                default,
                                Main.rand.NextFloat(1.15f, 1.5f)
                            );
                            dust.noGravity = true;
                            dust.color = Color.DeepSkyBlue;

                            GeneralParticleHandler.SpawnParticle(
                                new GlowOrbParticle(
                                    spawnPos,
                                    Vector2.Zero,
                                    false,
                                    23,
                                    Main.rand.NextFloat(0.5f, 1f),
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
                                Main.rand.NextFloat(1.15f, 1.5f)
                            );
                            dust.noGravity = true;
                            dust.color = Color.Cyan;

                            GeneralParticleHandler.SpawnParticle(
                                new GlowOrbParticle(
                                    spawnPos,
                                    Vector2.Zero,
                                    false,
                                    23,
                                    Main.rand.NextFloat(0.5f, 1f),
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
                            Owner.Center +
                            new Vector2(dustDistance + 5f, 0f)
                            .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f))
                            .RotatedByRandom(0.3f),
                            DustID.FireworksRGB,
                            dustVel * Main.rand.NextFloat(0.1f, 0.5f)
                        );

                        dust2.scale = Main.rand.NextFloat(0.55f, 1.05f);
                        dust2.noGravity = true;
                        dust2.color = Main.rand.NextBool(3) ? Color.LightSeaGreen : Color.Aqua;
                    }
                }
            }
        }

        private void DoGiantSwing()
        {
            if (!finalFlip)
                FlipAsSword = Owner.direction < 0;

            // 进入第五刀时，先卡在后方角度并开始放大
            if (!giantGrowing && !giantSlashing && !giantShrinking)
            {
                giantGrowing = true;
                giantTimer = 0;
                currentScale = 1f;
                Projectile.scale = 1f;
                CanHit = false;
                postSwing = false;
                swingSound = true;
            }

            // 放大阶段
            if (giantGrowing)
            {
                giantTimer++;
                float growProgress = Utils.GetLerpValue(0f, GiantGrowFrames, giantTimer, true);

                currentScale = MathHelper.Lerp(1f, 2.4f, growProgress);
                Projectile.scale = currentScale;
                CanHit = false;
                postSwing = false;

                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction),
                    0.2f
                );

                SpawnGiantChargeParticles(growProgress);

                if (giantTimer >= GiantGrowFrames)
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
                    // 巨大化挥砍：海洋喷射尾迹（持续）
                    // =========================
                    Vector2 forward = Vector2.One.RotatedBy(Projectile.rotation + RotationOffset + MathHelper.ToRadians(90f));

                    for (int i = 0; i < 5; i++)
                    {
                        float length = Main.rand.NextFloat(80f, 320f) * currentScale;

                        Vector2 spawnPos = Owner.Center +
                            new Vector2(length, 0f).RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                        Vector2 vel =
                            -forward.RotatedByRandom(0.25f) * Main.rand.NextFloat(6f, 18f)
                            - Owner.velocity * 0.7f;

                        GeneralParticleHandler.SpawnParticle(
                            new CustomSpark(
                                spawnPos,
                                vel,
                                "CalamityMod/Particles/BloomCircle",
                                false,
                                18,
                                Main.rand.NextFloat(0.4f, 0.8f) * currentScale * 0.6f,
                                Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                                new Vector2(1.2f, 0.6f),
                                shrinkSpeed: 0.92f
                            )
                        );
                    }
                }
                float slashDuration = useAnim - (useAnim / 3f);
                float slashProgress = Utils.GetLerpValue(0f, slashDuration, giantTimer, true);

                if (slashProgress >= 0.4f && swingSound)
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
                    MathHelper.ToRadians(
                        MathHelper.Lerp(
                            150f * Projectile.ai[1] * Owner.direction,
                            120f * -Projectile.ai[1] * Owner.direction,
                            slashProgress
                        )
                    ),
                    0.2f
                );

                if (slashProgress > 0.4f && slashProgress < 0.7f)
                {
                    CanHit = true;
                    postSwing = true;
                    SpawnGiantSlashParticles();
                }
                else
                {
                    CanHit = false;
                    postSwing = slashProgress < 0.75f;
                }

                if (giantTimer >= slashDuration)
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
                float shrinkProgress = Utils.GetLerpValue(0f, GiantShrinkFrames, giantTimer, true);

                currentScale = MathHelper.Lerp(2.4f, 1f, shrinkProgress);
                Projectile.scale = currentScale;
                CanHit = false;
                postSwing = false;

                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * -Projectile.ai[1] * Owner.direction),
                    0.2f
                );

                SpawnGiantShrinkParticles(shrinkProgress);

                if (giantTimer >= GiantShrinkFrames)
                {
                    giantShrinking = false;
                    currentScale = 1f;
                    Projectile.scale = 1f;
                    doSwing = false;
                }
            }
        }

        private void SpawnGiantChargeParticles(float growProgress)
        {
            float scaledDistance = MathHelper.Lerp(120f, 260f, growProgress);

            for (int i = 0; i < 3; i++)
            {
                Vector2 particleVel = new Vector2(0f, 8f * -Projectile.ai[1] * Owner.direction)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                Vector2 particlePos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(30f, scaledDistance), 0f)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                GeneralParticleHandler.SpawnParticle(
                    new LineParticle(
                        particlePos,
                        -particleVel.RotatedByRandom(0.2f) * MathHelper.Lerp(1.2f, 2.2f, growProgress),
                        false,
                        19,
                        Main.rand.NextFloat(0.6f, 1.2f) * currentScale * 0.6f,
                        Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan
                    )
                );

                Dust frost = Dust.NewDustPerfect(
                    particlePos,
                    DustID.Frost,
                    -particleVel.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.4f, 1.1f)
                );
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(1.0f, 1.6f) * currentScale * 0.5f;
                frost.color = Color.Aqua;
            }
        }

        private void SpawnGiantSlashParticles()
        {
            float dustDistance = 180f * currentScale;

            for (int i = 0; i < 8; i++)
            {
                Vector2 spawnPos = Owner.Center +
                    new Vector2(dustDistance, 0f)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f))
                    .RotatedByRandom(0.3f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool() ? DustID.GemSapphire : DustID.Frost,
                    Vector2.One.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(0.3f, 1.1f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.3f, 2.4f) * currentScale * 0.5f;
                dust.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
            }

            for (int i = 0; i < 4; i++)
            {
                float randRot = Main.rand.NextFloat(-20f, -100f);
                Vector2 dustVel = new Vector2(0f, 11f * -Projectile.ai[1] * Owner.direction)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));

                Vector2 placement = Owner.Center +
                    new Vector2(Main.rand.NextFloat(120f, 520f), 0f)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        placement,
                        dustVel,
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        33,
                        Main.rand.NextFloat(0.45f, 0.7f) * currentScale * 0.5f,
                        Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                        new Vector2(1f, 1f),
                        shrinkSpeed: 0.1f
                    )
                );
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 beamPos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(160f, 560f), 0f)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                Vector2 beamVel = -Vector2.UnitY
                    .RotatedBy(FinalRotation)
                    .RotatedByRandom(0.3f)
                    * Main.rand.NextFloat(10f, 20f);

                Dust gem = Dust.NewDustPerfect(beamPos, DustID.GemSapphire, beamVel);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(1.5f, 2.6f) * currentScale * 0.45f;
                gem.color = Color.DeepSkyBlue;
            }
        }

        private void SpawnGiantShrinkParticles(float shrinkProgress)
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 particlePos = Owner.Center +
                    new Vector2(Main.rand.NextFloat(40f, 180f * currentScale), 0f)
                    .RotatedBy(FinalRotation + MathHelper.ToRadians(-45f));

                Dust dust = Dust.NewDustPerfect(
                    particlePos,
                    DustID.Water,
                    Main.rand.NextVector2Circular(1.5f, 1.5f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.9f, 1.5f) * MathHelper.Lerp(1.2f, 0.6f, shrinkProgress);
                dust.color = Color.Cyan;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CurrentComboStage == 4)
                Owner.GetModPlayer<BBEXPlayer>().AddTide();

            if ((target.life <= 0 && target.realLife == -1) && Projectile.numHits > 0)
                Projectile.numHits -= 1;

            if (damageDone <= 2)
                armoredHits++;

            if (Projectile.numHits == 0)
            {
                SoundEngine.PlaySound(
                    SoundID.Item14 with
                    {
                        Volume = 0.5f,
                        Pitch = 0.25f
                    },
                    Projectile.Center
                );

                SoundEngine.PlaySound(
                    SoundID.Splash with
                    {
                        Volume = 0.65f,
                        Pitch = Main.rand.NextFloat(-0.1f, 0.1f)
                    },
                    Projectile.Center
                );
            }

            for (int i = 0; i < MathHelper.Clamp(10 - Projectile.numHits * 2, 2, 10); i++)
            {
                Vector2 vel = ((Owner.Center - Main.MouseWorld).SafeNormalize(Vector2.UnitY) * -35f)
                    .RotatedByRandom(0.7f) * Main.rand.NextFloat(0.2f, 1f);

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    vel,
                    0,
                    default,
                    Main.rand.NextFloat(1.15f, 1.8f) * currentScale
                );
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
            }

            SoundEngine.PlaySound(SoundID.Splash, target.Center);
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

                float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
                float drawScale = CurrentComboStage == 4 ? currentScale : 1f;

                if (Animation > useAnim * 0.2f || giantGrowing || giantSlashing || giantShrinking)
                {
                    Main.EntitySpriteDraw(
                        swoosh.Value,
                        Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
                        null,
                        Color.DeepSkyBlue with { A = 0 } * fadeIn * 0.65f,
                        (FinalRotation + MathHelper.ToRadians(45f)) + MathHelper.ToRadians(Projectile.ai[1] == 1 ? -90f : 90f) * -Owner.direction,
                        swoosh.Size() * 0.5f,
                        drawScale * (CurrentComboStage == 4 ? 1.35f : (CurrentComboStage == 3 ? 0.75f : 0.6f)),
                        SpriteEffects.None
                    );
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
                        spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
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
                    spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
                );
            }

            return false;
        }
    }
}
