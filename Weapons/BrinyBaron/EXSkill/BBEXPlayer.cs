using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.EXSkill
{
    internal class BBEXPlayer : ModPlayer
    {
        private static readonly int[] TideMaxValues = { 2, 3, 4, 5, 6, 7, 8 };

        public const int MaxDesignedTideCap = 8;
        public const float TideDamageBonusPerStack = 0.03f;
        public const int EXMax = 90 * 60;
        public const int EXDisplayMax = 90;
        private const int PassiveTideRegenInterval = 300;
        public int TideValue;
        public int EXValue;
        private bool wasTideReady;
        private bool wasEXReady;
        private int passiveTideRegenTimer;

        public int CurrentTideMax => GetCurrentTideMax() + Player.GetModPlayer<BBAccessoryPlayer>().BonusTideMax;
        public bool TideFull => TideValue >= CurrentTideMax;
        public bool EXFull => EXValue >= EXMax;
        public int EXDisplayValue => Utils.Clamp(EXValue / GetFramesPerDisplayUnit(), 0, EXDisplayMax);
        public float TideDamageMultiplier => 1f + TideValue * TideDamageBonusPerStack;

        public override void PostUpdateEquips()
        {
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;

            UpdatePassiveTideRegen();
            UpdateEXCharge();
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideReady, TideFull);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasEXReady, EXFull);
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

        public void ResetEX()
        {
            EXValue = 0;
        }

        public static int GetFramesPerDisplayUnit()
        {
            return Math.Max(1, EXMax / EXDisplayMax);
        }

        private void UpdateEXCharge()
        {
            bool holdingBrinyBaron = Player.HeldItem != null &&
                                     !Player.HeldItem.IsAir &&
                                     Player.HeldItem.ModItem is NewLegendBrinyBaron;

            if (holdingBrinyBaron)
                EXValue++;

            EXValue = Utils.Clamp(EXValue, 0, EXMax);
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
