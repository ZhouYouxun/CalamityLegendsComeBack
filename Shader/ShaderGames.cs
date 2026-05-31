using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Shader
{
    [Autoload(Side = ModSide.Client)]
    public sealed class ShaderGames : ModSystem
    {
        public const string ShaderPrefix = "CalamityLegendsComeBack:";

        private const string EffectsPath = "CalamityLegendsComeBack/Shader/XNBcoder/Effects/";
        private const string LegacyPath = "CalamityLegendsComeBack/Shader/222/";

        public static readonly Dictionary<string, Asset<Effect>> LoadedShaders = [];

        public static Effect RainbowShader => GetEffect("RainbowShader");
        public static Effect EdgeGlowShader => GetEffect("EdgeGlowShader");
        public static Effect GlassRefractionShader => GetEffect("GlassRefractionShader");
        public static Effect DistortionShader => GetEffect("DistortionShader");
        public static Effect EnchantmentShader => GetEffect("EnchantmentShader");
        public static Effect GlitchBlocksShader => GetEffect("GlitchBlocksShader");
        public static Effect KaleidoscopeShader => GetEffect("KaleidoscopeShader");
        public static Effect KaleidoscopeScreenShader => GetEffect("KaleidoscopeScreenShader");
        public static Effect ScanlineShader => GetEffect("ScanlineShader");
        public static Effect WormShader => GetEffect("WormShader");
        public static Effect GrayscaleShader => GetEffect("GrayscaleShader");
        public static Effect MagnifyDistortionShader => GetEffect("MagnifyDistortionShader");
        public static Effect CyberNeonGlow => GetEffect("CyberNeonGlow");
        public static Effect LiquidFlowShader => GetEffect("LiquidFlowShader");
        public static Effect FireBurnShader => GetEffect("FireBurnShader");
        public static Effect AuroraWaveShader => GetEffect("AuroraWaveShader");
        public static Effect PixelationShader => GetEffect("PixelationShader");
        public static Effect BlackHoleDistortionShader => GetEffect("BlackHoleDistortion");
        public static Effect ScreenSimplyDistortedShader => GetEffect("ScreenSimplyDistorted");

        public static Effect TailFirst => GetEffect("TailFirst");
        public static Effect TailSecond => GetEffect("TailSecond");
        public static Effect TailMagic => GetEffect("TailMagic");
        public static Effect TailModern => GetEffect("TailModern");
        public static Effect TailTechnology => GetEffect("TailTechnology");
        public static Effect TrailFrostCrystal => GetEffect("TrailFrostCrystal");
        public static Effect TrailGhostlyPhantom => GetEffect("TrailGhostlyPhantom");
        public static Effect TrailBlazingFlame => GetEffect("TrailBlazingFlame");
        public static Effect TrailWarpDistortion => GetEffect("TrailWarpDistortion");
        public static Effect ArtAttackTrail => GetEffect("ArtAttackTrail");

        public override void PostSetupContent()
        {
            if (Main.dedServ)
                return;

            LoadBundledShaders();
            RegisterTrailShaders();
            RegisterScreenShaders();
        }

        public override void Unload()
        {
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

        private static void LoadBundledShaders()
        {
            string[] effectShaderNames =
            [
                "ArtAttackTrail",
                "AuroraWaveShader",
                "BlackHoleDistortion",
                "CyberNeonGlow",
                "DistortionShader",
                "EdgeGlowShader",
                "EnchantmentShader",
                "FireBurnShader",
                "GlassRefractionShader",
                "GlitchBlocksShader",
                "GrayscaleShader",
                "KaleidoscopeScreenShader",
                "KaleidoscopeShader",
                "LiquidFlowShader",
                "MagnifyDistortionShader",
                "PixelationShader",
                "RainbowShader",
                "ScanlineShader",
                "ScreenSimplyDistorted",
                "TailFirst",
                "TailMagic",
                "TailModern",
                "TailSecond",
                "TailTechnology",
                "TrailBlazingFlame",
                "TrailFrostCrystal",
                "TrailGhostlyPhantom",
                "TrailWarpDistortion",
                "WormShader"
            ];

            foreach (string name in effectShaderNames)
                TryLoadShader(name, EffectsPath + name);

            string[] legacyShaderNames =
            [
                "DistortShader",
                "FirstShader",
                "HGTShader",
                "SecondShader",
                "ThirdShader"
            ];

            foreach (string name in legacyShaderNames)
                TryLoadShader(name, LegacyPath + name);
        }

        private static void RegisterTrailShaders()
        {
            RegisterMiscShader("TailFirst", "TrailPass", "TailFirstEffect");
            RegisterMiscShader("TailSecond", "TrailPass", "TailSecondEffect");
            RegisterMiscShader("TailMagic", "TrailPass", "TailMagicEffect");
            RegisterMiscShader("TailModern", "TrailPass", "TailModernEffect");
            RegisterMiscShader("TailTechnology", "TrailPass", "TailTechnologyEffect");
            RegisterMiscShader("TrailFrostCrystal", "TrailPass", "TrailFrostCrystalEffect");
            RegisterMiscShader("TrailGhostlyPhantom", "TrailPass", "TrailGhostlyPhantomEffect");
            RegisterMiscShader("TrailBlazingFlame", "TrailPass", "TrailBlazingFlameEffect");
            RegisterMiscShader("TrailWarpDistortion", "TrailPass", "TrailWarpDistortionEffect");
            RegisterMiscShader("ArtAttackTrail", "TrailPass", "ArtAttackTrail");
            RegisterMiscShader("HGTShader", "PiercePass", "HGTShader");
        }

        private static void RegisterScreenShaders()
        {
            RegisterSceneFilter("ScreenSimplyDistorted", "Pass1", "ScreenSimplyDistorted", EffectPriority.Medium);
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

            string key = ShaderPrefix + registrationName;
            Filters.Scene[key] = new Filter(new ScreenShaderData(shader, passName), priority);
            Filters.Scene[key].Load();
        }

        private static void UpdateScreenShaderParameters()
        {
            string key = ShaderPrefix + "ScreenSimplyDistorted";
            Filter filter = Filters.Scene[key];
            if (filter is null || !filter.IsActive())
                return;

            Effect shader = filter.GetShader().Shader;
            shader.Parameters["uScreenResolution"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
        }
    }
}
