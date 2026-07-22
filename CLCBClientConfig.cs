using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CalamityLegendsComeBack
{
    [BackgroundColor(15, 27, 52, 230)]
    public sealed class CLCBClientConfig : ModConfig
    {
        public static CLCBClientConfig Instance;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => Instance = this;

        [Header("Interface")]
        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        public bool ShowMatrixBossBar;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(false)]
        public bool LockAmmoWheelToScreenCenter;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        public bool ShowModListIconShine;

        [Header("Readability")]
        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        public bool ShowInternalEnglishNames;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(false)]
        public bool ShowHostileProjectileOutlines;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(2)]
        [Range(1, 4)]
        [Slider]
        public int HostileProjectileOutlineWidth;
    }
}
