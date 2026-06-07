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
        }

        internal static void SpawnAbortEffects(Vector2 center, Vector2 direction)
        {
        }
    }
}
