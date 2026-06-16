using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledAircraftCarrier : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.15f;
        protected override int TideCapBonus => 9;
        protected override float FullTideDamageBonus => 0.18f;
        protected override int Rarity => ItemRarityID.Cyan;
    }
}
