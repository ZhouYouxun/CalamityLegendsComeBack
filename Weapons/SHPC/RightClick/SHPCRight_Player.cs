using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    public class SHPCRight_Player : ModPlayer
    {
        public int HeatStage;
        public int AttackLockoutTimer;

        private int heatDecayTimer;

        public override void PostUpdate()
        {
            if (AttackLockoutTimer > 0)
                AttackLockoutTimer--;

            bool holdingRightClick =
                Player.ownedProjectileCounts[ModContent.ProjectileType<SHPCRight_HoulOut>()] > 0 ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClickMortar_HoldOut>()] > 0;

            if (holdingRightClick || AttackLockoutTimer > 0)
            {
                heatDecayTimer = 0;
                return;
            }

            if (HeatStage <= 0)
                return;

            heatDecayTimer++;
            if (heatDecayTimer >= 90)
            {
                HeatStage--;
                heatDecayTimer = 0;
            }
        }

        public void SetAttackLockout(int frames)
        {
            if (frames > AttackLockoutTimer)
                AttackLockoutTimer = frames;
        }
    }
}
