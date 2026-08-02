using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 战术同步率（Tactical Sync Rate）资源载体。
    /// - 命中获取、停火/未命中衰减、受伤损失。
    /// - 提供给持械弹幕读取阶段、消耗（右键重炮）、清空（大招）。
    /// </summary>
    public class M4A1Player : ModPlayer
    {
        /// <summary>当前战术同步率 0—100。</summary>
        public float SyncRate;

        /// <summary>本帧是否手持 M4A1（每帧 ResetEffects 清零，HoldItem 里置真）。</summary>
        public bool HoldingM4A1;

        /// <summary>距上次获取同步率的帧数（用于延迟衰减）。</summary>
        private int ticksSinceLastGain;

        /// <summary>UI 用：最近一次获取的高亮计时。</summary>
        public int GainFlashTimer;

        /// <summary>UI 用：最近一次受伤/消耗的警示计时。</summary>
        public int LossFlashTimer;

        public int SyncStage => BalanceM4A1.GetSyncStage(SyncRate);
        public bool FullySynced => SyncRate >= BalanceM4A1.Stage_FullSync;

        public override void ResetEffects()
        {
            HoldingM4A1 = false;
        }

        public void SetHolding() => HoldingM4A1 = true;

        /// <summary>命中获取同步率。boss 命中更多，暴击追加。</summary>
        public void GainSync(bool isBoss, bool crit)
        {
            float amount = isBoss ? BalanceM4A1.SyncGainPerBossHit : BalanceM4A1.SyncGainPerNormalHit;
            if (crit)
                amount += BalanceM4A1.SyncGainCritBonus;

            SyncRate = MathHelper.Clamp(SyncRate + amount, 0f, BalanceM4A1.MaxSyncRate);
            ticksSinceLastGain = 0;
            GainFlashTimer = 10;
        }

        /// <summary>右键重炮消耗：返回消耗前的阶段（决定重炮形态），并扣除同步率（可到 0）。</summary>
        public int SpendForRightClick()
        {
            int tierBeforeSpend = SyncStage;
            SyncRate = Math.Max(0f, SyncRate - BalanceM4A1.SyncCostRightClick);
            LossFlashTimer = 12;
            return tierBeforeSpend;
        }

        /// <summary>大招释放：清空同步率。</summary>
        public void ConsumeAllForUltimate()
        {
            SyncRate = 0f;
            LossFlashTimer = 18;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (SyncRate <= 0f)
                return;

            // 受伤损失 25，但不因一次失误直接清零。
            SyncRate = Math.Max(0f, SyncRate - BalanceM4A1.SyncLossOnHurt);
            ticksSinceLastGain = 0; // 受伤后重新计延迟，给玩家喘息
            LossFlashTimer = 14;
        }

        public override void PostUpdate()
        {
            if (GainFlashTimer > 0) GainFlashTimer--;
            if (LossFlashTimer > 0) LossFlashTimer--;

            if (SyncRate <= 0f)
                return;

            if (ticksSinceLastGain < BalanceM4A1.SyncDecayDelayTicks)
            {
                ticksSinceLastGain++;
                return;
            }

            SyncRate = Math.Max(BalanceM4A1.SyncMinAfterDecay, SyncRate - BalanceM4A1.SyncDecayPerSecond / 60f);
        }

        public static M4A1Player Get(Player player) => player.GetModPlayer<M4A1Player>();
    }
}
