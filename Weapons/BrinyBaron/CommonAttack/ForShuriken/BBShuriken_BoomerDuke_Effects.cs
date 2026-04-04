using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    internal static class BBShuriken_BoomerDuke_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 22f + projectile.identity * 0.63f;
            float sine = (float)Math.Sin(phase);
            Vector2 offset = right * sine * (projectile.width * 0.18f + projectile.width * 0.12f);
            Vector2 sparkVelocity = -forward * MathHelper.Lerp(1.6f, 3f, 0.5f + 0.5f * sine);

            GeneralParticleHandler.SpawnParticle(
                new CustomSpark(
                    projectile.Center + offset,
                    sparkVelocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    10,
                    0.22f * sizeScale,
                    new Color(90, 210, 255),
                    new Vector2(0.5f, 1f),
                    true,
                    false));

            GeneralParticleHandler.SpawnParticle(
                new CustomSpark(
                    projectile.Center - offset,
                    sparkVelocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    10,
                    0.22f * sizeScale,
                    new Color(140, 235, 255),
                    new Vector2(0.5f, 1f),
                    true,
                    false));
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale)
        {
            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        target.Center,
                        hitForward.RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 7f),
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        12,
                        0.28f * sizeScale,
                        i % 2 == 0 ? new Color(95, 210, 255) : Color.Cyan,
                        new Vector2(0.55f, 1.15f),
                        true,
                        false));
            }
        }

        public static void DrawBladeDisc(Projectile projectile)
        {
            Texture2D smearRound = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D smearHalf = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Texture2D projectileTexture = Terraria.GameContent.TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float baseDrawScale = projectile.width / (float)projectileTexture.Width;
            float discScale = baseDrawScale * 1.5f;
            float flash = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f + projectile.identity * 0.9f);

            Main.EntitySpriteDraw(
                smearHalf,
                drawPos,
                null,
                Color.Lerp(Color.DeepSkyBlue, Color.Cyan, flash) with { A = 0 } * 0.42f,
                projectile.rotation * 1.35f,
                smearHalf.Size() * 0.5f,
                discScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                smearRound,
                drawPos,
                null,
                Color.Lerp(Color.LightSeaGreen, Color.CornflowerBlue, flash) with { A = 0 } * 0.42f,
                projectile.rotation * 0.85f,
                smearRound.Size() * 0.5f,
                discScale,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * projectile.width * 0.08f;
                Color auraColor = Color.Lerp(Color.Cyan, Color.DeepSkyBlue, i / 6f) * 0.2f;
                Main.EntitySpriteDraw(projectileTexture, drawPos + offset, null, auraColor, projectile.rotation, origin, discScale, SpriteEffects.None, 0);
            }
        }
    }
}
