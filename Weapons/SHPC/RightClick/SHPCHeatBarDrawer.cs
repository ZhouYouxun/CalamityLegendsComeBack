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

            Texture2D sparkle = GetNoBlackTexture(ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value, spriteBatch.GraphicsDevice);
            Vector2 starCenter = drawPosition + new Vector2(32f, backTexture.Height * scale * 0.5f);
            Vector2 origin = sparkle.Size() * 0.5f;
            Color color = GetHeatStarColor(heatLevel);
            float time = Main.GlobalTimeWrappedHourly;
            float flicker = heatLevel >= 5 ? 0.78f + (float)System.Math.Sin(time * 12f) * 0.22f : 1f;
            float levelScale = MathHelper.Lerp(0.72f, 1.08f, Utils.Clamp((heatLevel - 1f) / 4f, 0f, 1f));
            Vector2 drawScale = new Vector2(0.18f, 0.58f) * scale * levelScale * flicker;
            float rotation = time * 0.35f;

            spriteBatch.Draw(sparkle, starCenter, null, color * (opacity * 0.78f), rotation, origin, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sparkle, starCenter, null, color * (opacity * 0.56f), rotation + MathHelper.PiOver2, origin, drawScale * 0.86f, SpriteEffects.None, 0f);
            spriteBatch.Draw(sparkle, starCenter, null, Color.White * (opacity * 0.28f * flicker), rotation, origin, drawScale * 0.52f, SpriteEffects.None, 0f);
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

        private static Color GetHeatStarColor(int heatLevel)
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
    }
}
