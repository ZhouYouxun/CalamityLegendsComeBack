using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class HellstoneEffect : LeonidMetalEffect
    {
        public override int EffectID => 13;

        protected override int EnergyVariant => 2;
        protected override float EnergySizeFactor => 1.05f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 10;
        protected override float EnergyOpacity => 0.26f;

        public override void OnKill(LeonidCometSmall meteor, Player owner, int timeLeft)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int explosion = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.Projectile.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<FuckYou>(), meteor.Projectile.damage, 0f, meteor.Projectile.owner, 132f);
            if (explosion >= 0 && explosion < Main.maxProjectiles)
            {
                Main.projectile[explosion].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
