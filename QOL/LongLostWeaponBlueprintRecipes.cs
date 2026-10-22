using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    internal sealed class LongLostWeaponBlueprintRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            foreach (int weaponType in LongLostWeaponBlueprints.GetBlueprintRecipeWeapons())
            {
                Recipe.Create(weaponType)
                    .AddIngredient(ModContent.ItemType<LongLostWeaponBlueprints>())
                    .Register();
            }
        }
    }
}
