using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.ChargedDiffusionChip
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
