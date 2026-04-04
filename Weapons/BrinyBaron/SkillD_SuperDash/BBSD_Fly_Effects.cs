using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the active dash visuals after the charge has been released.
    // This file focuses on propulsion, support-star launch accents, and the finishing burst when the dash ends.
    internal static class BBSD_Fly_Effects
    {
        internal static void SpawnDashStartBurst(Projectile projectile, Vector2 bladeDirection, Vector2 weaponTip)
        {
            if (Main.dedServ)
                return;

            Vector2 visualForward = projectile.rotation.ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 22; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f).RotatedBy(visualForward.ToRotation()) * Main.rand.NextFloat(5f, 18f);

                Dust water = Dust.NewDustPerfect(weaponTip, DustID.Water, burstVelocity, 100, new Color(70, 175, 255), Main.rand.NextFloat(1.15f, 1.8f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(weaponTip, DustID.Frost, burstVelocity * 0.72f, 100, new Color(210, 248, 255), Main.rand.NextFloat(0.95f, 1.35f));
                    frost.noGravity = true;
                }
            }

            SpawnRearRocketJet(projectile, projectile.Center - visualForward * 20f, visualForward, visualRight, 1.2f, 0);

            GlowOrbParticle orb = new GlowOrbParticle(
                weaponTip,
                Vector2.Zero,
                false,
                8,
                1.2f,
                new Color(110, 225, 255),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }

        internal static void SpawnDashWakeEffects(Projectile projectile, Player owner, Vector2 bladeDirection, Vector2 weaponTip, int dashTimer)
        {
            if (Main.dedServ)
                return;

            Vector2 visualForward = projectile.rotation.ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);
            Vector2 rear = projectile.Center - visualForward * 22f;

            float pulseA = 0.5f + 0.5f * (float)Math.Sin(dashTimer * 0.4f);
            float pulseB = 0.5f + 0.5f * (float)Math.Cos(dashTimer * 0.27f + 0.6f);
            float rocketIntensity = 0.8f + pulseA * 0.35f;

            SpawnRearRocketJet(projectile, rear, visualForward, visualRight, rocketIntensity, dashTimer);

            for (int side = -1; side <= 1; side += 2)
            {
                float ribbonPhase = dashTimer * 0.42f + side * 0.9f + projectile.identity * 0.14f;
                float ribbonOffset = side * (6f + 4f * (float)Math.Sin(ribbonPhase * 1.35f));
                Vector2 spawnPosition = rear - visualForward * Main.rand.NextFloat(10f, 30f) + visualRight * ribbonOffset;
                Vector2 wakeVelocity =
                    -visualForward * Main.rand.NextFloat(5.5f, 11.5f) +
                    visualRight * side * Main.rand.NextFloat(0.5f, 2f) +
                    owner.velocity * 0.14f;

                Dust water = Dust.NewDustPerfect(spawnPosition, DustID.Water, wakeVelocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.05f, 1.45f));
                water.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Dust frost = Dust.NewDustPerfect(spawnPosition, DustID.Frost, wakeVelocity * 0.72f, 100, new Color(205, 246, 255), Main.rand.NextFloat(0.9f, 1.2f));
                    frost.noGravity = true;
                }
            }

            if (dashTimer % 2 == 0)
            {
                WaterFlavoredParticle mist = new WaterFlavoredParticle(
                    rear + visualRight * Main.rand.NextFloat(-8f, 8f),
                    -visualForward * Main.rand.NextFloat(3.5f, 6.8f) + visualRight * Main.rand.NextFloat(-1.4f, 1.4f),
                    false,
                    Main.rand.Next(20, 28),
                    0.9f + Main.rand.NextFloat(0.25f),
                    Color.LightBlue * 0.94f);
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (dashTimer % 5 == 0)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip - visualForward * Main.rand.NextFloat(4f, 10f),
                    owner.velocity * 0.05f,
                    false,
                    5,
                    0.85f + pulseA * 0.25f,
                    Color.Lerp(new Color(65, 180, 255), Color.White, 0.35f + 0.35f * pulseB),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (dashTimer % 12 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item88 with
                {
                    Volume = 0.4f,
                    Pitch = -0.25f + pulseA * 0.15f
                }, projectile.Center);
            }
        }

        internal static void SpawnSupportStarLaunchEffects(Vector2 spawnPosition, Vector2 launchVelocity, float intensity)
        {
            if (Main.dedServ)
                return;

            Dust water = Dust.NewDustPerfect(
                spawnPosition,
                DustID.Water,
                launchVelocity * 0.28f,
                100,
                new Color(70, 175, 255),
                Main.rand.NextFloat(1f, 1.35f) * intensity);
            water.noGravity = true;
            water.fadeIn = 1.08f;

            Dust frost = Dust.NewDustPerfect(
                spawnPosition,
                DustID.Frost,
                launchVelocity * 0.2f,
                100,
                new Color(210, 248, 255),
                Main.rand.NextFloat(0.85f, 1.15f) * intensity);
            frost.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    spawnPosition,
                    Vector2.Zero,
                    false,
                    6 + Main.rand.Next(3),
                    0.7f + Main.rand.NextFloat(0.2f) * intensity,
                    Color.Lerp(new Color(65, 185, 255), Color.White, 0.35f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        internal static void SpawnOnKillEffects(Projectile projectile, Vector2 bladeDirection)
        {
            if (Main.dedServ)
                return;

            Vector2 visualForward = projectile.rotation.ToRotationVector2();

            for (int i = 0; i < 28; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f).RotatedBy(visualForward.ToRotation()) * Main.rand.NextFloat(5f, 16f);

                Dust water = Dust.NewDustPerfect(projectile.Center, DustID.Water, burstVelocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.15f, 1.75f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(projectile.Center, DustID.Frost, burstVelocity * 0.72f, 100, new Color(210, 245, 255), Main.rand.NextFloat(0.9f, 1.35f));
                    frost.noGravity = true;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1f, 1f),
                    false,
                    6,
                    0.95f + Main.rand.NextFloat(0.3f),
                    new Color(90, 205, 255),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.15f,
                Pitch = -0.3f
            }, projectile.Center);
        }

        private static void SpawnRearRocketJet(Projectile projectile, Vector2 exhaustOrigin, Vector2 visualForward, Vector2 visualRight, float intensity, int timer)
        {
            float[] branchOffsets =
            {
                -0.36f,
                0f,
                0.36f
            };

            float[] branchWeights =
            {
                0.72f,
                1f,
                0.72f
            };

            for (int branchIndex = 0; branchIndex < branchOffsets.Length; branchIndex++)
            {
                float branchOffset = branchOffsets[branchIndex];
                float branchWeight = branchWeights[branchIndex];
                Vector2 branchDirection = (-visualForward).RotatedBy(branchOffset);
                int lineCount = branchIndex == 1 ? 3 : 2;

                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    float localT = lineCount == 1 ? 0f : lineIndex / (float)(lineCount - 1);
                    float centered = localT * 2f - 1f;
                    Vector2 spawnPosition =
                        exhaustOrigin +
                        visualRight * branchOffset * 9f +
                        visualRight * centered * 2.6f +
                        Main.rand.NextVector2Circular(1.8f, 1.8f);

                    Particle line = new CustomSpark(
                        spawnPosition,
                        branchDirection.RotatedBy(centered * 0.05f) * Main.rand.NextFloat(7f, 15f) * (0.75f + branchWeight * 0.35f) * intensity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.1f, 0.16f) * (0.78f + branchWeight * 0.5f) * intensity,
                        Color.Lerp(new Color(90, 205, 255), Color.White, 0.28f + 0.3f * branchWeight) * 0.86f,
                        new Vector2(1.9f + branchWeight * 1.25f, 0.34f + branchWeight * 0.06f),
                        shrinkSpeed: 0.7f);
                    GeneralParticleHandler.SpawnParticle(line);
                }

                if ((timer + branchIndex) % 2 == 0)
                {
                    WaterFlavoredParticle mist = new WaterFlavoredParticle(
                        exhaustOrigin + visualRight * branchOffset * 7f,
                        branchDirection * Main.rand.NextFloat(2.6f, 5.2f) * intensity,
                        false,
                        Main.rand.Next(18, 24),
                        0.82f + Main.rand.NextFloat(0.2f),
                        Color.LightBlue * 0.92f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            GlowOrbParticle orb = new GlowOrbParticle(
                exhaustOrigin - visualForward * 3f,
                -visualForward * 0.18f,
                false,
                5,
                0.72f + intensity * 0.25f,
                Color.Lerp(new Color(90, 205, 255), Color.White, 0.42f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }
    }
}
