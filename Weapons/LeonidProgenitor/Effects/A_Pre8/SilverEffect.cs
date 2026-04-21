using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class SilverEffect : LeonidMetalEffect
    {
        public override int EffectID => 5;

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            if (meteor.HasFlag("silver_split") || meteor.Projectile.timeLeft > 190)
                return;

            meteor.SetFlag("silver_split");

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 velocity = meteor.Projectile.velocity.RotatedBy(MathHelper.ToRadians(16f * i)) * 0.92f;
                int split = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.Projectile.Center, velocity, ModContent.ProjectileType<LeonidSplitMeteor>(), meteor.Projectile.damage / 2, meteor.Projectile.knockBack, meteor.Projectile.owner);
                if (split >= 0 && split < Main.maxProjectiles)
                    Main.projectile[split].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
