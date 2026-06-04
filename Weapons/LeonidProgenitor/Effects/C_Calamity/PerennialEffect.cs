using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class PerennialEffect : LeonidMetalEffect
    {
        public override int EffectID => 23;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
            meteor.SetState("perennial_seed_timer", Main.rand.Next(12, 20));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            float timer = meteor.GetState("perennial_seed_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 16f : 24f;
                if (Main.myPlayer == projectile.owner)
                {
                    Vector2 spawnPosition = projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitY) * 12f + Main.rand.NextVector2Circular(18f, 18f);
                    int seed = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        spawnPosition,
                        -projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * Main.rand.NextFloat(1.6f, 3.1f),
                        ModContent.ProjectileType<Perennial_BloomSeed>(),
                        System.Math.Max(1, projectile.damage / 4),
                        projectile.knockBack * 0.15f,
                        projectile.owner,
                        -1f,
                        Main.rand.NextFloat(MathHelper.TwoPi));

                    if (seed >= 0 && seed < Main.maxProjectiles)
                        Main.projectile[seed].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("perennial_seed_timer", timer);
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int orb = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<Perennial_HealingOrb>(), 0, 0f, meteor.Projectile.owner);
            if (orb >= 0 && orb < Main.maxProjectiles)
                Main.projectile[orb].DamageType = meteor.Projectile.DamageType;

            int bloomCount = meteor.FromStealthRain ? 5 : 3;
            for (int i = 0; i < bloomCount; i++)
            {
                float angle = MathHelper.TwoPi * i / bloomCount + Main.rand.NextFloat(-0.25f, 0.25f);
                int seed = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(56f, 92f),
                    angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(2.5f, 4.5f),
                    ModContent.ProjectileType<Perennial_BloomSeed>(),
                    System.Math.Max(1, meteor.Projectile.damage / 3),
                    meteor.Projectile.knockBack * 0.2f,
                    meteor.Projectile.owner,
                    target.whoAmI,
                    angle);

                if (seed >= 0 && seed < Main.maxProjectiles)
                    Main.projectile[seed].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
