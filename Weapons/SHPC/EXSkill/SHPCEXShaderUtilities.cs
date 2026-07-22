using CalamityMod;
using CalamityLegendsComeBack.Shader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal static class SHPCEXShaderUtilities
    {
        private const string LaserTrailRegistrationName = "SHPCLaserDyeTrail";
        private const string MiniTrailRegistrationName = "SHPCMiniLaserDyeTrail";
        private const string StreakTexture = "CalamityMod/ExtraTextures/Trails/SylvestaffStreak";

        public static bool TryGetLaserTrailShader(out MiscShaderData shader)
        {
            if (!ShaderGames.TryGetMiscShader(LaserTrailRegistrationName, out shader))
                return false;

            ConfigureTrailEffect(
                ShaderGames.SHPCLaserDyeTrail,
                pulseSpeed: 7.6f,
                voidStrength: 0.74f,
                bandDensity: 46f,
                // A broad core makes the whole 10-tile trail read as the beam instead of a thin bright centerline.
                coreSharpness: 0.65f);

            shader
                .SetShaderTexture(ModContent.Request<Texture2D>(StreakTexture))
                .UseColor(new Color(18, 72, 215))
                .UseSecondaryColor(new Color(160, 246, 255))
                .UseOpacity(1f);

            return true;
        }

        public static bool TryGetMiniTrailShader(Color primaryColor, Color secondaryColor, float opacity, out MiscShaderData shader)
        {
            if (!ShaderGames.TryGetMiscShader(MiniTrailRegistrationName, out shader))
                return false;

            ConfigureTrailEffect(
                ShaderGames.SHPCMiniLaserDyeTrail,
                pulseSpeed: 9.2f,
                voidStrength: 0.58f,
                bandDensity: 58f,
                coreSharpness: 1f);

            shader
                .SetShaderTexture(ModContent.Request<Texture2D>(StreakTexture))
                .UseColor(primaryColor)
                .UseSecondaryColor(secondaryColor)
                .UseOpacity(opacity);

            return true;
        }

        private static void ConfigureTrailEffect(Effect effect, float pulseSpeed, float voidStrength, float bandDensity, float coreSharpness)
        {
            if (effect is null)
                return;

            SetFloat(effect, "uTime", Main.GlobalTimeWrappedHourly);
            SetFloat(effect, "uPulseSpeed", pulseSpeed);
            SetFloat(effect, "uVoidStrength", voidStrength);
            SetFloat(effect, "uBandDensity", bandDensity);
            SetFloat(effect, "uCoreSharpness", coreSharpness);
        }

        private static void SetFloat(Effect effect, string parameterName, float value)
        {
            effect.Parameters[parameterName]?.SetValue(value);
        }

    }
}
