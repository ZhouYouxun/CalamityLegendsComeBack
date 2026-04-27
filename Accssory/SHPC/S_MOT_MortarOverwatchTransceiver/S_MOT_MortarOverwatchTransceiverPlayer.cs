using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_MOT_MortarOverwatchTransceiver
{
    public sealed class S_MOT_MortarOverwatchTransceiverPlayer : ModPlayer
    {
        public bool S_MOT_MortarOverwatchTransceiverEquipped;

        public override void ResetEffects()
        {
            S_MOT_MortarOverwatchTransceiverEquipped = false;
        }
    }
}
