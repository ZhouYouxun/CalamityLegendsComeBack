using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    /// <summary>
    /// Draws a bright accessibility outline behind hostile projectile sprite frames.
    /// Hidden projectiles opt out because their source texture is not their visible form.
    /// </summary>
    internal sealed class HostileProjectileOutlineGlobalProjectile : GlobalProjectile
    {
        private const byte MinimumVisibleAlpha = 40;
        private const float MinimumLuminance = 0.14f;
        private const int MinimumVisiblePixels = 4;

        private static readonly Dictionary<int, OutlineTextureInfo> CachedTextureInfo = new();

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!ShouldDrawOutline(projectile) || !TryGetFrameColor(projectile, out Color outlineColor))
                return true;

            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            int frameCount = Math.Max(1, Main.projFrames[projectile.type]);
            Rectangle frame = texture.Frame(1, frameCount, 0, Math.Clamp(projectile.frame, 0, frameCount - 1));
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
            SpriteEffects effects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color alphaColor = projectile.GetAlpha(lightColor);
            float opacity = MathHelper.Clamp(alphaColor.A / 255f, 0f, 1f) * projectile.Opacity;
            Color drawColor = outlineColor * (0.94f * opacity);

            if (drawColor.A <= 0)
                return true;

            int outlineWidth = CLCBClientConfig.Instance?.HostileProjectileOutlineWidth ?? 2;
            int outlineDrawCount = 8 + outlineWidth * 4;
            for (int index = 0; index < outlineDrawCount; index++)
            {
                Vector2 offset = (MathHelper.TwoPi * index / outlineDrawCount).ToRotationVector2() * outlineWidth;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, drawColor, projectile.rotation, origin, projectile.scale, effects, 0);
            }

            return true;
        }

        private static bool ShouldDrawOutline(Projectile projectile)
        {
            return !Main.dedServ &&
                CLCBClientConfig.Instance != null &&
                CLCBClientConfig.Instance.ShowHostileProjectileOutlines &&
                projectile.active &&
                projectile.hostile &&
                !projectile.friendly &&
                !projectile.hide &&
                projectile.type > ProjectileID.None &&
                projectile.type < TextureAssets.Projectile.Length;
        }

        private static bool TryGetFrameColor(Projectile projectile, out Color color)
        {
            int projectileType = projectile.type;
            if (!CachedTextureInfo.TryGetValue(projectileType, out OutlineTextureInfo textureInfo))
            {
                textureInfo = BuildTextureInfo(projectileType);
                CachedTextureInfo[projectileType] = textureInfo;
            }

            int frameIndex = Math.Clamp(projectile.frame, 0, textureInfo.FrameColors.Length - 1);
            color = textureInfo.FrameColors[frameIndex];
            return textureInfo.FrameIsVisible[frameIndex];
        }

        private static OutlineTextureInfo BuildTextureInfo(int projectileType)
        {
            try
            {
                Texture2D texture = TextureAssets.Projectile[projectileType].Value;
                int frameCount = Math.Max(1, Main.projFrames[projectileType]);
                Color[] pixels = new Color[texture.Width * texture.Height];
                texture.GetData(pixels);

                Color[] frameColors = new Color[frameCount];
                bool[] frameIsVisible = new bool[frameCount];
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    Rectangle frame = texture.Frame(1, frameCount, 0, frameIndex);
                    frameIsVisible[frameIndex] = TryExtractDominantColor(pixels, texture.Width, frame, out frameColors[frameIndex]);
                }

                return new OutlineTextureInfo(frameColors, frameIsVisible);
            }
            catch (Exception)
            {
                // A missing or unreadable texture must remain unoutlined rather than breaking projectile rendering.
                return OutlineTextureInfo.Hidden;
            }
        }

        private static bool TryExtractDominantColor(Color[] pixels, int textureWidth, Rectangle frame, out Color dominantColor)
        {
            const int hueBins = 24;
            const int saturationBins = 4;
            const int valueBins = 4;
            const int binCount = hueBins * saturationBins * valueBins;
            float[] weights = new float[binCount];
            Vector3[] colorSums = new Vector3[binCount];
            int visiblePixels = 0;

            for (int y = frame.Top; y < frame.Bottom; y++)
            {
                int rowStart = y * textureWidth;
                for (int x = frame.Left; x < frame.Right; x++)
                {
                    Color pixel = pixels[rowStart + x];
                    if (pixel.A < MinimumVisibleAlpha)
                        continue;

                    visiblePixels++;
                    Vector3 rgb = pixel.ToVector3();
                    float maximum = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
                    float minimum = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
                    float luminance = rgb.X * 0.2126f + rgb.Y * 0.7152f + rgb.Z * 0.0722f;
                    if (luminance < MinimumLuminance)
                        continue;

                    float saturation = maximum <= 0f ? 0f : (maximum - minimum) / maximum;
                    float hue = GetHue(rgb, maximum, minimum);
                    int hueBin = Math.Min(hueBins - 1, (int)(hue * hueBins));
                    int saturationBin = Math.Min(saturationBins - 1, (int)(saturation * saturationBins));
                    int valueBin = Math.Min(valueBins - 1, (int)(maximum * valueBins));
                    int bin = (hueBin * saturationBins + saturationBin) * valueBins + valueBin;
                    float weight = pixel.A / 255f * (0.30f + saturation * 0.70f) * (0.50f + luminance * 0.50f);
                    weights[bin] += weight;
                    colorSums[bin] += rgb * weight;
                }
            }

            if (visiblePixels < MinimumVisiblePixels)
            {
                dominantColor = Color.Transparent;
                return false;
            }

            int dominantBin = 0;
            for (int index = 1; index < weights.Length; index++)
            {
                if (weights[index] > weights[dominantBin])
                    dominantBin = index;
            }

            if (weights[dominantBin] <= 0f)
            {
                dominantColor = Color.White;
                return true;
            }

            Vector3 average = colorSums[dominantBin] / weights[dominantBin];
            float maximumComponent = Math.Max(average.X, Math.Max(average.Y, average.Z));
            if (maximumComponent > 0f && maximumComponent < 0.64f)
                average *= 0.64f / maximumComponent;

            // Match the SHPC highlight language: keep the extracted hue, but pull it strongly toward white.
            // A dark projectile must never produce a dark, hard-to-see outline.
            dominantColor = Color.Lerp(new Color(average), Color.White, 0.62f);
            return true;
        }

        private static float GetHue(Vector3 rgb, float maximum, float minimum)
        {
            float delta = maximum - minimum;
            if (delta <= 0.0001f)
                return 0f;

            float hue;
            if (maximum == rgb.X)
                hue = ((rgb.Y - rgb.Z) / delta) % 6f;
            else if (maximum == rgb.Y)
                hue = (rgb.Z - rgb.X) / delta + 2f;
            else
                hue = (rgb.X - rgb.Y) / delta + 4f;

            hue /= 6f;
            return hue < 0f ? hue + 1f : hue;
        }

        private sealed class OutlineTextureInfo
        {
            public static readonly OutlineTextureInfo Hidden = new(new[] { Color.Transparent }, new[] { false });

            public readonly Color[] FrameColors;
            public readonly bool[] FrameIsVisible;

            public OutlineTextureInfo(Color[] frameColors, bool[] frameIsVisible)
            {
                FrameColors = frameColors;
                FrameIsVisible = frameIsVisible;
            }
        }
    }
}
