using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.MilitaryCaller
{
    public sealed class MilitaryCallerPlayer : ModPlayer
    {
        public bool MilitaryCallerEquipped;

        public override void ResetEffects()
        {
            MilitaryCallerEquipped = false;
        }
    }
}
