using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.General
{
    // 百合：印记队列 +1（共4）；远程伤害 +8%；纯化等级上限提高至 3 级。
    internal sealed class PFLily : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFury";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            PFAccessoryPlayer pf = player.GetModPlayer<PFAccessoryPlayer>();
            pf.BonusMarkSlots += 1;
            pf.PurificationCap = System.Math.Max(pf.PurificationCap, 3);
            pf.RangedDamageBonus += 0.08f;
        }
    }
}
