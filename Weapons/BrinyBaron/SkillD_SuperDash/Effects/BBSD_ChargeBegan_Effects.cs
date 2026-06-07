using System;
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

            Lighting.AddLight(tip, new Vector3(0.12f, 0.38f, 0.55f));
        }
    }
}
