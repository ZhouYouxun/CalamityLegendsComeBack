using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.FurnitureMonolith;
using CalamityMod.Items.Weapons.Summon;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    internal class SHPC_AdditionGI : ModSystem
    {
        public override void AddRecipes()
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowMassMaterialRecipes != true)
                return;

            RegisterEarlyGameRecipes();
            RegisterMidGameRecipes();
            RegisterLateGameRecipes();
            RegisterEctoplasmDuplicationRecipes();
            RegisterLunarFragmentRecipes();
        }

        private static void RegisterEarlyGameRecipes()
        {
            // 前期材料：钨钢废料 + 铁链与石块，在工作台复制。
            Recipe recipeWulfrumScrap = Recipe.Create(ModContent.ItemType<WulfrumMetalScrap>(), 2);
            recipeWulfrumScrap.AddIngredient(ModContent.ItemType<WulfrumMetalScrap>());
            recipeWulfrumScrap.AddIngredient(ItemID.Chain);
            recipeWulfrumScrap.AddIngredient(ItemID.StoneBlock, 5);
            recipeWulfrumScrap.AddTile(TileID.WorkBenches);
            recipeWulfrumScrap.Register();

            // 前期材料：能量核心由钨钢废料在工作台制作。
            Recipe recipeEnergyCore = Recipe.Create(ModContent.ItemType<EnergyCore>());
            recipeEnergyCore.AddIngredient(ModContent.ItemType<WulfrumMetalScrap>(), 2);
            recipeEnergyCore.AddTile(TileID.WorkBenches);
            recipeEnergyCore.Register();

            // 前期材料：风暴之颚可以由风颚法杖拆成十个，在工作台制作。
            Recipe recipeStormlionMandibleFromStaff = Recipe.Create(ModContent.ItemType<StormlionMandible>(), 10);
            recipeStormlionMandibleFromStaff.AddIngredient(ModContent.ItemType<StormjawStaff>());
            recipeStormlionMandibleFromStaff.AddTile(TileID.WorkBenches);
            recipeStormlionMandibleFromStaff.Register();

            // 前期材料：风暴之颚 + 仙人掌 + 蚁狮上颚，在工作台复制成两个。
            Recipe recipeStormlionMandible = Recipe.Create(ModContent.ItemType<StormlionMandible>(), 2);
            recipeStormlionMandible.AddIngredient(ModContent.ItemType<StormlionMandible>());
            recipeStormlionMandible.AddIngredient(ItemID.Cactus);
            recipeStormlionMandible.AddIngredient(ItemID.AntlionMandible);
            recipeStormlionMandible.AddTile(TileID.WorkBenches);
            recipeStormlionMandible.Register();

            // 前期材料：纯净凝胶 + 凝胶，在工作台复制成两个。
            Recipe recipePurifiedGel = Recipe.Create(ModContent.ItemType<PurifiedGel>(), 2);
            recipePurifiedGel.AddIngredient(ModContent.ItemType<PurifiedGel>());
            recipePurifiedGel.AddIngredient(ItemID.Gel, 2);
            recipePurifiedGel.AddTile(TileID.WorkBenches);
            recipePurifiedGel.Register();

            // 前期材料：硫磺鳞片 + 瓶装水，在工作台复制成两个。
            Recipe recipeSulphuricScale = Recipe.Create(ModContent.ItemType<SulphuricScale>(), 2);
            recipeSulphuricScale.AddIngredient(ModContent.ItemType<SulphuricScale>());
            recipeSulphuricScale.AddIngredient(ItemID.BottledWater, 2);
            recipeSulphuricScale.AddTile(TileID.WorkBenches);
            recipeSulphuricScale.Register();

            // 前期材料：泰坦之心 + 星辉瘟疫方尖碑，在工作台复制成两个。
            Recipe recipeTitanHeart = Recipe.Create(ModContent.ItemType<TitanHeart>(), 2);
            recipeTitanHeart.AddIngredient(ModContent.ItemType<TitanHeart>());
            recipeTitanHeart.AddIngredient(ModContent.ItemType<AstralMonolith>(), 5);
            recipeTitanHeart.AddTile(TileID.WorkBenches);
            recipeTitanHeart.Register();

            // 前期材料：泰坦之星 + 坠落之星，在工作台复制成两个。
            Recipe recipeStarblightSoot = Recipe.Create(ModContent.ItemType<StarblightSoot>(), 2);
            recipeStarblightSoot.AddIngredient(ModContent.ItemType<StarblightSoot>());
            recipeStarblightSoot.AddIngredient(ItemID.FallenStar);
            recipeStarblightSoot.AddTile(TileID.WorkBenches);
            recipeStarblightSoot.Register();

            // 前期材料：深渊细胞 + 血肉块 + 骨头，在工作台复制成两个。
            Recipe recipeDepthCells = Recipe.Create(ModContent.ItemType<DepthCells>(), 2);
            recipeDepthCells.AddIngredient(ModContent.ItemType<DepthCells>());
            recipeDepthCells.AddIngredient(ItemID.FleshBlock, 5);
            recipeDepthCells.AddIngredient(ItemID.Bone);
            recipeDepthCells.AddTile(TileID.WorkBenches);
            recipeDepthCells.Register();

            // 精华材料：冰精华 + 冰雪块，在工作台复制成两个。
            Recipe recipeEssenceofEleum = Recipe.Create(ModContent.ItemType<EssenceofEleum>(), 2);
            recipeEssenceofEleum.AddIngredient(ModContent.ItemType<EssenceofEleum>());
            recipeEssenceofEleum.AddIngredient(ItemID.IceBlock, 50);
            recipeEssenceofEleum.AddTile(TileID.WorkBenches);
            recipeEssenceofEleum.Register();

            // 精华材料：日光精华 + 日盘块，在工作台复制成两个。
            Recipe recipeEssenceofSunlight = Recipe.Create(ModContent.ItemType<EssenceofSunlight>(), 2);
            recipeEssenceofSunlight.AddIngredient(ModContent.ItemType<EssenceofSunlight>());
            recipeEssenceofSunlight.AddIngredient(ItemID.SunplateBlock, 50);
            recipeEssenceofSunlight.AddTile(TileID.WorkBenches);
            recipeEssenceofSunlight.Register();

            // 精华材料：混乱精华 + 灰烬块，在工作台复制成两个。
            Recipe recipeEssenceofHavoc = Recipe.Create(ModContent.ItemType<EssenceofHavoc>(), 2);
            recipeEssenceofHavoc.AddIngredient(ModContent.ItemType<EssenceofHavoc>());
            recipeEssenceofHavoc.AddIngredient(ItemID.AshBlock, 50);
            recipeEssenceofHavoc.AddTile(TileID.WorkBenches);
            recipeEssenceofHavoc.Register();
        }

        private static void RegisterMidGameRecipes()
        {
            // 中期材料：生命碎片 + 生命水晶，在秘银砧复制成两个。
            Recipe recipeLivingShard = Recipe.Create(ModContent.ItemType<LivingShard>(), 2);
            recipeLivingShard.AddIngredient(ModContent.ItemType<LivingShard>());
            recipeLivingShard.AddIngredient(ItemID.LifeCrystal);
            recipeLivingShard.AddTile(TileID.MythrilAnvil);
            recipeLivingShard.Register();

            // 中期材料：瘟疫细胞罐 + 纳米机器人，在秘银砧复制成两个。
            Recipe recipePlagueCellCanister = Recipe.Create(ModContent.ItemType<PlagueCellCanister>(), 2);
            recipePlagueCellCanister.AddIngredient(ModContent.ItemType<PlagueCellCanister>());
            recipePlagueCellCanister.AddIngredient(ItemID.Nanites, 50);
            recipePlagueCellCanister.AddTile(TileID.MythrilAnvil);
            recipePlagueCellCanister.Register();

            // 中期材料：灾厄尘 + 灰烬块 + 熔岩桶，在秘银砧复制成两个。
            Recipe recipeAshesofCalamity = Recipe.Create(ModContent.ItemType<AshesofCalamity>(), 2);
            recipeAshesofCalamity.AddIngredient(ModContent.ItemType<AshesofCalamity>());
            recipeAshesofCalamity.AddIngredient(ItemID.AshBlock, 50);
            recipeAshesofCalamity.AddIngredient(ItemID.LavaBucket);
            recipeAshesofCalamity.AddTile(TileID.MythrilAnvil);
            recipeAshesofCalamity.Register();
        }

        private static void RegisterLateGameRecipes()
        {
            // 后期材料：神圣晶石 + 邪恶精华，在远古操纵机复制成两个。
            Recipe recipeDivineGeode = Recipe.Create(ModContent.ItemType<DivineGeode>(), 2);
            recipeDivineGeode.AddIngredient(ModContent.ItemType<DivineGeode>());
            recipeDivineGeode.AddIngredient(ModContent.ItemType<UnholyEssence>(), 2);
            recipeDivineGeode.AddTile(TileID.LunarCraftingStation);
            recipeDivineGeode.Register();

            // 后期材料：死神遗牙 + 玻璃杯 + 深渊细胞 + 鲨鱼鳍，在远古操纵机复制成两个。
            Recipe recipeReaperTooth = Recipe.Create(ModContent.ItemType<ReaperTooth>(), 2);
            recipeReaperTooth.AddIngredient(ModContent.ItemType<ReaperTooth>());
            recipeReaperTooth.AddIngredient(ItemID.Mug);
            recipeReaperTooth.AddIngredient(ModContent.ItemType<DepthCells>());
            recipeReaperTooth.AddIngredient(ItemID.SharkFin);
            recipeReaperTooth.AddTile(TileID.LunarCraftingStation);
            recipeReaperTooth.Register();

            // 后期材料：扭曲虚空 + 画笔 + 黑漆 + 暗影漆，在远古操纵机复制成两个。
            Recipe recipeTwistingNether = Recipe.Create(ModContent.ItemType<TwistingNether>(), 2);
            recipeTwistingNether.AddIngredient(ModContent.ItemType<TwistingNether>());
            recipeTwistingNether.AddIngredient(ItemID.Paintbrush);
            recipeTwistingNether.AddIngredient(ItemID.BlackPaint, 150);
            recipeTwistingNether.AddIngredient(ItemID.ShadowPaint, 150);
            recipeTwistingNether.AddTile(TileID.LunarCraftingStation);
            recipeTwistingNether.Register();

            // 后期材料：暗离子体 + 画笔 + 黑漆 + 负色漆，在远古操纵机复制成两个。
            Recipe recipeDarkPlasma = Recipe.Create(ModContent.ItemType<DarkPlasma>(), 2);
            recipeDarkPlasma.AddIngredient(ModContent.ItemType<DarkPlasma>());
            recipeDarkPlasma.AddIngredient(ItemID.Paintbrush);
            recipeDarkPlasma.AddIngredient(ItemID.BlackPaint, 150);
            recipeDarkPlasma.AddIngredient(ItemID.NegativePaint, 150);
            recipeDarkPlasma.AddTile(TileID.LunarCraftingStation);
            recipeDarkPlasma.Register();

            // 后期材料：装甲外壳 + 神圣锭 + 夜明锭，在远古操纵机复制成两个。
            Recipe recipeArmoredShell = Recipe.Create(ModContent.ItemType<ArmoredShell>(), 2);
            recipeArmoredShell.AddIngredient(ModContent.ItemType<ArmoredShell>());
            recipeArmoredShell.AddIngredient(ItemID.HallowedBar, 2);
            recipeArmoredShell.AddIngredient(ItemID.LunarBar);
            recipeArmoredShell.AddTile(TileID.LunarCraftingStation);
            recipeArmoredShell.Register();

            // 后期材料：吸热能量 + 光明之魂 + 冰雪块，在远古操纵机复制成两个。
            Recipe recipeEndothermicEnergy = Recipe.Create(ModContent.ItemType<EndothermicEnergy>(), 2);
            recipeEndothermicEnergy.AddIngredient(ModContent.ItemType<EndothermicEnergy>());
            recipeEndothermicEnergy.AddIngredient(ItemID.SoulofLight);
            recipeEndothermicEnergy.AddIngredient(ItemID.IceBlock, 20);
            recipeEndothermicEnergy.AddTile(TileID.LunarCraftingStation);
            recipeEndothermicEnergy.Register();

            // 后期材料：梦魇燃料 + 暗影之魂 + 沙块，在远古操纵机复制成两个。
            Recipe recipeNightmareFuel = Recipe.Create(ModContent.ItemType<NightmareFuel>(), 2);
            recipeNightmareFuel.AddIngredient(ModContent.ItemType<NightmareFuel>());
            recipeNightmareFuel.AddIngredient(ItemID.SoulofNight);
            recipeNightmareFuel.AddIngredient(ItemID.SandBlock, 20);
            recipeNightmareFuel.AddTile(TileID.LunarCraftingStation);
            recipeNightmareFuel.Register();

            // 后期材料：日蚀之阴碎片 + 日耀碎片，在远古操纵机复制成两个。
            Recipe recipeDarksunFragment = Recipe.Create(ModContent.ItemType<DarksunFragment>(), 2);
            recipeDarksunFragment.AddIngredient(ModContent.ItemType<DarksunFragment>());
            recipeDarksunFragment.AddIngredient(ItemID.FragmentSolar, 2);
            recipeDarksunFragment.AddTile(TileID.LunarCraftingStation);
            recipeDarksunFragment.Register();

            // 后期材料：龙魂碎片 + 飞升之证，在远古操纵机复制成五个。
            Recipe recipeYharonSoulFragment = Recipe.Create(ModContent.ItemType<YharonSoulFragment>(), 5);
            recipeYharonSoulFragment.AddIngredient(ModContent.ItemType<YharonSoulFragment>());
            recipeYharonSoulFragment.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>());
            recipeYharonSoulFragment.AddTile(TileID.LunarCraftingStation);
            recipeYharonSoulFragment.Register();

            // 后期材料：湮灭余烬 + 灾厄尘，在远古操纵机复制成两个。
            Recipe recipeAshesofAnnihilation = Recipe.Create(ModContent.ItemType<AshesofAnnihilation>(), 2);
            recipeAshesofAnnihilation.AddIngredient(ModContent.ItemType<AshesofAnnihilation>());
            recipeAshesofAnnihilation.AddIngredient(ModContent.ItemType<AshesofCalamity>(), 10);
            recipeAshesofAnnihilation.AddTile(TileID.LunarCraftingStation);
            recipeAshesofAnnihilation.Register();

            // 后期材料：星流棱晶 + 玻璃，在远古操纵机复制成两个。
            Recipe recipeExoPrism = Recipe.Create(ModContent.ItemType<ExoPrism>(), 2);
            recipeExoPrism.AddIngredient(ModContent.ItemType<ExoPrism>());
            recipeExoPrism.AddIngredient(ItemID.Glass, 100);
            recipeExoPrism.AddTile(TileID.LunarCraftingStation);
            recipeExoPrism.Register();
        }

        private static void RegisterEctoplasmDuplicationRecipes()
        {
            RegisterEctoplasmDuplicationRecipe(ModContent.ItemType<BloodOrb>());
            RegisterEctoplasmDuplicationRecipe(ModContent.ItemType<EffulgentFeather>());
            RegisterEctoplasmDuplicationRecipe(ModContent.ItemType<Necroplasm>());
            RegisterEctoplasmDuplicationRecipe(ModContent.ItemType<UnholyEssence>());

            RegisterEctoplasmDuplicationRecipe(ItemID.FragmentSolar);
            RegisterEctoplasmDuplicationRecipe(ItemID.FragmentVortex);
            RegisterEctoplasmDuplicationRecipe(ItemID.FragmentNebula);
            RegisterEctoplasmDuplicationRecipe(ItemID.FragmentStardust);
        }

        private static void RegisterEctoplasmDuplicationRecipe(int materialType)
        {
            Recipe recipe = Recipe.Create(materialType, 2);
            recipe.AddIngredient(materialType);
            recipe.AddIngredient(ItemID.Ectoplasm);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }

        private static void RegisterLunarFragmentRecipes()
        {
            // 天界碎片互转：日耀 + 星旋 + 星尘，在远古操纵机合成四个星云碎片。
            Recipe recipeNebulaFragment = Recipe.Create(ItemID.FragmentNebula, 4);
            recipeNebulaFragment.AddIngredient(ItemID.FragmentSolar);
            recipeNebulaFragment.AddIngredient(ItemID.FragmentVortex);
            recipeNebulaFragment.AddIngredient(ItemID.FragmentStardust);
            recipeNebulaFragment.AddTile(TileID.LunarCraftingStation);
            recipeNebulaFragment.Register();

            // 天界碎片互转：星云 + 星旋 + 星尘，在远古操纵机合成四个日耀碎片。
            Recipe recipeSolarFragment = Recipe.Create(ItemID.FragmentSolar, 4);
            recipeSolarFragment.AddIngredient(ItemID.FragmentNebula);
            recipeSolarFragment.AddIngredient(ItemID.FragmentVortex);
            recipeSolarFragment.AddIngredient(ItemID.FragmentStardust);
            recipeSolarFragment.AddTile(TileID.LunarCraftingStation);
            recipeSolarFragment.Register();

            // 天界碎片互转：星云 + 日耀 + 星尘，在远古操纵机合成四个星旋碎片。
            Recipe recipeVortexFragment = Recipe.Create(ItemID.FragmentVortex, 4);
            recipeVortexFragment.AddIngredient(ItemID.FragmentNebula);
            recipeVortexFragment.AddIngredient(ItemID.FragmentSolar);
            recipeVortexFragment.AddIngredient(ItemID.FragmentStardust);
            recipeVortexFragment.AddTile(TileID.LunarCraftingStation);
            recipeVortexFragment.Register();

            // 天界碎片互转：星云 + 日耀 + 星旋，在远古操纵机合成四个星尘碎片。
            Recipe recipeStardustFragment = Recipe.Create(ItemID.FragmentStardust, 4);
            recipeStardustFragment.AddIngredient(ItemID.FragmentNebula);
            recipeStardustFragment.AddIngredient(ItemID.FragmentSolar);
            recipeStardustFragment.AddIngredient(ItemID.FragmentVortex);
            recipeStardustFragment.AddTile(TileID.LunarCraftingStation);
            recipeStardustFragment.Register();
        }
    }
}
