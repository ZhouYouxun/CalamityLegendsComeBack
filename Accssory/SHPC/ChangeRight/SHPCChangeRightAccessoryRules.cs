using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight
{
    /// <summary>
    /// SHPC 右键改造饰品互斥规则：同一时间只能装备一个右键改造模块。
    /// </summary>
    public static class SHPCChangeRightAccessoryRules
    {
        public static bool IsChangeRightAccessoryItem(int itemType)
        {
            return itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.CommandAscend.CommandAscend>() ||
                   itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.MilitaryCaller.MilitaryCaller>() ||
                   itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule.ProjectilePossessionModule>();
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
