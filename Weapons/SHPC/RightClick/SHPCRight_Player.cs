using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.CommandAscend;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.MilitaryCaller;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule;
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
        public int ForcedShutdownCoolingTimer;
        public int HeatDissipationPauseTimer;
        public int HeatBarOutlineTimer;

        private const int DetachedUiFadeTime = 60;
        private readonly BalanceSHPC balance = new();
        private int forcedShutdownCoolingDuration;

        private int GetHeatFillTime(int heatStage, int maxHeatStage)
        {
            int defaultFillTime = balance.GetHeatFillTime(heatStage, maxHeatStage);
            return Player.GetModPlayer<HeatModulePlayer>().GetHeatFillTime(defaultFillTime);
        }

        public override void PostUpdate()
        {
            if (AttackLockoutTimer > 0)
                AttackLockoutTimer--;

            if (HeatDissipationPauseTimer > 0)
                HeatDissipationPauseTimer--;

            HeatMaxStage = Utils.Clamp(
                HeatMaxStage > 0 ? HeatMaxStage : balance.GetRightClickMaxHeatLevel(),
                1,
                balance.GetRightClickMaxHeatLevel());

            if (ForcedShutdownCoolingTimer > 0)
            {
                UpdateForcedShutdownCooling();
                return;
            }

            bool holdingRightClick = HasActiveRightClickHoldout();
            bool hasHeat = HasAnyHeat();
            bool suppressHeatUI = ShouldSuppressHeatUI();

            if (hasHeat)
                HeatUiFadeTimer = DetachedUiFadeTime;
            else if (HeatUiFadeTimer > 0)
                HeatUiFadeTimer--;

            if (Main.myPlayer == Player.whoAmI && !suppressHeatUI && (hasHeat || HeatUiFadeTimer > 0))
                EnsureDetachedHeatUI();

            if (holdingRightClick)
                return;

            if (!hasHeat)
                return;

            float heatUnits = GetTotalHeatUnits();
            heatUnits -= 1f / BalanceSHPC.NormalHeatDecayTime;
            ApplyHeatUnits(heatUnits);
        }

        public void SyncHeatFromHoldout(int heatStage, int heatProgressTimer, int maxHeatStage)
        {
            HeatStage = Utils.Clamp(heatStage, 0, maxHeatStage);
            HeatMaxStage = Utils.Clamp(maxHeatStage, 1, balance.GetRightClickMaxHeatLevel());
            int fillTime = GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4), HeatMaxStage);
            HeatProgressTimer = HeatStage >= HeatMaxStage
                ? fillTime
                : Utils.Clamp(heatProgressTimer, 0, fillTime);
            HeatUiFadeTimer = DetachedUiFadeTime;
        }

        public bool IsForcedShutdownCooling()
        {
            return ForcedShutdownCoolingTimer > 0;
        }

        public bool CanSustainMaximumHeat()
        {
            return HeatMaxStage >= 5 && HeatStage >= 5;
        }

        public void StartForcedShutdownCooling(int frames, int maxHeatStage)
        {
            forcedShutdownCoolingDuration = System.Math.Max(1, frames);
            ForcedShutdownCoolingTimer = forcedShutdownCoolingDuration;
            HeatMaxStage = Utils.Clamp(maxHeatStage, 1, balance.GetRightClickMaxHeatLevel());
            ApplyHeatUnits(HeatMaxStage);
            HeatUiFadeTimer = DetachedUiFadeTime;
            SetAttackLockout(frames);
        }

        public bool IsHoldingSHPCLike()
        {
            int heldType = Player.HeldItem?.type ?? 0;
            return heldType == ModContent.ItemType<NewLegendSHPC>();
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

        public bool ShouldSuppressHeatUI()
        {
            return Player.GetModPlayer<CommandAscendPlayer>().CommandAscendEquipped ||
                Player.GetModPlayer<MilitaryCallerPlayer>().MilitaryCallerEquipped;
        }

        public float GetDetachedHeatProgress()
        {
            if (!HasAnyHeat())
                return 0f;

            if (HeatProgressTimer > 0)
            {
                int fillTime = GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4), HeatMaxStage);
                return Utils.Clamp(HeatProgressTimer / (float)fillTime, 0f, 1f);
            }

            return HeatStage > 0 ? 1f : 0f;
        }

        public float GetHeatUiFadeOpacity()
        {
            return Utils.Clamp(HeatUiFadeTimer / (float)DetachedUiFadeTime, 0f, 1f);
        }

        public int GetDisplayedHeatLevel()
        {
            if (!HasAnyHeat())
                return 0;

            if (HeatStage >= HeatMaxStage)
                return HeatMaxStage;

            return Utils.Clamp(HeatStage + 1, 1, HeatMaxStage);
        }

        public void SetAttackLockout(int frames)
        {
            if (frames > AttackLockoutTimer)
                AttackLockoutTimer = frames;
        }

        public void PauseHeatDissipation(int frames)
        {
            if (frames > HeatDissipationPauseTimer)
                HeatDissipationPauseTimer = frames;
        }

        public void TriggerHeatBarOutlinePulse(int frames)
        {
            if (frames > HeatBarOutlineTimer)
                HeatBarOutlineTimer = frames;
        }

        private void UpdateForcedShutdownCooling()
        {
            int duration = System.Math.Max(1, forcedShutdownCoolingDuration);
            ForcedShutdownCoolingTimer--;

            float heatUnits = HeatMaxStage * Utils.Clamp(ForcedShutdownCoolingTimer / (float)duration, 0f, 1f);
            ApplyHeatUnits(heatUnits);
            HeatUiFadeTimer = DetachedUiFadeTime;

            if (Main.myPlayer == Player.whoAmI && !ShouldSuppressHeatUI() && HasAnyHeat())
                EnsureDetachedHeatUI();
        }

        private float GetTotalHeatUnits()
        {
            if (HeatStage >= HeatMaxStage)
                return HeatMaxStage;

            int fillTime = GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4), HeatMaxStage);
            float progress = fillTime > 0 ? HeatProgressTimer / (float)fillTime : 0f;
            return System.Math.Max(0f, HeatStage + Utils.Clamp(progress, 0f, 1f));
        }

        private void ApplyHeatUnits(float heatUnits)
        {
            heatUnits = Utils.Clamp(heatUnits, 0f, HeatMaxStage);
            if (heatUnits <= 0f)
            {
                HeatStage = 0;
                HeatProgressTimer = 0;
                return;
            }

            if (heatUnits >= HeatMaxStage)
            {
                HeatStage = HeatMaxStage;
                HeatProgressTimer = GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4), HeatMaxStage);
                return;
            }

            HeatStage = Utils.Clamp((int)System.MathF.Floor(heatUnits), 0, HeatMaxStage);
            float fractionalHeat = heatUnits - HeatStage;
            int fillTime = GetHeatFillTime(Utils.Clamp(HeatStage, 0, 4), HeatMaxStage);
            HeatProgressTimer = Utils.Clamp((int)System.MathF.Round(fractionalHeat * fillTime), 0, fillTime);
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
