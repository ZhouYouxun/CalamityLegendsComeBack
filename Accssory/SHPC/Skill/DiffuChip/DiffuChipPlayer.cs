using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.DiffuChip
{
    public class DiffuChipPlayer : ModPlayer
    {
        public bool DiffuChipEquipped;

        public float EmpoweredLeftClickSpreadMultiplier => DiffuChipEquipped ? 0.4f : 1f;

        public override void ResetEffects()
        {
            DiffuChipEquipped = false;
        }
    }
}
