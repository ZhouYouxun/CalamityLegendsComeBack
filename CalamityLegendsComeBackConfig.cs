using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CalamityLegendsComeBack
{
    public sealed class CalamityLegendsComeBackConfig : ModConfig
    {
        public static CalamityLegendsComeBackConfig Instance;

        public override ConfigScope Mode => ConfigScope.ServerSide;

        public override void OnLoaded()
        {
            Instance = this;
        }

        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowMassMaterialRecipes;
    }
}
