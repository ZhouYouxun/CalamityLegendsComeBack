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
    internal sealed class AzureThunderLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";

        public override string Texture =>
            "CalamityLegendsComeBack/Texture/KsTexture/light_03";

        private int timer;

        private const float MaxHomingSpeed = 25.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 54;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
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

            Projectile.rotation =
                Projectile.velocity.ToRotation();

            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.2f, 0.8f, 1f) * 1.2f);

            NPC target =
                AzureThunderPlayer.FindNearestTarget(
                    Projectile.Center,
                    850f);

            if (target != null)
            {
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
                Projectile.velocity *= 1.012f;
            }

            // 青雷粒子
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
            target.AddBuff(BuffID.Electrified, 180);

            AzureThunderAccessoryPlayer
                .ApplyAzureThunderAccessoryOnHit(
                    Projectile,
                    target);

            AzureThunderSounds.PlayOrbImpact(target.Center);

            SpawnDisappearanceEffects(target.Center);
        }

        // Shader颜色控制
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

        // Shader宽度控制
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

        // Shader偏移
        internal Vector2 OffsetFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            return Projectile.Size * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom =
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/BloomCirclePinpoint")
                .Value;

            Vector2 drawPosition =
                Projectile.Center -
                Main.screenPosition;

            // 核心闪烁
            float pulse =
                1f +
                (float)Math.Sin(
                    Main.GlobalTimeWrappedHourly * 12f +
                    Projectile.identity)
                * 0.08f;

            Main.spriteBatch.EnterShaderRegion(
                BlendState.Additive);

            // 外层青雷光
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(90, 255, 255, 0) * 0.85f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.9f * pulse,
                SpriteEffects.None);

            // 白色核心
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

            // SylvRay 同款 Shader 拖尾
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

        // 雷暴命中特效
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