using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal static class SHPCHeatBarDrawer
    {
        private static readonly Dictionary<Texture2D, Rectangle> OpaqueBoundsCache = new();
        private static readonly Dictionary<Texture2D, Texture2D> NoBlackTextureCache = new();
        private static readonly Dictionary<Texture2D, Texture2D> OutlineTextureCache = new();

        public static void Draw(SpriteBatch spriteBatch, Texture2D backTexture, Texture2D frontTexture, Vector2 drawPosition, float progress, Color backColor, Color frontColor, float scale)
        {
            spriteBatch.Draw(backTexture, drawPosition, null, backColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            progress = MathHelper.Clamp(progress, 0f, 1f);
            if (progress <= 0f)
                return;

            Rectangle sourceBounds = GetOpaqueBounds(frontTexture);
            int visibleWidth = (int)System.MathF.Ceiling(sourceBounds.Width * progress);
            visibleWidth = Utils.Clamp(visibleWidth, 1, sourceBounds.Width);

            Rectangle sourceRectangle = new(sourceBounds.X, sourceBounds.Y, visibleWidth, sourceBounds.Height);
            Vector2 frontPosition = drawPosition + new Vector2(sourceBounds.X, sourceBounds.Y) * scale;

            spriteBatch.Draw(frontTexture, frontPosition, sourceRectangle, frontColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawHeatStar(SpriteBatch spriteBatch, Texture2D backTexture, Vector2 drawPosition, int heatLevel, float opacity, float scale)
        {
            if (heatLevel <= 0 || opacity <= 0f)
                return;

            const float StarVisualScale = 0.67f;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkle = GetNoBlackTexture(ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value, spriteBatch.GraphicsDevice);
            Vector2 starCenter = drawPosition + new Vector2(32f - 1.5f * 16f + 6f, backTexture.Height * scale * 0.5f + 3f);
            Color effectsColor = GetHeatColor(heatLevel);
            Color coreWhite = Color.Lerp(new Color(205, 245, 255), Color.White, Utils.Clamp((heatLevel - 1f) / 4f, 0f, 1f) * 0.55f);
            float time = Main.GlobalTimeWrappedHourly;
            float heatPower = MathHelper.Lerp(0.72f, 1f, Utils.Clamp((heatLevel - 1f) / 4f, 0f, 1f));
            float reverseHeatPower = MathHelper.Lerp(0.7f, 0.1f, heatPower);
            float topHeatPulse = heatLevel >= 5 ? 0.9f + (float)System.Math.Sin(time * 12f) * 0.1f : 1f;

            for (int i = 0; i < 5; i++)
            {
                float iMult = 1f - 0.1f * i;
                Color layerColor = Color.Lerp(effectsColor, coreWhite, i * 0.1f) with { A = 0 };

                spriteBatch.Draw(
                    bloom,
                    starCenter,
                    null,
                    layerColor * (opacity * 0.6f),
                    Main.rand.NextFloat(-5f, 5f),
                    bloom.Size() * 0.5f,
                    new Vector2(1f, 0.35f) * 0.75f * heatPower * topHeatPulse * Main.rand.NextFloat(0.7f, 1.3f) * iMult * StarVisualScale,
                    SpriteEffects.None,
                    0f);

                for (int b = -1; b <= 1; b += 2)
                {
                    float sine = MathHelper.Lerp((float)System.Math.Sin(time * 20f / MathHelper.Pi), reverseHeatPower * b, 0.75f);
                    Vector2 starScale = new Vector2(0.3f, sine * b) *
                        (Main.rand.NextFloat(3f, 4.5f) * iMult + heatPower * 1.2f) *
                        topHeatPulse *
                        StarVisualScale;
                    float rotation = time * heatPower * System.Math.Max(i - 2, 0) * 0.2f + MathHelper.PiOver4 * b;

                    spriteBatch.Draw(
                        sparkle,
                        starCenter,
                        null,
                        layerColor * opacity,
                        rotation,
                        sparkle.Size() * 0.5f,
                        starScale,
                        SpriteEffects.None,
                        0f);
                }
            }
        }

        public static void DrawHeatBackOutline(SpriteBatch spriteBatch, Texture2D backTexture, Vector2 drawPosition, int heatLevel, float opacity, float scale)
        {
            if (heatLevel <= 0 || opacity <= 0f)
                return;

            Texture2D outlineTexture = GetOutlineTexture(backTexture, spriteBatch.GraphicsDevice);
            Color outlineColor = GetHeatColor(heatLevel) * (opacity * 0.82f);
            spriteBatch.Draw(outlineTexture, drawPosition, null, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawOutlinePulse(SpriteBatch spriteBatch, Texture2D backTexture, Vector2 drawPosition, float scale, float opacity, int timer, int duration)
        {
            if (timer <= 0 || duration <= 0 || opacity <= 0f)
                return;

            float pulse = timer / (float)duration;
            float fade = pulse * pulse;
            float outlineStrength = MathHelper.Lerp(0.6f, 3.4f, fade);
            Color outlineColor = Color.Lerp(new Color(255, 74, 42), Color.White, 0.35f) * (opacity * fade * 0.85f);

            Vector2[] offsets =
            {
                new(outlineStrength, 0f),
                new(-outlineStrength, 0f),
                new(0f, outlineStrength),
                new(0f, -outlineStrength),
                new(outlineStrength, outlineStrength),
                new(-outlineStrength, outlineStrength),
                new(outlineStrength, -outlineStrength),
                new(-outlineStrength, -outlineStrength),
            };

            foreach (Vector2 offset in offsets)
                spriteBatch.Draw(backTexture, drawPosition + offset, null, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static Color GetHeatColor(int heatLevel)
        {
            return heatLevel switch
            {
                <= 1 => new Color(78, 190, 255),
                2 => new Color(255, 226, 82),
                3 => new Color(255, 132, 38),
                4 => new Color(255, 58, 34),
                _ => Color.Lerp(new Color(255, 28, 24), Color.White, 0.22f + 0.18f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 14f))
            };
        }

        private static Rectangle GetOpaqueBounds(Texture2D texture)
        {
            if (OpaqueBoundsCache.TryGetValue(texture, out Rectangle cachedBounds))
                return cachedBounds;

            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            int minX = texture.Width;
            int minY = texture.Height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < texture.Height; y++)
            {
                for (int x = 0; x < texture.Width; x++)
                {
                    if (pixels[y * texture.Width + x].A <= 0)
                        continue;

                    if (x < minX)
                        minX = x;
                    if (y < minY)
                        minY = y;
                    if (x > maxX)
                        maxX = x;
                    if (y > maxY)
                        maxY = y;
                }
            }

            Rectangle bounds = maxX >= minX && maxY >= minY
                ? new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)
                : new Rectangle(0, 0, texture.Width, texture.Height);

            OpaqueBoundsCache[texture] = bounds;
            return bounds;
        }

        private static Texture2D GetNoBlackTexture(Texture2D texture, GraphicsDevice graphicsDevice)
        {
            if (NoBlackTextureCache.TryGetValue(texture, out Texture2D cachedTexture))
                return cachedTexture;

            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                if (pixel.A > 0 && pixel.R < 32 && pixel.G < 32 && pixel.B < 32)
                    pixels[i] = Color.Transparent;
            }

            Texture2D filteredTexture = new(graphicsDevice, texture.Width, texture.Height);
            filteredTexture.SetData(pixels);
            NoBlackTextureCache[texture] = filteredTexture;
            return filteredTexture;
        }

        private static Texture2D GetOutlineTexture(Texture2D texture, GraphicsDevice graphicsDevice)
        {
            if (OutlineTextureCache.TryGetValue(texture, out Texture2D cachedTexture))
                return cachedTexture;

            Color[] source = new Color[texture.Width * texture.Height];
            Color[] outline = new Color[source.Length];
            texture.GetData(source);

            for (int y = 0; y < texture.Height; y++)
            {
                for (int x = 0; x < texture.Width; x++)
                {
                    int index = y * texture.Width + x;
                    bool opaque = source[index].A > 16;
                    bool edge = false;

                    for (int oy = -2; oy <= 2 && !edge; oy++)
                    {
                        for (int ox = -2; ox <= 2; ox++)
                        {
                            if (ox == 0 && oy == 0)
                                continue;
                            if (ox * ox + oy * oy > 5)
                                continue;

                            int nx = x + ox;
                            int ny = y + oy;
                            bool neighborOpaque = nx >= 0 && nx < texture.Width && ny >= 0 && ny < texture.Height && source[ny * texture.Width + nx].A > 16;
                            if (opaque != neighborOpaque)
                            {
                                edge = true;
                                break;
                            }
                        }
                    }

                    outline[index] = edge ? Color.White * (opaque ? 0.62f : 1f) : Color.Transparent;
                }
            }

            Texture2D outlineTexture = new(graphicsDevice, texture.Width, texture.Height);
            outlineTexture.SetData(outline);
            OutlineTextureCache[texture] = outlineTexture;
            return outlineTexture;
        }
    }
}
