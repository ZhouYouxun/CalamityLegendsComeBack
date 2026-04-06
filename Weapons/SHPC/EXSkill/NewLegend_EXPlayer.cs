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
        public const int EXDisplayMax = 60;
        public const int FramesPerDisplayUnit = EXMax / EXDisplayMax;

        // 是否已满
        public bool EXFull => EXValue >= EXMax;
        public int EXDisplayValue => Utils.Clamp(EXValue / FramesPerDisplayUnit, 0, EXDisplayMax);
        public bool EXUnlocked => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        public override void ResetEffects()
        {
            // 这里先不放别的效果，保持干净
        }

        public override void PostUpdate()
        {
            if (!EXUnlocked)
            {
                EXValue = 0;
                return;
            }

            // 检测当前是否手持 SHPC
            bool holdingSHPC = Player.HeldItem != null &&
                               !Player.HeldItem.IsAir &&
                               Player.HeldItem.ModItem is NewLegendSHPC;

            if (holdingSHPC)
            {
                // 手持：每帧增长1，满值正好需要 7200 帧（两分钟）
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

            if (EXValue > EXMax)
                EXValue = EXMax;
        }

        // 供外部调用：清空 EX 条
        public void ResetEX()
        {
            EXValue = 0;
        }
    }
}
