using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    public sealed class SeedOfSilva : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedOfSilva";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 14);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().SeedOfSilvaEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CalamityMod.Items.Accessories.BloomStone>()
                .AddIngredient<CalamityMod.Items.Placeables.Abyss.PlantyMush>(50)
                .AddIngredient<CalamityMod.Items.Materials.PerennialBar>(10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
