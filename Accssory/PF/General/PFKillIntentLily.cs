using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.General
{
    // 杀意的百合：印记队列 +4（共7）；远程伤害 +22%；纯化等级上限提高至 6 级。
    internal sealed class PFKillIntentLily : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/PF/General/PFKillIntentLily";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 20);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            PFAccessoryPlayer pf = player.GetModPlayer<PFAccessoryPlayer>();
            pf.BonusMarkSlots += 4;
            pf.PurificationCap = System.Math.Max(pf.PurificationCap, 6);
            pf.RangedDamageBonus += 0.22f;
        }
    }
}
