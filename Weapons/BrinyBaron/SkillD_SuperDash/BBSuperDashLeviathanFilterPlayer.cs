using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSuperDashLeviathanFilterPlayer : ModPlayer
    {
        private bool enableLeviathanFilter;

        public override void ResetEffects()
        {
            enableLeviathanFilter = false;
        }

        public void EnableFilter()
        {
            enableLeviathanFilter = true;
        }

        public override void PostUpdateMiscEffects()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (enableLeviathanFilter)
                Player.Calamity().monolithLeviathanShader = 30;
        }
    }
}
