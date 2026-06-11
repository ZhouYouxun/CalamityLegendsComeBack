using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class TitaniumEffect : LeonidMetalEffect
    {
        public override int EffectID => 19;

        protected override int EnergyVariant => 7;
        protected override float EnergySizeFactor => 1f;
        protected override int EnergyMoteCount => 4;
        protected override int EnergyDustInterval => 15;
        protected override float EnergyOpacity => 0.25f;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            owner.GetModPlayer<LeonidProgenitorPlayer>().ActivateTitaniumStompers(240);
        }
    }
}
