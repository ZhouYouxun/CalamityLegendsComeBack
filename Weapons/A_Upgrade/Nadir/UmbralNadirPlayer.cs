using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir
{
    /// <summary>
    /// 维护左键三段连招的进度。
    /// 连招顺序固定为 上劈(0) → 下劈(1) → 冲刺(2) → 上劈(0)…，
    /// 每次左键 holdout 生成时读取并推进；停手一段时间后自动复位到上劈。
    /// </summary>
    public class UmbralNadirPlayer : ModPlayer
    {
        /// <summary>下一次挥砍所处的连招阶段（0 上劈 / 1 下劈 / 2 冲刺）。</summary>
        public int ComboStage;

        /// <summary>距离上一次挥砍生成的帧数；超过阈值则复位连招。</summary>
        private int comboIdleTimer;

        // 略大于单段挥砍的存在时长，保证连续挥砍不会被误判为断连。
        private const int ComboResetFrames = 40;

        /// <summary>读取当前连招阶段并推进到下一段（holdout 生成时调用）。</summary>
        public int ConsumeComboStage()
        {
            int stage = ComboStage;
            ComboStage = (ComboStage + 1) % 3;
            comboIdleTimer = 0;
            return stage;
        }

        /// <summary>holdout 存活期间每帧调用，保持连招不被判定为断连。</summary>
        public void KeepComboAlive() => comboIdleTimer = 0;

        public override void PostUpdate()
        {
            if (Player.HeldItem is null || Player.HeldItem.type != ModContent.ItemType<UmbralNadir>())
            {
                ComboStage = 0;
                comboIdleTimer = 0;
                return;
            }

            comboIdleTimer++;
            if (comboIdleTimer > ComboResetFrames)
                ComboStage = 0;
        }
    }
}
