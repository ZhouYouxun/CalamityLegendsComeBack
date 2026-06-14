using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    public class OtherRecipes : ModSystem
    {
        public override void AddRecipes()
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowOtherRecipes != true)
                return;

            // 大自然恩赐 (Nature's Gift)
            // 5 Daybloom (太阳花) + 5 Moonglow (月光草) + 1 Mana Crystal (魔力水晶) @ Work Bench (工作台)
            Recipe recipe = Recipe.Create(ItemID.NaturesGift);
            recipe.AddIngredient(ItemID.Daybloom, 5);
            recipe.AddIngredient(ItemID.Moonglow, 5);
            recipe.AddIngredient(ItemID.ManaCrystal, 1);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}
