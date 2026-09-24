using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.YC
{
    // Compatibility shim while the retired Greek-letter accessory set is hidden.
    // Yharim's Crystal keeps its unmodified base behavior.
    internal sealed class YCAccessoryPlayer : ModPlayer
    {
        public float WeaponDamageMultiplier => 1f;
        public float ManaCostMultiplier => 1f;
        public float ExCooldownMultiplier => 1f;
        public int ExChargeGain => 1;
    }
}
