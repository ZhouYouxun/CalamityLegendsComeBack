using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.B_PreOther
{
    public class CrimtaneEffect : DemoniteEffect
    {
        public override int EffectID => 10;

        protected override int EnergyVariant => 6;
        protected override float EnergySizeFactor => 0.92f;
        protected override int EnergyMoteCount => 3;
        protected override int EnergyDustInterval => 11;
        protected override float EnergySpinOffset => 0.2f;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnWisps(meteor, target, true);
        }
    }
}
