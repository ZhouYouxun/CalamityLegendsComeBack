using CalamityMod.Items.Accessories.Vanity;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL;

// Cosmetic transformations: craftable with pre-Hardmode materials, without boss flags.
public sealed class EarlyVanityRecipes : ModSystem
{
    public override void AddRecipes()
    {
        Recipe.Create(ModContent.ItemType<XyksBlessingBlue>())
            .AddIngredient(ItemID.Silk, 10)
            .AddIngredient(ItemID.FallenStar, 3)
            .AddIngredient(ItemID.Sapphire)
            .AddIngredient(ItemID.BlueDye)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<XyksBlessingOrange>())
            .AddIngredient(ItemID.Silk, 10)
            .AddIngredient(ItemID.FallenStar, 3)
            .AddIngredient(ItemID.Amber)
            .AddIngredient(ItemID.OrangeDye)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<HapuFruit>())
            .AddIngredient(ItemID.Apple, 2)
            .AddIngredient(ItemID.Daybloom, 3)
            .AddIngredient(ItemID.BottledWater)
            .AddTile(TileID.WorkBenches)
            .Register();

        Recipe.Create(ModContent.ItemType<SharkyPlush>())
            .AddIngredient(ItemID.Silk, 10)
            .AddIngredient(ItemID.SharkFin)
            .AddIngredient(ItemID.BlueDye)
            .AddTile(TileID.Loom)
            .Register();

        Recipe.Create(ModContent.ItemType<OracleHeadphones>())
            .AddRecipeGroup("IronBar", 5)
            .AddIngredient(ItemID.Lens, 2)
            .AddIngredient(ItemID.Silk, 3)
            .AddIngredient(ItemID.FallenStar, 2)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
