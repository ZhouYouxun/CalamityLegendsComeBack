using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public static class LeonidVisualUtils
    {
        public static readonly Color StratusBlue = new(82, 216, 255);
        public static readonly Color DeepStratusBlue = new(18, 42, 168);
        public static readonly Color MoonViolet = new(172, 150, 255);
        public static readonly Color MoonWhite = new(224, 240, 255);
        public static readonly Color StarGold = new(255, 226, 104);
        public static readonly Color NightSkyBlue = new(12, 38, 128);

        public static Color GetMeteorColor(float phaseOffset = 0f)
        {
            float phase = Main.GlobalTimeWrappedHourly * 2.2f + phaseOffset;
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(phase);
            Color skyBody = Color.Lerp(StratusBlue, MoonViolet, pulse * 0.7f);
            Color brightBody = Color.Lerp(skyBody, MoonWhite, 0.18f + 0.12f * pulse);
            return Color.Lerp(brightBody, StarGold, 0.05f + 0.04f * (1f - pulse));
        }

        public static Color GetCelestialColor(float progress = 0f, float phaseOffset = 0f)
        {
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3f + phaseOffset);
            Color baseColor = Color.Lerp(DeepStratusBlue, StratusBlue, 0.65f + 0.2f * pulse);
            Color violetEdge = Color.Lerp(baseColor, MoonViolet, MathHelper.Clamp(progress, 0f, 1f) * 0.58f);
            return Color.Lerp(violetEdge, MoonWhite, 0.12f + 0.1f * pulse);
        }

        public static Color GetReadyGold(float phaseOffset = 0f)
        {
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 6f + phaseOffset);
            return Color.Lerp(StarGold, MoonWhite, 0.24f + 0.2f * pulse);
        }

        public static void SpawnDustBurst(Vector2 center, Color color, int count, float speed, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(speed, speed),
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.35f)),
                    Main.rand.NextFloat(scale * 0.75f, scale * 1.25f));
                dust.noGravity = true;
            }
        }

        public static void SpawnStratusDust(Vector2 center, Vector2 direction, float scale = 1f, int alpha = 100)
        {
            Color color = Color.Lerp(StratusBlue, MoonWhite, Main.rand.NextFloat(0.28f));
            Vector2 velocity = -direction.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.15f, 0.75f) + Main.rand.NextVector2Circular(0.75f, 0.75f);
            Dust dust = Dust.NewDustPerfect(
                center,
                Main.rand.NextBool(3) ? DustID.Electric : DustID.TintableDustLighted,
                velocity,
                alpha,
                color,
                Main.rand.NextFloat(0.65f, 1.15f) * scale);
            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.08f, 0.26f);
        }

        public static void DrawBloom(Vector2 drawPosition, Color color, float scale, float rotation = 0f)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            color.A = 0;
            Main.EntitySpriteDraw(
                bloom,
                drawPosition - Main.screenPosition,
                null,
                color,
                rotation,
                bloom.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        public static void DrawCelestialHead(Vector2 worldPosition, Color color, float opacity, float scale, float rotation = 0f)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            Vector2 drawPosition = worldPosition - Main.screenPosition;

            color.A = 0;
            Color white = MoonWhite * opacity;
            white.A = 0;

            Main.EntitySpriteDraw(bloom, drawPosition, null, color * (0.42f * opacity), 0f, bloom.Size() * 0.5f, 0.16f * scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPosition, null, color * (0.52f * opacity), -rotation * 0.7f, ring.Size() * 0.5f, 0.115f * scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(sparkle, drawPosition, null, white * 0.44f, rotation, sparkle.Size() * 0.5f, 0.36f * scale, SpriteEffects.None, 0f);
        }

        public static void DrawGlowBlade(Vector2 worldPosition, Vector2 direction, Color color, float opacity, float lengthScale, float widthScale)
        {
            Texture2D blade = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowBlade").Value;
            Vector2 drawPosition = worldPosition - Main.screenPosition;
            Vector2 origin = new(blade.Width * 0.5f, blade.Height);
            color.A = 0;

            Main.EntitySpriteDraw(
                blade,
                drawPosition,
                null,
                color * opacity,
                direction.SafeNormalize(Vector2.UnitY).ToRotation() + MathHelper.PiOver2,
                origin,
                new Vector2(widthScale, lengthScale),
                SpriteEffects.None,
                0f);
        }

        public static void DrawSparkle(Vector2 worldPosition, Color color, float opacity, float scale, float rotation)
        {
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            color.A = 0;
            Main.EntitySpriteDraw(
                sparkle,
                worldPosition - Main.screenPosition,
                null,
                color * opacity,
                rotation,
                sparkle.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        public static void BeginAdditiveSpriteBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void BeginAlphaBlendSpriteBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void SpawnBloomBurst(Vector2 center, Color color, float scale, int lifetime)
        {
            if (Main.dedServ)
                return;

            color.A = 0;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, color, scale, lifetime));
        }

        public static void SpawnCelestialPulse(Vector2 center, Vector2 direction, Color color, float scale, int lifetime = 20)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitY);
            color.A = 0;
            GeneralParticleHandler.SpawnParticle(new BloomParticle(center, Vector2.Zero, color, 0.24f * scale, 0.36f * scale, 2, false));
            GeneralParticleHandler.SpawnParticle(new SparkParticle(center, direction * 0.01f, false, lifetime, 0.82f * scale, Color.Lerp(color, MoonWhite, 0.35f)));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, direction * 0.55f, color, new Vector2(0.72f, 2.1f) * scale, direction.ToRotation(), 0.16f, 0.026f, lifetime));
        }
    }
}
