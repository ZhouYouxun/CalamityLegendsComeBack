using CalamityLegendsComeBack.Weapons.A_Tools.SHPlatform.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.SHPlatform
{
    public class SHPCPlatform : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Placeables";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 200;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SHPCPlatformTile>());
            Item.width = 22;
            Item.height = 16;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            CreateRecipe(2)
                .AddIngredient(ItemID.WoodPlatform, 2)
                .Register();
        }
    }
}
