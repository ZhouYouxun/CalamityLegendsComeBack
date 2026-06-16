using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.General
{
    // 绽放的百合：印记队列 +3（共6）；远程伤害 +16%；纯化等级上限提高至 5 级。
    internal sealed class PFBloomLily : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/PF/General/PFBloomLily";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            PFAccessoryPlayer pf = player.GetModPlayer<PFAccessoryPlayer>();
            pf.BonusMarkSlots += 3;
            pf.PurificationCap = System.Math.Max(pf.PurificationCap, 5);
            pf.RangedDamageBonus += 0.16f;
        }
    }
}
