using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.General
{
    // 稍微绽放的百合：印记队列 +2（共5）；远程伤害 +12%；纯化等级上限提高至 4 级。
    internal sealed class PFSlightlyBloomLily : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFury";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            PFAccessoryPlayer pf = player.GetModPlayer<PFAccessoryPlayer>();
            pf.BonusMarkSlots += 2;
            pf.PurificationCap = System.Math.Max(pf.PurificationCap, 4);
            pf.RangedDamageBonus += 0.12f;
        }
    }
}
