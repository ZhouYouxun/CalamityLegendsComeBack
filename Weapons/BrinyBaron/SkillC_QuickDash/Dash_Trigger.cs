using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class Dash_Trigger : ModPlayer
    {
        private const int BaseDoubleTapWindow = 15;
        private const int FishronDoubleTapWindow = 13;
        private const int BoomerDukeDoubleTapWindow = 11;
        private const int YharonDoubleTapWindow = 9;

        private const float BaseDashDamageMultiplier = 1f;
        private const float FishronDashDamageMultiplier = 1.18f;
        private const float BoomerDukeDashDamageMultiplier = 1.38f;
        private const float YharonDashDamageMultiplier = 1.65f;

        private int doubleTapTimer = 0;
        private int lastTapDirection = 0;
        public bool IsUsingSlashDash;
        public bool DashEnabled = true; // 默认开启

        public override void ResetEffects()
        {
            if (doubleTapTimer > 0)
                doubleTapTimer--;
            else
                lastTapDirection = 0;

            IsUsingSlashDash = false;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Player player = Player;

            if (!DashEnabled)
                return;

            if (player.HeldItem.type != ModContent.ItemType<NewLegendBrinyBaron>())
                return;

            if (HasAnyActiveSkillProjectile(player) || player.GetModPlayer<Dash_Trigger>().IsUsingSlashDash)
                return;

            bool triggerDash = false;
            int dashDirection = 0;
            DashGrowthProfile growthProfile = ResolveDashGrowthProfile();

            // =========================
            // 神秘按键逻辑（优先级最高）
            // =========================
            var keys = CalamityKeybinds.DashHotkey.GetAssignedKeysOrEmpty();
            bool manualHotkeyBound = (keys?.Count ?? 0) > 0;
            bool pressedManualHotkey = manualHotkeyBound && CalamityKeybinds.DashHotkey.JustPressed;

            if (pressedManualHotkey)
            {
                // 优先用面向方向
                dashDirection = player.direction;

                // 如果方向异常，用鼠标兜底
                if (dashDirection == 0)
                {
                    dashDirection = Main.MouseWorld.X > player.Center.X ? 1 : -1;
                }

                triggerDash = true;
            }
            // =========================
            // fallback：只有“未绑定”时才允许双击
            // =========================
            else if (!manualHotkeyBound)
            {
                // 同时按左右 → 锁死（什么都不做）
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
                        doubleTapTimer = growthProfile.DoubleTapWindow;
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
                        doubleTapTimer = growthProfile.DoubleTapWindow;
                        lastTapDirection = 1;
                    }
                }
            }

            // =========================
            // 触发冲刺
            // =========================
            if (triggerDash)
            {
                doubleTapTimer = 0;
                lastTapDirection = 0;

                Vector2 dir = new Vector2(dashDirection, 0f);
                int dashDamage = player.GetWeaponDamage(player.HeldItem);
                dashDamage = (int)(dashDamage * growthProfile.DamageMultiplier);

                Projectile.NewProjectile(
                    player.GetSource_FromThis(),
                    player.Center,
                    dir,
                    ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                    dashDamage,
                    player.GetWeaponKnockback(player.HeldItem),
                    player.whoAmI,
                    0f,
                    dashDirection
                );
            }
        }

        private DashGrowthProfile ResolveDashGrowthProfile()
        {
            if (DownedBossSystem.downedYharon)
            {
                return new DashGrowthProfile(YharonDoubleTapWindow, YharonDashDamageMultiplier);
            }

            if (DownedBossSystem.downedBoomerDuke)
            {
                return new DashGrowthProfile(BoomerDukeDoubleTapWindow, BoomerDukeDashDamageMultiplier);
            }

            if (NPC.downedFishron)
            {
                return new DashGrowthProfile(FishronDoubleTapWindow, FishronDashDamageMultiplier);
            }

            return new DashGrowthProfile(BaseDoubleTapWindow, BaseDashDamageMultiplier);
        }

        private bool HasAnyActiveSkillProjectile(Player player)
        {
            int p1 = ModContent.ProjectileType<BrinyBaron_LeftClick_Swing>();
            int p2 = ModContent.ProjectileType<BrinyBaron_SkillDashTornado_BladeDash>();
            int p3 = ModContent.ProjectileType<BrinyBaron_SkillSpinRush_SpinBlade>();
            int p4 = ModContent.ProjectileType<BrinyBaron_SkillSuperCharge_SuperDash>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (!p.active || p.owner != player.whoAmI)
                    continue;

                if (p.type == p1 || p.type == p2 || p.type == p3 || p.type == p4)
                    return true;
            }

            return false;
        }

        private struct DashGrowthProfile
        {
            public int DoubleTapWindow;
            public float DamageMultiplier;

            public DashGrowthProfile(int doubleTapWindow, float damageMultiplier)
            {
                DoubleTapWindow = doubleTapWindow;
                DamageMultiplier = damageMultiplier;
            }
        }
    }
}
