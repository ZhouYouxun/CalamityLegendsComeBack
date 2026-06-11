using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class IronEffect : LeonidMetalEffect
    {
        public override int EffectID => 3;

        protected override int EnergyVariant => 2;
        protected override float EnergySizeFactor => 0.9f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 16;

        public override void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DefenseEffectiveness *= 0.5f;
        }
    }
}
