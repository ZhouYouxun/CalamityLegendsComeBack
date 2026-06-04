using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class TungstenEffect : LeonidMetalEffect
    {
        public override int EffectID => 6;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            Microsoft.Xna.Framework.Vector2 center = meteor.Projectile.Center;
            meteor.Projectile.width = 32;
            meteor.Projectile.height = 32;
            meteor.Projectile.Center = center;
            meteor.Projectile.scale *= 1.25f;
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate + 1, 2);
        }
    }
}
