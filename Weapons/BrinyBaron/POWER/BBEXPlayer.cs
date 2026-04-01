using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.POWER
{
    internal class BBEXPlayer : ModPlayer
    {
        public const int TideMax = 3;

        public int TideValue;
        public bool TideFull => TideValue >= TideMax;

        public override void ResetEffects()
        {
        }

        public void AddTide(int amount = 1)
        {
            if (amount <= 0)
                return;

            TideValue += amount;
            if (TideValue > TideMax)
                TideValue = TideMax;
        }

        public void ResetTide()
        {
            TideValue = 0;
        }
    }
}
