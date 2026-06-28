using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    public sealed class IUMWWorldSystem : ModSystem
    {
        public static bool IUMWModeEnabled { get; private set; }

        public static void SetModeEnabled(bool enabled)
        {
            IUMWModeEnabled = enabled;
        }

        public override void OnWorldLoad()
        {
            IUMWModeEnabled = false;
        }

        public override void OnWorldUnload()
        {
            IUMWModeEnabled = false;
        }
    }
}
