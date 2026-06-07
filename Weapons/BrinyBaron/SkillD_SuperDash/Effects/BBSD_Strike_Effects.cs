using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Strike_Effects
    {
        internal static void SpawnStrikeLaunchEffects(Projectile projectile, Vector2 strikeStart, Vector2 targetCenter, Vector2 dashDirection, int strikeIndex)
        {
        }

        internal static void SpawnStrikeTravelEffects(Projectile projectile, Vector2 previousCenter, Vector2 currentCenter, Vector2 dashDirection, int phaseTimer, int strikeIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (currentCenter - previousCenter).SafeNormalize(Vector2.UnitX);
            float dist = Vector2.Distance(previousCenter, currentCenter);
            int stepSize = 8;
            for (float d = 0f; d < dist; d += stepSize)
            {
                Vector2 spawnPos = previousCenter + direction * d;

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    spawnPos,
                    -direction * Main.rand.NextFloat(0.7f, 1.8f),
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.2f, 0.38f),
                    Color.Lerp(new Color(95, 206, 255), Color.White, Main.rand.NextFloat(0.1f, 0.42f))));

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        spawnPos + Main.rand.NextVector2Circular(4f, 4f),
                        -direction * Main.rand.NextFloat(0.2f, 0.75f),
                        false,
                        Main.rand.Next(6, 10),
                        Main.rand.NextFloat(0.18f, 0.32f),
                        Color.Lerp(new Color(95, 206, 255), Color.Cyan, Main.rand.NextFloat()),
                        true,
                        false,
                        true));
                }
            }
        }

        internal static void SpawnStrikeImpactEffects(Projectile projectile, Vector2 impactCenter, Vector2 dashDirection, int strikeIndex, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1.0f,
                Pitch = -0.15f + Main.rand.NextFloat(-0.05f, 0.05f)
            }, impactCenter);

            // Spawn CritSparks highly parallel to the dash direction
            for (int i = 0; i < 6; i++)
            {
                float sparkSpeed = Main.rand.NextFloat(6f, 12f);
                float angleDev = Main.rand.NextFloat(-0.04f, 0.04f); // Highly parallel
                Vector2 sparkVelocity = forward.RotatedBy(angleDev) * sparkSpeed;
                Particle spark = new CritSpark(
                    impactCenter + Main.rand.NextVector2Circular(8f, 8f),
                    sparkVelocity,
                    Color.White,
                    new Color(95, 206, 255),
                    Main.rand.NextFloat(0.9f, 1.4f),
                    14 + Main.rand.Next(6),
                    sparkVelocity.ToRotation(), // Position 7: rotation
                    1.1f); // Position 8: bloomScale
                GeneralParticleHandler.SpawnParticle(spark);
            }

            Vector2 perpendicular = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector2 spreadVelocity = perpendicular * side * Main.rand.NextFloat(3f, 8f) + forward * Main.rand.NextFloat(-2f, 2f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    impactCenter,
                    spreadVelocity,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.24f, 0.48f),
                    Color.Lerp(new Color(110, 214, 255), Color.White, Main.rand.NextFloat(0.2f, 0.6f))));
            }

            // Spawn circular shockwave
            for (int i = 0; i < 2; i++)
            {
                DirectionalPulseRing ring = new DirectionalPulseRing(
                    impactCenter,
                    forward * 0.5f,
                    Color.Lerp(new Color(95, 206, 255), Color.White, i * 0.3f),
                    new Vector2(0.52f, 1.28f),
                    forward.ToRotation(),
                    0.18f + i * 0.05f,
                    0.02f,
                    14 + i * 2);
                GeneralParticleHandler.SpawnParticle(ring);
            }

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    impactCenter + Main.rand.NextVector2Circular(5f, 5f),
                    forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(2.2f, 6.4f),
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Color.Lerp(new Color(110, 214, 255), Color.White, Main.rand.NextFloat()),
                    true,
                    false,
                    true));
            }
        }

        internal static void SpawnFinalBurst(Vector2 center, Vector2 direction, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                DirectionalPulseRing wave = new DirectionalPulseRing(
                    center,
                    direction * 0.16f,
                    Color.Lerp(new Color(120, 220, 255), Color.White, 0.32f),
                    new Vector2(0.5f + i * 0.1f, 1.65f + i * 0.22f),
                    direction.ToRotation() + i * MathHelper.PiOver4,
                    0.16f + i * 0.02f,
                    0.02f,
                    14 + i * 2);
                GeneralParticleHandler.SpawnParticle(wave);
            }

            for (int i = 0; i < Math.Max(14, totalStrikes); i++)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center,
                    direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.8f, 5.2f),
                    false,
                    Main.rand.Next(9, 15),
                    Main.rand.NextFloat(0.24f, 0.46f),
                    Color.Lerp(new Color(120, 220, 255), Color.White, Main.rand.NextFloat(0.15f, 0.55f))));
            }
        }
    }
}
