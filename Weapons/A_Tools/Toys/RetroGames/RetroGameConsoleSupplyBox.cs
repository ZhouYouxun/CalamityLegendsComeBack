using CalamityMod.Rarities;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    public class RetroGameConsoleSupplyBox : ModItem, ILocalizedModType
    {
        //public override string Texture => "CalamityLegendsComeBack/LegendarySupplyBox";
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Tetris>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Game2048>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Snake>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Minesweeper>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<STG>()));
        }
    }
}
