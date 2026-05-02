using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryEmblemPlayer : ModPlayer
    {
        public bool EXAccessoryEquipped;

        public override void ResetEffects()
        {
            EXAccessoryEquipped = false;
        }
    }
}
