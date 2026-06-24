using CalamityLegendsComeBack.Weapons.SHPC.SHPlatform.Tiles;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.SHPlatform
{
    public class SHPPPlatform : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Placeables";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SHPPPlatformTile>());
            Item.width = 22;
            Item.height = 16;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient<MysteriousCircuitry>(10)
                .Register();
        }
    }
}
