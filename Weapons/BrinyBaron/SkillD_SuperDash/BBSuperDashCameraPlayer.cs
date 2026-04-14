using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSuperDashCameraPlayer : ModPlayer
    {
        private bool cameraLocked;
        private int targetNpcIndex = -1;
        private float impactShakePower;

        public override void Initialize()
        {
            ClearLock();
        }

        public override void UpdateDead()
        {
            ClearLock();
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || !cameraLocked)
                return;

            if (!BBSuperDashTargeting.IsTargetValid(targetNpcIndex) || !HasActiveSuperDashProjectile())
                ClearLock();
        }

        public override void ModifyScreenPosition()
        {
            if (Player.whoAmI != Main.myPlayer || !cameraLocked)
                return;

            if (!BBSuperDashTargeting.IsTargetValid(targetNpcIndex))
            {
                ClearLock();
                return;
            }

            Vector2 focus = Main.npc[targetNpcIndex].Center;
            Main.screenPosition = focus - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            float totalShake = impactShakePower + Player.Calamity().GeneralScreenShakePower * 0.35f;
            if (totalShake > 0.05f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(totalShake, totalShake);
                impactShakePower *= 0.82f;
            }
            else
                impactShakePower = 0f;

            Main.screenPosition.X = MathHelper.Clamp(Main.screenPosition.X, 0f, Main.maxTilesX * 16f - Main.screenWidth);
            Main.screenPosition.Y = MathHelper.Clamp(Main.screenPosition.Y, 0f, Main.maxTilesY * 16f - Main.screenHeight);
        }

        public void LockToTarget(int npcIndex)
        {
            if (Player.whoAmI != Main.myPlayer || !BBSuperDashTargeting.IsTargetValid(npcIndex))
                return;

            cameraLocked = true;
            targetNpcIndex = npcIndex;
        }

        public void ClearLock()
        {
            cameraLocked = false;
            targetNpcIndex = -1;
            impactShakePower = 0f;
        }

        public void AddImpactShake(float power)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            impactShakePower = MathHelper.Max(impactShakePower, power);
        }

        private bool HasActiveSuperDashProjectile()
        {
            int superDashType = ModContent.ProjectileType<Z_BrinyBaron_SkillSuperCharge_SuperDash>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == superDashType)
                    return true;
            }

            return false;
        }
    }
}
