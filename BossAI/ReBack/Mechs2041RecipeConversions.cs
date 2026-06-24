using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.BossAI.ReBack.Prime2041;

namespace CalamityLegendsComeBack.BossAI.ReBack
{
    public sealed class Mechs2041RecipeConversions : ModSystem
    {
        public override void AddRecipes()
        {
            RegisterVanillaConversion<Prime2041Summoner>(ItemID.MechanicalSkull);
            RegisterVanillaConversion<Destroyer2041Summoner>(ItemID.MechanicalWorm);
            RegisterVanillaConversion<Twins2041Summoner>(ItemID.MechanicalEye);
        }

        private static void RegisterVanillaConversion<TModItem>(int vanillaItemType) where TModItem : ModItem
        {
            Recipe.Create(vanillaItemType)
                .AddIngredient(ModContent.ItemType<TModItem>())
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
