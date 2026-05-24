using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class Dash_Trigger : ModPlayer
    {
        private const int DoubleTapInputWindow = 15;
        private const int DashBaseDamage = 650;

        private static readonly float[] QuickDashBaseDamageMultipliers = { 1f, 1.25f, 1.55f, 2f };
        private static readonly int[] QuickDashCooldowns = { 120, 120, 90, 60 };

        private int doubleTapTimer = 0;
        private int dashCooldownTimer = 0;
        private int lastTapDirection = 0;

        public bool IsUsingSlashDash;
        public bool DashEnabled = true;

        public override void ResetEffects()
        {
            if (doubleTapTimer > 0)
                doubleTapTimer--;
            else
                lastTapDirection = 0;

            if (dashCooldownTimer > 0)
                dashCooldownTimer--;

            IsUsingSlashDash = false;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Player player = Player;
            BrinyBaronFocusModePlayer focusPlayer = player.GetModPlayer<BrinyBaronFocusModePlayer>();

            if (!DashEnabled)
                return;
            if (player.HeldItem.type != ModContent.ItemType<NewLegendBrinyBaron>())
                return;
            if (!CanUseQuickDash())
                return;
            if (HasAnyActiveSkillProjectile(player) || IsUsingSlashDash)
                return;
            if (dashCooldownTimer > 0)
                return;

            bool triggerDash = false;
            int dashDirection = 0;
            QuickDashProfile growthProfile = ResolveDashGrowthProfile();

            var keys = CalamityKeybinds.DashHotkey.GetAssignedKeysOrEmpty();
            bool manualHotkeyBound = (keys?.Count ?? 0) > 0;
            bool pressedManualHotkey = manualHotkeyBound && CalamityKeybinds.DashHotkey.JustPressed;

            if (pressedManualHotkey)
            {
                dashDirection = player.direction;
                if (dashDirection == 0)
                    dashDirection = Main.MouseWorld.X > player.Center.X ? 1 : -1;

                triggerDash = true;
            }
            else if (!manualHotkeyBound)
            {
                if (player.controlLeft && player.controlRight)
                    return;

                if (player.controlLeft && player.releaseLeft)
                {
                    if (doubleTapTimer > 0 && lastTapDirection == -1)
                    {
                        triggerDash = true;
                        dashDirection = -1;
                    }
                    else
                    {
                        doubleTapTimer = DoubleTapInputWindow;
                        lastTapDirection = -1;
                    }
                }

                if (player.controlRight && player.releaseRight)
                {
                    if (doubleTapTimer > 0 && lastTapDirection == 1)
                    {
                        triggerDash = true;
                        dashDirection = 1;
                    }
                    else
                    {
                        doubleTapTimer = DoubleTapInputWindow;
                        lastTapDirection = 1;
                    }
                }
            }

            if (!triggerDash)
                return;

            doubleTapTimer = 0;
            lastTapDirection = 0;
            IsUsingSlashDash = true;
            dashCooldownTimer = focusPlayer.IsFocusModeActive
                ? Math.Max(1, growthProfile.DashCooldown / 2)
                : growthProfile.DashCooldown;

            Vector2 dir = new Vector2(dashDirection, 0f);
            int dashBaseDamage = (int)(DashBaseDamage * growthProfile.BaseDamageMultiplier);
            if (focusPlayer.IsFocusModeActive)
                dashBaseDamage *= 5;

            int dashDamage = (int)player.GetTotalDamage(player.HeldItem.DamageType).ApplyTo(dashBaseDamage);

            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                player.Center,
                dir,
                ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                dashDamage,
                player.GetWeaponKnockback(player.HeldItem),
                player.whoAmI,
                0f,
                dashDirection);
        }

        private static bool CanUseQuickDash()
        {
            return Main.hardMode;
        }

        private static QuickDashProfile ResolveDashGrowthProfile()
        {
            int tier = GetQuickDashGrowthTier();
            return new QuickDashProfile(QuickDashBaseDamageMultipliers[tier], QuickDashCooldowns[tier]);
        }

        private static int GetQuickDashGrowthTier()
        {
            if (CalamityMod.DownedBossSystem.downedYharon)
                return 3;
            if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                return 2;
            if (NPC.downedFishron)
                return 1;

            return 0;
        }

        private bool HasAnyActiveSkillProjectile(Player player)
        {
            int left = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            int rightDash = ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();
            int superDash = ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI)
                    continue;

                if (projectile.type == left || projectile.type == rightDash || projectile.type == superDash)
                    return true;
            }

            return false;
        }

        private readonly struct QuickDashProfile
        {
            public readonly float BaseDamageMultiplier;
            public readonly int DashCooldown;

            public QuickDashProfile(float baseDamageMultiplier, int dashCooldown)
            {
                BaseDamageMultiplier = baseDamageMultiplier;
                DashCooldown = dashCooldown;
            }
        }
    }
}
