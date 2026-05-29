using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General
{
    // 左键第一段释放的发光体：高速追踪目标，并用 PrimitiveRenderer 绘制雷光拖尾。
    internal sealed class AzureThunderLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";

        public override string Texture =>
            "CalamityLegendsComeBack/Texture/KsTexture/light_03";

        private int timer;

        // 追踪期望速度上限，实际速度通过 Lerp 慢慢贴近。
        private const float MaxHomingSpeed = 51f;

        public override void SetStaticDefaults()
        {
            // 长拖尾缓存用于 Sylvestaff 风格 shader 轨迹。
            ProjectileID.Sets.TrailCacheLength[Type] = 54;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            // 光球是轻量多段魔法弹幕，最多穿透 3 个敌人。
            Projectile.width = 24;
            Projectile.height = 24;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.DamageType = DamageClass.Magic;

            Projectile.penetrate = 3;

            Projectile.timeLeft = 150;

            Projectile.extraUpdates = 1;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            timer++;

            // 贴图朝向跟随速度方向。
            Projectile.rotation =
                Projectile.velocity.ToRotation();

            // 常驻青色照明，强化发光体存在感。
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.2f, 0.8f, 1f) * 1.2f);

            NPC target =
                AzureThunderPlayer.FindNearestTarget(
                    Projectile.Center,
                    850f);

            if (target != null)
            {
                // 找到目标后缓慢转向最高 51 速度，避免瞬间折角太硬。
                Vector2 desiredVelocity =
                    (target.Center - Projectile.Center)
                    .SafeNormalize(
                        Projectile.velocity.SafeNormalize(Vector2.UnitX))
                    * MaxHomingSpeed;

                Projectile.velocity =
                    Vector2.Lerp(
                        Projectile.velocity,
                        desiredVelocity,
                        0.055f);
            }
            else
            {
                // 无目标时略微加速直飞，保持射出去的动势。
                Projectile.velocity *= 1.012f;
            }

            // 青雷粒子：核心周围的 GlowOrb 负责亮点。
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Projectile.Center +
                        Main.rand.NextVector2Circular(6f, 6f),

                        -Projectile.velocity *
                        Main.rand.NextFloat(0.04f, 0.12f),

                        false,

                        Main.rand.Next(8, 13),

                        Main.rand.NextFloat(0.22f, 0.38f),

                        Main.rand.NextBool()
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        true,
                        false,
                        true));
            }

            if (Main.rand.NextBool(3))
            {
                // LineParticle 向后喷射，补出细长电火花。
                Vector2 sparkVelocity =
                    -Projectile.velocity
                    .SafeNormalize(Vector2.UnitX)
                    .RotatedByRandom(0.35f)
                    * Main.rand.NextFloat(1.1f, 3.6f);

                GeneralParticleHandler.SpawnParticle(
                    new LineParticle(
                        Projectile.Center -
                        Projectile.velocity *
                        Main.rand.NextFloat(0.12f, 0.48f) +
                        Main.rand.NextVector2Circular(5f, 5f),

                        sparkVelocity,

                        false,

                        Main.rand.Next(10, 16),

                        Main.rand.NextFloat(0.45f, 0.75f),

                        Main.rand.NextBool(3)
                            ? Color.Cyan
                            : new Color(120, 220, 255)));
            }

            if (Main.rand.NextBool(4))
            {
                // 原版 RGB dust 作为第三层粒子，保证低端画质仍有反馈。
                Dust dust =
                    Dust.NewDustPerfect(
                        Projectile.Center -
                        Projectile.velocity *
                        Main.rand.NextFloat(0.2f, 0.7f) +
                        Main.rand.NextVector2Circular(7f, 7f),

                        DustID.FireworksRGB,

                        -Projectile.velocity *
                        Main.rand.NextFloat(0.015f, 0.055f),

                        0,

                        Main.rand.NextBool()
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        Main.rand.NextFloat(0.65f, 1f));

                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            // 命中附加电击、触发饰品联动，并播放短促电击音。
            target.AddBuff(BuffID.Electrified, 180);

            AzureThunderAccessoryPlayer
                .ApplyAzureThunderAccessoryOnHit(
                    Projectile,
                    target);

            AzureThunderSounds.PlayOrbImpact(target.Center);

            SpawnDisappearanceEffects(target.Center);
        }

        // Shader 颜色控制：拖尾从透明青白之间脉动。
        internal Color ColorFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            float opacity =
                MathF.Pow(
                    Utils.GetLerpValue(
                        1f,
                        0.64f,
                        completionRatio,
                        true),
                    3f);

            float colorInterpolant =
                MathF.Cos(
                    MathHelper.Pi *
                    completionRatio -
                    Main.GlobalTimeWrappedHourly * 7.2f)
                * 0.5f + 0.5f;

            Color cyan =
                new Color(70, 255, 255);

            Color blue =
                new Color(80, 170, 255);

            Color baseColor =
                CalamityUtils.MulticolorLerp(
                    colorInterpolant,
                    cyan,
                    Color.White,
                    blue,
                    Color.White);

            return baseColor * opacity;
        }

        // Shader 宽度控制：拖尾前端展开，后段用余弦轻微起伏。
        internal float WidthFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            float expansionCompletion =
                1f -
                MathF.Pow(
                    1f -
                    Utils.GetLerpValue(
                        0f,
                        0.3f,
                        completionRatio,
                        true),
                    2f);

            float undulation =
                MathF.Cos(
                    MathHelper.Pi *
                    completionRatio *
                    5f -
                    Main.GlobalTimeWrappedHourly * 23f)
                * 2.4f;

            float maxWidth =
                undulation + 26f;

            return MathHelper.Lerp(
                0f,
                Projectile.scale * maxWidth,
                expansionCompletion);
        }

        // Shader 偏移：让拖尾顶点围绕弹幕中心计算。
        internal Vector2 OffsetFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            return Projectile.Size * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // bloom 绘制光球核心，trail shader 绘制高速拖尾。
            Texture2D bloom =
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/BloomCirclePinpoint")
                .Value;

            Vector2 drawPosition =
                Projectile.Center -
                Main.screenPosition;

            // 核心闪烁：随时间和弹幕 identity 错开，避免多颗完全同步。
            float pulse =
                1f +
                (float)Math.Sin(
                    Main.GlobalTimeWrappedHourly * 12f +
                    Projectile.identity)
                * 0.08f;

            Main.spriteBatch.EnterShaderRegion(
                BlendState.Additive);

            // 外层青雷光。
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(90, 255, 255, 0) * 0.85f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.9f * pulse,
                SpriteEffects.None);

            // 白色核心。
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.55f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.42f,
                SpriteEffects.None);

            Main.spriteBatch.ExitShaderRegion();

            // SylvRay 同款 Shader 拖尾，使用旧位置缓存作为曲线输入。
            MiscShaderData rayShader =
                GameShaders.Misc[
                    "CalamityMod:SylvestaffProjectile"];

            rayShader.SetShaderTexture(
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,

                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    OffsetFunction,
                    pixelate: false,
                    shader: rayShader),

                32);

            return false;
        }

        // 雷暴命中特效：命中点喷射电弧、dust 和两层脉冲。
        private void SpawnDisappearanceEffects(
            Vector2 impactCenter)
        {
            float fxScale = 2.4f;

            for (int i = 0; i < 18; i++)
            {
                Particle bolt =
                    new BoltParticle(
                        impactCenter,

                        (new Vector2(4f, 4f) * fxScale)
                        .RotatedByRandom(100f)
                        * Main.rand.NextFloat(0.3f, 1.9f),

                        true,

                        13,

                        Main.rand.NextFloat(0.1f, 0.16f)
                        * fxScale,

                        Main.rand.NextBool(4)
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        new Vector2(1.8f, 0.8f),

                        true,
                        true,
                        false,
                        0.7f);

                GeneralParticleHandler.SpawnParticle(bolt);

                Dust dust =
                    Dust.NewDustPerfect(
                        impactCenter,

                        226,

                        (new Vector2(5f, 5f) * fxScale)
                        .RotatedByRandom(100f)
                        * Main.rand.NextFloat(0.5f, 1f),

                        0,

                        default,

                        Main.rand.NextFloat(0.4f, 0.55f)
                        * fxScale);

                dust.noGravity = true;

                dust.color =
                    Main.rand.NextBool(4)
                        ? Color.Cyan
                        : new Color(120, 220, 255);
            }

            Particle pulse =
                new CustomPulse(
                    impactCenter,
                    Vector2.Zero,
                    Color.Cyan,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                    Vector2.One,
                    0f,
                    0f,
                    0.12f,
                    10);

            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 2; i++)
            {
                Particle orb =
                    new CustomPulse(
                        impactCenter,
                        Vector2.Zero,
                        new Color(90, 255, 255),
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-10f, 10f),
                        1.6f,
                        0.42f,
                        14);

                GeneralParticleHandler.SpawnParticle(orb);

                Particle whiteOrb =
                    new CustomPulse(
                        impactCenter,
                        Vector2.Zero,
                        Color.White,
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-10f, 10f),
                        1f,
                        0.2f,
                        14);

                GeneralParticleHandler.SpawnParticle(whiteOrb);
            }
        }
    }
}
