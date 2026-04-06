using Terraria;
using Terraria.ModLoader;
namespace CalamityLegendsComeBack.Weapons.BrinyBaron.POWER
{
    internal class BBEXPlayer : ModPlayer
    {
        public int TideValue;
        public int CurrentTideMax
        {
            get
            {
                int value = 2; // 初始值改成2

                if (Main.hardMode)
                    value++;

                if (CalamityMod.DownedBossSystem.downedCalamitasClone || NPC.downedPlantBoss)
                    value++;

                if (NPC.downedFishron)
                    value++;

                if (NPC.downedMoonlord)
                    value++;

                if (CalamityMod.DownedBossSystem.downedBoomerDuke)
                    value++;

                if (CalamityMod.DownedBossSystem.downedYharon)
                    value++;

                return value;
            }
        }

        public bool TideFull => TideValue >= CurrentTideMax;

        public override void ResetEffects()
        {
            if (TideValue > CurrentTideMax)
                TideValue = CurrentTideMax;
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
    }
}
