using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip
{
    public class FastChipPlayer : ModPlayer
    {
        public bool FastChipEquipped;
        public bool FastChipDrawbackActive;

        public override void ResetEffects()
        {
            FastChipEquipped = false;
            FastChipDrawbackActive = false;
        }

        public override void UpdateLifeRegen()
        {
            if (!FastChipDrawbackActive || Player.lifeRegen <= 0)
                return;

            Player.lifeRegen /= 2;

            int heatStage = Player.GetModPlayer<global::CalamityLegendsComeBack.Weapons.SHPC.RightClick.SHPCRight_Player>().HeatStage;
            if (heatStage > 0)
                Player.lifeRegen -= heatStage * 4;
        }
    }
}
