using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Teleport_Effects
    {
        internal static void SpawnTeleportBurst(Vector2 startPos, Vector2 targetCenter, Vector2 dashDirection, int strikeIndex, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);

            SoundEngine.PlaySound(SoundID.Item8 with
            {
                Volume = 0.62f,
                Pitch = Main.rand.NextFloat(-0.12f, 0.08f)
            }, startPos);

            // Spawn circular shockwaves (entry portal)
            for (int i = 0; i < 2; i++)
            {
                DirectionalPulseRing portalWave = new DirectionalPulseRing(
                    startPos,
                    Vector2.Zero,
                    Color.Lerp(new Color(95, 206, 255), Color.White, i * 0.25f),
                    new Vector2(1.2f + i * 0.3f, 1.2f + i * 0.3f),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    0.2f + i * 0.05f,
                    0.03f,
                    16 + i * 2);
                GeneralParticleHandler.SpawnParticle(portalWave);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 sparkVel = forward.RotatedByRandom(0.72f) * Main.rand.NextFloat(2.2f, 6.2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    startPos + Main.rand.NextVector2Circular(14f, 14f),
                    sparkVel,
                false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Color.Lerp(new Color(145, 225, 255), Color.White, Main.rand.NextFloat(0.1f, 0.45f))));

                Dust dust = Dust.NewDustPerfect(
                    startPos + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.GemSapphire,
                    sparkVel * 0.45f,
                    100,
                    Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.78f, 1.08f));
                dust.noGravity = true;
                }
        }

        internal static void SpawnTeleportHoldEffects(Projectile projectile, Vector2 targetCenter, int phaseTimer, int windupFrames)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2().SafeNormalize(Vector2.UnitX);
            Vector2 tip = projectile.Center + forward * (50f * projectile.scale);
            float completion = Utils.GetLerpValue(0f, Math.Max(1, windupFrames), phaseTimer, true);

            if (phaseTimer == 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    DirectionalPulseRing ring = new(
                        tip - forward * (4f + i * 6f),
                        forward * 1.2f,
                        Color.Lerp(new Color(55, 175, 255, 0), Color.White, 0.18f + i * 0.18f) * 0.78f,
                        new Vector2(0.7f, 2.2f),
                        forward.ToRotation(),
                        0.18f + i * 0.04f,
                        0.025f,
                        12 + i * 2);
                    GeneralParticleHandler.SpawnParticle(ring);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 velocity = -forward.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.4f, 3.2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    tip + Main.rand.NextVector2Circular(5f, 5f),
                    velocity,
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(new Color(140, 235, 255), Color.White, completion * 0.45f)));
            }
        }

        internal static void SpawnAbortEffects(Vector2 center, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            DirectionalPulseRing ring = new(
                center,
                -forward * 0.6f,
                new Color(70, 170, 255, 0) * 0.55f,
                new Vector2(0.5f, 1.25f),
                forward.ToRotation(),
                0.12f,
                0.02f,
                12);
            GeneralParticleHandler.SpawnParticle(ring);
        }
    }
}
