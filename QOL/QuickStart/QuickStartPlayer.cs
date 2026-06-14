using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.QuickStart
{
    public class QuickStartPlayer : ModPlayer
    {
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            if (mediumCoreDeath)
                yield break;

            if (CalamityLegendsComeBackConfig.Instance?.GiveQuickStartBoxOnSpawn == true)
            {
                Item box = new Item(ModContent.ItemType<QuickStartBox>());
                yield return box;
            }
        }
    }
}
