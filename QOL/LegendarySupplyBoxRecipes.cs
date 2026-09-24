using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    internal sealed class LegendarySupplyBoxRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            foreach (int weaponType in LegendarySupplyBox.GetBoxRecipeWeapons())
            {
                Recipe.Create(weaponType)
                    .AddIngredient(ModContent.ItemType<LegendarySupplyBox>())
                    .Register();
            }
        }
    }
}
