using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.CoreOfCalamity
{

    /// <summary>
    /// 主弹爆炸时的短命伤害判定。视觉由主弹自身生成，这里保持不可见。
    /// </summary>
    /// <summary>
    /// 灾劫核心爆炸后的四色分裂弹。
    /// 每帧固定向左拐一度，经过短暂展开后再对最近目标追加追踪修正。
    /// </summary>
    internal sealed class CoreOfCalamitySplitOrb : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int ActivationDelay = 10;

        private static readonly Color[] Palette =
        {
            new(24, 62, 188),
            new(106, 218, 255),
            new(232, 56, 62),
            new(255, 194, 62)
        };

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private Color OrbColor => Palette[Utils.Clamp((int)Projectile.ai[0], 0, Palette.Length - 1)];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 48;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            Projectile.tileCollide = false;
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = 120f + Projectile.ai[0] * 90f;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation();
            Lighting.AddLight(Projectile.Center, OrbColor.ToVector3() * 0.58f);
            SpawnFlightEffects(direction);
            SpawnHyperiusFlightEffects(direction);
        }

        public override bool? CanDamage() => Timer >= ActivationDelay ? null : false;

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                SpawnDamageExplosion();

            Color secondary = Color.Lerp(OrbColor, Color.White, 0.6f);

            // 原有脉冲环
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                OrbColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.1f,
                0.54f,
                14));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                OrbColor,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.45f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.48f,
                0.05f,
                16));

            // Hyperius 风格：白色二次闪光脉冲
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                secondary,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.22f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.74f,
                0.035f,
                11));

            // Hyperius 风格：8方向十字状细长火花（主色+副色交替），以飞行方向为基准轴
            for (int i = 0; i < 8; i++)
            {
                Vector2 crossDir = (MathHelper.PiOver4 * i + Projectile.rotation).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    crossDir * Main.rand.NextFloat(2.8f, 7.5f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 19),
                    Main.rand.NextFloat(0.14f, 0.30f),
                    i % 2 == 0 ? OrbColor : secondary,
                    new Vector2(0.20f, 1.65f),
                    true,
                    true,
                    extraRotation: 0f,
                    shrinkSpeed: 0.52f,
                    glowOpacity: 0.90f));
            }

            // Hyperius 风格：放射状 SparkParticle 爆发
            //for (int i = 0; i < 10; i++)
            //{
            //    Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.5f, 12f);
            //    GeneralParticleHandler.SpawnParticle(new SparkParticle(
            //        Projectile.Center,
            //        vel,
            //        false,
            //        Main.rand.Next(12, 22),
            //        Main.rand.NextFloat(0.8f, 1.45f),
            //        Main.rand.NextBool(3) ? secondary : OrbColor));
            //}

            // Hyperius 彩虹尘：用调色盘四色轮番喷出（disco burst）
            for (int i = 0; i < 18; i++)
            {
                Color discoColor = Palette[i % Palette.Length];
                Dust discoDust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowMk2,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.8f, 8f),
                    90,
                    discoColor,
                    Main.rand.NextFloat(0.7f, 1.3f));
                discoDust.noGravity = true;
            }

            // 原有色调尘
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 5.4f),
                    70,
                    OrbColor,
                    Main.rand.NextFloat(0.7f, 1.3f));
                dust.noGravity = true;
            }
        }

        private void SpawnDamageExplosion()
        {
            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(Projectile.damage * 0.75),
                Projectile.knockBack,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(explosionIndex))
                return;

            Projectile explosion = Main.projectile[explosionIndex];
            int explosionSize = (int)(new BalanceSHPC().GetDefaultOrbExplosionSize() * 0.66f);
            explosion.Resize(explosionSize, explosionSize);
            explosion.Center = Projectile.Center;
            explosion.DamageType = DamageClass.Magic;
            explosion.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 position = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    Color.Lerp(OrbColor, Color.White, i * 0.35f) with { A = 0 } * 0.76f,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(0.21f, 0.15f) * (1f - i * 0.2f),
                        SpriteEffects.None);
            }

            for (int i = 0; i < 4; i++)
            {
                float pulse = 0.86f + MathF.Sin((Main.GlobalTimeWrappedHourly * 5.2f) + Projectile.identity + i) * 0.08f;
                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    Color.Lerp(OrbColor, Color.White, i * 0.18f) with { A = 0 } * (0.42f - i * 0.06f),
                    Projectile.rotation + MathHelper.PiOver2 * i,
                    bloom.Size() * 0.5f,
                    new Vector2(0.1f, 0.34f) * Projectile.scale * pulse,
                    SpriteEffects.None);
            }

            DrawHyperiusGlow(position);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = CoreOfCalamityEnergyOrb.BuildTrailPoints(Projectile);
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    (completion, _) => MathF.Sin((1f - completion) * MathHelper.PiOver2) * 18f,
                    (completion, _) =>
                    {
                        Color color = Color.Lerp(OrbColor, Color.Transparent, completion);
                        color.A = 0;
                        return color;
                    },
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);
        }

        private void SpawnFlightEffects(Vector2 direction)
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.TintableDustLighted,
                -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.6f, 2.4f),
                70,
                OrbColor,
                Main.rand.NextFloat(0.64f, 1.08f));
            dust.noGravity = true;
        }

        private void SpawnHyperiusFlightEffects(Vector2 direction)
        {
            if (Main.dedServ || (Projectile.numUpdates != 0 && Main.rand.NextBool(3)))
                return;

            float phase = Projectile.localAI[1] + Timer * 0.09f;
            Color secondary = Color.Lerp(OrbColor, Color.White, 0.42f);
            float wave = 0.12f + 0.08f * MathF.Sin(phase * 1.7f);

            // 原有侧翼波动火花（速度严格沿飞行方向，偏移由位置体现）
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 perp = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 orbPos = Projectile.Center + perp * (side * (4f + MathF.Abs(MathF.Sin(phase)) * 5f)) - direction * 15f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    orbPos,
                    -direction * Main.rand.NextFloat(0.35f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.12f, 0.23f),
                    Main.rand.NextBool() ? OrbColor : secondary,
                    new Vector2(0.34f, 1.15f),
                    true,
                    true,
                    extraRotation: 0f,
                    shrinkSpeed: 0.48f,
                    glowOpacity: 0.78f));
            }

            // 原有彩虹尘拖尾
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f),
                    DustID.RainbowMk2,
                    -direction.RotatedByRandom(0.26f) * Main.rand.NextFloat(0.45f, 1.8f),
                    90,
                    secondary,
                    Main.rand.NextFloat(0.62f, 1.05f));
                dust.noGravity = true;
            }

            // Hyperius 风格：垂直方向随机漂移的柔性光球
            if (Main.rand.NextBool(4))
            {
                Vector2 perp = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-14f, 14f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(10f, 28f) + perp,
                    -direction * Main.rand.NextFloat(0.3f, 1.0f),
                    Main.rand.NextFloat(0.14f, 0.27f),
                    Color.Lerp(OrbColor, Color.White, Main.rand.NextFloat(0.3f, 0.7f)),
                    Main.rand.Next(7, 13)));
            }

            // 每帧冲向前方的两条平行线
            {
                Vector2 perp = direction.RotatedBy(3f * MathHelper.PiOver4);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 spawnPos = Projectile.Center + perp * (side * 5f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        spawnPos,
                        direction * Main.rand.NextFloat(3f, 6f),
                        "CalamityMod/Particles/GlowSpark",
                        false,
                        Main.rand.Next(4, 8),
                        Main.rand.NextFloat(0.05f, 0.11f),
                        side == -1 ? OrbColor : secondary,
                        new Vector2(0.09f, 0.82f),
                        true,
                        true,
                        extraRotation: 0f,
                        shrinkSpeed: 0.40f,
                        glowOpacity: 0.92f));
                }
            }
        }

        private void DrawHyperiusGlow(Vector2 drawPosition)
        {
            Texture2D glowSpark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Color color = OrbColor with { A = 0 };
            Color white = Color.White with { A = 0 };
            Color secondary = Color.Lerp(OrbColor, Color.White, 0.65f) with { A = 0 };
            float pulse = 0.92f + 0.08f * MathF.Sin(Timer * 0.24f + Projectile.identity);

            // 主色4射线十字（正轴），顺时针缓慢旋转
            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.025f;
                Main.EntitySpriteDraw(
                    glowSpark,
                    drawPosition,
                    null,
                    Color.Lerp(color, white, i * 0.16f) * (0.42f - i * 0.045f),
                    rotation,
                    glowSpark.Size() * 0.5f,
                    new Vector2(0.12f + i * 0.014f, 0.82f + i * 0.12f) * Projectile.scale * pulse,
                    SpriteEffects.None,
                    0f);
            }

            // Hyperius 风格：副色4射线（45°偏转），逆时针慢转，构成8尖星
            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4 + MathHelper.PiOver2 * i - Timer * 0.018f;
                Main.EntitySpriteDraw(
                    glowSpark,
                    drawPosition,
                    null,
                    Color.Lerp(secondary, white, i * 0.10f) * (0.28f - i * 0.030f),
                    rotation,
                    glowSpark.Size() * 0.5f,
                    new Vector2(0.08f + i * 0.010f, 0.56f + i * 0.08f) * Projectile.scale * pulse,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}
