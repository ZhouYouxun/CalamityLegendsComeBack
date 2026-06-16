using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.PastLingering
{
    public sealed class PastLingering : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/ChangeZS/PastLingering/PastLingering";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 16);
            Item.rare = ItemRarityID.Cyan;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().PastLingeringEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Phantasm)
                .AddIngredient(ItemID.LunarBar, 12)
                .AddIngredient(ItemID.FragmentVortex, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
