using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Charge_Effects
    {
        internal static void SpawnChargingEffects(Projectile projectile, Player owner, Vector2 focusPoint, NPC target, float chargeCompletion, int timer)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = projectile.Center + forward * (46f * projectile.scale);
            float swing = (float)Math.Sin(timer * 0.18f) * MathHelper.Lerp(0.16f, 0.62f, chargeCompletion);
            float fanArc = MathHelper.Lerp(0.18f, 0.45f, chargeCompletion);
            Color blueColor = Color.Lerp(new Color(92, 204, 255), Color.White, 0.2f + chargeCompletion * 0.18f);
            string[] starTextures =
            {
                "CalamityLegendsComeBack/Texture/KsTexture/star_01",
                "CalamityLegendsComeBack/Texture/KsTexture/star_02",
                "CalamityLegendsComeBack/Texture/KsTexture/star_04",
                "CalamityLegendsComeBack/Texture/KsTexture/star_05",
                "CalamityLegendsComeBack/Texture/KsTexture/star_06",
                "CalamityLegendsComeBack/Texture/KsTexture/star_07",
                "CalamityLegendsComeBack/Texture/KsTexture/star_08",
                "CalamityLegendsComeBack/Texture/KsTexture/star_09"
            };

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 pointerDirection = forward.RotatedBy(swing + fanArc * 0.75f * side);
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        tip + right * side * Main.rand.NextFloat(1f, 3f),
                        pointerDirection * Main.rand.NextFloat(1.8f, 4.2f),
                        "CalamityLegendsComeBack/Texture/KsTexture/star_09",
                        false,
                        10,
                        0.2f + chargeCompletion * 0.04f,
                        blueColor * 0.95f,
                        Vector2.One,
                        glowCenter: true,
                        shrinkSpeed: 0.58f,
                        glowCenterScale: 0.86f,
                        glowOpacity: 0.58f));
            }

            for (int i = 0; i < 2; i++)
            {
                string randomStar = starTextures[Main.rand.Next(starTextures.Length)];
                Vector2 sprayDirection = forward.RotatedBy(swing + Main.rand.NextFloat(-fanArc, fanArc));
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        tip + right * Main.rand.NextFloat(-4f, 4f),
                        sprayDirection * Main.rand.NextFloat(1.4f, 3.8f),
                        randomStar,
                        false,
                        9,
                        Main.rand.NextFloat(0.16f, 0.22f) + chargeCompletion * 0.03f,
                        blueColor * Main.rand.NextFloat(0.78f, 1f),
                        Vector2.One,
                        glowCenter: true,
                        shrinkSpeed: 0.62f,
                        glowCenterScale: 0.82f,
                        glowOpacity: 0.52f));
            }

            if (Main.myPlayer == projectile.owner && timer % 2 == 0)
            {
                Vector2 spawnDirection = forward.RotatedByRandom(0.65f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    tip + spawnDirection * 160f,
                    spawnDirection * 0.2f,
                    ModContent.ProjectileType<BBSD_VirtualPROJ>(),
                    0,
                    0f,
                    projectile.owner,
                    projectile.whoAmI,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }

            if (timer % 6 == 0)
            {
                Vector2 searchAnchor = target?.Center ?? focusPoint;
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Vector2.Lerp(tip, searchAnchor, 0.2f + 0.08f * (float)Math.Sin(timer * 0.11f)),
                        Vector2.Zero,
                        false,
                        8,
                        0.32f + chargeCompletion * 0.08f,
                        new Color(108, 212, 255),
                        true,
                        false,
                        true));
            }
        }
    }
}
