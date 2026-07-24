using CalamityLegendsComeBack.Accssory.BB;
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

        public BrinyBaronQuickDashDevice EquippedDashDevice => GetActiveDashDevice();

        private BrinyBaronQuickDashDevice GetActiveDashDevice()
        {
            BrinyBaronQuickDashDevice device = BrinyBaronDashPassiveEffectRegistry.FromDashID(GetActiveDashIDString());
            if (device != BrinyBaronQuickDashDevice.None)
                return device;

            if (Player.dashType == 1 || Player.dash == 1)
            {
                int highRulerType = ModContent.ItemType<CalamityMod.Items.Accessories.ShieldoftheHighRuler>();
                if (highRulerType > 0)
                {
                    for (int i = 0; i < Player.armor.Length; i++)
                    {
                        if (Player.armor[i].type == highRulerType)
                            return BrinyBaronQuickDashDevice.ShieldOfTheHighRuler;
                    }
                }

                return BrinyBaronQuickDashDevice.ShieldOfCthulhu;
            }

            return BrinyBaronQuickDashDevice.None;
        }

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
            BrinyBaronQuickDashDevice activeDevice = GetActiveDashDevice();
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
            if (!DashEnabled || activeDevice == BrinyBaronQuickDashDevice.None || !Player.active || Player.dead)
                return false;

            // 正常情况：手持海爵剑
            if (Player.HeldItem != null && !Player.HeldItem.IsAir &&
                Player.HeldItem.type == ModContent.ItemType<NewLegendBrinyBaron>())
                return true;

            // 潮汐被动触媒：背包中存有海爵剑即可激活被动，无需手持
            if (!Player.GetModPlayer<BBAccessoryPlayer>().BBPassiveChannelerEquipped)
                return false;

            int bbType = ModContent.ItemType<NewLegendBrinyBaron>();
            foreach (Item item in Player.inventory)
            {
                if (item.type == bbType)
                    return true;
            }
            return false;
        }

        private bool IsDashingWithTrackedAccessory(BrinyBaronQuickDashDevice activeDevice)
        {
            return Player.dashDelay < 0 &&
                Player.velocity.Length() > 4f &&
                activeDevice != BrinyBaronQuickDashDevice.None;
        }

        private string GetActiveDashIDString()
        {
            string lastUsedDash = Player.Calamity().LastUsedDashID;
            return !string.IsNullOrEmpty(lastUsedDash) ? lastUsedDash : Player.Calamity().DashID;
        }
    }
}
