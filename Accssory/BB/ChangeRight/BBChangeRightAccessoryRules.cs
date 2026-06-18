using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.ChangeRight
{
    public static class BBChangeRightAccessoryRules
    {
        public static bool IsChangeRightAccessoryItem(int itemType)
        {
            return itemType == ModContent.ItemType<CeruleanShield>() ||
                   itemType == ModContent.ItemType<LostGarment>() ||
                   itemType == ModContent.ItemType<VortexPortal>();
        }

        public static bool CanEquipWith(Item equippedItem, Item incomingItem)
        {
            return equippedItem == null ||
                   incomingItem == null ||
                   !IsChangeRightAccessoryItem(equippedItem.type) ||
                   !IsChangeRightAccessoryItem(incomingItem.type);
        }
    }
}
