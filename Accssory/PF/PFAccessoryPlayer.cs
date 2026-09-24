using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF
{
    // Keeps Pristine Fury's base behavior while its optional accessories are retired.
    internal sealed class PFAccessoryPlayer : ModPlayer
    {
        public int BonusMarkSlots => 0;
        public int PurificationCap => 3;
        public bool NineTailsEquipped => false;
        public bool LingmuFOMOEquipped => false;
    }
}
