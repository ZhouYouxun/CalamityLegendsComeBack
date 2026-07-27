using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir
{
    /// <summary>
    /// 冥蚀天底的玩家侧状态：
    /// 1) 左键三段连招进度（上挑→劈落→冲刺→复位）；
    /// 2) 奇点充能——左键/回旋命中累积，回旋斩迹蓄满后释放黑洞新星消耗它。
    /// </summary>
    public class UmbralNadirPlayer : ModPlayer
    {
        // ===== 连招 =====
        public int ComboStage;
        private int comboIdleTimer;
        private const int ComboResetFrames = 40;

        // ===== 奇点充能 =====
        public float SingularityCharge;
        public int NovaCooldown;

        public bool NovaReady => SingularityCharge >= UmbralNadirBalance.SingularityChargeMax && NovaCooldown <= 0;
        public float ChargeRatio => System.Math.Clamp(SingularityCharge / UmbralNadirBalance.SingularityChargeMax, 0f, 1f);

        public int ConsumeComboStage()
        {
            int stage = ComboStage;
            ComboStage = (ComboStage + 1) % 3;
            comboIdleTimer = 0;
            return stage;
        }

        public void KeepComboAlive() => comboIdleTimer = 0;

        public void AddCharge(float amount)
        {
            if (NovaCooldown <= 0)
                SingularityCharge = System.Math.Min(UmbralNadirBalance.SingularityChargeMax, SingularityCharge + amount);
        }

        public void SpendChargeForNova()
        {
            SingularityCharge = 0f;
            NovaCooldown = UmbralNadirBalance.SpinNovaCooldown;
        }

        public override void PostUpdate()
        {
            if (NovaCooldown > 0)
                NovaCooldown--;

            bool holding = Player.HeldItem is not null && Player.HeldItem.type == ModContent.ItemType<UmbralNadir>();
            if (!holding)
            {
                ComboStage = 0;
                comboIdleTimer = 0;
                // 收起武器后奇点缓慢流失
                if (SingularityCharge > 0f)
                    SingularityCharge = System.Math.Max(0f, SingularityCharge - 0.5f);
                return;
            }

            comboIdleTimer++;
            if (comboIdleTimer > ComboResetFrames)
                ComboStage = 0;
        }
    }
}
