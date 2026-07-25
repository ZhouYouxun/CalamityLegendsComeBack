using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.ChangeRight
{
    public static class BBChangeRightAccessoryRules
    {
        public static bool IsChangeRightAccessoryItem(int itemType)
        {
            return itemType == ModContent.ItemType<VortexEye.VortexEye>();
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
