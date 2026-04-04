using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    internal static class BBShuriken_Fishron_Effects
    {
        public static void SpawnFlight(Projectile projectile, float sizeScale)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 16f + projectile.identity * 0.55f;
            float orbitRadius = projectile.width * 0.36f;
            float helixDepth = projectile.width * 0.12f;

            for (int arm = 0; arm < 4; arm++)
            {
                if (!Main.rand.NextBool(2))
                    continue;

                float armPhase = phase + MathHelper.TwoPi * arm / 4f;
                Vector2 orbitOffset =
                    right * (float)Math.Cos(armPhase) * orbitRadius +
                    forward * (float)Math.Sin(armPhase) * helixDepth;

                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        projectile.Center + orbitOffset,
                        right * (float)-Math.Sin(armPhase) * 0.2f + forward * (float)Math.Cos(armPhase) * 0.1f,
                        false,
                        8,
                        0.48f * sizeScale,
                        arm % 2 == 0 ? new Color(75, 190, 255) : Color.Cyan,
                        true,
                        false,
                        true));

                if (Main.rand.NextBool(3))
                {
                    Dust frost = Dust.NewDustPerfect(projectile.Center + orbitOffset * 0.92f, DustID.Frost, -forward * 0.4f);
                    frost.noGravity = true;
                    frost.color = new Color(220, 250, 255);
                    frost.scale = Main.rand.NextFloat(0.82f, 1.08f) * sizeScale;
                }
            }
        }

        public static void SpawnHitBurst(Projectile projectile, NPC target, Vector2 hitForward, float sizeScale)
        {
            Vector2 hitRight = hitForward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 4; i++)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    target.Center + Main.rand.NextVector2Circular(projectile.width * 0.18f, projectile.width * 0.18f),
                    hitForward * Main.rand.NextFloat(0.4f, 1.3f) + hitRight * Main.rand.NextFloat(-0.7f, 0.7f),
                    false,
                    8,
                    0.52f * sizeScale,
                    i % 2 == 0 ? Color.DeepSkyBlue : Color.Cyan,
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }
}
