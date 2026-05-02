using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Accssory.SHPC.S_OVR_ChargeLimitBreaker;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class NewLegend_EXPlayer : ModPlayer
    {
        // EX条当前值
        public int EXValue;

        // 两分钟攒满：2 × 60 × 60 = 7200帧
        public const int BaseEXMax = 7200;
        public const int EXDisplayMax = 60;

        // 是否已满
        public bool EXFull => EXValue >= GetCurrentEXMax(Player);
        public int EXDisplayValue => Utils.Clamp(EXValue / GetFramesPerDisplayUnit(Player), 0, EXDisplayMax);
        public bool EXUnlocked => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        public static int GetCurrentEXMax(Player player)
        {
            if (player.GetModPlayer<S_OVR_ChargeLimitBreakerPlayer>().S_OVR_ChargeLimitBreakerEquipped)
                return BaseEXMax / 2;

            return BaseEXMax;
        }

        public static int GetFramesPerDisplayUnit(Player player)
        {
            return System.Math.Max(1, GetCurrentEXMax(player) / EXDisplayMax);
        }

        public override void ResetEffects()
        {
            // 这里先不放别的效果，保持干净
        }

        public override void PostUpdate()
        {
            int maxEX = GetCurrentEXMax(Player);

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
                EXValue = Utils.Clamp(EXValue, 0, maxEX);
            }
            else
            {
                // 不手持：以两倍速度下降
                EXValue -= 2;

                if (EXValue < 0)
                    EXValue = 0;
            }

            if (EXValue > maxEX)
                EXValue = maxEX;
        }

        // 供外部调用：清空 EX 条
        public void ResetEX()
        {
            EXValue = 0;
        }
    }
}
