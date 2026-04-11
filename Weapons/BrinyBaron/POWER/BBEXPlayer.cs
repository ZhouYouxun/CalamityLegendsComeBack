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
                return BB_Balance.GetCurrentTideMax();
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
