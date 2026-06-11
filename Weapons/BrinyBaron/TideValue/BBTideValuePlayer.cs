using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue
{
    internal class BBTideValuePlayer : ModPlayer
    {
        private static readonly int[] TideMaxValues = { 2, 3, 4, 5, 6, 7, 8 };

        public const int MaxDesignedTideCap = 8;
        public const float TideDamageBonusPerStack = 0.03f;
        public const int TideChargeMax = 90 * 60;
        public const int TideDisplayMax = 90;
        private const int PassiveTideRegenInterval = 300;
        public int TideValue;
        public int TideChargeValue;
        private bool wasTideReady;
        private bool wasTideChargeReady;
        private int passiveTideRegenTimer;

        public int CurrentTideMax => GetCurrentTideMax() + Player.GetModPlayer<BBAccessoryPlayer>().BonusTideMax;
        public bool TideFull => TideValue >= CurrentTideMax;
        public bool TideChargeFull => TideChargeValue >= TideChargeMax;
        public int TideDisplayValue => Utils.Clamp(TideChargeValue / GetFramesPerDisplayUnit(), 0, TideDisplayMax);
        public float TideDamageMultiplier => 1f + TideValue * TideDamageBonusPerStack;

        public override void PostUpdateEquips()
        {
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;

            UpdatePassiveTideRegen();
            UpdateTideCharge();
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideReady, TideFull);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideChargeReady, TideChargeFull);
        }

        public void AddTide(int amount = 1)
        {
            if (amount <= 0)
                return;

            TideValue += amount;
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;
        }

        public bool TryConsumeTide(int amount = 1)
        {
            if (amount <= 0)
                return true;

            if (TideValue < amount)
                return false;

            TideValue -= amount;
            if (TideValue < 0)
                TideValue = 0;

            return true;
        }

        public void ResetTide()
        {
            TideValue = 0;
            passiveTideRegenTimer = 0;
        }

        public void ResetTideCharge()
        {
            TideChargeValue = 0;
        }

        public static int GetFramesPerDisplayUnit()
        {
            return Math.Max(1, TideChargeMax / TideDisplayMax);
        }

        private void UpdateTideCharge()
        {
            bool holdingBrinyBaron = Player.HeldItem != null &&
                                     !Player.HeldItem.IsAir &&
                                     Player.HeldItem.ModItem is NewLegendBrinyBaron;

            if (holdingBrinyBaron)
                TideChargeValue++;

            TideChargeValue = Utils.Clamp(TideChargeValue, 0, TideChargeMax);
        }

        private void UpdatePassiveTideRegen()
        {
            if (!Player.active || Player.dead || TideValue >= CurrentTideMax)
            {
                passiveTideRegenTimer = 0;
                return;
            }

            passiveTideRegenTimer++;
            if (passiveTideRegenTimer < PassiveTideRegenInterval)
                return;

            passiveTideRegenTimer = 0;
            AddTide();
        }

        private static int GetCurrentTideMax()
        {
            return TideMaxValues[GetTideGrowthTier()];
        }

        private static int GetTideGrowthTier()
        {
            if (DownedBossSystem.downedYharon)
                return 6;
            if (DownedBossSystem.downedBoomerDuke)
                return 5;
            if (NPC.downedMoonlord)
                return 4;
            if (NPC.downedFishron)
                return 3;
            if (DownedBossSystem.downedCalamitasClone || NPC.downedPlantBoss)
                return 2;
            if (Main.hardMode)
                return 1;

            return 0;
        }
    }
}
