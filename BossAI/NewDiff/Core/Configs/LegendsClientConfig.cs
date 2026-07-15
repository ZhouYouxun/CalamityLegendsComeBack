using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Configs
{
    public sealed class LegendsClientConfig : ModConfig
    {
        public static LegendsClientConfig Instance;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        public bool ShowBossAIDebugText = true;

        public override void OnLoaded()
        {
            Instance = this;
        }
    }
}
