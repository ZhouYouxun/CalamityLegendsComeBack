using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip
{
    public class FastChipPlayer : ModPlayer
    {
        public bool FastChipEquipped;

        public override void ResetEffects()
        {
            FastChipEquipped = false;
        }

        public override void UpdateLifeRegen()
        {
            if (!FastChipEquipped || Player.lifeRegen <= 0)
                return;

            Player.lifeRegen /= 2;
        }
    }
}
