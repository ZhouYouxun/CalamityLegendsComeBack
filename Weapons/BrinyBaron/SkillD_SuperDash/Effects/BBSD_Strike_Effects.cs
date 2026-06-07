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
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);
            int strikeComboIndex = strikeIndex % 5;

            // Base launch shockwave ring
            DirectionalPulseRing ring = new DirectionalPulseRing(
                strikeStart,
                forward * 2f,
                new Color(95, 206, 255, 0),
                new Vector2(0.6f, 1.2f),
                forward.ToRotation(),
                0.22f,
                0.03f,
                12);
            GeneralParticleHandler.SpawnParticle(ring);

            // Custom burst particles depending on strike type
            if (strikeComboIndex == 0 || strikeComboIndex == 2) // Wave Strike
            {
                // Splash lines in a forward fan shape
                for (int i = 0; i < 12; i++)
                {
                    float angleOffset = MathHelper.Lerp(-0.4f, 0.4f, i / 11f);
                    Vector2 vel = forward.RotatedBy(angleOffset) * Main.rand.NextFloat(6f, 11f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        strikeStart,
                        vel,
                        false,
                        Main.rand.Next(10, 16),
                        0.35f,
                        Color.Cyan));
                }
            }
            else if (strikeComboIndex == 1 || strikeComboIndex == 3) // Shuriken Strike
            {
                // Circular spray of sharp sparks
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(4f, 8f);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        strikeStart,
                        vel,
                        Color.White,
                        new Color(95, 206, 255),
                        0.75f,
                        12,
                        vel.ToRotation(),
                        1f));
                }
            }
            else // Tornado Strike
            {
                // Swirling vortex launch lines
                for (int i = 0; i < 10; i++)
                {
                    Vector2 vel = forward.RotatedBy(MathHelper.ToRadians(15f * (i - 5))) * Main.rand.NextFloat(5f, 10f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        strikeStart,
                        vel,
                        false,
                        Main.rand.Next(12, 18),
                        0.4f,
                        Color.DeepSkyBlue));
                }
            }
        }

        internal static void SpawnStrikeTravelEffects(Projectile projectile, Vector2 previousCenter, Vector2 currentCenter, Vector2 dashDirection, int phaseTimer, int strikeIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (currentCenter - previousCenter).SafeNormalize(Vector2.UnitX);
            float dist = Vector2.Distance(previousCenter, currentCenter);
            int stepSize = 14;
            int strikeComboIndex = strikeIndex % 5;

            for (float d = 0f; d < dist; d += stepSize)
            {
                Vector2 spawnPos = previousCenter + direction * d;

                // Base travel line particle
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    spawnPos,
                    -direction * Main.rand.NextFloat(0.7f, 1.8f),
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.2f, 0.38f),
                    Color.Lerp(new Color(95, 206, 255), Color.White, Main.rand.NextFloat(0.1f, 0.42f))));

                // Theme specific trail additions
                if (strikeComboIndex == 0 || strikeComboIndex == 2) // Wave Strike
                {
                    if (Main.rand.NextBool(3))
                    {
                        Dust water = Dust.NewDustPerfect(
                            spawnPos + Main.rand.NextVector2Circular(8f, 8f),
                            DustID.Water,
                            -direction * Main.rand.NextFloat(0.5f, 2f),
                            100,
                            new Color(90, 205, 255),
                            Main.rand.NextFloat(0.7f, 1.1f));
                        water.noGravity = true;
                    }
                }
                else if (strikeComboIndex == 1 || strikeComboIndex == 3) // Shuriken Strike
                {
                    if (Main.rand.NextBool(3))
                    {
                        GeneralParticleHandler.SpawnParticle(new CritSpark(
                            spawnPos,
                            -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 3f),
                            Color.White,
                            new Color(95, 206, 255),
                            Main.rand.NextFloat(0.35f, 0.65f),
                            10,
                            0f,
                            0.8f));
                    }
                }
                else // Tornado Strike
                {
                    if (Main.rand.NextBool(3))
                    {
                        // Spiral offset around the center line to simulate a tornado vortex
                        float spiralAngle = d * 0.15f + phaseTimer * 0.22f;
                        Vector2 offset = direction.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY) * (float)Math.Sin(spiralAngle) * 12f;
                        Dust water = Dust.NewDustPerfect(
                            spawnPos + offset,
                            Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                            -direction * Main.rand.NextFloat(0.3f, 1.2f),
                            100,
                            Color.Cyan,
                            Main.rand.NextFloat(0.6f, 0.95f));
                        water.noGravity = true;
                    }
                }

                if (Main.rand.NextBool(3))
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
            int strikeComboIndex = strikeIndex % 5;

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1.0f,
                Pitch = -0.15f + Main.rand.NextFloat(-0.05f, 0.05f)
            }, impactCenter);

            // 1. Spawning basic particles (pulse rings, lines, glow orbs)
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

            // 2. Custom theme impact particles based on strike index
            if (strikeComboIndex == 0 || strikeComboIndex == 2) // Wave Strike
            {
                // Wave impact: big water/frost splash and parallel/perpendicular lines
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dustVel = forward.RotatedByRandom(0.85f) * Main.rand.NextFloat(3f, 9f);
                    Dust water = Dust.NewDustPerfect(
                        impactCenter,
                        Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                        dustVel,
                        100,
                        new Color(95, 206, 255),
                        Main.rand.NextFloat(0.95f, 1.45f));
                    water.noGravity = true;
                }

                for (int i = 0; i < 10; i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    Vector2 spreadVelocity = right * side * Main.rand.NextFloat(3f, 8f) + forward * Main.rand.NextFloat(-2f, 2f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        impactCenter,
                        spreadVelocity,
                        false,
                        Main.rand.Next(8, 14),
                        Main.rand.NextFloat(0.24f, 0.48f),
                        Color.Lerp(new Color(110, 214, 255), Color.White, Main.rand.NextFloat(0.2f, 0.6f))));
                }
            }
            else if (strikeComboIndex == 1 || strikeComboIndex == 3) // Shuriken Strike
            {
                // Shuriken impact: sharp 8-directional spark explosion
                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkVel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(5f, 10f);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        impactCenter,
                        sparkVel,
                        Color.White,
                        new Color(95, 206, 255),
                        Main.rand.NextFloat(1.1f, 1.5f),
                        18,
                        sparkVel.ToRotation(),
                        1.2f));
                }
            }
            else // Tornado Strike
            {
                // Tornado impact: swirling whirlpool of line particles
                for (int i = 0; i < 18; i++)
                {
                    float angle = MathHelper.TwoPi * i / 18f;
                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 spiralVel = outward.RotatedBy(MathHelper.PiOver4 * 1.2f) * Main.rand.NextFloat(4.5f, 8.5f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        impactCenter,
                        spiralVel,
                        false,
                        Main.rand.Next(14, 22),
                        0.42f,
                        Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat())));
                }
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
