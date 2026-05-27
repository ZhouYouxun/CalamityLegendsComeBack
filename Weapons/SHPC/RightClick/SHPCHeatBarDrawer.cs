using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal static class SHPCHeatBarDrawer
    {
        private static readonly Dictionary<Texture2D, Rectangle> OpaqueBoundsCache = new();

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
    }
}
