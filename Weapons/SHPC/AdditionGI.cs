using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons;
using CalamityMod.Items.Weapons.Summon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod.Items.Placeables.FurnitureMonolith;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal class AdditionGI : ModSystem
    {
        public override void AddRecipes()
        {

            // 能量核心
            Recipe recipe1 = Recipe.Create(ModContent.ItemType<EnergyCore>());
            recipe1.AddIngredient(ModContent.ItemType<WulfrumMetalScrap>(), 2);
            recipe1.AddTile(TileID.WorkBenches);
            recipe1.Register();

            // 风暴上颚
            Recipe recipe2 = Recipe.Create(ModContent.ItemType<StormlionMandible>(), 10);
            recipe2.AddIngredient(ModContent.ItemType<StormjawStaff>(), 1);
            recipe2.AddTile(TileID.WorkBenches);
            recipe2.Register();
            
            // 纯净凝胶
            Recipe recipe3 = Recipe.Create(ModContent.ItemType <PurifiedGel> (), 2);
            recipe3.AddIngredient(ModContent.ItemType<PurifiedGel>(), 1);
            recipe3.AddIngredient(ItemID.Gel, 2);
            recipe3.AddTile(TileID.WorkBenches);
            recipe3.Register();
            
            // 硫磺鳞片
            Recipe recipe4 = Recipe.Create(ModContent.ItemType <SulphuricScale> (), 2);
            recipe4.AddIngredient(ModContent.ItemType<SulphuricScale>(), 1);
            recipe4.AddIngredient(ItemID.BottledWater, 2);
            recipe4.AddTile(TileID.WorkBenches);
            recipe4.Register();



            // 泰坦之心
            Recipe recipe5 = Recipe.Create(ModContent.ItemType<TitanHeart> (), 2);
            recipe5.AddIngredient(ModContent.ItemType<SulphuricScale>(), 1);
            recipe5.AddIngredient(ModContent.ItemType<AstralMonolith>(), 5);
            recipe5.AddTile(TileID.WorkBenches);
            recipe5.Register();



            // 深渊细胞
            Recipe recipe6 = Recipe.Create(ModContent.ItemType <DepthCells> (), 2);
            recipe6.AddIngredient(ModContent.ItemType<DepthCells>(), 1);
            recipe6.AddIngredient(ItemID.FleshBlock, 5);
            recipe6.AddIngredient(ItemID.Bone, 1);
            recipe6.AddTile(TileID.WorkBenches);
            recipe6.Register();



            // ================= 精华复制 =================

            // 冰精华 EssenceofEleum + 冰块 → 2个精华
            Recipe recipeEE = Recipe.Create(ModContent.ItemType<EssenceofEleum>(), 2);
            recipeEE.AddIngredient(ModContent.ItemType<EssenceofEleum>(), 1);
            recipeEE.AddIngredient(ItemID.IceBlock, 100);
            recipeEE.AddTile(TileID.WorkBenches);
            recipeEE.Register();

            // 日光精华 EssenceofSunlight + 日盘砖 → 2个精华
            Recipe recipeES = Recipe.Create(ModContent.ItemType<EssenceofSunlight>(), 2);
            recipeES.AddIngredient(ModContent.ItemType<EssenceofSunlight>(), 1);
            recipeES.AddIngredient(ItemID.SunplateBlock, 100);
            recipeES.AddTile(TileID.WorkBenches);
            recipeES.Register();

            // 混乱精华 EssenceofHavoc + 灰烬块 → 2个精华
            Recipe recipeEH = Recipe.Create(ModContent.ItemType<EssenceofHavoc>(), 2);
            recipeEH.AddIngredient(ModContent.ItemType<EssenceofHavoc>(), 1);
            recipeEH.AddIngredient(ItemID.AshBlock, 100);
            recipeEH.AddTile(TileID.WorkBenches);
            recipeEH.Register();


            // ================= 生命碎片 =================

            // 生命碎片 LivingShard + 生命水晶 → 2个
            Recipe recipeLS = Recipe.Create(ModContent.ItemType<LivingShard>(), 2);
            recipeLS.AddIngredient(ModContent.ItemType<LivingShard>(), 1);
            recipeLS.AddIngredient(ItemID.LifeCrystal, 1);
            recipeLS.AddTile(TileID.WorkBenches);
            recipeLS.Register();


            // ================= 神圣晶石 =================

            // 神圣晶石 DivineGeode + Unholy Essence×2 → 2个
            Recipe recipeDG = Recipe.Create(ModContent.ItemType<DivineGeode>(), 2);
            recipeDG.AddIngredient(ModContent.ItemType<DivineGeode>(), 1);
            recipeDG.AddIngredient(ModContent.ItemType<UnholyEssence>(), 2);
            recipeDG.AddTile(TileID.WorkBenches);
            recipeDG.Register();


            // ================= 颜料系复制 =================

            // 扭曲虚空 TwistingNether + 画笔 + 黑漆 + 暗影漆 → 2个
            Recipe recipeTN = Recipe.Create(ModContent.ItemType<TwistingNether>(), 2);
            recipeTN.AddIngredient(ModContent.ItemType<TwistingNether>(), 1);
            recipeTN.AddIngredient(ItemID.Paintbrush, 1);
            recipeTN.AddIngredient(ItemID.BlackPaint, 150);
            recipeTN.AddIngredient(ItemID.ShadowPaint, 150);
            recipeTN.AddTile(TileID.WorkBenches);
            recipeTN.Register();

            // 暗离子体 DarkPlasma + 画笔 + 黑漆 + 负色漆 → 2个
            Recipe recipeDP = Recipe.Create(ModContent.ItemType<DarkPlasma>(), 2);
            recipeDP.AddIngredient(ModContent.ItemType<DarkPlasma>(), 1);
            recipeDP.AddIngredient(ItemID.Paintbrush, 1);
            recipeDP.AddIngredient(ItemID.BlackPaint, 150);
            recipeDP.AddIngredient(ItemID.NegativePaint, 150);
            recipeDP.AddTile(TileID.WorkBenches);
            recipeDP.Register();


            // ================= 龙魂碎片 =================

            // 龙魂碎片 YharonSoulFragment + AscendantSpiritEssence → 5个
            Recipe recipeYSF = Recipe.Create(ModContent.ItemType<YharonSoulFragment>(), 5);
            recipeYSF.AddIngredient(ModContent.ItemType<YharonSoulFragment>(), 1);
            recipeYSF.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 1);
            recipeYSF.AddTile(TileID.LunarCraftingStation);
            recipeYSF.Register();










        }
    }
}