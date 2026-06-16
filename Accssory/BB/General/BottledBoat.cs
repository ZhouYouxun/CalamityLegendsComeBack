using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledBoat : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.08f;
        protected override int TideCapBonus => 4;
        protected override float FullTideDamageBonus => 0.10f;
        protected override bool ShurikenBoatEnhanced => true;
        protected override int Rarity => ItemRarityID.LightRed;
    }
}
