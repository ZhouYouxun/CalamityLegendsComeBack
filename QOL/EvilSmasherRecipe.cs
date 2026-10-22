using CalamityMod.Items.Weapons.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL;

public sealed class EvilSmasherRecipe : ModSystem
{
    public override void AddRecipes()
    {
        Recipe.Create(ModContent.ItemType<EvilSmasher>())
            .AddIngredient(ItemID.Pwnhammer)
            .AddIngredient(ItemID.SoulofNight, 5)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
