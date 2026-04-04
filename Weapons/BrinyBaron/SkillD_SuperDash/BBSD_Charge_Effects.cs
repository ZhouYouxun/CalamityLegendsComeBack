using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the stable buildup before the dash releases.
    // The shape language is fixed: one center stream, two mirrored -45 degree streams, and two mirrored +45 degree streams.
    internal static class BBSD_Charge_Effects
    {
        private const int ChargeTime = 90;

        private static readonly float[] StreamAngles =
        {
            0f,
            -MathHelper.PiOver4,
            -MathHelper.PiOver4,
            MathHelper.PiOver4,
            MathHelper.PiOver4
        };

        private static readonly float[] StreamOffsets =
        {
            0f,
            -4.5f,
            -9f,
            4.5f,
            9f
        };

        private static readonly float[] StreamWeights =
        {
            1f,
            0.82f,
            0.68f,
            0.82f,
            0.68f
        };

        private static Vector2 BladeForward(Projectile projectile) => (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();

        internal static void SpawnChargeEffects(Projectile projectile, Player owner, Vector2 weaponTip, int timer)
        {
            if (Main.dedServ)
                return;

            float progress = Utils.GetLerpValue(0f, ChargeTime, timer, true);
            float intensity = 0.34f + progress * 0.66f;
            Vector2 forward = BladeForward(projectile);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            SpawnFrontStreams(weaponTip, forward, right, intensity, timer, readyState: false);
            SpawnCondensedCore(weaponTip, forward, right, intensity, timer, readyState: false);

            if (timer % 5 == 0)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    weaponTip + forward * (4f + intensity * 4f),
                    forward * (0.16f + intensity * 0.12f),
                    Color.Lerp(new Color(70, 190, 255), Color.White, 0.32f + intensity * 0.25f),
                    new Vector2(0.34f + intensity * 0.08f, 0.92f + intensity * 0.26f),
                    forward.ToRotation(),
                    0.08f + intensity * 0.02f,
                    0.014f,
                    9 + (int)(intensity * 3f));
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        internal static void SpawnReadyHoldEffects(Projectile projectile, Vector2 weaponTip, int readyTimer)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = BladeForward(projectile);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(readyTimer * 0.22f);
            float intensity = 0.66f + pulse * 0.16f;

            SpawnFrontStreams(weaponTip, forward, right, intensity, readyTimer, readyState: true);
            SpawnCondensedCore(weaponTip, forward, right, intensity, readyTimer, readyState: true);

            if (readyTimer % 6 == 0)
            {
                DirectionalPulseRing pulseRing = new DirectionalPulseRing(
                    weaponTip + forward * 8f,
                    forward * (0.16f + pulse * 0.08f),
                    Color.Lerp(new Color(90, 210, 255), Color.White, 0.45f),
                    new Vector2(0.38f, 1.08f + pulse * 0.18f),
                    forward.ToRotation(),
                    0.1f,
                    0.013f,
                    11);
                GeneralParticleHandler.SpawnParticle(pulseRing);
            }
        }

        // Emits the five main lanes every frame, with the center lane carrying the most weight.
        private static void SpawnFrontStreams(Vector2 weaponTip, Vector2 forward, Vector2 right, float intensity, int timer, bool readyState)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(timer * (readyState ? 0.18f : 0.24f));
            float stability = readyState ? 0.12f : 0.3f;

            for (int i = 0; i < StreamAngles.Length; i++)
            {
                float weight = StreamWeights[i];
                float laneWave = readyState
                    ? (float)Math.Sin(timer * 0.16f + i * 0.8f) * 0.45f
                    : (float)Math.Sin(timer * 0.18f + i * 0.92f) * (0.55f + intensity * 1.15f);

                Vector2 laneDirection = forward.RotatedBy(StreamAngles[i] + laneWave * 0.008f * stability);
                Vector2 spawnPosition =
                    weaponTip +
                    right * StreamOffsets[i] * (0.85f + intensity * 0.12f) +
                    right * laneWave +
                    forward * Main.rand.NextFloat(1.5f, 6.5f + intensity * 5f) +
                    Main.rand.NextVector2Circular(0.85f, 0.85f);

                float speed = MathHelper.Lerp(7f, 13.5f, intensity) * (0.72f + weight * 0.34f) * MathHelper.Lerp(0.92f, 1.06f, pulse);
                float scale = MathHelper.Lerp(0.052f, 0.088f, intensity) * (0.82f + weight * 0.22f) * (readyState ? 0.85f : 1f);
                int lifetime = 9 + (int)(4f * intensity + weight * 2f);
                Color lineColor = Color.Lerp(new Color(76, 196, 255), Color.White, 0.26f + weight * 0.26f + pulse * 0.08f);

                Particle line = new CustomSpark(
                    spawnPosition,
                    laneDirection * speed,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    lifetime,
                    scale,
                    lineColor * (readyState ? 0.74f : 0.84f),
                    new Vector2(
                        1.65f + weight * 0.52f + intensity * 0.55f,
                        0.42f + weight * 0.05f),
                    shrinkSpeed: readyState ? 0.76f : 0.7f);
                GeneralParticleHandler.SpawnParticle(line);

                if ((timer + i) % (readyState ? 4 : 3) == 0)
                {
                    Dust dust = Dust.NewDustPerfect(
                        spawnPosition,
                        Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                        laneDirection * Main.rand.NextFloat(1.2f, 2.8f) * (0.55f + weight * 0.3f),
                        100,
                        i == 0 ? new Color(100, 220, 255) : new Color(205, 248, 255),
                        Main.rand.NextFloat(0.6f, 0.9f) * (0.7f + intensity * 0.25f));
                    dust.noGravity = true;
                }
            }
        }

        // Keeps the charge rooted at the muzzle so the five streams feel focused instead of noisy.
        private static void SpawnCondensedCore(Vector2 weaponTip, Vector2 forward, Vector2 right, float intensity, int timer, bool readyState)
        {
            float coreRadius = readyState ? 2.2f : 3.8f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(timer * 0.28f);

            if (Main.rand.NextBool(readyState ? 3 : 2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip - forward * Main.rand.NextFloat(0f, 3f) + Main.rand.NextVector2Circular(coreRadius, coreRadius),
                    forward * Main.rand.NextFloat(0.08f, 0.38f),
                    false,
                    6,
                    0.46f + intensity * 0.16f + pulse * 0.05f,
                    Color.Lerp(new Color(105, 220, 255), Color.White, 0.42f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (readyState && timer % 5 == 0)
            {
                CritSpark spark = new CritSpark(
                    weaponTip + forward * Main.rand.NextFloat(4f, 10f) + right * Main.rand.NextFloat(-3f, 3f),
                    forward.RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f)) * Main.rand.NextFloat(2.6f, 4.8f),
                    Color.White,
                    Color.LightBlue,
                    0.62f,
                    10);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
