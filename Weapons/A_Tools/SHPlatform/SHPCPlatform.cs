using CalamityLegendsComeBack.Weapons.A_Tools.SHPlatform.Tiles;
using CalamityMod.Items.Materials;
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
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SHPCPlatformTile>());
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
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.SoulofNight, 5)
                .Register();
        }
    }
}
