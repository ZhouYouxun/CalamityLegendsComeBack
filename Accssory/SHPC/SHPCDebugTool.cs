using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC;

// Intentionally has no recipe, loot, shop entry, or accessory effect.
public sealed class SHPCDebugTool : ModItem
{
    public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/ChangeRight/MilitaryCaller/MilitaryCaller";

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Yellow;
        Item.value = 0;
    }

    internal static bool IsEnabled(Player player)
    {
        if (player == null || player.whoAmI != Main.myPlayer)
            return false;

        // Normal inventory only: neither banks, the cursor nor equipment enable it.
        for (int i = 0; i < 50; i++)
        {
            Item item = player.inventory[i];
            if (item is { IsAir: false, favorited: true } && item.ModItem is SHPCDebugTool)
                return true;
        }
        return false;
    }
}
