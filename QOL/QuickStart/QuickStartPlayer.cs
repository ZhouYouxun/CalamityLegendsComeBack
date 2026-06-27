using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.QuickStart
{
    public class QuickStartPlayer : ModPlayer
    {
        private bool receivedLegendaryStarterItems;

        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            if (mediumCoreDeath)
                yield break;

            receivedLegendaryStarterItems = true;
            yield return CreateItem(ModContent.ItemType<LegendarySupplyBox>());
            yield return CreateItem(ModContent.ItemType<LegendaryCodex>());

            if (CalamityLegendsComeBackConfig.Instance?.GiveQuickStartBoxOnSpawn == true)
                yield return CreateItem(ModContent.ItemType<QuickStartBox>());
        }

        public override void OnEnterWorld()
        {
            if (Main.myPlayer != Player.whoAmI || receivedLegendaryStarterItems)
                return;

            receivedLegendaryStarterItems = true;
            Player.QuickSpawnItem(Player.GetSource_Misc("LegendaryStarterItemsFallback"), ModContent.ItemType<LegendarySupplyBox>());
            Player.QuickSpawnItem(Player.GetSource_Misc("LegendaryStarterItemsFallback"), ModContent.ItemType<LegendaryCodex>());
        }

        public override void SaveData(TagCompound tag)
        {
            if (receivedLegendaryStarterItems)
                tag["ReceivedLegendaryStarterItems"] = true;
        }

        public override void LoadData(TagCompound tag)
        {
            receivedLegendaryStarterItems = tag.GetBool("ReceivedLegendaryStarterItems");
        }

        private static Item CreateItem(int type)
        {
            Item item = new Item();
            item.SetDefaults(type);
            return item;
        }
    }
}
