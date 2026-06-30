using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 检测玩家在物品栏中对沙漠之鹰进行中键点击，并生成格子槽 UI 弹幕。
    /// </summary>
    public class DesertEagleSlotInputSystem : ModSystem
    {
        private bool prevMiddleClick;

        public override void PostUpdateInput()
        {
            if (!Main.playerInventory)
            {
                prevMiddleClick = false;
                return;
            }

            bool currentMiddle = Main.mouseMiddle;
            bool justClicked = currentMiddle && !prevMiddleClick;
            prevMiddleClick = currentMiddle;

            if (!justClicked)
                return;

            // Main.HoverItem 是当前悬停物品栏格子显示的物品
            if (Main.HoverItem == null || Main.HoverItem.type != ModContent.ItemType<DesertEagle>())
                return;

            Player player = Main.LocalPlayer;
            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();

            // 如果已有 UI 在开启，先关闭再开，否则直接开
            if (slotPlayer.SlotUIOpen)
                return;

            Projectile.NewProjectile(
                player.GetSource_Misc("DESlotUI"),
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DesertEagleSlotUI>(),
                0, 0f, player.whoAmI, 0f, 0f);
        }
    }
}
