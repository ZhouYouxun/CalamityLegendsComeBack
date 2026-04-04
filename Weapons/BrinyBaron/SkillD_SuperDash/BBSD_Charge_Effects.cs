using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the per-frame visuals before the dash actually starts.
    // This file handles both the charging buildup and the quiet ready-hold state after charge completes.
    internal static class BBSD_Charge_Effects
    {
        private const int ChargeTime = 90;
        private const float CustomSparkIntensity = 0.15f;

        internal static void SpawnChargeEffects(Projectile projectile, Player owner, Vector2 weaponTip, int timer)
        {
            if (Main.dedServ)
                return;

            float chargeProgress = Utils.GetLerpValue(0f, ChargeTime, timer, true);
            float intensity = 0.24f + 0.76f * (1f - (float)Math.Pow(1f - chargeProgress, 2.35f));
            Vector2 visualForward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);

            SpawnChargeFunnel(projectile, owner, weaponTip, visualForward, intensity, timer);
            SpawnTriJetBurst(weaponTip, visualForward, visualRight, intensity, 0.44f + 0.56f * chargeProgress, timer, false);

            if (timer % 5 == 0)
            {
                DirectionalPulseRing tipPulse = new DirectionalPulseRing(
                    weaponTip + visualForward * 2f,
                    visualForward * (0.12f + intensity * 0.24f),
                    Color.Lerp(new Color(70, 180, 255), Color.White, 0.35f + intensity * 0.28f),
                    new Vector2(0.35f + intensity * 0.18f, 1f + intensity * 0.5f),
                    projectile.rotation,
                    0.08f + intensity * 0.05f,
                    0.014f,
                    10 + (int)(intensity * 6f));
                GeneralParticleHandler.SpawnParticle(tipPulse);
            }
        }

        internal static void SpawnReadyHoldEffects(Projectile projectile, Vector2 weaponTip, int readyTimer)
        {
            if (Main.dedServ)
                return;

            Vector2 visualForward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 visualRight = visualForward.RotatedBy(MathHelper.PiOver2);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(readyTimer * 0.22f);
            float intensity = 0.7f + pulse * 0.14f;

            SpawnTriJetBurst(weaponTip, visualForward, visualRight, intensity, 0.84f, readyTimer, true);

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip + visualForward * Main.rand.NextFloat(6f, 16f) + visualRight * Main.rand.NextFloat(-5f, 5f),
                    visualForward * Main.rand.NextFloat(0.12f, 0.4f),
                    false,
                    7,
                    0.78f + Main.rand.NextFloat(0.18f),
                    Color.Lerp(new Color(95, 210, 255), Color.White, 0.45f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (readyTimer % 6 == 0)
            {
                CritSpark spark = new CritSpark(
                    weaponTip + visualForward * Main.rand.NextFloat(10f, 22f) + visualRight * Main.rand.NextFloat(-6f, 6f),
                    visualForward.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * Main.rand.NextFloat(4.2f, 7.2f),
                    Color.White,
                    Color.LightBlue,
                    0.9f,
                    14);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private static void SpawnChargeFunnel(Projectile projectile, Player owner, Vector2 weaponTip, Vector2 bladeForward, float intensity, int timer)
        {
            Vector2 right = bladeForward.RotatedBy(MathHelper.PiOver2);
            Vector2 lowerFocus = owner.Bottom + Vector2.UnitY * MathHelper.Lerp(24f, 46f, intensity);
            int laneCount = 4 + (int)Math.Round(intensity * 3f);

            for (int lane = 0; lane < laneCount; lane++)
            {
                float laneRatio = lane / (float)Math.Max(1, laneCount - 1);
                float side = MathHelper.Lerp(-1f, 1f, laneRatio);
                float spiral = timer * 0.24f + lane * 0.7f + projectile.identity * 0.17f;
                float spread = MathHelper.Lerp(18f, 84f, intensity) * (0.42f + 0.58f * (0.5f + 0.5f * (float)Math.Sin(spiral)));

                Vector2 spawnPosition =
                    lowerFocus +
                    right * side * spread +
                    Vector2.UnitY * Main.rand.NextFloat(-6f, 12f) -
                    bladeForward * Main.rand.NextFloat(8f, 22f) +
                    right * (float)Math.Sin(spiral * 1.35f) * 16f +
                    Main.rand.NextVector2Circular(3f, 3f);

                Vector2 inward = (weaponTip - spawnPosition).SafeNormalize(bladeForward);
                Vector2 curl = inward.RotatedBy(MathHelper.PiOver2 * (side >= 0f ? 1f : -1f));
                Vector2 flowVelocity =
                    inward * MathHelper.Lerp(4.2f, 11.8f, intensity) +
                    curl * MathHelper.Lerp(1.8f, 5.2f, intensity) +
                    owner.velocity * 0.08f;

                Dust water = Dust.NewDustPerfect(
                    spawnPosition,
                    DustID.Water,
                    flowVelocity,
                    100,
                    Color.Lerp(new Color(60, 160, 255), new Color(150, 235, 255), 0.35f + 0.35f * intensity),
                    Main.rand.NextFloat(0.95f, 1.35f));
                water.noGravity = true;
                water.fadeIn = 1.08f;

                if ((timer + lane) % 2 == 0)
                {
                    WaterFlavoredParticle mist = new WaterFlavoredParticle(
                        spawnPosition,
                        flowVelocity * 0.48f,
                        false,
                        Main.rand.Next(18, 24),
                        0.84f + Main.rand.NextFloat(0.2f),
                        Color.LightBlue * 0.92f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }
        }

        private static void SpawnTriJetBurst(Vector2 weaponTip, Vector2 visualForward, Vector2 visualRight, float intensity, float stability, int timer, bool stableReadyJet)
        {
            float clampedIntensity = MathHelper.Clamp(intensity, 0f, 1.4f);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(timer * (stableReadyJet ? 0.18f : 0.27f));
            float lineLength = MathHelper.Lerp(10f, 34f, clampedIntensity) * MathHelper.Lerp(0.9f, 1.12f, pulse) * CustomSparkIntensity;
            float lineScale = MathHelper.Lerp(0.08f, 0.22f, clampedIntensity) * CustomSparkIntensity;
            float lineLifetime = MathHelper.Lerp(11f, 20f, clampedIntensity) * 0.4f;

            float[] branchAngles =
            {
                -MathHelper.PiOver4,
                0f,
                MathHelper.PiOver4
            };

            float[] branchWeights =
            {
                0.6f,
                1f,
                0.6f
            };

            for (int branchIndex = 0; branchIndex < branchAngles.Length; branchIndex++)
            {
                float branchAngle = branchAngles[branchIndex];
                float branchWeight = branchWeights[branchIndex];
                int lineCount = 1;

                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    float localT = lineCount == 1 ? 0f : lineIndex / (float)(lineCount - 1);
                    float centered = localT * 2f - 1f;
                    float wobble = centered * MathHelper.Lerp(0.028f, 0.085f, stability);
                    Vector2 branchDirection = visualForward.RotatedBy(branchAngle + wobble);
                    Vector2 spawnPosition =
                        weaponTip +
                        visualForward * Main.rand.NextFloat(2f, 7f + clampedIntensity * 10f) +
                        visualRight * (float)Math.Sin(branchAngle) * Main.rand.NextFloat(0f, 5f) +
                        Main.rand.NextVector2Circular(1.5f, 1.5f);

                    Vector2 velocity = branchDirection * (lineLength * (0.55f + branchWeight * 0.45f));
                    Color lineColor = Color.Lerp(new Color(95, 210, 255), Color.White, 0.35f + 0.35f * branchWeight);
                    Vector2 stretch = new Vector2(
                        (1.8f + clampedIntensity * 2.2f + branchWeight * 0.7f) * 0.25f,
                        (0.34f + branchWeight * 0.08f) * 0.45f);

                    Particle line = new CustomSpark(
                        spawnPosition,
                        velocity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        (int)(lineLifetime + branchWeight * 4f),
                        lineScale * (0.9f + branchWeight * 0.75f),
                        lineColor * (stableReadyJet ? 0.9f : 0.84f) * CustomSparkIntensity,
                        stretch,
                        shrinkSpeed: stableReadyJet ? 0.72f : 0.64f);
                    GeneralParticleHandler.SpawnParticle(line);
                }

                Vector2 branchCoreDirection = visualForward.RotatedBy(branchAngle);
                Dust water = Dust.NewDustPerfect(
                    weaponTip + branchCoreDirection * Main.rand.NextFloat(2f, 8f),
                    DustID.Water,
                    branchCoreDirection * Main.rand.NextFloat(3f, 8f) * (0.65f + branchWeight * 0.45f),
                    100,
                    Color.Lerp(new Color(80, 195, 255), Color.White, 0.25f + 0.35f * branchWeight),
                    Main.rand.NextFloat(0.85f, 1.22f) * (0.8f + branchWeight * 0.3f));
                water.noGravity = true;
            }

            GlowOrbParticle orb = new GlowOrbParticle(
                weaponTip + visualForward * MathHelper.Lerp(6f, 16f, clampedIntensity),
                visualForward * MathHelper.Lerp(0.16f, 0.48f, clampedIntensity),
                false,
                6,
                0.64f + clampedIntensity * 0.38f,
                Color.Lerp(new Color(90, 205, 255), Color.White, 0.45f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }
    }
}
