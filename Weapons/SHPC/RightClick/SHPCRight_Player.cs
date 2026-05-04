using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    public class SHPCRight_Player : ModPlayer
    {
        public int HeatStage;
        public int HeatProgressTimer;
        public int HeatMaxStage;
        public int HeatUiFadeTimer;
        public int AttackLockoutTimer;

        private const int DetachedUiFadeTime = 24;
        private readonly BalanceSHPC balance = new();
        private int heatDecayTimer;

        public override void PostUpdate()
        {
            if (AttackLockoutTimer > 0)
                AttackLockoutTimer--;

            HeatMaxStage = Utils.Clamp(
                HeatMaxStage > 0 ? HeatMaxStage : balance.GetRightClickMaxHeatLevel(),
                1,
                balance.GetRightClickMaxHeatLevel());

            bool holdingRightClick = HasActiveRightClickHoldout();
            bool holdingSHPC = IsHoldingSHPCLike();
            bool hasHeat = HasAnyHeat();

            if (holdingSHPC && hasHeat)
                HeatUiFadeTimer = DetachedUiFadeTime;
            else if (HeatUiFadeTimer > 0)
                HeatUiFadeTimer--;

            if (Main.myPlayer == Player.whoAmI && hasHeat && (holdingSHPC || HeatUiFadeTimer > 0))
                EnsureDetachedHeatUI();

            if (holdingRightClick || AttackLockoutTimer > 0)
            {
                heatDecayTimer = 0;
                return;
            }

            if (!hasHeat)
            {
                heatDecayTimer = 0;
                return;
            }

            int coolingStep = holdingSHPC ? 2 : 1;
            if (HeatProgressTimer > 0)
            {
                HeatProgressTimer = System.Math.Max(0, HeatProgressTimer - coolingStep);
                heatDecayTimer = 0;
                return;
            }

            int decayTime = GetHeatDecayTime(holdingSHPC);
            heatDecayTimer++;
            if (heatDecayTimer >= decayTime)
            {
                HeatStage--;
                heatDecayTimer = 0;
            }
        }

        public void SyncHeatFromHoldout(int heatStage, int heatProgressTimer, int maxHeatStage)
        {
            HeatStage = Utils.Clamp(heatStage, 0, maxHeatStage);
            HeatMaxStage = Utils.Clamp(maxHeatStage, 1, balance.GetRightClickMaxHeatLevel());
            int fillTime = balance.GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4));
            HeatProgressTimer = HeatStage >= HeatMaxStage
                ? fillTime
                : Utils.Clamp(heatProgressTimer, 0, fillTime);
            HeatUiFadeTimer = DetachedUiFadeTime;
            heatDecayTimer = 0;
        }

        public bool IsHoldingSHPCLike()
        {
            int heldType = Player.HeldItem?.type ?? 0;
            return heldType == ModContent.ItemType<NewLegendSHPC>() ||
                   heldType == ModContent.ItemType<NewLegendSHPCTest>();
        }

        public bool HasActiveRightClickHoldout()
        {
            return Player.ownedProjectileCounts[ModContent.ProjectileType<SHPCRight_HoulOut>()] > 0 ||
                   Player.ownedProjectileCounts[ModContent.ProjectileType<RightClickMortar_HoldOut>()] > 0;
        }

        public bool HasAnyHeat()
        {
            return HeatStage > 0 || HeatProgressTimer > 0;
        }

        public float GetDetachedHeatProgress()
        {
            if (!HasAnyHeat())
                return 0f;

            if (HeatProgressTimer > 0)
            {
                int fillTime = balance.GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4));
                return Utils.Clamp(HeatProgressTimer / (float)fillTime, 0f, 1f);
            }

            int decayTime = GetHeatDecayTime(IsHoldingSHPCLike());
            return 1f - Utils.Clamp(heatDecayTimer / (float)decayTime, 0f, 1f);
        }

        public void SetAttackLockout(int frames)
        {
            if (frames > AttackLockoutTimer)
                AttackLockoutTimer = frames;
        }

        private int GetHeatDecayTime(bool holdingSHPC)
        {
            int completedHeatLevel = Utils.Clamp(HeatStage - 1, 0, 4);
            int normalFillTime = balance.GetHeatFillTime(completedHeatLevel);
            return holdingSHPC
                ? System.Math.Max(1, normalFillTime / 2)
                : System.Math.Max(90, normalFillTime);
        }

        private void EnsureDetachedHeatUI()
        {
            int uiType = ModContent.ProjectileType<SHPCRight_HeatUI>();
            if (Player.ownedProjectileCounts[uiType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Top,
                Microsoft.Xna.Framework.Vector2.Zero,
                uiType,
                0,
                0f,
                Player.whoAmI);
        }
    }
}
