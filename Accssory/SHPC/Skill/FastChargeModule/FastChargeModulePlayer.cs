using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChargeModule
{
    public class FastChargeModulePlayer : ModPlayer
    {
        public bool FastChargeModuleEquipped;

        public override void ResetEffects()
        {
            FastChargeModuleEquipped = false;
        }

        public override void UpdateLifeRegen()
        {
            if (!FastChargeModuleEquipped || Player.lifeRegen <= 0)
                return;

            Player.lifeRegen /= 2;
        }
    }
}
