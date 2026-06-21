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

            // 酸雨泪 (Caustic Tear)
            // 1 Bottled Water (水瓶) @ Near Water (水旁)
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CausticTear", out ModItem causticTear))
                {
                    Recipe causticTearRecipe = Recipe.Create(causticTear.Type);
                    causticTearRecipe.AddIngredient(ItemID.BottledWater, 1);
                    causticTearRecipe.AddCondition(Condition.NearWater);
                    causticTearRecipe.Register();
                }

                // Bobbit Hook: 10 Ruinous Soul + 1 Lunar Hook
                if (calamity.TryFind<ModItem>("BobbitHook", out ModItem bobbitHook) &&
                    calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe bobbitHookRecipe = Recipe.Create(bobbitHook.Type);
                    bobbitHookRecipe.AddIngredient(ruinousSoul.Type, 10);
                    bobbitHookRecipe.AddIngredient(ItemID.LunarHook);
                    bobbitHookRecipe.Register();
                }
            }
        }
    }
}
