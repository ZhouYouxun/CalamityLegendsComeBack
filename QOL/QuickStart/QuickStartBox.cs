using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.QuickStart
{
    public class QuickStartBox : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/LegendarySupplyBox";
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 0);
        }

        public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            // 饰品 (Accessories)
            itemLoot.Add(ItemDropRule.Common(ItemID.HorseshoeBundle));
            itemLoot.Add(ItemDropRule.Common(ItemID.TerrasparkBoots));

            // 护甲 (Armor)
            itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumHelmet));
            itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumChainmail));
            itemLoot.Add(ItemDropRule.Common(ItemID.PlatinumGreaves));

            // 药水每个各 50 瓶 (Potions, 50 of each)
            itemLoot.Add(ItemDropRule.Common(ItemID.FeatherfallPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.ObsidianSkinPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.GravitationPotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 1, 50, 50));
            itemLoot.Add(ItemDropRule.Common(ItemID.NightOwlPotion, 1, 50, 50));

            // 工具 (Tools)
            itemLoot.Add(ItemDropRule.Common(ItemID.ReaverShark));
            itemLoot.Add(ItemDropRule.Common(ItemID.MoltenHamaxe));

            // 制造站 (Crafting Stations)
            itemLoot.Add(ItemDropRule.Common(ItemID.TinkerersWorkshop));
            itemLoot.Add(ItemDropRule.Common(ItemID.IronAnvil));
            itemLoot.Add(ItemDropRule.Common(ItemID.WorkBench));
            itemLoot.Add(ItemDropRule.Common(ItemID.Furnace));

            // 物资各 9999 个 (Resources, 9999 of each)
            itemLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 9999, 9999));
            itemLoot.Add(ItemDropRule.Common(ItemID.Glass, 1, 9999, 9999));
            itemLoot.Add(ItemDropRule.Common(ItemID.Wood, 1, 9999, 9999));
            itemLoot.Add(ItemDropRule.Common(ItemID.StoneBlock, 1, 9999, 9999));
        }
    }
}
