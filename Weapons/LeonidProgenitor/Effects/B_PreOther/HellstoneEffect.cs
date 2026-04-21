using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class HellstoneEffect : LeonidMetalEffect
    {
        public override int EffectID => 13;

        public override void OnKill(LeonidCometSmall meteor, Player owner, int timeLeft)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int explosion = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.Projectile.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<LeonidShockwave>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
            if (explosion >= 0 && explosion < Main.maxProjectiles)
            {
                Main.projectile[explosion].scale = 1.45f;
                Main.projectile[explosion].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
