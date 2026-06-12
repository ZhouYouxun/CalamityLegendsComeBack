using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.ConcentrationModule
{
    public class ConcentrationModulePlayer : ModPlayer
    {
        public bool ConcentrationModuleEquipped;

        public float EmpoweredLeftClickSpreadMultiplier => ConcentrationModuleEquipped ? 0.4f : 1f;

        public override void ResetEffects()
        {
            ConcentrationModuleEquipped = false;
        }
    }
}
