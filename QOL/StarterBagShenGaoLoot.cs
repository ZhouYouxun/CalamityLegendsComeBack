using CalamityLegendsComeBack.Weapons.A_Tools.Tools.ShenGao;
using CalamityMod.Items.TreasureBags.MiscGrabBags;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL;

public sealed class StarterBagShenGaoLoot : GlobalItem
{
    public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
    {
        if (item.type == ModContent.ItemType<StarterBag>())
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShenGao>()));
    }
}
