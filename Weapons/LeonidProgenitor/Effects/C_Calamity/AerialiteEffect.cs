using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.C_Calamity
{
    public class AerialiteEffect : LeonidMetalEffect
    {
        public override int EffectID => 11;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.DisableGravity();
            meteor.Projectile.velocity *= 1.2f;
        }
    }
}
