using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChargedDiffusionChip
{
    public class ChargedDiffusionChipPlayer : ModPlayer
    {
        public bool ChargedDiffusionChipEquipped;

        public override void ResetEffects()
        {
            ChargedDiffusionChipEquipped = false;
        }
    }
}
