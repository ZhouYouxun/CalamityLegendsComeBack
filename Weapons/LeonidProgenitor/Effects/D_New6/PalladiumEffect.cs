using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.D_New6
{
    public class PalladiumEffect : LeonidMetalEffect
    {
        public override int EffectID => 15;

        protected override int EnergyVariant => 6;
        protected override float EnergySizeFactor => 0.92f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 13;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            owner.GetModPlayer<LeonidProgenitorPlayer>().TryTriggerPalladiumHeal(4);
        }
    }
}
