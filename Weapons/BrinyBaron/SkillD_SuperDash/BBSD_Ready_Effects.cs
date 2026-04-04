using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the single, explosive visual burst that fires the instant charge completes.
    // The branch layout is deliberately symmetric: a dominant center lance with two weaker +/-45 degree side lances.
    internal static class BBSD_Ready_Effects
    {
        internal static void SpawnChargeReadyBurst(Projectile projectile, Vector2 weaponTip)
        {
            if (Main.dedServ)
                return;

            Vector2 visualForward = projectile.rotation.ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);

            SpawnForwardJetBurst(projectile, weaponTip, 2.15f);

            for (int i = 0; i < 28; i++)
            {
                float t = i / 27f;
                float spread = MathHelper.Lerp(-0.6f, 0.6f, t);
                Vector2 burstVelocity =
                    visualForward.RotatedBy(spread * 0.75f) * Main.rand.NextFloat(8f, 18f) +
                    visualRight * spread * 4.5f;

                Dust water = Dust.NewDustPerfect(
                    weaponTip + visualRight * spread * 8f,
                    DustID.Water,
                    burstVelocity,
                    100,
                    new Color(90, 205, 255),
                    Main.rand.NextFloat(1.1f, 1.65f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        weaponTip,
                        DustID.Frost,
                        burstVelocity * 0.65f,
                        100,
                        new Color(225, 250, 255),
                        Main.rand.NextFloat(0.9f, 1.25f));
                    frost.noGravity = true;
                }
            }

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                weaponTip + visualForward * 6f,
                visualForward * 0.6f,
                Color.Lerp(new Color(105, 220, 255), Color.White, 0.52f),
                new Vector2(1.25f, 2.95f),
                projectile.rotation,
                0.2f,
                0.018f,
                18);
            GeneralParticleHandler.SpawnParticle(pulse);

            GlowOrbParticle orb = new GlowOrbParticle(
                weaponTip + visualForward * 10f,
                visualForward * 0.55f,
                false,
                9,
                1.35f,
                Color.White,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }

        private static void SpawnForwardJetBurst(Projectile projectile, Vector2 weaponTip, float intensity)
        {
            Vector2 visualForward = projectile.rotation.ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);

            float[] branchAngles =
            {
                -MathHelper.PiOver4,
                0f,
                MathHelper.PiOver4
            };

            float[] branchWeights =
            {
                0.58f,
                1f,
                0.58f
            };

            for (int branchIndex = 0; branchIndex < branchAngles.Length; branchIndex++)
            {
                float branchAngle = branchAngles[branchIndex];
                float branchWeight = branchWeights[branchIndex];
                int lineCount = branchIndex == 1 ? 7 : 4;

                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    float localT = lineCount == 1 ? 0f : lineIndex / (float)(lineCount - 1);
                    float centered = localT * 2f - 1f;
                    float wobble = centered * (branchIndex == 1 ? 0.055f : 0.08f);
                    Vector2 branchDirection = visualForward.RotatedBy(branchAngle + wobble);
                    Vector2 spawnPosition =
                        weaponTip +
                        visualForward * Main.rand.NextFloat(2f, 10f) +
                        visualRight * (float)System.Math.Sin(branchAngle) * Main.rand.NextFloat(0f, 7f) +
                        Main.rand.NextVector2Circular(1.8f, 1.8f);

                    Particle line = new CustomSpark(
                        spawnPosition,
                        branchDirection * Main.rand.NextFloat(11f, 20f) * (0.72f + branchWeight * 0.45f) * intensity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        Main.rand.Next(15, 22),
                        Main.rand.NextFloat(0.14f, 0.22f) * (0.8f + branchWeight * 0.95f) * intensity,
                        Color.Lerp(new Color(105, 220, 255), Color.White, 0.42f + 0.3f * branchWeight) * 0.92f,
                        new Vector2(2.15f + branchWeight * 1.65f, 0.42f + branchWeight * 0.08f),
                        shrinkSpeed: 0.66f);
                    GeneralParticleHandler.SpawnParticle(line);
                }

                Vector2 branchDirectionCore = visualForward.RotatedBy(branchAngle);
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip + branchDirectionCore * Main.rand.NextFloat(6f, 15f),
                    branchDirectionCore * Main.rand.NextFloat(0.25f, 0.75f),
                    false,
                    7,
                    0.72f + branchWeight * 0.48f,
                    Color.Lerp(new Color(95, 210, 255), Color.White, 0.45f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }
}
