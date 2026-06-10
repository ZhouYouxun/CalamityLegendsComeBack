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
            float textureScale = sizeFactor * 1.3f * pulse;

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

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                projectile.Center - forward * 4f,
                projectile.velocity * 0.025f,
                false,
                3,
                0.00465f * MathHelper.Lerp(1f, 1.75f, sizeFactor),
                color,
                new Vector2(0.3f, 0.9f) * MathHelper.Lerp(0.95f, 1.25f, sizeFactor),
                false,
                false));

            if ((Main.GameUpdateCount + projectile.identity) % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center - forward * (8f + 4f * sizeFactor),
                    projectile.velocity * 0.018f,
                    false,
                    Main.rand.Next(4, 7),
                    Main.rand.NextFloat(0.0045f, 0.0075f) * MathHelper.Lerp(0.9f, 1.25f, sizeFactor),
                    Color.Lerp(color, Color.White, 0.24f),
                    new Vector2(0.24f, 0.72f),
                    false,
                    false));
            }

            if (Main.rand.NextBool(2))
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

        public static void SpawnBurst(Vector2 center, Vector2 forward, float scale, float countMultiplier = 1f, float particleScaleMultiplier = 1f)
        {
            int particleCount = (int)System.MathF.Round(14f * countMultiplier);
            for (int i = 0; i < particleCount; i++)
            {
                Color color = RandomPearlColor();
                Vector2 velocity = (forward.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(1.2f, 5.2f)) + Main.rand.NextVector2Circular(0.8f, 0.8f);
                SpawnPearlParticle(center + Main.rand.NextVector2Circular(8f, 8f) * scale, velocity, Main.rand.NextFloat(0.28f, 0.58f) * scale * particleScaleMultiplier, Main.rand.Next(22, 42));

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        center + Main.rand.NextVector2Circular(6f, 6f) * scale,
                        velocity * Main.rand.NextFloat(0.65f, 1.25f),
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.012f, 0.026f) * scale * particleScaleMultiplier,
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
