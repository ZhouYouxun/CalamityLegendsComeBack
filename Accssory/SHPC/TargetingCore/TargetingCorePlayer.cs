using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.TargetingCore
{
    public class TargetingCorePlayer : ModPlayer
    {
        public bool TargetingCoreEquipped;

        public override void ResetEffects()
        {
            TargetingCoreEquipped = false;
        }
    }
}
