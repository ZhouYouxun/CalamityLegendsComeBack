using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode.PearlShard
{
    internal static class PearlShardVisuals
    {
        private const string PearlTexturePath = "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PearlShard/PearlShardParticle";
        private const string PearlGlowTexturePath = "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PearlShard/PearlShardParticleGlow";

        public static Color RandomPearlColor()
        {
            return Main.rand.Next(3) switch
            {
                0 => Color.LightPink,
                1 => Color.LightBlue,
                _ => Color.Khaki
            };
        }

        public static void DrawPearl(Projectile projectile, float sizeFactor)
        {
            Texture2D pearl = ModContent.Request<Texture2D>(PearlTexturePath).Value;
            Texture2D glow = ModContent.Request<Texture2D>(PearlGlowTexturePath).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Color pearlColor = Color.Lerp(Color.White, new Color(255, 198, 226), 0.22f) * projectile.Opacity;
            Color haloColor = new Color(255, 170, 220, 0) * (0.26f * projectile.Opacity);
            float pulse = 0.94f + 0.06f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 9f + projectile.identity);
            float textureScale = sizeFactor * pulse;

            for (int i = 1; i < projectile.oldPos.Length; i++)
            {
                Vector2 oldCenter = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)projectile.oldPos.Length;
                Main.EntitySpriteDraw(glow, oldCenter, null, haloColor * (0.38f * completion), projectile.rotation, glow.Size() * 0.5f, textureScale * (0.8f + completion * 0.2f), SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, haloColor * 0.8f, 0f, bloom.Size() * 0.5f, 0.18f * sizeFactor * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, drawPosition, null, new Color(255, 228, 180, 0) * (0.22f * projectile.Opacity), projectile.rotation * 0.55f, ring.Size() * 0.5f, 0.13f * sizeFactor, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(reticle, drawPosition, null, new Color(150, 220, 255, 0) * (0.18f * projectile.Opacity), -projectile.rotation * 0.42f, reticle.Size() * 0.5f, 0.11f * sizeFactor, SpriteEffects.FlipHorizontally, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, new Color(255, 198, 226, 0) * (0.7f * projectile.Opacity), projectile.rotation, glow.Size() * 0.5f, textureScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(pearl, drawPosition, null, pearlColor, projectile.rotation, pearl.Size() * 0.5f, textureScale, SpriteEffects.None, 0f);
        }

        public static void SpawnPearlParticle(Vector2 position, Vector2 velocity, float scale, int lifetime)
        {
            GeneralParticleHandler.SpawnParticle(new PearlParticle(position, velocity, false, lifetime, scale, RandomPearlColor(), 0.95f, Main.rand.NextFloat(-0.25f, 0.25f), true));
        }

        public static void SpawnPearlGodTrail(Projectile projectile, float sizeFactor)
        {
            if (Main.dedServ || projectile.velocity.LengthSquared() < 0.01f)
                return;

            Color color = PearlGodColor(projectile.identity);
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float helix = (float)System.Math.Sin(Main.GameUpdateCount * 0.34f + projectile.identity * 0.41f);

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                projectile.Center + projectile.velocity * 1.5f,
                -projectile.velocity * 0.05f,
                false,
                3,
                0.0093f * MathHelper.Lerp(1f, 1.75f, sizeFactor),
                color,
                new Vector2(0.6f, 1.8f) * MathHelper.Lerp(0.95f, 1.25f, sizeFactor),
                false,
                false));

            if ((Main.GameUpdateCount + projectile.identity) % 2 == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 position = projectile.Center + normal * helix * side * (5f + 5f * sizeFactor) - forward * 4f;
                    Vector2 velocity = -projectile.velocity * Main.rand.NextFloat(0.035f, 0.12f) + normal * side * Main.rand.NextFloat(0.08f, 0.26f);
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position,
                        velocity,
                        false,
                        Main.rand.Next(5, 9),
                        Main.rand.NextFloat(0.011f, 0.024f) * MathHelper.Lerp(0.9f, 1.45f, sizeFactor),
                        Color.Lerp(color, Color.White, 0.24f),
                        new Vector2(0.45f, 1.55f),
                        false,
                        false));
                }
            }

            if (Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new PearlParticle(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f) * MathHelper.Lerp(0.8f, 1.5f, sizeFactor),
                    -projectile.velocity * Main.rand.NextFloat(0.05f, 0.3f),
                    false,
                    Main.rand.Next(15, 21),
                    Main.rand.NextFloat(0.4f, 0.55f) * MathHelper.Lerp(0.85f, 1.2f, sizeFactor),
                    color,
                    0.9f,
                    Main.rand.NextFloat(1f, -1f),
                    true));
            }
        }

        private static Color PearlGodColor(int identity)
        {
            int colorIndex = (identity % 3 + 3) % 3;
            return colorIndex switch
            {
                0 => Color.LightBlue,
                1 => Color.LightPink,
                _ => Color.Khaki
            };
        }

        public static void SpawnBurst(Vector2 center, Vector2 forward, float scale)
        {
            for (int i = 0; i < 14; i++)
            {
                Color color = RandomPearlColor();
                Vector2 velocity = (forward.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(1.2f, 5.2f)) + Main.rand.NextVector2Circular(0.8f, 0.8f);
                SpawnPearlParticle(center + Main.rand.NextVector2Circular(8f, 8f) * scale, velocity, Main.rand.NextFloat(0.28f, 0.58f) * scale, Main.rand.Next(22, 42));

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        center + Main.rand.NextVector2Circular(6f, 6f) * scale,
                        velocity * Main.rand.NextFloat(0.65f, 1.25f),
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.012f, 0.026f) * scale,
                        color,
                        new Vector2(0.55f, 1.65f),
                        false,
                        false));
                }
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, new Color(255, 196, 224), "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-0.3f, 0.3f), 0.04f * scale, 0.22f * scale, 18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, new Color(170, 220, 255), "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-0.3f, 0.3f), 0.025f * scale, 0.32f * scale, 20));
        }
    }
}
