using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    // 记录 BlossomFlux 的传奇槽蓄积，每秒长 1 层，触发后清空。
    internal class BFEXPlayer : ModPlayer
    {
        public const int UltimateDisplayMax = 5;
        public const int FramesPerDisplayUnit = 60;
        public const int MaxChargeFrames = UltimateDisplayMax * FramesPerDisplayUnit;

        public int UltimateChargeFrames;

        public int DisplayValue => Utils.Clamp(UltimateChargeFrames / FramesPerDisplayUnit, 0, UltimateDisplayMax);
        public bool CanTriggerUltimate => Player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked && UltimateChargeFrames >= MaxChargeFrames;

        public override void ResetEffects()
        {
            if (UltimateChargeFrames > MaxChargeFrames)
                UltimateChargeFrames = MaxChargeFrames;
        }

        public override void PostUpdate()
        {
            if (!Player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked)
            {
                UltimateChargeFrames = 0;
                return;
            }

            if (UltimateChargeFrames < MaxChargeFrames)
                UltimateChargeFrames++;
        }

        public void ConsumeUltimateCharge()
        {
            UltimateChargeFrames = 0;
        }

        public void FillUltimateCharge()
        {
            UltimateChargeFrames = MaxChargeFrames;
        }
    }
}
