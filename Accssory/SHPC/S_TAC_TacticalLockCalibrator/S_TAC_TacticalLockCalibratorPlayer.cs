using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_TAC_TacticalLockCalibrator
{
    public class S_TAC_TacticalLockCalibratorPlayer : ModPlayer
    {
        public bool S_TAC_TacticalLockCalibratorEquipped;

        public override void ResetEffects()
        {
            S_TAC_TacticalLockCalibratorEquipped = false;
        }
    }
}
