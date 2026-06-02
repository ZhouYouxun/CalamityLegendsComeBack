using CalamityLegendsComeBack.Accssory.BB;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.EXSkill
{
    internal class BBEXPlayer : ModPlayer
    {
        private static readonly int[] TideMaxValues = { 2, 3, 4, 5, 6, 7, 8 };

        public const int MaxDesignedTideCap = 8;
        public int TideValue;
        private bool wasTideReady;

        public int CurrentTideMax => GetCurrentTideMax() + Player.GetModPlayer<BBAccessoryPlayer>().BonusTideMax;
        public bool TideFull => TideValue >= CurrentTideMax;

        public override void PostUpdateEquips()
        {
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasTideReady, TideFull);
        }

        public void AddTide(int amount = 1)
        {
            if (amount <= 0)
                return;

            TideValue += amount;
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;
        }

        public void ResetTide()
        {
            TideValue = 0;
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
