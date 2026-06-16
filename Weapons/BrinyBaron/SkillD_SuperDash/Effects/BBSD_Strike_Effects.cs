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
            float sequenceProgress = totalStrikes <= 1 ? 1f : MathHelper.Clamp(strikeIndex / (float)(totalStrikes - 1), 0f, 1f);
            float density = MathHelper.Lerp(1f, 0.48f, sequenceProgress);

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = MathHelper.Lerp(1f, 0.72f, sequenceProgress),
                Pitch = MathHelper.Lerp(-0.22f, 0.16f, sequenceProgress) + Main.rand.NextFloat(-0.05f, 0.05f)
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

            int glowCount = Math.Max(3, (int)(8 * density));
            for (int i = 0; i < glowCount; i++)
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
                int splashCount = Math.Max(3, (int)(8 * density));
                for (int i = 0; i < splashCount; i++)
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

                int lineCount = Math.Max(4, (int)(10 * density));
                for (int i = 0; i < lineCount; i++)
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
                int sparkCount = Math.Max(4, (int)(8 * density));
                for (int i = 0; i < sparkCount; i++)
                {
                    Vector2 sparkVel = (MathHelper.TwoPi * i / sparkCount).ToRotationVector2() * Main.rand.NextFloat(5f, 10f);
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
                int vortexCount = Math.Max(8, (int)(18 * density));
                for (int i = 0; i < vortexCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / vortexCount;
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

        internal static void SpawnStrikeSlowdownEffects(Projectile projectile, Vector2 impactCenter, Vector2 dashDirection, float sequenceProgress, float glowIntensity)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color bladeBlue = Color.Lerp(new Color(80, 210, 255), Color.White, MathHelper.Clamp(glowIntensity / 1.4f, 0f, 1f) * 0.45f);

            DirectionalPulseRing brakeRing = new(
                impactCenter,
                -forward * 0.45f,
                bladeBlue * MathHelper.Lerp(0.8f, 0.45f, sequenceProgress),
                new Vector2(0.35f, 1.75f),
                forward.ToRotation(),
                0.11f,
                0.018f,
                10);
            GeneralParticleHandler.SpawnParticle(brakeRing);

            int lineCount = sequenceProgress < 0.65f ? 7 : 4;
            for (int i = 0; i < lineCount; i++)
            {
                float side = i - (lineCount - 1) * 0.5f;
                Vector2 offset = right * side * 5f + Main.rand.NextVector2Circular(4f, 4f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    impactCenter + offset,
                    -forward * Main.rand.NextFloat(1.1f, 3.2f),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.24f, 0.42f),
                    Color.Lerp(Color.DeepSkyBlue, bladeBlue, Main.rand.NextFloat(0.3f, 0.8f))));
            }
        }

        internal static void SpawnFinalPauseStartEffects(Projectile projectile, Vector2 targetCenter, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing ring = new(
                    targetCenter,
                    Vector2.Zero,
                    Color.Lerp(new Color(80, 210, 255), Color.White, i * 0.22f),
                    new Vector2(0.62f + i * 0.18f, 1.35f + i * 0.32f),
                    forward.ToRotation() + i * MathHelper.PiOver2,
                    0.13f + i * 0.025f,
                    0.015f,
                    12 + i * 2);
                GeneralParticleHandler.SpawnParticle(ring);
            }

            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f;
                Vector2 inward = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    targetCenter + inward * Main.rand.NextFloat(72f, 116f),
                    -inward * Main.rand.NextFloat(3f, 6.5f),
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.34f, 0.58f),
                    Color.Lerp(Color.DeepSkyBlue, Color.White, Main.rand.NextFloat(0.16f, 0.5f))));
            }
        }

        internal static void SpawnFinalPauseEffects(Projectile projectile, Vector2 targetCenter, Vector2 direction, int timer, int duration)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float completion = Utils.GetLerpValue(0f, duration, timer, true);

            if (timer % 3 == 0)
            {
                float radius = MathHelper.Lerp(92f, 34f, completion);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 spawnPos = targetCenter + right * side * radius + forward * Main.rand.NextFloat(-20f, 20f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        spawnPos,
                        -right * side * Main.rand.NextFloat(2.4f, 5.2f),
                        false,
                        Main.rand.Next(9, 14),
                        Main.rand.NextFloat(0.28f, 0.48f),
                        Color.Lerp(new Color(110, 225, 255), Color.White, completion * 0.35f)));
                }
            }

            if (timer % 5 == 0)
            {
                DirectionalPulseRing ring = new(
                    targetCenter,
                    Vector2.Zero,
                    new Color(90, 215, 255, 0) * MathHelper.Lerp(0.45f, 0.82f, completion),
                    new Vector2(0.45f, MathHelper.Lerp(1.2f, 2.1f, completion)),
                    forward.ToRotation() + timer * 0.08f,
                    0.08f,
                    0.014f,
                    10);
                GeneralParticleHandler.SpawnParticle(ring);
            }
        }

        internal static void SpawnFinalExecutionLaunchEffects(Projectile projectile, Vector2 strikeStart, Vector2 targetCenter, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            DirectionalPulseRing launchRing = new(
                strikeStart,
                forward * 1.8f,
                new Color(120, 235, 255, 0),
                new Vector2(0.72f, 2.45f),
                forward.ToRotation(),
                0.2f,
                0.018f,
                16);
            GeneralParticleHandler.SpawnParticle(launchRing);

            for (int i = 0; i < 14; i++)
            {
                Vector2 position = Vector2.Lerp(strikeStart, targetCenter, Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    position + Main.rand.NextVector2Circular(18f, 18f),
                    forward * Main.rand.NextFloat(4.2f, 9.5f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.32f, 0.58f),
                    Color.Lerp(Color.Cyan, Color.White, Main.rand.NextFloat(0.25f, 0.7f))));
            }
        }

        internal static void SpawnFinalExecutionImpactEffects(Projectile projectile, Vector2 center, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 5; i++)
            {
                DirectionalPulseRing wave = new(
                    center,
                    forward * 0.22f,
                    Color.Lerp(new Color(110, 225, 255), Color.White, i * 0.16f),
                    new Vector2(0.72f + i * 0.12f, 2.2f + i * 0.36f),
                    forward.ToRotation() + i * MathHelper.PiOver4,
                    0.16f + i * 0.018f,
                    0.014f,
                    16 + i * 2);
                GeneralParticleHandler.SpawnParticle(wave);
            }

            for (int i = 0; i < 34; i++)
            {
                float spread = Main.rand.NextFloat(-0.72f, 0.72f);
                Vector2 velocity = forward.RotatedBy(spread) * Main.rand.NextFloat(5f, 13f) +
                    right * Main.rand.NextFloat(-3.4f, 3.4f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + Main.rand.NextVector2Circular(14f, 14f),
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.38f, 0.72f),
                    Color.Lerp(new Color(100, 215, 255), Color.White, Main.rand.NextFloat(0.25f, 0.75f))));
            }

            for (int i = 0; i < 22; i++)
            {
                Vector2 dustVelocity = (MathHelper.TwoPi * i / 22f).ToRotationVector2() * Main.rand.NextFloat(4f, 10f);
                Dust water = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    dustVelocity,
                    100,
                    Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                    Main.rand.NextFloat(1.05f, 1.55f));
                water.noGravity = true;
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
