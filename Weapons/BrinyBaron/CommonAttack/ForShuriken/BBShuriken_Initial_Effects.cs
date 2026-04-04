using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    internal static class BBShuriken_Initial_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            if (!Main.rand.NextBool(3))
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 10f + projectile.identity * 0.55f;
            float backDistance = projectile.width * Main.rand.NextFloat(0.22f, 0.38f);
            Vector2 spawnPos = projectile.Center - forward * backDistance + right * (float)Math.Sin(phase) * projectile.width * 0.1f;
            Vector2 dustVelocity = -forward * Main.rand.NextFloat(0.9f, 2.2f) + right * (float)Math.Cos(phase) * Main.rand.NextFloat(0.2f, 0.95f);

            Dust water = Dust.NewDustPerfect(
                spawnPos,
                DustID.Water,
                dustVelocity,
                100,
                new Color(70, 180, 255),
                Main.rand.NextFloat(0.8f, 1.1f) * sizeScale);
            water.noGravity = true;
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale, int highestUnlockedStage)
        {
            int hitBurstCount = 8 + highestUnlockedStage * 2;
            Vector2 hitRight = hitForward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < hitBurstCount; i++)
            {
                float t = i / (float)Math.Max(1, hitBurstCount - 1);
                float spread = MathHelper.Lerp(-0.55f, 0.55f, t);
                Vector2 burstVelocity =
                    hitForward.RotatedBy(spread) * Main.rand.NextFloat(3.4f, 7.8f) +
                    hitRight * spread * 2f;

                Dust water = Dust.NewDustPerfect(target.Center, DustID.Water, burstVelocity);
                water.noGravity = true;
                water.color = new Color(70, 180, 255);
                water.scale = Main.rand.NextFloat(0.85f, 1.3f) * sizeScale;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, burstVelocity * 0.72f);
                    frost.noGravity = true;
                    frost.color = new Color(210, 248, 255);
                    frost.scale = Main.rand.NextFloat(0.78f, 1.15f) * sizeScale;
                }
            }
        }

        public static void SpawnStickyAmbient(Projectile projectile, NPC target, float sizeScale, int highestUnlockedStage)
        {
            int ambientCount = 1 + Math.Min(highestUnlockedStage, 1);
            for (int i = 0; i < ambientCount; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(3.2f, 3.2f) + target.velocity * 0.15f;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, dustVelocity);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.85f, 1.18f) * sizeScale;

                if (Main.rand.NextBool())
                {
                    Dust water = Dust.NewDustPerfect(projectile.Center, DustID.Water, dustVelocity * 0.8f);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(0.92f, 1.22f) * sizeScale;
                }
            }
        }

        public static void SpawnStickySliceBurst(Projectile projectile, float sizeScale, int highestUnlockedStage)
        {
            int burstCount = 4 + highestUnlockedStage;
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 6f);

                Dust gem = Dust.NewDustPerfect(projectile.Center, DustID.GemSapphire, burstVelocity);
                gem.noGravity = true;
                gem.scale = Main.rand.NextFloat(0.9f, 1.25f) * sizeScale;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, burstVelocity * 0.7f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.82f, 1.12f) * sizeScale;
            }
        }

        public static void SpawnDeathBurst(Projectile projectile, float sizeScale)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust water = Dust.NewDustPerfect(projectile.Center, DustID.Water, burstVelocity);
                water.noGravity = true;
                water.scale = Main.rand.NextFloat(1.05f, 1.45f) * sizeScale;

                Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, burstVelocity * 0.85f);
                frost.noGravity = true;
                frost.scale = Main.rand.NextFloat(0.9f, 1.2f) * sizeScale;
            }
        }

        public static void DrawOutlineAndBody(Projectile projectile, Texture2D projectileTexture, Color lightColor)
        {
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float drawScale = projectile.width / (float)projectileTexture.Width;
            float outlineRadius = Math.Max(2f, projectile.width * 0.05f);
            Color outlineColor = new Color(90, 210, 255, 0) * 0.32f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * outlineRadius;
                Main.EntitySpriteDraw(
                    projectileTexture,
                    drawPos + offset,
                    null,
                    outlineColor,
                    projectile.rotation,
                    origin,
                    drawScale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                projectileTexture,
                drawPos,
                null,
                projectile.GetAlpha(lightColor),
                projectile.rotation,
                origin,
                drawScale,
                SpriteEffects.None,
                0);
        }
    }
}
