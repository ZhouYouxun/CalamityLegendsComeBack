using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14EBRPlayer : ModPlayer
    {
        private bool lastRightMouse;

        public bool HoldingMK14EBR { get; private set; }
        public int LaserKillDamageTimer { get; private set; }
        public int SpreadResetSignal { get; private set; }

        public override void ResetEffects()
        {
            HoldingMK14EBR = false;
        }

        public override void PostUpdate()
        {
            if (LaserKillDamageTimer > 0)
                LaserKillDamageTimer--;

            if (Player.whoAmI == Main.myPlayer && Player.HeldItem.ModItem is not NewLegendMK14EBR)
                lastRightMouse = false;
        }

        public void SetHoldingMK14EBR()
        {
            HoldingMK14EBR = true;
        }

        public bool ConsumeRightClickPress(Player player)
        {
            bool rightDown = player.Calamity().mouseRight || Main.mouseRight;
            bool pressed = rightDown && !lastRightMouse;
            lastRightMouse = rightDown;
            return pressed;
        }

        public void TriggerLaserKillBonus()
        {
            SpreadResetSignal++;
            LaserKillDamageTimer = 180;
        }
    }
}
