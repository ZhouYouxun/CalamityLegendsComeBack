using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash.DashEffects;
using CalamityMod;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    internal class Dash_Trigger : ModPlayer
    {
        private bool enhancedDashActiveLastFrame;
        private int passiveDashTimer;

        public bool IsUsingSlashDash;
        public bool DashEnabled = true;

        public BrinyBaronQuickDashDevice EquippedDashDevice =>
            BrinyBaronDashPassiveEffectRegistry.FromDashID(Player.Calamity().DashID);

        public string EquippedDashDeviceLocalizationKey =>
            BrinyBaronDashPassiveEffectRegistry.GetLocalizationKey(EquippedDashDevice);

        public override void ResetEffects()
        {
            IsUsingSlashDash = false;
        }

        public override void UpdateDead()
        {
            enhancedDashActiveLastFrame = false;
            passiveDashTimer = 0;
            IsUsingSlashDash = false;
        }

        public override void PostUpdate()
        {
            BrinyBaronQuickDashDevice activeDevice = BrinyBaronDashPassiveEffectRegistry.FromDashID(GetActiveDashID());
            bool active = CanApplyPassive(activeDevice) && IsDashingWithTrackedAccessory(activeDevice);

            if (!active)
            {
                enhancedDashActiveLastFrame = false;
                passiveDashTimer = 0;
                return;
            }

            if (!enhancedDashActiveLastFrame)
            {
                passiveDashTimer = 0;
                BrinyBaronDashPassiveEffectRegistry.ApplyDashStarted(Player, activeDevice);
            }

            passiveDashTimer++;
            BrinyBaronDashPassiveEffectRegistry.ApplyDashUpdate(Player, activeDevice, passiveDashTimer);
            enhancedDashActiveLastFrame = true;
        }

        private bool CanApplyPassive(BrinyBaronQuickDashDevice activeDevice)
        {
            return DashEnabled &&
                activeDevice != BrinyBaronQuickDashDevice.None &&
                Player.active &&
                !Player.dead &&
                Player.HeldItem != null &&
                !Player.HeldItem.IsAir &&
                Player.HeldItem.type == ModContent.ItemType<NewLegendBrinyBaron>();
        }

        private bool IsDashingWithTrackedAccessory(BrinyBaronQuickDashDevice activeDevice)
        {
            return Player.dashDelay < 0 &&
                Player.velocity.Length() > 4f &&
                activeDevice != BrinyBaronQuickDashDevice.None;
        }

        private string GetActiveDashID()
        {
            string lastUsedDash = Player.Calamity().LastUsedDashID;
            return !string.IsNullOrEmpty(lastUsedDash) ? lastUsedDash : Player.Calamity().DashID;
        }
    }
}
