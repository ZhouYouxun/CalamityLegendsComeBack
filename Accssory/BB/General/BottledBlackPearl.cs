using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledBlackPearl : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.12f;
        protected override int TideCapBonus => 6;
        protected override float FullTideDamageBonus => 0.14f;
        protected override bool WaveInfinitePenetration => true;
        protected override bool BottledBlackPearlEquipped => true;
        protected override int Rarity => ItemRarityID.Yellow;
    }
}
