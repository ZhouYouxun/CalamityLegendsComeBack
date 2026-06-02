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
            ActivateScreenShader("ScreenSimplyDistorted");
        }

        public static void ActivateScreenShader(string registrationName)
        {
            if (Main.dedServ)
                return;

            string key = ShaderGames.SceneFilterKey(registrationName);
            if (!Filters.Scene[key].IsActive())
                Filters.Scene.Activate(key);
        }

        public static void DeactivateScreenShader(string registrationName)
        {
            if (Main.dedServ)
                return;

            Filters.Scene.Deactivate(ShaderGames.SceneFilterKey(registrationName));
        }
    }
}
