using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class ChlorophyteEffect : LeonidMetalEffect
    {
        public override int EffectID => 22;

        protected override int EnergyVariant => 4;
        protected override float EnergySizeFactor => 0.94f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 11;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            meteor.EnableSimpleHoming(0.09f, 920f);
        }
    }
}
