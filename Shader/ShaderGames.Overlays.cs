using Microsoft.Xna.Framework.Graphics;

namespace CalamityLegendsComeBack.Shader
{
    public sealed partial class ShaderGames
    {
        public static Effect RainbowShader => GetEffect("RainbowShader");
        public static Effect EdgeGlowShader => GetEffect("EdgeGlowShader");
        public static Effect GlassRefractionShader => GetEffect("GlassRefractionShader");
        public static Effect DistortionShader => GetEffect("DistortionShader");
        public static Effect EnchantmentShader => GetEffect("EnchantmentShader");
        public static Effect GlitchBlocksShader => GetEffect("GlitchBlocksShader");
        public static Effect KaleidoscopeShader => GetEffect("KaleidoscopeShader");
        public static Effect ScanlineShader => GetEffect("ScanlineShader");
        public static Effect WormShader => GetEffect("WormShader");
        public static Effect GrayscaleShader => GetEffect("GrayscaleShader");
        public static Effect MagnifyDistortionShader => GetEffect("MagnifyDistortionShader");
        public static Effect CyberNeonGlow => GetEffect("CyberNeonGlow");
        public static Effect LiquidFlowShader => GetEffect("LiquidFlowShader");
        public static Effect FireBurnShader => GetEffect("FireBurnShader");
        public static Effect AuroraWaveShader => GetEffect("AuroraWaveShader");
        public static Effect PixelationShader => GetEffect("PixelationShader");

        private static readonly ShaderDefinition[] OverlayShaders =
        [
            // Overlay shaders affect only the sprite currently being drawn.
            new("AuroraWaveShader", ShaderCategory.Overlay, "P0", "AuroraWaveShader"),
            new("CyberNeonGlow", ShaderCategory.Overlay, "P0", "CyberNeonGlow"),
            new("DistortionShader", ShaderCategory.Overlay, "P0", "DistortionShader"),
            new("EdgeGlowShader", ShaderCategory.Overlay, "P0", "EdgeGlowShader"),
            new("EnchantmentShader", ShaderCategory.Overlay, "P0", "EnchantmentShader"),
            new("FireBurnShader", ShaderCategory.Overlay, "P0", "FireBurnShader"),
            new("GlassRefractionShader", ShaderCategory.Overlay, "P0", "GlassRefractionShader"),
            new("GlitchBlocksShader", ShaderCategory.Overlay, "P0", "GlitchBlocksShader"),
            new("GrayscaleShader", ShaderCategory.Overlay, "P0", "GrayscaleShader"),
            new("KaleidoscopeShader", ShaderCategory.Overlay, "P0", "KaleidoscopeShader"),
            new("LiquidFlowShader", ShaderCategory.Overlay, "P0", "LiquidFlowShader"),
            new("MagnifyDistortionShader", ShaderCategory.Overlay, "P0", "MagnifyDistortionShader"),
            new("PixelationShader", ShaderCategory.Overlay, "P0", "PixelationShader"),
            new("RainbowShader", ShaderCategory.Overlay, "P0", "RainbowShader"),
            new("ScanlineShader", ShaderCategory.Overlay, "P0", "ScanlineShader"),
            new("WormShader", ShaderCategory.Overlay, "P0", "WormShader")
        ];

        private static void RegisterOverlayShaders()
        {
            foreach (ShaderDefinition shader in OverlayShaders)
                RegisterMiscShader(shader.Name, shader.PassName, shader.RegistrationName);
        }
    }
}
