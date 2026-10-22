using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL;

// Additional early recipes; retain Calamity's existing acquisition routes.
public sealed class EarlyShrineAccessoryRecipes : ModSystem
{
    public override void AddRecipes()
    {
        Recipe.Create(ModContent.ItemType<LuxorsGift>())
            .AddIngredient(ItemID.SandBlock, 20)
            .AddIngredient(ItemID.Cactus, 10)
            .AddIngredient(ItemID.Amber)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<FrozenCube>())
            .AddIngredient(ItemID.IceBlock, 20)
            .AddIngredient(ItemID.SnowBlock, 10)
            .AddIngredient(ItemID.IceTorch, 5)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<TrinketofChi>())
            .AddIngredient(ItemID.DirtBlock, 20)
            .AddIngredient(ItemID.Wood, 10)
            .AddIngredient(ItemID.Daybloom, 2)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<GladiatorsLocket>())
            .AddIngredient(ItemID.MarbleBlock, 20)
            .AddRecipeGroup("IronBar", 3)
            .AddIngredient(ItemID.Chain)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<UnstableGraniteCore>())
            .AddIngredient(ItemID.GraniteBlock, 20)
            .AddRecipeGroup("AnyCopperBar", 3)
            .AddIngredient(ItemID.Gel, 5)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<FungalSymbiote>())
            .AddIngredient(ItemID.MudBlock, 20)
            .AddIngredient(ItemID.GlowingMushroom, 10)
            .AddIngredient(ItemID.Gel, 5)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<IlmerisSpark>())
            .AddIngredient(ItemID.SandstoneBrick, 50)
            .AddIngredient(ItemID.Diamond)
            .AddIngredient<StormlionMandible>()
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}
