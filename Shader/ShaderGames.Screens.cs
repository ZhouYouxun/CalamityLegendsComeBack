using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;

namespace CalamityLegendsComeBack.Shader
{
    public sealed partial class ShaderGames
    {
        public static Effect BlackHoleDistortionShader => GetEffect("BlackHoleDistortion");
        public static Effect ScreenSimplyDistortedShader => GetEffect("ScreenSimplyDistorted");

        private static readonly ShaderDefinition[] ScreenShaders =
        [
            // Screen shaders are activated through Filters.Scene.
            new("BlackHoleDistortion", ShaderCategory.Screen, "Pass1", "BlackHoleDistortion"),
            new("ScreenSimplyDistorted", ShaderCategory.Screen, "Pass1", "ScreenSimplyDistorted")
        ];

        private static void RegisterScreenShaders()
        {
            foreach (ShaderDefinition shader in ScreenShaders)
                RegisterSceneFilter(shader.Name, shader.PassName, shader.RegistrationName, EffectPriority.Medium);
        }

        private static void UpdateScreenShaderParameters()
        {
            foreach (ShaderDefinition shader in ScreenShaders)
            {
                string key = SceneFilterKey(shader.RegistrationName);
                Filter filter = Filters.Scene[key];
                if (filter is null || !filter.IsActive())
                    continue;

                Effect effect = filter.GetShader().Shader;
                switch (shader.Name)
                {
                    case "BlackHoleDistortion":
                        effect.Parameters["uCenter"]?.SetValue(new Vector2(0.5f, 0.5f));
                        effect.Parameters["uRadius"]?.SetValue(0.42f);
                        effect.Parameters["uStrength"]?.SetValue(0.16f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f));
                        effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                        break;

                    case "ScreenSimplyDistorted":
                        effect.Parameters["uScreenResolution"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
                        break;
                }
            }
        }
    }
}
