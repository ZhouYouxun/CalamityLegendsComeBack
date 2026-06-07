using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Charge_Effects
    {
        internal static void SpawnChargingEffects(Projectile projectile, Player owner, Vector2 focusPoint, NPC target, float chargeCompletion, int timer)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 tip = projectile.Center + forward * (50f * projectile.scale);

            // Periodically emit expanding water pulse rings from the weapon tip to simulate ocean waves building up
            int ringInterval = Math.Max(8, 20 - (int)(chargeCompletion * 10f));
            if (timer % ringInterval == 0)
            {
                DirectionalPulseRing ring = new DirectionalPulseRing(
                    tip,
                    Vector2.Zero,
                    Color.Lerp(new Color(60, 180, 255, 0), new Color(130, 230, 255, 0), chargeCompletion) * (0.35f + chargeCompletion * 0.45f),
                    new Vector2(0.35f, 0.35f),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    0.08f + chargeCompletion * 0.12f,
                    0.02f,
                    14 + (int)(chargeCompletion * 6));
                GeneralParticleHandler.SpawnParticle(ring);
            }

            // Inward swirling particles converging onto the sword tip
            int particleSpawnCount = 1 + (int)(chargeCompletion * 3f);
            for (int i = 0; i < particleSpawnCount; i++)
            {
                float radius = MathHelper.Lerp(50f, 160f - chargeCompletion * 40f, (float)Main.rand.NextDouble());
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 spawnPos = tip + angle.ToRotationVector2() * radius;

                // Calculate direction towards the tip
                Vector2 toTip = (tip - spawnPos).SafeNormalize(Vector2.UnitX);
                // Add a perpendicular tangent component for a swirling/spiral path
                float swirlDir = Main.rand.NextBool() ? 1f : -1f;
                Vector2 tangent = toTip.RotatedBy(MathHelper.PiOver2 * swirlDir);

                // Speed increases slightly as they get closer/as charge builds up
                float inwardSpeed = Main.rand.NextFloat(2.5f, 6f);
                float tangentSpeed = Main.rand.NextFloat(1.2f, 3.5f);
                Vector2 velocity = toTip * inwardSpeed + tangent * tangentSpeed;

                Color particleColor = Color.Lerp(new Color(75, 195, 255), Color.Cyan, Main.rand.NextFloat());

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        spawnPos,
                        velocity,
                        false,
                        Main.rand.Next(16, 26),
                        Main.rand.NextFloat(0.18f, 0.32f) * (0.7f + chargeCompletion * 0.4f),
                        particleColor,
                        true,
                        false,
                        true));
                }
                else
                {
                    Dust dust = Dust.NewDustPerfect(
                        spawnPos,
                        Main.rand.NextBool() ? DustID.Water : (Main.rand.NextBool() ? DustID.Frost : DustID.GemSapphire),
                        velocity,
                        100,
                        particleColor,
                        Main.rand.NextFloat(0.8f, 1.25f));
                    dust.noGravity = true;
                }
            }
        }
    }
}
