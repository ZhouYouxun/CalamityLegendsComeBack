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
            // 覆盖类：只处理当前绘制的贴图，不接管整个屏幕。
            new("AuroraWaveShader", OverlayCategory, "P0", "AuroraWaveShader"),
            new("CyberNeonGlow", OverlayCategory, "P0", "CyberNeonGlow"),
            new("DistortionShader", OverlayCategory, "P0", "DistortionShader"),
            new("EdgeGlowShader", OverlayCategory, "P0", "EdgeGlowShader"),
            new("EnchantmentShader", OverlayCategory, "P0", "EnchantmentShader"),
            new("FireBurnShader", OverlayCategory, "P0", "FireBurnShader"),
            new("GlassRefractionShader", OverlayCategory, "P0", "GlassRefractionShader"),
            new("GlitchBlocksShader", OverlayCategory, "P0", "GlitchBlocksShader"),
            new("GrayscaleShader", OverlayCategory, "P0", "GrayscaleShader"),
            new("KaleidoscopeShader", OverlayCategory, "P0", "KaleidoscopeShader"),
            new("LiquidFlowShader", OverlayCategory, "P0", "LiquidFlowShader"),
            new("MagnifyDistortionShader", OverlayCategory, "P0", "MagnifyDistortionShader"),
            new("PixelationShader", OverlayCategory, "P0", "PixelationShader"),
            new("RainbowShader", OverlayCategory, "P0", "RainbowShader"),
            new("ScanlineShader", OverlayCategory, "P0", "ScanlineShader"),
            new("WormShader", OverlayCategory, "P0", "WormShader")
        ];

        private static void RegisterOverlayShaders()
        {
            foreach (ShaderDefinition shader in OverlayShaders)
                RegisterMiscShader(shader.Name, shader.PassName, shader.RegistrationName);
        }
    }
}
