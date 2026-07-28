using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash.DashEffects;
using CalamityMod;
using Microsoft.Xna.Framework;
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

        public bool HasDashCapability => CanPlayerDash(Player);

        public string EquippedDashDeviceLocalizationKey =>
            HasDashCapability ? "PassiveDevice_Any" : "PassiveDevice_None";

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

        public static bool CanPlayerDash(Player player)
        {
            if (player == null || !player.active || player.dead)
                return false;

            // 1. 原版冲刺能力检测（如克苏鲁之盾:1, 忍者大师/分身:2, 日耀套:3, 水晶刺客:4 等）
            if (player.dashType > 0 || player.dash > 0)
                return true;

            // 2. 灾厄冲刺能力检测
            string dashID = player.Calamity().DashID;
            if (!string.IsNullOrEmpty(dashID))
                return true;

            string lastDashID = player.Calamity().LastUsedDashID;
            if (!string.IsNullOrEmpty(lastDashID))
                return true;

            // 3. 当前已在冲刺中
            if (player.dashDelay < 0)
                return true;

            return false;
        }

        private bool CanApplyPassive()
        {
            if (!DashEnabled || !Player.active || Player.dead)
                return false;

            if (!HasDashCapability)
                return false;

            // 正常情况：手持海爵剑
            if (Player.HeldItem != null && !Player.HeldItem.IsAir &&
                Player.HeldItem.type == ModContent.ItemType<NewLegendBrinyBaron>())
                return true;

            // 潮汐被动触媒：背包中存有海爵剑即可激活被动，无需手持
            if (Player.GetModPlayer<BBAccessoryPlayer>().TideRadarEquipped)
            {
                int bbType = ModContent.ItemType<NewLegendBrinyBaron>();
                foreach (Item item in Player.inventory)
                {
                    if (item.type == bbType)
                        return true;
                }
            }

            return false;
        }

        private bool IsPlayerDashing()
        {
            if (!CanApplyPassive())
                return false;

            // 1. 灾厄冲刺按键检测：如果玩家绑定了快捷键，按键触发即可判定
            var dashHotkey = CalamityKeybinds.DashHotkey;
            bool hasDashHotkey = dashHotkey != null && dashHotkey.GetAssignedKeys().Count > 0;
            if (hasDashHotkey && dashHotkey.JustPressed)
                return true;

            // 2. 冲刺状态检测：适用于未绑定快捷键的双击冲刺（A/D双击）或已经在冲刺位移中的情况
            if (Player.dashDelay < 0 && Player.velocity.Length() > 2f)
                return true;

            return false;
        }

        public override void PostUpdate()
        {
            bool active = IsPlayerDashing();

            if (!active)
            {
                enhancedDashActiveLastFrame = false;
                passiveDashTimer = 0;
                return;
            }

            if (!enhancedDashActiveLastFrame)
            {
                passiveDashTimer = 0;
                BrinyBaronDashPassiveEffect.ApplyDashStarted(Player);
            }

            passiveDashTimer++;
            BrinyBaronDashPassiveEffect.ApplyDashUpdate(Player, passiveDashTimer);
            enhancedDashActiveLastFrame = true;
        }
    }
}
