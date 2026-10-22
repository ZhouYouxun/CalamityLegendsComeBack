using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryEmblemPlayer : ModPlayer
    {
        private bool temporaryEXUnlock;

        // Shared gate for legendary ultimates. Old character-consumable saves are no longer used.
        // The temporary override is only used by the unregistered developer test item.
        public bool EXAccessoryEquipped
        {
            get => Main.hardMode || temporaryEXUnlock;
            set => temporaryEXUnlock = value;
        }

        public override void ResetEffects()
        {
            temporaryEXUnlock = false;
        }
    }
}
