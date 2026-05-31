using CalamityLegendsComeBack.Shader;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    public sealed class ShaderFullScreenSystem : ModSystem
    {
        // Kept as an opt-in example. Call ActivateScreenSimplyDistorted() from gameplay code
        // instead of enabling the screen shader globally every frame.
        public static void ActivateScreenSimplyDistorted()
        {
            if (Main.dedServ)
                return;

            string key = ShaderGames.ShaderPrefix + "ScreenSimplyDistorted";
            if (!Filters.Scene[key].IsActive())
                Filters.Scene.Activate(key);
        }
    }
}
