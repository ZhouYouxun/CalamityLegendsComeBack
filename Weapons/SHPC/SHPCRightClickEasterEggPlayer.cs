using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal sealed class SHPCRightClickEasterEggPlayer : ModPlayer
    {
        private const int RequiredClicks = 6;
        private const ulong ClickWindowTicks = 60;
        private const ulong CooldownTicks = 120;

        private bool wasRightDown;
        private int clickCount;
        private ulong firstClickTick;
        private ulong cooldownUntilTick;

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            bool rightDown = Main.mouseRight;
            bool justPressed = rightDown && !wasRightDown;
            wasRightDown = rightDown;

            if (Player.dead ||
                Player.HeldItem.ModItem is not NewLegendSHPC ||
                Language.ActiveCulture.Name != "zh-Hans" ||
                Main.playerInventory ||
                !NewLegendSHPC.CanUseWorldRightClick(Player))
            {
                clickCount = 0;
                return;
            }

            if (!justPressed)
                return;

            ulong now = Main.GameUpdateCount;
            if (now < cooldownUntilTick)
                return;

            if (clickCount == 0 || now - firstClickTick > ClickWindowTicks)
            {
                clickCount = 1;
                firstClickTick = now;
            }
            else
            {
                clickCount++;
            }

            if (clickCount < RequiredClicks)
                return;

            CombatText.NewText(Player.Hitbox, new Color(255, 210, 115), "抖动一些枪械？");
            clickCount = 0;
            cooldownUntilTick = now + CooldownTicks;
        }
    }
}
