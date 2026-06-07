using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_ChargeFiniah_Effects
    {
        internal static void SpawnChargeReadyEffects(Projectile projectile, Player owner)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 tip = projectile.Center + forward * (50f * projectile.scale);

            // Layered expanding pulse rings representing a powerful shockwave
            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing ring = new DirectionalPulseRing(
                    tip,
                    Vector2.Zero,
                    Color.Lerp(new Color(80, 200, 255, 0), Color.White, i * 0.35f) * 0.85f,
                    new Vector2(0.3f + i * 0.18f, 0.3f + i * 0.18f),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    0.22f + i * 0.06f,
                    0.025f,
                    16 + i * 3);
                GeneralParticleHandler.SpawnParticle(ring);
            }

            // Burst of cyan line particles shooting outwards in all directions
            const int lineCount = 24;
            for (int i = 0; i < lineCount; i++)
            {
                float angle = MathHelper.TwoPi * i / lineCount;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4.5f, 9.5f);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    tip,
                    velocity,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.35f, 0.55f),
                    Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat())));
            }

            // Splash of water/sapphire dust
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1f, 2.5f);
                Dust dust = Dust.NewDustPerfect(
                    tip,
                    Main.rand.NextBool() ? DustID.Water : DustID.GemSapphire,
                    velocity,
                    100,
                    new Color(90, 210, 255),
                    Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }
    }
}
