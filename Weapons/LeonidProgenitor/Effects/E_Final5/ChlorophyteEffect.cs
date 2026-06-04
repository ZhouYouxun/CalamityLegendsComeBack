using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class ChlorophyteEffect : LeonidMetalEffect
    {
        public override int EffectID => 22;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.EnableSimpleHoming(0.09f, 920f);
        }
    }
}
