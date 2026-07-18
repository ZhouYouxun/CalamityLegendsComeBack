using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Shader
{
    internal enum ShaderCategory
    {
        Trail,
        CalamityTrail,
        Overlay,
        Screen
    }

    [Autoload(Side = ModSide.Client)]
    public sealed partial class ShaderGames : ModSystem
    {
        public const string ShaderPrefix = "CalamityLegendsComeBack:";

        private const string LibraryRoot = "CalamityLegendsComeBack/Shader/Library/";

        public static readonly Dictionary<string, Asset<Effect>> LoadedShaders = [];

        internal readonly record struct ShaderDefinition(
            string Name,
            ShaderCategory Category,
            string PassName,
            string RegistrationName);

        public override void PostSetupContent()
        {
            if (Main.dedServ)
                return;

            LoadShaderGroup(TrailShaders);
            LoadShaderGroup(OverlayShaders);
            LoadShaderGroup(ScreenShaders);

            RegisterTrailShaders();
            RegisterOverlayShaders();
            RegisterScreenShaders();
        }

        public override void Unload()
        {
            ResetDarkPlasmaKamuiVortexFadeOut();
            LoadedShaders.Clear();
        }

        public override void PostUpdateEverything()
        {
            UpdateScreenShaderParameters();
        }

        public static Asset<Effect> GetShaderAsset(string name)
        {
            LoadedShaders.TryGetValue(name, out Asset<Effect> shader);
            return shader;
        }

        public static Effect GetEffect(string name)
        {
            return GetShaderAsset(name)?.Value;
        }

        public static string SceneFilterKey(string registrationName)
        {
            return ShaderPrefix + registrationName;
        }

        public static bool TryGetMiscShader(string registrationName, out MiscShaderData shader)
        {
            return GameShaders.Misc.TryGetValue(ShaderPrefix + registrationName, out shader) && shader is not null;
        }

        public static bool TryGetCalamityMiscShader(string registrationName, out MiscShaderData shader)
        {
            return GameShaders.Misc.TryGetValue("CalamityMod:" + registrationName, out shader) && shader is not null;
        }

        internal static IReadOnlyList<ShaderDefinition> GetShaders(ShaderCategory category)
        {
            return category switch
            {
                ShaderCategory.Trail => TrailShaders,
                ShaderCategory.CalamityTrail => CalamityTrailShaders,
                ShaderCategory.Overlay => OverlayShaders,
                ShaderCategory.Screen => ScreenShaders,
                _ => Array.Empty<ShaderDefinition>()
            };
        }

        internal static bool TryGetShaderByCatalogIndex(int catalogIndex, out ShaderDefinition shader)
        {
            int currentIndex = 0;
            foreach (ShaderCategory category in Enum.GetValues<ShaderCategory>())
            {
                foreach (ShaderDefinition candidate in GetShaders(category))
                {
                    if (currentIndex == catalogIndex)
                    {
                        shader = candidate;
                        return true;
                    }

                    currentIndex++;
                }
            }

            shader = default;
            return false;
        }

        internal static int GetCatalogIndex(ShaderCategory category, int categoryIndex)
        {
            int catalogIndex = categoryIndex;
            foreach (ShaderCategory candidateCategory in Enum.GetValues<ShaderCategory>())
            {
                if (candidateCategory == category)
                    return catalogIndex;

                catalogIndex += GetShaders(candidateCategory).Count;
            }

            return -1;
        }

        internal static int FindCatalogIndex(ShaderCategory category, string shaderName)
        {
            IReadOnlyList<ShaderDefinition> shaders = GetShaders(category);
            for (int i = 0; i < shaders.Count; i++)
            {
                if (shaders[i].Name == shaderName)
                    return GetCatalogIndex(category, i);
            }

            return -1;
        }

        private static void LoadShaderGroup(IEnumerable<ShaderDefinition> shaders)
        {
            foreach (ShaderDefinition shader in shaders)
                TryLoadShader(shader.Name, LibraryRoot + shader.Category + "/" + shader.Name);
        }

        private static void TryLoadShader(string name, string path)
        {
            if (LoadedShaders.ContainsKey(name))
                return;

            try
            {
                LoadedShaders[name] = ModContent.Request<Effect>(path, AssetRequestMode.ImmediateLoad);
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<CalamityLegendsComeBack>().Logger.Warn($"Failed to load shader '{name}' at '{path}'.", ex);
            }
        }

        private static void RegisterMiscShader(string shaderName, string passName, string registrationName)
        {
            Asset<Effect> shader = GetShaderAsset(shaderName);
            if (shader is null)
                return;

            GameShaders.Misc[ShaderPrefix + registrationName] = new MiscShaderData(shader, passName);
        }

        private static void RegisterSceneFilter(string shaderName, string passName, string registrationName, EffectPriority priority)
        {
            Asset<Effect> shader = GetShaderAsset(shaderName);
            if (shader is null)
                return;

            string key = SceneFilterKey(registrationName);
            Filters.Scene[key] = new Filter(new ScreenShaderData(shader, passName), priority);
            Filters.Scene[key].Load();
        }
    }
}
