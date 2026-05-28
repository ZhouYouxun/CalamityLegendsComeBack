using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ExpansionCanisterI
{
    public class ExpansionCanisterIPlayer : ModPlayer
    {
        public bool ExpansionCanisterIEquipped;

        public override void ResetEffects()
        {
            ExpansionCanisterIEquipped = false;
        }
    }
}
