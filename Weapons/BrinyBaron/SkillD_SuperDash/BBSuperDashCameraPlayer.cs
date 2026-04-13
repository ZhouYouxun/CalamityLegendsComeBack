using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSuperDashCameraPlayer : ModPlayer
    {
        private bool cameraLocked;
        private bool focusInitialized;
        private int targetNpcIndex = -1;
        private Vector2 smoothedFocus;

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

            Vector2 targetCenter = Main.npc[targetNpcIndex].Center;
            Vector2 desiredFocus = Vector2.Lerp(targetCenter, Player.Center, 0.1f);
            if (!focusInitialized)
            {
                smoothedFocus = desiredFocus;
                focusInitialized = true;
            }
            else
            {
                float distance = Vector2.Distance(smoothedFocus, desiredFocus);
                float smoothing = MathHelper.Lerp(0.08f, 0.2f, Utils.GetLerpValue(32f, 280f, distance, true));
                smoothedFocus = Vector2.SmoothStep(smoothedFocus, desiredFocus, smoothing);
            }

            Main.screenPosition = smoothedFocus - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
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
            focusInitialized = false;
            targetNpcIndex = -1;
            smoothedFocus = Vector2.Zero;
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
