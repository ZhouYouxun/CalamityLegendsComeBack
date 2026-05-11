using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SilvaHarp
{
    public sealed class SilvaHarp : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 18);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Feather, 12)
                .AddIngredient(ItemID.ChlorophyteBar, 30)
                .AddIngredient(ItemID.GoldBar, 12)
                .AddIngredient(ItemID.SoulofLight, 3)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }
}
