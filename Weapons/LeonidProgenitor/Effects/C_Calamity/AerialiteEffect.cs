using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class AerialiteEffect : LeonidMetalEffect
    {
        public override int EffectID => 11;

        protected override int EnergyVariant => 7;
        protected override float EnergySizeFactor => 0.9f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 9;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.EnableSimpleHoming(0.028f, 680f);
            meteor.Projectile.velocity *= 1.24f;
            meteor.Projectile.extraUpdates += 1;
            meteor.SetState("aerialite_feather_timer", Main.rand.Next(4, 10));
        }

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            Projectile projectile = meteor.Projectile;
            projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.velocity.SafeNormalize(Vector2.UnitY) * System.Math.Max(projectile.velocity.Length(), 15.5f), 0.035f);

            if (Main.rand.NextBool(2))
            {
                Vector2 side = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
                Dust wind = Dust.NewDustPerfect(
                    projectile.Center + side * Main.rand.NextFloat(4f, 14f),
                    DustID.Cloud,
                    -projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f) + side * Main.rand.NextFloat(-0.8f, 0.8f),
                    120,
                    new Color(154, 238, 255),
                    Main.rand.NextFloat(0.7f, 1.05f));
                wind.noGravity = true;
            }

            float timer = meteor.GetState("aerialite_feather_timer") - 1f;
            if (timer <= 0f)
            {
                timer = meteor.FromStealthRain ? 10f : 16f;
                if (Main.myPlayer == projectile.owner)
                {
                    Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
                    Vector2 normal = forward.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
                    int feather = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center + normal * Main.rand.NextFloat(16f, 34f),
                        (forward * 8.5f + normal * Main.rand.NextFloat(2.4f, 4.8f)).RotatedByRandom(0.14f),
                        ModContent.ProjectileType<Aerialite_Feather>(),
                        System.Math.Max(1, projectile.damage / 3),
                        projectile.knockBack * 0.25f,
                        projectile.owner,
                        -normal.X,
                        -normal.Y,
                        Main.rand.NextFloat(18f, 30f));

                    if (feather >= 0 && feather < Main.maxProjectiles)
                        Main.projectile[feather].DamageType = projectile.DamageType;
                }
            }

            meteor.SetState("aerialite_feather_timer", timer);
        }

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int gale = Projectile.NewProjectile(
                meteor.Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<Aerialite_Gale>(),
                System.Math.Max(1, meteor.Projectile.damage / 3),
                0f,
                meteor.Projectile.owner,
                target.whoAmI);

            if (gale >= 0 && gale < Main.maxProjectiles)
                Main.projectile[gale].DamageType = meteor.Projectile.DamageType;
        }
    }
}
