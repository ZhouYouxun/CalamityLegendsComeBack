using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledRaft : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.05f;
        protected override int TideCapBonus => 2;
        protected override float FullTideDamageBonus => 0.07f;
        protected override int Rarity => ItemRarityID.Orange;
    }
}
