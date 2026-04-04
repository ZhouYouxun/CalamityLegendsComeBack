using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the active propulsion language while the dash is underway.
    // The emphasis is on restrained rear exhaust and thin streamlines hugging the blade.
    internal static class BBSD_Fly_Effects
    {
        internal static void SpawnDashStartBurst(Projectile projectile, Vector2 bladeDirection, Vector2 weaponTip)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = bladeDirection.SafeNormalize((projectile.rotation - MathHelper.PiOver4).ToRotationVector2());
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 rear = projectile.Center - forward * 20f;

            SpawnRearPropulsionStreams(rear, forward, right, 1.2f, 0);
            SpawnBladeEdgeStreams(weaponTip, forward, right, 0.85f, 0);

            DirectionalPulseRing rearPulse = new DirectionalPulseRing(
                rear,
                -forward * 0.3f,
                new Color(90, 205, 255),
                new Vector2(0.55f, 1.35f),
                (-forward).ToRotation(),
                0.16f,
                0.015f,
                13);
            GeneralParticleHandler.SpawnParticle(rearPulse);
        }

        internal static void SpawnDashWakeEffects(Projectile projectile, Player owner, Vector2 bladeDirection, Vector2 weaponTip, int dashTimer)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = bladeDirection.SafeNormalize((projectile.rotation - MathHelper.PiOver4).ToRotationVector2());
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 rear = projectile.Center - forward * 22f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(dashTimer * 0.34f);
            float intensity = 0.74f + pulse * 0.22f;

            SpawnRearPropulsionStreams(rear, forward, right, intensity, dashTimer);
            SpawnBladeEdgeStreams(weaponTip, forward, right, 0.58f + pulse * 0.16f, dashTimer);

            if (dashTimer % 4 == 0)
            {
                DirectionalPulseRing pulseRing = new DirectionalPulseRing(
                    rear - forward * 2f,
                    -forward * (0.22f + pulse * 0.1f),
                    Color.Lerp(new Color(75, 188, 255), Color.White, 0.28f + pulse * 0.2f),
                    new Vector2(0.42f, 1.08f + pulse * 0.18f),
                    (-forward).ToRotation(),
                    0.1f,
                    0.012f,
                    10);
                GeneralParticleHandler.SpawnParticle(pulseRing);
            }

            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip - forward * Main.rand.NextFloat(2f, 8f) + right * Main.rand.NextFloat(-3f, 3f),
                    owner.velocity * 0.03f,
                    false,
                    5,
                    0.5f + pulse * 0.12f,
                    Color.Lerp(new Color(70, 185, 255), Color.White, 0.32f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (dashTimer % 18 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item88 with
                {
                    Volume = 0.28f,
                    Pitch = -0.32f
                }, projectile.Center);
            }
        }

        internal static void SpawnSupportStarLaunchEffects(Vector2 spawnPosition, Vector2 launchVelocity, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = launchVelocity.SafeNormalize(-Vector2.UnitY);

            Particle line = new CustomSpark(
                spawnPosition,
                forward * Main.rand.NextFloat(3.5f, 6.5f) * intensity,
                "CalamityMod/Particles/BloomLineSoftEdge",
                false,
                8,
                0.045f * intensity,
                new Color(95, 210, 255) * 0.74f,
                new Vector2(1.5f, 0.4f),
                shrinkSpeed: 0.74f);
            GeneralParticleHandler.SpawnParticle(line);

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    spawnPosition,
                    Vector2.Zero,
                    false,
                    5,
                    0.42f + 0.14f * intensity,
                    Color.Lerp(new Color(90, 205, 255), Color.White, 0.34f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            Dust dust = Dust.NewDustPerfect(
                spawnPosition,
                Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                launchVelocity * 0.22f,
                100,
                new Color(95, 210, 255),
                Main.rand.NextFloat(0.65f, 0.95f) * intensity);
            dust.noGravity = true;
        }

        internal static void SpawnOnKillEffects(Projectile projectile, Vector2 bladeDirection)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = bladeDirection.SafeNormalize((projectile.rotation - MathHelper.PiOver4).ToRotationVector2());
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 10; i++)
            {
                float spread = MathHelper.Lerp(-0.75f, 0.75f, i / 9f);
                Vector2 velocity =
                    forward.RotatedBy(spread) * Main.rand.NextFloat(3.5f, 8.5f) +
                    right * spread * 2.8f;

                Particle line = new CustomSpark(
                    projectile.Center,
                    velocity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.05f, 0.075f),
                    Color.Lerp(new Color(85, 200, 255), Color.White, 0.28f) * 0.72f,
                    new Vector2(1.75f, 0.42f),
                    shrinkSpeed: 0.72f);
                GeneralParticleHandler.SpawnParticle(line);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    velocity * 0.45f,
                    100,
                    new Color(105, 220, 255),
                    Main.rand.NextFloat(0.72f, 1.02f));
                dust.noGravity = true;
            }

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                projectile.Center,
                forward * 0.18f,
                new Color(90, 205, 255),
                new Vector2(0.7f, 1.55f),
                forward.ToRotation(),
                0.15f,
                0.016f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 0.85f,
                Pitch = -0.34f
            }, projectile.Center);
        }

        // Rear exhaust stays behind the dash and keeps a clean, rocket-like silhouette.
        private static void SpawnRearPropulsionStreams(Vector2 rear, Vector2 forward, Vector2 right, float intensity, int timer)
        {
            float[] branchAngles = { 0f, -0.22f, 0.22f };
            float[] branchOffsets = { 0f, -6.5f, 6.5f };
            float[] branchWeights = { 1f, 0.78f, 0.78f };

            for (int i = 0; i < branchAngles.Length; i++)
            {
                float weight = branchWeights[i];
                float sway = (float)Math.Sin(timer * 0.24f + i * 1.1f) * (0.35f + 0.45f * intensity);
                Vector2 direction = (-forward).RotatedBy(branchAngles[i] + sway * 0.01f);
                Vector2 spawnPosition =
                    rear +
                    right * branchOffsets[i] +
                    right * sway +
                    Main.rand.NextVector2Circular(1.1f, 1.1f);

                Particle line = new CustomSpark(
                    spawnPosition,
                    direction * Main.rand.NextFloat(8.5f, 14.5f) * (0.8f + weight * 0.28f) * intensity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.05f, 0.078f) * (0.85f + weight * 0.2f) * intensity,
                    Color.Lerp(new Color(85, 200, 255), Color.White, 0.22f + weight * 0.2f) * 0.76f,
                    new Vector2(1.75f + weight * 0.4f, 0.38f + weight * 0.04f),
                    shrinkSpeed: 0.74f);
                GeneralParticleHandler.SpawnParticle(line);

                if ((timer + i) % 3 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(
                        spawnPosition,
                        Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                        direction * Main.rand.NextFloat(1.8f, 3.4f),
                        100,
                        new Color(95, 210, 255),
                        Main.rand.NextFloat(0.65f, 0.92f));
                    dust.noGravity = true;
                }
            }

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    rear - forward * 2f,
                    -forward * 0.12f,
                    false,
                    5,
                    0.42f + intensity * 0.1f,
                    Color.Lerp(new Color(90, 205, 255), Color.White, 0.3f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        // Thin body lines keep the blade visually tied into the exhaust without making the whole dash noisy.
        private static void SpawnBladeEdgeStreams(Vector2 weaponTip, Vector2 forward, Vector2 right, float intensity, int timer)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float oscillation = (float)Math.Sin(timer * 0.32f + side * 0.9f) * 1.5f;
                Vector2 spawnPosition =
                    weaponTip -
                    forward * Main.rand.NextFloat(3f, 11f) +
                    right * side * (4f + oscillation);

                Particle line = new CustomSpark(
                    spawnPosition,
                    -forward * Main.rand.NextFloat(4.5f, 8f) * intensity + right * side * Main.rand.NextFloat(0.25f, 0.9f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(7, 10),
                    Main.rand.NextFloat(0.034f, 0.052f) * intensity,
                    new Color(95, 210, 255) * 0.58f,
                    new Vector2(1.35f, 0.34f),
                    shrinkSpeed: 0.78f);
                GeneralParticleHandler.SpawnParticle(line);
            }
        }
    }
}
