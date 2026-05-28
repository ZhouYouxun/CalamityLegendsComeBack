using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.CommandAscend
{
    public sealed class CommandAscendPlayer : ModPlayer
    {
        public bool CommandAscendEquipped;

        public override void ResetEffects()
        {
            CommandAscendEquipped = false;
        }
    }
}
