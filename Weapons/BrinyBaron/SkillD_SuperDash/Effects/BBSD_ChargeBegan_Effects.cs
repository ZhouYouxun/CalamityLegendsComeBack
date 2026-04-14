using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_ChargeBegan_Effects
    {
        internal static void SpawnChargeStartEffects(Projectile projectile, Player owner)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = projectile.Center + forward * (46f * projectile.scale);

            for (int i = 0; i < 3; i++)
            {
                float completion = i / 2f;
                GeneralParticleHandler.SpawnParticle(
                    new DirectionalPulseRing(
                        tip + forward * MathHelper.Lerp(62f, 18f, completion),
                        -forward * MathHelper.Lerp(1.8f, 0.8f, completion),
                        Color.Lerp(new Color(95, 206, 255), new Color(255, 226, 126), completion * 0.35f),
                        new Vector2(1.05f + completion * 0.35f, 2.6f + completion * 0.85f),
                        forward.ToRotation(),
                        0.18f,
                        0.04f,
                        14 + i * 2));
            }

            for (int i = 0; i < 12; i++)
            {
                float side = Main.rand.NextBool() ? 1f : -1f;
                Vector2 spawnPos = tip + forward * Main.rand.NextFloat(18f, 54f) + right * side * Main.rand.NextFloat(4f, 20f);
                Vector2 inwardVelocity = (tip - spawnPos).SafeNormalize(forward) * Main.rand.NextFloat(2.2f, 5.2f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.GemSapphire,
                    inwardVelocity,
                    100,
                    new Color(132, 220, 255),
                    Main.rand.NextFloat(0.82f, 1.18f));
                dust.noGravity = true;
                dust.fadeIn = 1.12f;
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 spawnPos = tip + forward * 30f + right * side * 14f;
                Vector2 wakeVelocity = (tip - spawnPos).SafeNormalize(forward) * 1.6f;

                Gore bubble = Gore.NewGorePerfect(
                    projectile.GetSource_FromAI(),
                    spawnPos + right * side * 1.6f,
                    projectile.velocity * 0.1f + wakeVelocity + Main.rand.NextVector2Circular(0.2f, 0.2f),
                    Main.rand.NextBool(3) ? 412 : 411);
                bubble.timeLeft = 8 + Main.rand.Next(6);
                bubble.scale = Main.rand.NextFloat(0.6f, 0.9f);

                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        spawnPos,
                        wakeVelocity * 1.2f,
                        new Color(88, 170, 205),
                        23,
                        Main.rand.NextFloat(0.42f, 0.68f),
                        0.65f));
            }
        }
    }
}
