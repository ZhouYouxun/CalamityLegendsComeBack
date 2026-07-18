using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using CalamityMod.Items.Materials;

namespace CalamityLegendsComeBack.QOL
{
    public class OtherRecipes : ModSystem
    {
        private const string AnyHammerRecipeGroup = "CalamityLegendsComeBack:AnyHammer";

        public override void AddRecipeGroups()
        {
            List<int> hammers = new();
            foreach (string hammerName in new[]
            {
                "WoodenHammer", "CopperHammer", "TinHammer", "IronHammer", "LeadHammer",
                "SilverHammer", "TungstenHammer", "GoldHammer", "PlatinumHammer",
                "CactusHammer", "BorealWoodHammer", "PalmWoodHammer", "RichMahoganyHammer",
                "EbonwoodHammer", "ShadewoodHammer", "PearlwoodHammer", "CandyCaneHammer",
                "SpookyWoodHammer", "PumpkinHammer", "TheBreaker", "Pwnhammer",
                "ChlorophyteWarhammer", "SpectreHamaxe", "ShroomiteHamaxe", "TheAxe"
            })
            {
                if (ItemID.Search.TryGetId(hammerName, out int hammerType))
                    hammers.Add(hammerType);
            }

            RecipeGroup.RegisterGroup(AnyHammerRecipeGroup, new RecipeGroup(
                () => "任何锤子",
                hammers.ToArray()));
        }

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

                // 博比特虫 Hook: 2 Ruinous Soul + 1 Lunar Hook
                if (calamity.TryFind<ModItem>("BobbitHook", out ModItem bobbitHook) &&
                    calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe bobbitHookRecipe = Recipe.Create(bobbitHook.Type);
                    bobbitHookRecipe.AddIngredient(ruinousSoul.Type, 2);
                    bobbitHookRecipe.AddIngredient(ItemID.LunarHook);
                    bobbitHookRecipe.Register();
                }

                // 钨钢屏障生成器 Rover Drive: Energy Core + 2 Wulfrum Metal Scrap + 5 Chains @ Iron/Lead Anvil.
                if (calamity.TryFind<ModItem>("RoverDrive", out ModItem roverDrive) &&
                    calamity.TryFind<ModItem>("WulfrumMetalScrap", out ModItem wulfrumMetalScrap))
                {
                    Recipe roverDriveRecipe = Recipe.Create(roverDrive.Type);
                    roverDriveRecipe.AddIngredient<EnergyCore>();
                    roverDriveRecipe.AddIngredient(wulfrumMetalScrap.Type, 2);
                    roverDriveRecipe.AddIngredient(ItemID.Chain, 5);
                    roverDriveRecipe.AddTile(TileID.Anvils);
                    roverDriveRecipe.Register();
                }

                // 深渊碾碎者Depth Crusher: 10 Abyss Gravel + 10 Silver/Tungsten Bars + any hammer.
                if (calamity.TryFind<ModItem>("DepthCrusher", out ModItem depthCrusher) &&
                    calamity.TryFind<ModItem>("AbyssGravel", out ModItem abyssGravel))
                {
                    RegisterDepthCrusherRecipe(depthCrusher.Type, abyssGravel.Type, ItemID.SilverBar);
                    RegisterDepthCrusherRecipe(depthCrusher.Type, abyssGravel.Type, ItemID.TungstenBar);
                }

                // 乌贼之哀歌Calamari's Lament: Black Ink + 10 Planty Mush + 10 Ruinous Soul + Terrarium.
                if (calamity.TryFind<ModItem>("CalamarisLament", out ModItem calamarisLament) &&
                    calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush) &&
                    calamity.TryFind<ModItem>("RuinousSoul", out ModItem lamentRuinousSoul))
                {
                    Recipe calamarisLamentRecipe = Recipe.Create(calamarisLament.Type);
                    calamarisLamentRecipe.AddIngredient(ItemID.BlackInk);
                    calamarisLamentRecipe.AddIngredient(plantyMush.Type, 10);
                    calamarisLamentRecipe.AddIngredient(lamentRuinousSoul.Type, 10);
                    calamarisLamentRecipe.AddIngredient(ItemID.Terrarium);
                    calamarisLamentRecipe.Register();
                }

