using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.RemembranceCore
{
    public class RemembranceCorePlayer : ModPlayer
    {
        public bool RemembranceCoreEquipped;

        public override void ResetEffects()
        {
            RemembranceCoreEquipped = false;
        }
    }
}
