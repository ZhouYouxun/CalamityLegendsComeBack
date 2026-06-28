using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
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
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LegendarySupplyBox>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LegendaryCodex>()));
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RetroGameConsoleSupplyBox>()));
            }
        }
    }
}
