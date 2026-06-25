using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.SHPCBook;
using CalamityMod.Items.TreasureBags.MiscGrabBags;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    internal class NeverGonnaGiveYouWeapon : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ItemType<StarterBag>();
        }

        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (item.type == ModContent.ItemType<StarterBag>())
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NewLegendSHPC>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SHPCBook>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LegendaryCodex>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RetroGameConsoleSupplyBox>()));
            }
        }
    }
}
