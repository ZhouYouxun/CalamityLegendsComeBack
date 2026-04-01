using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class NewLegend_EXPlayer : ModPlayer
    {
        // EX条当前值
        public int EXValue;

        // 两分钟攒满：2 × 60 × 60 = 7200帧
        public const int EXMax = 7200;

        // 是否已满
        public bool EXFull => EXValue >= EXMax;

        public override void ResetEffects()
        {
            // 这里先不放别的效果，保持干净
        }

        public override void PostUpdate()
        {
            // 检测当前是否手持 SHPC
            bool holdingSHPC = Player.HeldItem != null &&
                               !Player.HeldItem.IsAir &&
                               Player.HeldItem.ModItem is NewLegendSHPC;

            if (holdingSHPC)
            {
                // 手持：正常增长
                if (EXValue < EXMax)
                    EXValue++;
            }
            else
            {
                // 不手持：以两倍速度下降
                EXValue -= 2;

                if (EXValue < 0)
                    EXValue = 0;
            }

            // 手持时逐渐积攒
            if (EXValue < EXMax)
                EXValue++;
            else
                EXValue = EXMax;
        }

        // 供外部调用：清空 EX 条
        public void ResetEX()
        {
            EXValue = 0;
        }
    }
}