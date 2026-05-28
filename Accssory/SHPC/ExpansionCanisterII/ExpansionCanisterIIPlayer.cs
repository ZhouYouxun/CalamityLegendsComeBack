using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ExpansionCanisterII
{
    public class ExpansionCanisterIIPlayer : ModPlayer
    {
        public bool ExpansionCanisterIIEquipped;

        public override void ResetEffects()
        {
            ExpansionCanisterIIEquipped = false;
        }
    }
}