                RegisterReaperWeaponRecipe(calamity, "SoulEdge");
                RegisterReaperWeaponRecipe(calamity, "DeepSeaDumbbell");

                // 存疑镀层 Dubious Plating: 5 任意铁锭 + 5 任意铜锭 + 5 玻璃块 -> 20个
                if (calamity.TryFind<ModItem>("DubiousPlating", out ModItem dubiousPlating))
                {
                    Recipe dubiousPlatingRecipe = Recipe.Create(dubiousPlating.Type, 20);
                    dubiousPlatingRecipe.AddRecipeGroup("IronBar", 5);
                    dubiousPlatingRecipe.AddRecipeGroup("AnyCopperBar", 5);
                    dubiousPlatingRecipe.AddIngredient(ItemID.Glass, 5);
                    dubiousPlatingRecipe.Register();
                }

                // 诡秘线路板 Mysterious Circuitry: 5 任意铁锭 + 5 任意铜锭 + 5 电线 -> 10个（与其微光转化的批量一致）
                if (calamity.TryFind<ModItem>("MysteriousCircuitry", out ModItem mysteriousCircuitry))
                {
                    Recipe mysteriousCircuitryRecipe = Recipe.Create(mysteriousCircuitry.Type, 10);
                    mysteriousCircuitryRecipe.AddRecipeGroup("IronBar", 5);
                    mysteriousCircuitryRecipe.AddRecipeGroup("AnyCopperBar", 5);
                    mysteriousCircuitryRecipe.AddIngredient(ItemID.Wire, 5);
                    mysteriousCircuitryRecipe.Register();
                }

                // 死灵骨髓法杖 Staff of Necrosteocytes: 1 魔力水晶 + 20 骨头
                if (calamity.TryFind<ModItem>("StaffOfNecrosteocytes", out ModItem necrosteocytesStaff))
                {
                    Recipe necrosteocytesRecipe = Recipe.Create(necrosteocytesStaff.Type);
                    necrosteocytesRecipe.AddIngredient(ItemID.ManaCrystal, 1);
                    necrosteocytesRecipe.AddIngredient(ItemID.Bone, 20);
                    necrosteocytesRecipe.Register();
                }

                // Anechoic Plating: 50 任意铁锭 + 1 天使雕像
                if (calamity.TryFind<ModItem>("AnechoicPlating", out ModItem anechoicPlating))
                {
                    Recipe anechoicPlatingRecipe = Recipe.Create(anechoicPlating.Type);
                    anechoicPlatingRecipe.AddRecipeGroup("IronBar", 50);
                    anechoicPlatingRecipe.AddIngredient(ItemID.AngelStatue, 1);
                    anechoicPlatingRecipe.Register();
                }
            }
        }

        private static void RegisterReaperWeaponRecipe(Mod calamity, string weaponName)
        {
            if (!calamity.TryFind<ModItem>(weaponName, out ModItem weapon) ||
                !calamity.TryFind<ModItem>("ReaperTooth", out ModItem reaperTooth) ||
                !calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
            {
                return;
            }

            Recipe recipe = Recipe.Create(weapon.Type);
            recipe.AddIngredient(reaperTooth.Type, 5);
            recipe.AddIngredient(ruinousSoul.Type, 5);
            recipe.Register();
        }

        private static void RegisterDepthCrusherRecipe(int depthCrusherType, int abyssGravelType, int barType)
        {
            Recipe recipe = Recipe.Create(depthCrusherType);
            recipe.AddIngredient(abyssGravelType, 10);
            recipe.AddIngredient(barType, 10);
            recipe.AddRecipeGroup(AnyHammerRecipeGroup);
            recipe.Register();
        }
    }
}
