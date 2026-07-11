using Terraria;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip;
using CalamityLegendsComeBack.Weapons;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret;
namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class NewLegend_EXPlayer : ModPlayer
    {
        // EX条当前值
        public int EXValue;
        private bool wasEXReady;
        private int fastChipRightClickChargeTimer;

// 两分钟攒满：2 × 60 × 60 = 7200帧
        public const int BaseEXMax = 7200;
        public const int EXDisplayMax = 60;
        private const int PassiveChargeTime = 60;

        // 是否已满
        public bool EXFull => EXValue >= GetCurrentEXMax(Player);
        public int EXDisplayValue => Utils.Clamp(EXValue / GetFramesPerDisplayUnit(Player), 0, EXDisplayMax);
        public bool EXUnlocked =>
            BalanceSHPC.GetCompletedStageIndex() >= 5 ||
            Player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().PermanentEXUnlock ||
            Player.GetModPlayer<global::CalamityLegendsComeBack.Accssory.LegendaryEmblemPlayer>().EXAccessoryEquipped;

        public static int GetCurrentEXMax(Player player)
        {
            return BaseEXMax;
        }

        public static int GetFramesPerDisplayUnit(Player player)
        {
            return System.Math.Max(1, GetCurrentEXMax(player) / EXDisplayMax);
        }

        public static int GetBaseFramesPerDisplayUnit()
        {
            return System.Math.Max(1, BaseEXMax / EXDisplayMax);
        }

        public override void ResetEffects()
        {
            // 这里先不放别的效果，保持干净
        }

        public override void PostUpdate()
        {
            int maxEX = GetCurrentEXMax(Player);

            if (!EXUnlocked)
            {
                EXValue = 0;
                LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, false);
                return;
            }

            // 检测当前是否手持 SHPC
            bool holdingSHPC = Player.HeldItem != null &&
                               !Player.HeldItem.IsAir &&
                               Player.HeldItem.ModItem is NewLegendSHPC;

            if (holdingSHPC)
            {
                EXValue += System.Math.Max(1, GetBaseFramesPerDisplayUnit() / PassiveChargeTime);
                ApplyFastChipRightClickCharge();
                EXValue = Utils.Clamp(EXValue, 0, maxEX);
            }
            else
            {
                fastChipRightClickChargeTimer = 0;
            }

            if (EXValue > maxEX)
                EXValue = maxEX;

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, EXValue >= maxEX);
        }

        // 供外部调用：清空 EX 条
        public void ResetEX()
        {
            EXValue = 0;
        }

        private void ApplyFastChipRightClickCharge()
        {
            if (!Player.GetModPlayer<FastChipPlayer>().FastChipEquipped ||
                !HasActiveSHPCRightClickHoldout())
            {
                fastChipRightClickChargeTimer = 0;
                return;
            }

            fastChipRightClickChargeTimer++;
            if (fastChipRightClickChargeTimer < 60)
                return;

            fastChipRightClickChargeTimer = 0;
            EXValue += GetBaseFramesPerDisplayUnit();
        }

        private bool HasActiveSHPCRightClickHoldout()
        {
            return Player.ownedProjectileCounts[ModContent.ProjectileType<SHPCRight_HoulOut>()] > 0 ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClickMortar_HoldOut>()] > 0 ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<MilitaryCaller_HoldOut>()] > 0 ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<ProjectilePossessionHoldout>()] > 0;
        }
    }
}
