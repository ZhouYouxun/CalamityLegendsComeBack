using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class TungstenEffect : LeonidMetalEffect
    {
        public override int EffectID => 6;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.Projectile.scale *= 1.18f;
            meteor.Projectile.penetrate = System.Math.Max(meteor.Projectile.penetrate, 2);
        }
    }
}
