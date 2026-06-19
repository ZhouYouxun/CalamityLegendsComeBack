using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CalamityLegendsComeBack
{
    public sealed class CLCBClientConfig : ModConfig
    {
        public static CLCBClientConfig Instance;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => Instance = this;

        [DefaultValue(true)]
        public bool ShowMatrixBossBar;
    }
}
