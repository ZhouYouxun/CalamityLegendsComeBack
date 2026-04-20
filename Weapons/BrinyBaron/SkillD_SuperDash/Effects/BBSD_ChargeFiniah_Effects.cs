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
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = projectile.Center + forward * (48f * projectile.scale);

            for (int i = 0; i < 12; i++)
            {
                Vector2 sparkleVelocity = forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.8f, 4.8f);
                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        tip,
                        sparkleVelocity,
                        false,
                        14,
                        0.22f,
                        Color.Gold,
                        new Vector2(1.8f, 0.45f),
                        true,
                        false));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float spread = MathHelper.Lerp(0.18f, 0.8f, i / 3f) * side;
                    float crest = 0.5f + 0.5f * (float)Math.Sin(i * 1.2f + Main.GlobalTimeWrappedHourly * 8f);
                    Vector2 spawnPos = tip + forward * (8f + i * 8f);
                    Vector2 wakeVelocity = forward.RotatedBy(spread) * MathHelper.Lerp(1.2f, 3.2f, crest) + right * side * (0.55f + 0.35f * crest);

                    Gore bubble = Gore.NewGorePerfect(
                        projectile.GetSource_FromAI(),
                        spawnPos + right * side * (1.4f + crest * 0.9f),
                        projectile.velocity * 0.2f + wakeVelocity * 0.85f + Main.rand.NextVector2Circular(0.35f, 0.35f),
                        Main.rand.NextBool(3) ? 412 : 411);
                    bubble.timeLeft = 8 + Main.rand.Next(6);
                    bubble.scale = Main.rand.NextFloat(0.6f, 1f) * (1.05f + crest * 0.35f);
                }
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 dustVelocity = forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 5.2f);
                Dust dust = Dust.NewDustPerfect(
                    tip + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? DustID.GemTopaz : DustID.Frost,
                    dustVelocity,
                    100,
                    Color.Lerp(new Color(255, 224, 135), new Color(150, 222, 255), Main.rand.NextFloat(0.25f)),
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
                dust.fadeIn = 1.25f;
            }

            for (int arm = -1; arm <= 1; arm += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    float arc = MathHelper.Lerp(0.16f, 0.78f, t) * arm;
                    Vector2 flareVelocity = forward.RotatedBy(arc) * MathHelper.Lerp(1.2f, 3.2f, t) + right * arm * (0.2f + 0.35f * t);
                    Vector2 flarePos = tip + forward * (6f + 12f * t);

                    GeneralParticleHandler.SpawnParticle(
                        new LineParticle(
                            flarePos,
                            flareVelocity,
                            false,
                            12,
                            MathHelper.Lerp(0.18f, 0.34f, 1f - t),
                            Color.Lerp(new Color(98, 196, 255), new Color(255, 236, 166), t * 0.42f)));

                    GeneralParticleHandler.SpawnParticle(
                        new GlowOrbParticle(
                            flarePos + right * arm * (2f + 4f * t),
                            flareVelocity * 0.16f,
                            false,
                            10,
                            MathHelper.Lerp(0.2f, 0.12f, t),
                            Color.Lerp(new Color(125, 228, 255), Color.White, 0.3f),
                            true,
                            false,
                            true));
                }
            }
        }
    }
}
