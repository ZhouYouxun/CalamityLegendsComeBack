using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    internal class Dash_Trigger : ModPlayer
    {
        private int doubleTapTimer = 0;

        public override void ResetEffects()
        {
            if (doubleTapTimer > 0)
                doubleTapTimer--;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Player player = Player;

            // =========================
            // 条件：必须手持武器
            // =========================
            if (player.HeldItem.type != ModContent.ItemType<NewLegendBrinyBaron>())
                return;

            // =========================
            // 禁止条件：已有弹幕
            // =========================
            if (HasAnyActiveSkillProjectile(player))
                return;

            bool triggerDash = false;

            // =========================
            // 优先：专用冲刺键（如果你以后绑定）
            // =========================
            // 这里暂时留空，你以后可以加 Keybind

            // =========================
            // fallback：双击左右
            // =========================
            if (player.controlLeft && player.releaseLeft)
            {
                if (doubleTapTimer > 0)
                    triggerDash = true;
                else
                    doubleTapTimer = 15;
            }

            if (player.controlRight && player.releaseRight)
            {
                if (doubleTapTimer > 0)
                    triggerDash = true;
                else
                    doubleTapTimer = 15;
            }

            // =========================
            // 触发冲刺
            // =========================
            if (triggerDash)
            {
                Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX);

                Projectile.NewProjectile(
                    player.GetSource_FromThis(),
                    player.Center,
                    dir * 1f,
                    ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                    player.GetWeaponDamage(player.HeldItem),
                    player.GetWeaponKnockback(player.HeldItem),
                    player.whoAmI
                );
            }
        }

        // =========================
        // 检测是否已有技能弹幕
        // =========================
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
    }
}