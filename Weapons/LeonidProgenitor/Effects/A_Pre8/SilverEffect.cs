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
                Vector2 velocity = meteor.Projectile.velocity.RotatedBy(MathHelper.ToRadians(18f * i)) * 0.92f;
                int split = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    meteor.Projectile.Center + velocity.SafeNormalize(Vector2.UnitX) * 8f,
                    velocity,
                    ModContent.ProjectileType<LeonidCometSmall>(),
                    meteor.Projectile.damage / 2,
                    meteor.Projectile.knockBack,
                    meteor.Projectile.owner,
                    meteor.PrimaryEffectID,
                    meteor.SecondaryEffectID,
                    meteor.SpawnFlags | LeonidCometSmall.SilverSplitFlag);

                if (split >= 0 && split < Main.maxProjectiles)
                    Main.projectile[split].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
