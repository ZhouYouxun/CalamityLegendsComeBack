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
            float spin = timer * 0.22f;
            float helixRadius = MathHelper.Lerp(10f, 30f, chargeCompletion);
            float helixLength = MathHelper.Lerp(18f, 68f, chargeCompletion);
            Color blueColor = Color.Lerp(new Color(88, 210, 255), Color.White, 0.18f + chargeCompletion * 0.22f);
            Color accentColor = Color.Lerp(new Color(84, 168, 255), new Color(220, 252, 255), 0.55f);
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
                for (int step = 0; step < 3; step++)
                {
                    float t = step / 2f;
                    float localSpin = spin + side * (0.55f + t * 1.2f);
                    float localRadius = MathHelper.Lerp(helixRadius, helixRadius * 0.38f, t);
                    Vector2 helixOffset =
                        -forward * (8f + helixLength * t) +
                        right * side * (float)Math.Sin(localSpin) * localRadius;
                    Vector2 helixPos = tip + helixOffset;
                    Vector2 tangentVelocity =
                        -forward * MathHelper.Lerp(0.4f, 0.95f, t) +
                        right * side * (float)Math.Cos(localSpin) * MathHelper.Lerp(0.18f, 0.5f, chargeCompletion);
                    string randomStar = starTextures[(timer + step + (side > 0 ? 2 : 0)) % starTextures.Length];

                    GeneralParticleHandler.SpawnParticle(
                        new CustomSpark(
                            helixPos,
                            tangentVelocity,
                            randomStar,
                            false,
                            10,
                            MathHelper.Lerp(0.12f, 0.2f, 1f - t) + chargeCompletion * 0.04f,
                            Color.Lerp(blueColor, accentColor, t * 0.55f),
                            new Vector2(0.9f, 0.9f),
                            glowCenter: true,
                            shrinkSpeed: 0.58f,
                            glowCenterScale: 0.8f,
                            glowOpacity: 0.54f));

                    GeneralParticleHandler.SpawnParticle(
                        new GlowOrbParticle(
                            helixPos,
                            tangentVelocity * 0.28f,
                            false,
                            8,
                            MathHelper.Lerp(0.22f, 0.12f, t) + chargeCompletion * 0.04f,
                            Color.Lerp(blueColor, Color.White, 0.28f),
                            true,
                            false,
                            true));
                }
            }

            if (timer % 5 == 0)
            {
                float pulseStretch = MathHelper.Lerp(1.15f, 2.35f, chargeCompletion);
                GeneralParticleHandler.SpawnParticle(
                    new DirectionalPulseRing(
                        tip - forward * MathHelper.Lerp(6f, 18f, chargeCompletion),
                        -forward * 0.55f,
                        new Color(94, 214, 255),
                        new Vector2(0.72f, pulseStretch),
                        forward.ToRotation() - MathHelper.PiOver2,
                        0.14f,
                        0.02f,
                        12));
            }

            if (Main.myPlayer == projectile.owner && timer % 4 == 0)
            {
                float spawnAngle = timer * 0.47f + Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 spawnDirection = spawnAngle.ToRotationVector2();
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    owner.MountedCenter + spawnDirection * BBSD_VirtualPROJ.SpawnRingRadius,
                    Vector2.Zero,
                    ModContent.ProjectileType<BBSD_VirtualPROJ>(),
                    0,
                    0f,
                    projectile.owner,
                    projectile.whoAmI,
                    spawnAngle);
            }

            if (timer % 6 == 0)
            {
                Vector2 searchAnchor = target?.Center ?? focusPoint;
                float beamSample = 0.18f + 0.1f * (0.5f + 0.5f * (float)Math.Sin(timer * 0.12f));
                Vector2 samplePos = Vector2.Lerp(tip, searchAnchor, beamSample) + right * (float)Math.Sin(spin * 0.8f) * MathHelper.Lerp(3f, 11f, chargeCompletion);
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        samplePos,
                        Vector2.Zero,
                        false,
                        9,
                        0.24f + chargeCompletion * 0.08f,
                        accentColor,
                        true,
                        false,
                        true));
            }
        }
    }
}
