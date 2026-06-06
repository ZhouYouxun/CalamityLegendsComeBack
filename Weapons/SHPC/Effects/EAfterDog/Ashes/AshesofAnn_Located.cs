using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Utilities;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal class AshesofAnn_Located : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 自定义计时器（禁止用localAI）
        private int timer = 0;
        private const float AshEffectIntensity = 0.8f;
        private const int ExplosionTriggerFrame = 15;
        private const int PixelRingLifetime = 42;
        private bool exploded;
        private int pixelRingTimer = -1;
        private Vector2 pixelRingCenter;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.timeLeft = 20;
            Projectile.timeLeft = 19 * 3; // 生命周期
            Projectile.timeLeft = 20;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
        }

        public override bool? CanDamage()
        {
            return false; // 永远不造成伤害
        }

        public override void AI()
        {
            if (exploded)
            {
                pixelRingTimer++;
                Projectile.Center = pixelRingCenter;

                if (pixelRingTimer >= PixelRingLifetime)
                    Projectile.Kill();

                return;
            }

            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.npc.Length)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];

            if (!target.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            timer++;

            if (timer != ExplosionTriggerFrame)
                return;

            exploded = true;
            pixelRingTimer = 0;
            pixelRingCenter = Projectile.Center;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, PixelRingLifetime);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofAnn_SuperEXP>(),
                (int)(Projectile.damage * 1.0f),
                Projectile.knockBack,
                Projectile.owner
            );

            float shakePower = 12f * AshEffectIntensity;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);

            if (!Main.dedServ)
                CreateUltimateAshExplosionFX(Projectile.Center);
        }

        /*
            // 读取目标NPC
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.npc.Length)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];

            if (!target.active)
            {
                Projectile.Kill();
                return;
            }

            // 始终粘在敌人身上
            Projectile.Center = target.Center;

            // 每5帧触发一次爆炸
            timer++;
            if (timer != 15)
                return;

            if (timer % 15 == 0)
            {
                // 大范围低伤害爆炸
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AshesofAnn_SuperEXP>(),
                    (int)(Projectile.damage * 1.0f),
                    Projectile.knockBack,
                    Projectile.owner
                );

                //Projectile explosion = Main.projectile[explosionIndex];
                //explosion.width = 2500;
                //explosion.height = 2500;

                // 屏幕震动效果
                float shakePower = 35f; // 设置震动强度
                float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true); // 距离衰减
                shakePower = 12f;
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);



                CreateUltimateAshExplosionFX(Projectile.Center);
            }
        }
                        
        */
        // ====================
        // 颜色定义（放在类里面）
        // ====================

        // 主紫色
        private static readonly Color BladePurple = new(185, 35, 255);

        // 血红色
        private static readonly Color BladeBlood = new(135, 0, 42);

        // 深暗色
        private static readonly Color BladeDark = new(12, 0, 20);

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 主颜色
            Color mainColor = BladePurple;

            // ====================
            // 原版尾迹
            // ====================

            /*
            // Temporarily disabled: MagicPixel trail effect.
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 pos =
                    Projectile.oldPos[i] +
                    Projectile.Size / 2f -
                    Main.screenPosition;

                float progress =
                    i / (float)Projectile.oldPos.Length;

                float width =
                    MathHelper.Lerp(1.2f, 0.1f, progress);

                Color color =
                    Color.Lerp(
                        mainColor,
                        Color.Transparent,
                        progress) * 1.25f;

                Vector2 dir =
                    Projectile.oldPos[i - 1] -
                    Projectile.oldPos[i];

                float rot = dir.ToRotation();

                Main.EntitySpriteDraw(
                    pixel,
                    pos,
                    null,
                    color,
                    rot,
                    new Vector2(0f, 0.5f),
                    new Vector2(dir.Length(), width),
                    SpriteEffects.None,
                    0
                );
            }
            */

            // ====================
            // 赤红像素冲击碎片
            // 不再围成圆
            // 而是体素碎片化扩散
            // ====================

            /*
            // Temporarily disabled: MagicPixel voxel shock effect.
            Texture2D shockPixel = TextureAssets.MagicPixel.Value;

            // 世界坐标中心
            Vector2 shockCenter = Projectile.Center;

            // 扩散进度
            float progressValue =
                MathHelper.Clamp(
                    (260f - Projectile.timeLeft) / 260f,
                    0f,
                    1f);

            // 最大扩散范围
            float maxRadius = 30f * 16f;

            // 当前扩散半径
            float currentRadius =
                progressValue * maxRadius;

            // 像素块数量
            int fragmentCount = 320;

            // 固定随机种子
            UnifiedRandom seededRandom =
                new UnifiedRandom(Projectile.identity * 997);

            for (int i = 0; i < fragmentCount; i++)
            {
                // ====================
                // 固定随机方向
                // ====================

                float angle =
                    seededRandom.NextFloat(
                        MathHelper.TwoPi);

                // 固定随机距离
                float distanceFactor =
                    seededRandom.NextFloat();

                // 更集中于中间
                distanceFactor =
                    (float)Math.Sqrt(distanceFactor);

                // 当前距离
                float distance =
                    distanceFactor * currentRadius;

                // 波动
                float wobble =
                    (float)Math.Sin(
                        angle * 5f +
                        progressValue * 15f)
                    * 12f;

                // 世界坐标
                Vector2 worldPos =
                    shockCenter +
                    angle.ToRotationVector2() *
                    (distance + wobble);

                // 转屏幕坐标
                Vector2 finalDrawPos =
                    worldPos - Main.screenPosition;

                // ====================
                // 像素大小
                // ====================

                float cellSize =
                    seededRandom.NextFloat(6f, 18f);

                // ====================
                // 强度
                // ====================

                float intensity =
                    0.45f +
                    0.55f *
                    (float)Math.Sin(
                        angle * 9f +
                        i * 0.15f +
                        Main.GlobalTimeWrappedHourly * 7f);

                // ====================
                // 颜色
                // ====================

                Color shockColor =
                    Color.Lerp(
                        new Color(70, 0, 0),
                        new Color(255, 45, 35),
                        intensity);

                // 部分高亮
                if (i % 9 == 0)
                {
                    shockColor =
                        Color.Lerp(
                            shockColor,
                            Color.White,
                            0.3f);
                }

                // ====================
                // 透明度
                // ====================

                float alpha =
                    MathHelper.Lerp(
                        0.95f,
                        0.15f,
                        distanceFactor);

                // ====================
                // 绘制像素块
                // ====================

                Rectangle voxelRect = new Rectangle(
                  (int)finalDrawPos.X,
                  (int)finalDrawPos.Y,
                  (int)cellSize,
                  (int)cellSize);

                Main.spriteBatch.Draw(
                    shockPixel,
                    voxelRect,
                    null,
                    shockColor * alpha,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f);
            }
            */






            // ====================
            // BloomCircle
            // ====================

            Texture2D bloom =
                ModContent.Request<Texture2D>(
                    "CalamityMod/Particles/BloomCircle").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                BladeDark * 1.15f,
                0f,
                bloom.Size() / 2f,
                0.0625f,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                mainColor * 1.2f,
                0f,
                bloom.Size() / 2f,
                0.036f,
                SpriteEffects.None
            );

            // ====================
            // VerticalSmearRagged
            // ====================

            Texture2D smear =
                ModContent.Request<Texture2D>(
                    "CalamityMod/Particles/VerticalSmearRagged").Value;

            Vector2 smearPos = drawPos;

            for (int i = 0; i < 7; i++)
            {
                float rotation =
                    Projectile.rotation +
                    i * MathHelper.TwoPi / 7f +
                    Main.GlobalTimeWrappedHourly *
                    (i % 2 == 0 ? 2.1f : -1.7f);

                Color smearColor =
                    i % 3 == 0
                    ? BladeDark
                    : Color.Lerp(
                        mainColor,
                        BladeBlood,
                        i / 6f);

                smearColor.A = 0;

                Main.EntitySpriteDraw(
                    smear,
                    smearPos,
                    null,
                    smearColor * (0.72f - i * 0.055f),
                    rotation,
                    smear.Size() / 2f,
                    new Vector2(
                        0.34f + i * 0.025f,
                        0.96f + i * 0.08f) * 0.05f,
                    SpriteEffects.None
                );
            }

            // ====================
            // 星芒
            // ====================

            Texture2D star =
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/SimpleStar").Value;

            for (int i = 0; i < 4; i++)
            {
                Main.EntitySpriteDraw(
                    star,
                    drawPos,
                    null,
                    Color.Lerp(
                        Color.White,
                        mainColor,
                        0.45f) * (0.54f - i * 0.07f),
                    Projectile.rotation +
                    i * MathHelper.PiOver2,
                    star.Size() / 2f,
                    new Vector2(
                        0.55f,
                        1.55f + i * 0.24f) * 0.05f,
                    SpriteEffects.None
                );
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }






        private static int ScaledCount(int count) => Math.Max(1, (int)MathF.Round(count * AshEffectIntensity));

        private static float ScaledPower(float value) => value * AshEffectIntensity;

        /*
        // Temporarily disabled: MagicPixel crimson ring helper.
        private void DrawCrimsonMagicPixelRing(Vector2 center, int frame)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 drawCenter = center - Main.screenPosition;

            // 爆炸扩散进度：0 → 1
            float progress = Utils.GetLerpValue(0f, PixelRingLifetime, frame, true);

            // 平滑进度，让扩散不是匀速死板外推
            float smoothProgress = progress * progress * (3f - 2f * progress);

            // 冲击波进度：前期快，后期慢，更像爆炸冲出去
            float shockProgress = 1f - (1f - progress) * (1f - progress);

            // 主圆环半径，从中心炸到超大范围
            float radius = MathHelper.Lerp(32f, ScaledPower(1280f), shockProgress);

            // 前几帧淡入，最后十几帧淡出
            float alpha =
                Utils.GetLerpValue(0f, 5f, frame, true) *
                Utils.GetLerpValue(PixelRingLifetime, PixelRingLifetime - 13f, frame, true);

            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            // 每发弹幕都有不同相位，避免多个爆炸完全一样
            float seed = Projectile.identity * 0.731f + Projectile.owner * 0.197f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // ==============================
            // 1. 主圆环：方块拼成的赤红数据爆炸环
            // ==============================
            int segmentCount = ScaledCount(128);
            float baseBlockSize = MathHelper.Lerp(20f, 8f, smoothProgress);

            for (int i = 0; i < segmentCount; i++)
            {
                // 用正弦制造“故障断层”，不是纯随机
                float glitchWave =
                    (float)Math.Sin(i * 2.391f + seed + frame * 0.04f) * 0.5f +
                    (float)Math.Sin(i * 5.113f + seed * 1.7f - frame * 0.025f) * 0.5f;

                // 后期让一部分方块消失，形成数据流崩坏感
                float disappearGate = MathHelper.Lerp(-1.2f, 0.46f, progress);
                if (glitchWave < disappearGate && i % 3 == 0)
                    continue;

                float angle = MathHelper.TwoPi * i / segmentCount + glitchWave * 0.018f;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                // 半径抖动：让圆不是完美数学圆，而是爆炸故障圆
                float radialJitter =
                    (float)Math.Sin(i * 3.71f + seed * 2.3f + frame * 0.11f) *
                    MathHelper.Lerp(6f, 52f, progress);

                Vector2 position = drawCenter + outward * (radius + radialJitter);

                // 方块呼吸变化
                float blockPulse = 0.84f + 0.28f * (float)Math.Sin(i * 1.97f + frame * 0.22f + seed);
                float blockSize = baseBlockSize * blockPulse;

                // 赤红主色，中心早期略带金白高温
                Color ringColor = Color.Lerp(new Color(255, 84, 24), new Color(120, 0, 0), progress * 0.82f);
                ringColor = Color.Lerp(ringColor, new Color(255, 210, 100), 0.15f * (1f - progress));
                ringColor.A = 0;

                // 方块本体
                Main.EntitySpriteDraw(
                    pixel,
                    position,
                    null,
                    ringColor * alpha,
                    angle + MathHelper.Pi / 4f,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(blockSize, blockSize),
                    SpriteEffects.None,
                    0
                );

                // 少量切线短条：像数据流沿圆周撕裂
                if (i % 4 == 0)
                {
                    float lineLength = MathHelper.Lerp(34f, 82f, progress) * (0.75f + Math.Abs(glitchWave) * 0.35f);
                    float lineWidth = MathHelper.Lerp(4.5f, 1.8f, progress);

                    Color lineColor = Color.Lerp(new Color(255, 110, 36), new Color(180, 0, 0), progress);
                    lineColor.A = 0;

                    Main.EntitySpriteDraw(
                        pixel,
                        position + tangent * glitchWave * 10f,
                        null,
                        lineColor * alpha * 0.72f,
                        tangent.ToRotation(),
                        new Vector2(0f, 0.5f),
                        new Vector2(lineLength, lineWidth),
                        SpriteEffects.None,
                        0
                    );
                }
            }

            // ==============================
            // 2. 内侧残留环：更密、更暗，像矩阵内圈正在崩解
            // ==============================
            int innerCount = ScaledCount(84);
            float innerRadius = radius * MathHelper.Lerp(0.74f, 0.88f, smoothProgress);

            for (int i = 0; i < innerCount; i++)
            {
                float wave = (float)Math.Sin(i * 4.817f + seed * 0.9f + frame * 0.16f);

                // 做断裂感，不要画满一整圈
                if (wave < MathHelper.Lerp(-0.95f, 0.25f, progress))
                    continue;

                float angle = MathHelper.TwoPi * i / innerCount + wave * 0.026f;
                Vector2 outward = angle.ToRotationVector2();

                float blockSize = MathHelper.Lerp(12f, 5f, progress) * (0.85f + Math.Abs(wave) * 0.25f);

                Color color = Color.Lerp(new Color(180, 0, 0), new Color(45, 0, 0), progress);
                color.A = 0;

                Main.EntitySpriteDraw(
                    pixel,
                    drawCenter + outward * (innerRadius + wave * MathHelper.Lerp(8f, 40f, progress)),
                    null,
                    color * alpha * 0.62f,
                    -angle + frame * 0.025f,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(blockSize, blockSize),
                    SpriteEffects.None,
                    0
                );
            }

            // ==============================
            // 3. 外侧碎片：飞出去的赤红像素残骸
            // ==============================
            int shardCount = ScaledCount(64);
            float shardAlpha = alpha * Utils.GetLerpValue(0.06f, 0.22f, progress, true);

            for (int i = 0; i < shardCount; i++)
            {
                float wave = (float)Math.Sin(i * 4.119f + seed * 3.1f + frame * 0.07f);

                // 越到后期，碎片越稀疏，像数据块正在消失
                if (wave < MathHelper.Lerp(-0.8f, 0.5f, progress))
                    continue;

                float angle = MathHelper.TwoPi * (i + 0.5f) / shardCount + wave * 0.04f;
                Vector2 outward = angle.ToRotationVector2();

                float shardRadius =
                    radius +
                    MathHelper.Lerp(42f, 220f, progress) +
                    wave * MathHelper.Lerp(12f, 86f, progress);

                float blockSize = MathHelper.Lerp(17f, 6f, progress) * (0.8f + 0.3f * Math.Abs(wave));

                Color shardColor = Color.Lerp(new Color(255, 58, 18), new Color(70, 0, 0), progress);
                shardColor.A = 0;

                Main.EntitySpriteDraw(
                    pixel,
                    drawCenter + outward * shardRadius,
                    null,
                    shardColor * shardAlpha,
                    -angle + frame * 0.04f,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(blockSize, blockSize),
                    SpriteEffects.None,
                    0
                );
            }

            // ==============================
            // 4. 径向数据流：从中心向外扫出的短线
            // ==============================
            int streamCount = ScaledCount(36);
            for (int i = 0; i < streamCount; i++)
            {
                float wave = (float)Math.Sin(i * 6.13f + seed * 1.4f + frame * 0.19f);

                if (wave < 0.05f)
                    continue;

                float angle = MathHelper.TwoPi * i / streamCount + wave * 0.05f;
                Vector2 outward = angle.ToRotationVector2();

                float streamRadius = radius * MathHelper.Lerp(0.22f, 0.98f, progress) + wave * 80f;
                float streamLength = MathHelper.Lerp(40f, 150f, progress) * wave;
                float streamWidth = MathHelper.Lerp(5f, 1.6f, progress);

                Color streamColor = Color.Lerp(new Color(255, 120, 42), new Color(130, 0, 0), progress);
                streamColor.A = 0;

                Main.EntitySpriteDraw(
                    pixel,
                    drawCenter + outward * streamRadius,
                    null,
                    streamColor * alpha * 0.48f,
                    outward.ToRotation(),
                    new Vector2(0f, 0.5f),
                    new Vector2(streamLength, streamWidth),
                    SpriteEffects.None,
                    0
                );
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        */

        private void CreateUltimateAshExplosionFX(Vector2 center)
        {
            // 爆炸音效
            SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/最后通牒爆炸")
            {
                Volume = 0.82f * AshEffectIntensity,
                Pitch = -0.08f,
                PitchVariance = 0.16f,
                MaxInstances = 5
            }, center);

            Particle compactPulse = new CustomPulse(
                center,
                Vector2.Zero,
                new Color(200, 30, 30),
                "CalamityMod/Particles/LargeBloom",
                Vector2.One * AshEffectIntensity,
                Main.rand.NextFloat(-0.12f, 0.12f),
                0.18f,
                0.82f,
                16,
                false);
            GeneralParticleHandler.SpawnParticle(compactPulse);

            for (int i = 0; i < ScaledCount(24); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(ScaledPower(4f), ScaledPower(13f));
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(ScaledPower(24f), ScaledPower(24f)),
                    DustID.Torch,
                    velocity,
                    80,
                    Color.Lerp(new Color(255, 70, 24), new Color(90, 0, 0), Main.rand.NextFloat(0.2f, 0.75f)),
                    Main.rand.NextFloat(1f, 1.7f) * AshEffectIntensity);
                dust.noGravity = true;
            }

            for (int i = 0; i < ScaledCount(10); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(ScaledPower(3f), ScaledPower(9f));
                Particle line = new LineParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.45f, 0.82f) * AshEffectIntensity,
                    Color.Lerp(new Color(255, 96, 40), new Color(120, 0, 0), Main.rand.NextFloat(0.25f, 0.7f)));
                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < ScaledCount(6); i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(ScaledPower(24f), ScaledPower(24f)),
                    Main.rand.NextVector2Circular(ScaledPower(2.5f), ScaledPower(2.5f)),
                    Main.rand.NextBool() ? new Color(70, 0, 0) : Color.Black,
                    Main.rand.Next(22, 34),
                    Main.rand.NextFloat(0.65f, 1.05f) * AshEffectIntensity,
                    0.34f * AshEffectIntensity,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // return;

            Particle expandingPulse = new DirectionalPulseRing(
                center,
                Vector2.Zero,
                new Color(200, 30, 30), // 赤红写死
                new Vector2(0.72f, 0.72f) * AshEffectIntensity,
                0f,
                0.28f,
                ScaledPower(12f), // scaled burst radius
                14
            );
            GeneralParticleHandler.SpawnParticle(expandingPulse);

            for (int i = 0; i < ScaledCount(24); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(ScaledPower(4f), ScaledPower(13f));
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(ScaledPower(24f), ScaledPower(24f)),
                    DustID.Torch,
                    velocity,
                    80,
                    Color.Lerp(new Color(255, 70, 24), new Color(90, 0, 0), Main.rand.NextFloat(0.2f, 0.75f)),
                    Main.rand.NextFloat(1f, 1.7f) * AshEffectIntensity);
                dust.noGravity = true;
            }

            for (int i = 0; i < ScaledCount(10); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(ScaledPower(3f), ScaledPower(9f));
                Particle line = new LineParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.45f, 0.82f) * AshEffectIntensity,
                    Color.Lerp(new Color(255, 96, 40), new Color(120, 0, 0), Main.rand.NextFloat(0.25f, 0.7f)));
                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < ScaledCount(6); i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(ScaledPower(24f), ScaledPower(24f)),
                    Main.rand.NextVector2Circular(ScaledPower(2.5f), ScaledPower(2.5f)),
                    Main.rand.NextBool() ? new Color(70, 0, 0) : Color.Black,
                    Main.rand.Next(22, 34),
                    Main.rand.NextFloat(0.65f, 1.05f) * AshEffectIntensity,
                    0.34f * AshEffectIntensity,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // return;

            // 1. 最内核：高温内爆，只放亮而烫的熔岩球
            CreateAshInnerCollapse(center);

            // 2. 中层：黑红邪术魔法阵 + 六芒/圆环结构
            CreateAshMagicCircleDust(center);

            // 3. 中层空隙：黑烟为主，骷髅头为辅，比例约 8:2
            CreateAshSmokeAndSkulls(center);

            // 4. 外层：细碎火花与暗红碎屑，负责把整体撑到超大范围
            CreateAshOuterSparks(center);
        }

            private void CreateAshInnerCollapse(Vector2 center)
            {
                // ===== 1. 最中心：规则连通的高温核心 =====
                // 不是随机乱撒，而是用“涂黑的圆”思路铺满
                // 每一圈的相邻点距离都控制在半径直径以内，保证视觉上彼此连接
                CreateConnectedRancorCore(center);

                // ===== 2. 四条银河悬臂式 inward 弧臂 =====
                // 用 4 条向内弯曲的弧线来做“邪恶吸收感”
                // 每个点都带向心速度，同时保留少量切线速度，既有秩序也有混沌
                CreateConnectedInwardArms(center);

                // ===== 3. 核心高温 Dust =====
                // 保留你喜欢的核心区扩散感，但把扩散范围整体扩大到原来的约 3 倍
                // 同时做三层：内圈高热、中过渡、外圈无序碎裂
                for (int layer = 0; layer < 3; layer++)
                {
                    int count;
                    float minRadius;
                    float maxRadius;
                    float minSpeed;
                    float maxSpeed;
                    Color colorA;
                    Color colorB;

                    switch (layer)
                    {
                        case 0:
                            count = ScaledCount(70);
                            minRadius = ScaledPower(30f);
                            maxRadius = ScaledPower(120f);
                            minSpeed = ScaledPower(8f);
                            maxSpeed = ScaledPower(18f);
                            colorA = new Color(255, 120, 30);
                            colorB = new Color(255, 240, 150);
                            break;

                        case 1:
                            count = ScaledCount(90);
                            minRadius = ScaledPower(100f);
                            maxRadius = ScaledPower(220f);
                            minSpeed = ScaledPower(10f);
                            maxSpeed = ScaledPower(24f);
                            colorA = new Color(255, 80, 20);
                            colorB = new Color(255, 180, 70);
                            break;

                        default:
                            count = ScaledCount(120);
                            minRadius = ScaledPower(180f);
                            maxRadius = ScaledPower(360f); // scaled from the restored large ring
                            minSpeed = ScaledPower(12f);
                            maxSpeed = ScaledPower(30f);
                            colorA = new Color(200, 40, 10);
                            colorB = new Color(255, 150, 40);
                            break;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        // 有序：基础放射
                        float angle = MathHelper.TwoPi * i / count;

                        // 无序：叠一点随机偏移和伪噪声扰动
                        float angleJitter = Main.rand.NextFloat(-0.08f, 0.08f);
                        float noise = (float)Math.Sin(angle * 3f + i * 0.37f) * 0.5f + 0.5f;
                        float radius = MathHelper.Lerp(minRadius, maxRadius, Main.rand.NextFloat() * 0.55f + noise * 0.45f);

                        Vector2 outward = (angle + angleJitter).ToRotationVector2();
                        Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);
                        Vector2 spawnPos = center + outward * radius;

                        Dust coreDust = Dust.NewDustPerfect(spawnPos, DustID.PortalBoltTrail);
                        coreDust.noGravity = true;
                        coreDust.scale = Main.rand.NextFloat(1.6f, 3.2f) * AshEffectIntensity;

                        // 速度：以向外爆散为主，同时掺一点切线旋感
                        coreDust.velocity =
                            outward * Main.rand.NextFloat(minSpeed, maxSpeed) +
                            tangent * Main.rand.NextFloat(ScaledPower(-2.2f), ScaledPower(2.2f));

                        coreDust.color = Color.Lerp(colorA, colorB, Main.rand.NextFloat());
                    }
                }
            }

            private void CreateConnectedRancorCore(Vector2 center)
            {
                // 整体随机：统一旋转 + 轻微整体缩放
                float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                float globalScale = Main.rand.NextFloat(0.92f, 1.08f);

                // ===== 中心填充（防空洞）=====
                for (int i = 0; i < ScaledCount(10); i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(ScaledPower(18f), ScaledPower(18f));
                    float radius = Main.rand.NextFloat(ScaledPower(42f), ScaledPower(72f)) * globalScale;

                    RancorLavaMetaball.SpawnParticle(center + offset, radius);
                }

                // ===== 同心结构（核心逻辑）=====
                float[] ringRadii = new float[] { 44f, 84f, 132f, 188f };

                for (int ringIndex = 0; ringIndex < ringRadii.Length; ringIndex++)
                {
                    float ringRadius = ScaledPower(ringRadii[ringIndex]) * globalScale;

                    // 点密度随半径变化
                    float spacing = 48f;
                    int pointCount = Math.Max(8, (int)Math.Ceiling(MathHelper.TwoPi * ringRadius / spacing));

                    // 每圈额外随机偏移（但统一）
                    float ringOffset = Main.rand.NextFloat(-0.14f, 0.14f);

                    for (int i = 0; i < pointCount; i++)
                    {
                        float angle = MathHelper.TwoPi * i / pointCount + globalRotation + ringOffset;

                        // 轻微波动（保证不死板但仍连通）
                        float radialJitter = (float)Math.Sin(i * 2.17f + ringIndex * 1.31f) * ScaledPower(10f + ringIndex * 8f);

                        Vector2 pos = center + angle.ToRotationVector2() * (ringRadius + radialJitter);

                        float size = Main.rand.NextFloat(ScaledPower(26f + ringIndex * 3f), ScaledPower(48f + ringIndex * 8f)) * globalScale;

                        RancorLavaMetaball.SpawnParticle(pos, size);
                    }
                }
            }
        private void CreateConnectedInwardArms(Vector2 center)
        {
            // 整体随机（统一作用）
            float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            float globalTightness = Main.rand.NextFloat(0.85f, 1.2f) * AshEffectIntensity;

            int armCount = 7;
            int pointsPerArm = ScaledCount(18);

            for (int arm = 0; arm < armCount; arm++)
            {
                float armBaseAngle = MathHelper.TwoPi * arm / armCount + globalRotation + Main.rand.NextFloat(-0.08f, 0.08f);

                for (int i = 0; i < pointsPerArm; i++)
                {
                    float t = i / (float)(pointsPerArm - 1);

                    float radius = MathHelper.Lerp(ScaledPower(42f), ScaledPower(520f), t);

                    // 螺旋强度带随机（但整条臂一致）
                    float spiral = 0f;

                    // 轻微波动
                    float wave = (float)Math.Sin(t * MathHelper.TwoPi * 3.2f + arm * 0.9f) * MathHelper.Lerp(0.035f, 0.11f, t) * globalTightness;

                    float angle = armBaseAngle + spiral + wave;

                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    Vector2 spawnPos = center + outward * radius;

                    Vector2 velocity =
                        outward * MathHelper.Lerp(ScaledPower(8.5f), ScaledPower(22f), t) +
                        tangent * Main.rand.NextFloat(ScaledPower(-1.2f), ScaledPower(1.2f));

                    float size = MathHelper.Lerp(ScaledPower(62f), ScaledPower(24f), t) + Main.rand.NextFloat(ScaledPower(-4f), ScaledPower(5f));

                    GruesomeMetaball.SpawnParticle(spawnPos, velocity, size);

                    // ===== 补连接点（关键，防断裂）=====
                    if (i % 4 == 1 && i < pointsPerArm - 1)
                    {
                        float t2 = (i + 1) / (float)(pointsPerArm - 1);
                        float r2 = MathHelper.Lerp(ScaledPower(42f), ScaledPower(520f), t2);

                        float spiral2 = 0f;
                        float wave2 = (float)Math.Sin(t2 * MathHelper.TwoPi * 3.2f + arm * 0.9f) * MathHelper.Lerp(0.035f, 0.11f, t2) * globalTightness;

                        float angle2 = armBaseAngle + spiral2 + wave2;

                        Vector2 pos2 = center + angle2.ToRotationVector2() * r2;

                        Vector2 mid = (spawnPos + pos2) * 0.5f;

                        Vector2 midDir = (mid - center).SafeNormalize(Vector2.Zero);
                        Vector2 midTan = midDir.RotatedBy(MathHelper.PiOver2);

                        Vector2 midVel =
                            midDir * MathHelper.Lerp(ScaledPower(7f), ScaledPower(17f), t) +
                            midTan * Main.rand.NextFloat(ScaledPower(-0.65f), ScaledPower(0.65f));

                        GruesomeMetaball.SpawnParticle(mid, midVel, size * 0.9f);
                    }
                }
            }
        }


            private void CreateAshMagicCircleDust(Vector2 center)
            {
                // 三个固定主圈：40格、50格、60格
                float[] mainRings = new float[]
                {
                    ScaledPower(40f * 16f),
                    ScaledPower(50f * 16f),
                    ScaledPower(60f * 16f)
                };

                Color[] innerColors = new Color[]
                {
                    new Color(70, 0, 0),
                    new Color(45, 0, 0),
                    new Color(30, 0, 0)
                };

                Color[] outerColors = new Color[]
                {
                    new Color(170, 20, 20),
                    new Color(145, 15, 15),
                    new Color(110, 10, 10)
                };

                int[] ringCounts = new int[]
                {
                    48,
                    60,
                    72
                };

                float[] outwardSpeeds = new float[]
                {
                    1.2f,
                    1.0f,
                    0.85f
                };

                float[] tangentialSpeeds = new float[]
                {
                    1.9f,
                    1.6f,
                    1.35f
                };

                // 每次调用整体随机旋转一下，但三圈相互仍保持统一审美
                float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int i = 0; i < mainRings.Length; i++)
                {
                    // 先画固定主圈
                    CreateAshCircleRing(
                        center,
                        mainRings[i],
                        ringCounts[i],
                        267,
                        innerColors[i],
                        outerColors[i],
                        outwardSpeeds[i],
                        tangentialSpeeds[i]
                    );

                    // 再在这一圈上随机散布“小圆环 / 小三角”
                    CreateAshScatterGlyphsOnRing(
                        center,
                        mainRings[i],
                        globalRotation,
                        innerColors[i],
                        outerColors[i]
                    );
                }
            }
        private void CreateAshScatterGlyphsOnRing(Vector2 center, float ringRadius, float globalRotation, Color innerColor, Color outerColor)
        {
            // 每一圈上随机散布一些符文位点
            int glyphCount = Main.rand.Next(ScaledCount(8), ScaledCount(13) + 1);

            for (int i = 0; i < glyphCount; i++)
            {
                // 用“分区 + 抖动”保证随机但不至于扎堆
                float baseAngle = MathHelper.TwoPi * i / glyphCount;
                float randomOffset = Main.rand.NextFloat(-0.22f, 0.22f);
                float angle = baseAngle + randomOffset + globalRotation;

                Vector2 direction = angle.ToRotationVector2();
                Vector2 glyphCenter = center + direction * ringRadius;

                // 小圆环 / 小三角随机出现
                switch (Main.rand.Next(3))
                {
                    case 0:
                        {
                            // 小圆环
                            float smallRingRadius = Main.rand.NextFloat(ScaledPower(28f), ScaledPower(52f));
                            int smallRingCount = Main.rand.Next(10, 16);

                            CreateAshCircleRing(
                                glyphCenter,
                                smallRingRadius,
                                smallRingCount,
                                267,
                                innerColor,
                                Color.Lerp(outerColor, new Color(220, 35, 35), 0.35f),
                                0.55f,
                                0.9f
                            );
                            break;
                        }

                    case 1:
                        {
                            // 正三角
                            float triangleRadius = Main.rand.NextFloat(ScaledPower(26f), ScaledPower(46f));
                            CreateAshTriangle(
                                glyphCenter,
                                triangleRadius,
                                267,
                                Color.Lerp(innerColor, outerColor, 0.65f),
                                true
                            );
                            break;
                        }

                    default:
                        {
                            // 倒三角
                            float triangleRadius = Main.rand.NextFloat(ScaledPower(26f), ScaledPower(46f));
                            CreateAshTriangle(
                                glyphCenter,
                                triangleRadius,
                                267,
                                Color.Lerp(innerColor, outerColor, 0.65f),
                                false
                            );
                            break;
                        }
                }

                // 每个位点再补一点朝外散的小碎屑，让它不显得太死板
                int shardCount = Main.rand.Next(ScaledCount(4), ScaledCount(8) + 1);
                for (int j = 0; j < shardCount; j++)
                {
                    Dust shardDust = Dust.NewDustPerfect(glyphCenter + Main.rand.NextVector2Circular(ScaledPower(12f), ScaledPower(12f)), DustID.RainbowMk2);
                    shardDust.noGravity = true;
                    shardDust.scale = Main.rand.NextFloat(1.0f, 1.45f) * AshEffectIntensity;
                    shardDust.color = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());
                    shardDust.velocity =
                        direction * Main.rand.NextFloat(ScaledPower(0.8f), ScaledPower(2.2f)) +
                        direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(ScaledPower(-0.5f), ScaledPower(0.5f));
                }
            }
        }

        private void CreateAshCircleRing(Vector2 center, float radius, int count, int dustType, Color innerColor, Color outerColor, float outwardSpeed, float tangentialSpeed)
        {
            int effectiveCount = ScaledCount(count);
            for (int i = 0; i < effectiveCount; i++)
            {
                float angle = MathHelper.TwoPi * i / effectiveCount;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                // 圆环位置
                Vector2 spawnPos = center + outward * radius;

                // 让 Dust 有“向外略扩散 + 沿切线旋转”的观感
                Vector2 velocity = outward * Main.rand.NextFloat(ScaledPower(outwardSpeed * 0.8f), ScaledPower(outwardSpeed * 1.25f)) +
                                   tangent * Main.rand.NextFloat(ScaledPower(-tangentialSpeed * 0.45f), ScaledPower(tangentialSpeed * 0.45f));

                Dust ringDust = Dust.NewDustPerfect(spawnPos, dustType);
                ringDust.noGravity = true;
                ringDust.scale = Main.rand.NextFloat(1.15f, 1.85f) * AshEffectIntensity;
                ringDust.color = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());
                ringDust.velocity = velocity;
                ringDust.rotation = angle;
                ringDust.fadeIn = Main.rand.NextFloat(0.8f, 1.4f);
            }
        }

        private void CreateAshTriangle(Vector2 center, float radius, int dustType, Color dustColor, bool upright)
        {
            float baseAngle = upright ? -MathHelper.PiOver2 : MathHelper.PiOver2;
            Vector2[] points = new Vector2[3];

            for (int i = 0; i < 3; i++)
                points[i] = center + (baseAngle + MathHelper.TwoPi * i / 3f).ToRotationVector2() * radius;

            for (int i = 0; i < 3; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % 3];

                int lineCount = ScaledCount(28);
                for (int j = 0; j < lineCount; j++)
                {
                    float t = j / (float)Math.Max(1, lineCount - 1);
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    // 边线 Dust 轻微向外扩散，同时带一点旋转感
                    Vector2 outward = (pos - center).SafeNormalize(Vector2.Zero);
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    Dust lineDust = Dust.NewDustPerfect(pos, dustType);
                    lineDust.noGravity = true;
                    lineDust.scale = Main.rand.NextFloat(1.15f, 1.7f) * AshEffectIntensity;
                    lineDust.color = Color.Lerp(dustColor, Color.Black, Main.rand.NextFloat(0.35f));
                    lineDust.velocity = outward * Main.rand.NextFloat(ScaledPower(0.8f), ScaledPower(1.9f)) + tangent * Main.rand.NextFloat(ScaledPower(-0.45f), ScaledPower(0.45f));
                }
            }
        }


        private void CreateAshSmokeAndSkulls(Vector2 center)
        {
            // ===== 1. 先铺一层高密度黑烟圆环 =====
            // 小半径 = 10×16，大半径 = 20×16
            float innerRadius = ScaledPower(10f * 16f);
            float outerRadius = ScaledPower(20f * 16f);

            // 用多圈 + 多点的方式把圆环铺密，同时让它向外旋转着散掉
            int ringLayers = ScaledCount(6);
            for (int layer = 0; layer < ringLayers; layer++)
            {
                float layerT = layer / (float)Math.Max(1, ringLayers - 1);
                float radius = MathHelper.Lerp(innerRadius, outerRadius, layerT);

                // 外圈点更多，保证不会稀
                int pointCount = ScaledCount((int)MathHelper.Lerp(36f, 64f, layerT));

                // 每一层整体随机转一点，避免太死板
                float layerRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int i = 0; i < pointCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / pointCount + layerRotation;
                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    // 位置轻微抖动，但整体仍然是圆环
                    Vector2 spawnPos = center
                        + outward * (radius + Main.rand.NextFloat(ScaledPower(-10f), ScaledPower(10f)))
                        + Main.rand.NextVector2Circular(ScaledPower(4f), ScaledPower(4f));

                    // 向外 + 切线速度，形成“往外旋转着消失”的感觉
                    Vector2 smokeVel =
                        outward * Main.rand.NextFloat(ScaledPower(1.2f), ScaledPower(2.8f)) +
                        tangent * Main.rand.NextFloat(ScaledPower(-0.8f), ScaledPower(0.8f));

                    float smokeScale = Main.rand.NextFloat(1.15f, 1.95f) * AshEffectIntensity;

                    Particle smoke = new SmallSmokeParticle(
                        spawnPos,
                        smokeVel,
                        Color.DimGray,
                        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                        smokeScale,
                        100
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

            // ===== 2. 保留原本的随机黑烟填充 =====
            for (int i = 0; i < ScaledCount(64); i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);

                // 中层范围，专门填满法阵与内爆之间的空隙
                Vector2 spawnPos = center + direction * Main.rand.NextFloat(ScaledPower(180f), ScaledPower(860f));

                // 也给这层一点旋转 outward 的感觉
                Vector2 smokeVel =
                    direction * Main.rand.NextFloat(ScaledPower(1.6f), ScaledPower(5.2f)) +
                    tangent * Main.rand.NextFloat(ScaledPower(-0.7f), ScaledPower(0.7f)) +
                    Main.rand.NextVector2Circular(ScaledPower(0.8f), ScaledPower(0.8f));

                float smokeScale = Main.rand.NextFloat(1.3f, 2.5f) * AshEffectIntensity;

                Particle smoke = new SmallSmokeParticle(
                    spawnPos,
                    smokeVel,
                    Color.DimGray,
                    Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                    smokeScale,
                    100
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private void CreateAshOuterSparks(Vector2 center)
        {
            // ===== 最外层细火花：负责把整体观感撑到超大直径 =====
            // 这里用更细、更远、更稀碎的 Dust 当 spark 层来铺外圈
            int outerSparkCount = ScaledCount(180);
            for (int i = 0; i < outerSparkCount; i++)
            {
                float angle = MathHelper.TwoPi * i / outerSparkCount + Main.rand.NextFloat(-0.02f, 0.02f);
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                // 外层起始半径很大，确保整体视觉接近直径 2700
                Vector2 spawnPos = center + outward * Main.rand.NextFloat(ScaledPower(980f), ScaledPower(1280f));

                Dust spark = Dust.NewDustPerfect(spawnPos, DustID.PortalBoltTrail);
                spark.noGravity = true;
                spark.scale = Main.rand.NextFloat(0.85f, 1.45f) * AshEffectIntensity;
                spark.color = Color.Lerp(new Color(255, 70, 30), new Color(255, 180, 80), Main.rand.NextFloat());

                // 小 outward + 强一点切线，形成“旋着散掉”的薄外圈
                spark.velocity = outward * Main.rand.NextFloat(ScaledPower(1.4f), ScaledPower(3.4f)) + tangent * Main.rand.NextFloat(ScaledPower(-0.9f), ScaledPower(0.9f));
            }

            // ===== 再补一层暗红碎片，让外圈不至于全是亮点 =====
            for (int i = 0; i < ScaledCount(90); i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 outward = angle.ToRotationVector2();

                Vector2 spawnPos = center + outward * Main.rand.NextFloat(ScaledPower(220f), ScaledPower(580f));
                Vector2 velocity = outward * Main.rand.NextFloat(ScaledPower(10f), ScaledPower(24f)) + outward.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(ScaledPower(-1.4f), ScaledPower(1.4f));

                float size = Main.rand.NextFloat(18f, 42f) * AshEffectIntensity;
                GruesomeMetaball.SpawnParticle(spawnPos, velocity, size);
            }
        }








    }
}
