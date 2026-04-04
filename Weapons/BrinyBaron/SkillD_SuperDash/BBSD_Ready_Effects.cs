using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns the one-shot burst that fires when charging completes.
    // It keeps the same five-lane geometry as the charge phase, but compresses it into a bright release spike.
    internal static class BBSD_Ready_Effects
    {
        private static readonly float[] BurstAngles =
        {
            0f,
            0f,
            0f,
            -MathHelper.PiOver4,
            -MathHelper.PiOver4,
            MathHelper.PiOver4,
            MathHelper.PiOver4
        };

        private static readonly float[] BurstOffsets =
        {
            0f,
            -2f,
            2f,
            -5.5f,
            -10.5f,
            5.5f,
            10.5f
        };

        private static readonly float[] BurstWeights =
        {
            1f,
            0.94f,
            0.94f,
            0.8f,
            0.66f,
            0.8f,
            0.66f
        };

        private static Vector2 BladeForward(Projectile projectile) => (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();

        internal static void SpawnChargeReadyBurst(Projectile projectile, Vector2 weaponTip)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = BladeForward(projectile);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            SpawnReleaseLances(weaponTip, forward, right);
            SpawnReleasePulse(weaponTip, forward);
            SpawnReleaseCore(weaponTip, forward, right);
            SpawnReleaseDust(weaponTip, forward, right);
        }

        private static void SpawnReleaseLances(Vector2 weaponTip, Vector2 forward, Vector2 right)
        {
            for (int i = 0; i < BurstAngles.Length; i++)
            {
                float weight = BurstWeights[i];
                Vector2 direction = forward.RotatedBy(BurstAngles[i]);
                Vector2 spawnPosition =
                    weaponTip +
                    right * BurstOffsets[i] +
                    forward * Main.rand.NextFloat(2f, 7f) +
                    Main.rand.NextVector2Circular(1.2f, 1.2f);

                Particle line = new CustomSpark(
                    spawnPosition,
                    direction * Main.rand.NextFloat(12f, 18.5f) * (0.8f + weight * 0.34f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.062f, 0.092f) * (0.82f + weight * 0.22f),
                    Color.Lerp(new Color(90, 210, 255), Color.White, 0.34f + weight * 0.3f) * 0.82f,
                    new Vector2(
                        1.95f + weight * 0.75f,
                        0.45f + weight * 0.06f),
                    shrinkSpeed: 0.72f);
                GeneralParticleHandler.SpawnParticle(line);
            }
        }

        private static void SpawnReleasePulse(Vector2 weaponTip, Vector2 forward)
        {
            DirectionalPulseRing frontPulse = new DirectionalPulseRing(
                weaponTip + forward * 8f,
                forward * 0.5f,
                Color.Lerp(new Color(110, 228, 255), Color.White, 0.5f),
                new Vector2(0.62f, 1.68f),
                forward.ToRotation(),
                0.18f,
                0.017f,
                15);
            GeneralParticleHandler.SpawnParticle(frontPulse);

            DirectionalPulseRing backPulse = new DirectionalPulseRing(
                weaponTip - forward * 5f,
                forward * 0.18f,
                new Color(80, 190, 255) * 0.8f,
                new Vector2(0.48f, 1.18f),
                forward.ToRotation(),
                0.12f,
                0.013f,
                12);
            GeneralParticleHandler.SpawnParticle(backPulse);
        }

        private static void SpawnReleaseCore(Vector2 weaponTip, Vector2 forward, Vector2 right)
        {
            for (int i = 0; i < 3; i++)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    weaponTip + forward * Main.rand.NextFloat(4f, 12f) + right * Main.rand.NextFloat(-3f, 3f),
                    forward * Main.rand.NextFloat(0.18f, 0.6f),
                    false,
                    7,
                    0.78f + Main.rand.NextFloat(0.18f),
                    i == 0 ? Color.White : new Color(105, 220, 255),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            for (int i = 0; i < 4; i++)
            {
                CritSpark spark = new CritSpark(
                    weaponTip + forward * Main.rand.NextFloat(8f, 18f) + right * Main.rand.NextFloat(-5f, 5f),
                    forward.RotatedBy(Main.rand.NextFloat(-0.18f, 0.18f)) * Main.rand.NextFloat(4.5f, 7.5f),
                    Color.White,
                    Color.LightBlue,
                    0.72f,
                    12);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private static void SpawnReleaseDust(Vector2 weaponTip, Vector2 forward, Vector2 right)
        {
            for (int i = 0; i < 14; i++)
            {
                float spread = MathHelper.Lerp(-0.42f, 0.42f, i / 13f);
                Vector2 direction = forward.RotatedBy(spread);
                Vector2 velocity = direction * Main.rand.NextFloat(6f, 13f) + right * spread * 4f;

                Dust water = Dust.NewDustPerfect(
                    weaponTip + right * spread * 8f,
                    DustID.Water,
                    velocity,
                    100,
                    new Color(90, 205, 255),
                    Main.rand.NextFloat(0.9f, 1.25f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        weaponTip,
                        DustID.Frost,
                        velocity * 0.68f,
                        100,
                        new Color(220, 250, 255),
                        Main.rand.NextFloat(0.75f, 1.05f));
                    frost.noGravity = true;
                }
            }
        }
    }
}
