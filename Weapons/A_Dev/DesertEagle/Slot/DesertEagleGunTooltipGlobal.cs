using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// GlobalItem：当沙漠之鹰在玩家快捷栏时，
    /// 鼠标悬停在已注册的手枪上会出现提示行。
    /// </summary>
    public class DesertEagleGunTooltipGlobal : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!DEBulletRegistry.IsRegisteredGun(item.type))
                return;

            // 检查本地玩家快捷栏是否有沙漠之鹰
            Player player = Main.LocalPlayer;
            if (!PlayerHasDEInHotbar(player))
                return;

            DEBulletRule rule = DEBulletRegistry.GetRule(item.type);

            // 当前装填状态
            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();
            bool isSlotted = slotPlayer.SlottedGunType == item.type;

            bool isChinese = Language.ActiveCulture.Name.StartsWith("zh");

            string slotHint = isChinese
                ? (isSlotted ? "[c/FFD700:已装填至] [c/A0C4FF:沙漠之鹰]"
                             : "[c/A0C4FF:可装填至] [c/FFD700:沙漠之鹰] [c/888888:（中键打开格子槽）]")
                : (isSlotted ? "[c/FFD700:Currently slotted in] [c/A0C4FF:Desert Eagle]"
                             : "[c/A0C4FF:Can be slotted into] [c/FFD700:Desert Eagle] [c/888888:(middle-click DE to open slot)]");

            tooltips.Add(new TooltipLine(Mod, "DESlotHint", slotHint));

            string effectText = isChinese ? rule.TooltipEffectZH : rule.TooltipEffectEN;
            if (!string.IsNullOrEmpty(effectText))
            {
                string label = isChinese ? "[c/C8FFDD:效果：]" : "[c/C8FFDD:Effect: ]";
                tooltips.Add(new TooltipLine(Mod, "DEEffect", label + effectText)
                {
                    OverrideColor = new Color(200, 255, 220)
                });
            }
        }

        private static bool PlayerHasDEInHotbar(Player player)
        {
            int deType = ModContent.ItemType<DesertEagle>();
            for (int i = 0; i < 10; i++)
            {
                if (player.inventory[i].type == deType)
                    return true;
            }
            return false;
        }
    }
}
