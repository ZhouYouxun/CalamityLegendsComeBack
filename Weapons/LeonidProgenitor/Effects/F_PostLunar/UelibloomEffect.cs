using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.F_PostLunar
{
    public class UelibloomEffect : LeonidMetalEffect
    {
        public override int EffectID => 29;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 3);
            meteor.EnableSimpleHoming(0.045f, 840f);
            meteor.SetState("uelibloom_thorn_timer", Main.rand.Next(10, 16));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            float timer = meteor.GetState("uelibloom_thorn_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 10f : 17f;
                if (Main.myPlayer == projectile.owner)
                {
                    Vector2 normal = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
                    int thorn = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center + normal * Main.rand.NextFloat(28f, 54f),
                        projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.32f) * Main.rand.NextFloat(8f, 12f),
                        ModContent.ProjectileType<Uelibloom_Thorn>(),
                        System.Math.Max(1, projectile.damage / 4),
                        projectile.knockBack * 0.2f,
                        projectile.owner,
                        -1f,
                        Main.rand.NextFloat(MathHelper.TwoPi),
                        0f);

                    if (thorn >= 0 && thorn < Main.maxProjectiles)
                        Main.projectile[thorn].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("uelibloom_thorn_timer", timer);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(9f, 9f), DustID.GrassBlades, -projectile.velocity * 0.04f, 100, new Color(132, 255, 90), Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int thornCount = meteor.FromStealthRain ? 8 : 6;
            for (int i = 0; i < thornCount; i++)
            {
                float angle = MathHelper.TwoPi * i / thornCount;
                Vector2 spawnPosition = target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(92f, 138f);
                Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(5.8f, 8.8f);
                int thorn = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<Uelibloom_Thorn>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    meteor.Projectile.knockBack * 0.25f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    angle,
                    1f);

                if (thorn >= 0 && thorn < Main.maxProjectiles)
                    Main.projectile[thorn].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
