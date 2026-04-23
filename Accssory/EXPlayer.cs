using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class EXPlayer : ModPlayer
    {
        public bool EXAccessoryEquipped;

        public override void ResetEffects()
        {
            EXAccessoryEquipped = false;
        }
    }
}
