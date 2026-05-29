using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.Passive
{
    // 承天之佑被动：手持青霆剑时提供一次可消耗闪避和回血。
    internal sealed class AzureThunderPassivePlayer : ModPlayer
    {
        // 普通状态 99 秒冷却，天理真和期间冷却被压缩到 9 秒。
        private const int NormalCooldown = 99 * 60;
        private const int HarmonyCooldown = 9 * 60;

        // holdingAzureThunder 每帧重置，由 AzureThunder.HoldItem 重新写入。
        private bool holdingAzureThunder;
        private int dodgeCooldown;

        public override void ResetEffects()
        {
            // 防止切走武器后被动继续生效。
            holdingAzureThunder = false;
        }

        public override void UpdateDead()
        {
            // 死亡时清空手持标记和闪避冷却，复活后重新计算。
            holdingAzureThunder = false;
            dodgeCooldown = 0;
        }

        public override void PostUpdate()
        {
            // 冷却以帧为单位递减。
            if (dodgeCooldown > 0)
                dodgeCooldown--;

            // 进入终极状态时，把剩余冷却压到终极状态上限以内。
            if (IsHarmonyActive() && dodgeCooldown > HarmonyCooldown)
                dodgeCooldown = HarmonyCooldown;
        }

        public void SetHoldingAzureThunder()
        {
            holdingAzureThunder = true;
        }

        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            // tModLoader 闪避钩子返回 true 表示这次伤害被青霆剑消耗闪避抵消。
            if (!CanDodge())
                return false;

            TriggerDodge();
            return true;
        }

        private bool CanDodge()
        {
            // 必须手持青霆剑、进度已解锁、玩家存活且冷却结束。
            return holdingAzureThunder &&
                AzureThunderProgression.DodgeUnlocked &&
                Player.active &&
                !Player.dead &&
                Player.HeldItem != null &&
                Player.HeldItem.type == ModContent.ItemType<AzureThunder>() &&
                dodgeCooldown <= 0;
        }

        private bool IsHarmonyActive()
        {
            return Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>());
        }

        private void TriggerDodge()
        {
            // 先写冷却，后续治疗和无敌帧都属于这次闪避的即时收益。
            dodgeCooldown = IsHarmonyActive() ? HarmonyCooldown : NormalCooldown;

            // 回血不能超过当前最大生命。
            int healAmount = System.Math.Min(AzureThunderProgression.DodgeHealAmount, Player.statLifeMax2 - Player.statLife);
            if (healAmount > 0)
            {
                Player.statLife += healAmount;
                Player.HealEffect(healAmount, true);
            }

            // 给予短暂无敌帧，免疫本次伤害后的连击。
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = System.Math.Max(Player.immuneTime, 60);

            // 闪避成功时爆出青/金粒子，给玩家明确反馈。
            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height),
                    DustID.FireworksRGB,
                    Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 8f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
            }

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.72f, Pitch = 0.18f }, Player.Center);
        }
    }
}
