using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools
{
    // 调试工具共用的物品图标包边效果：改用原版贴图后，用这层描边区分各个工具
    internal static class DebugToolOutline
    {
        public static void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, float scale, Color color)
        {
            float pulse = 0.7f + 0.3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.4f);
            Color outlineColor = color with { A = 0 };
            float distance = 2f + pulse * 1.6f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * distance;
                spriteBatch.Draw(texture, position + offset, frame, outlineColor * (0.5f * pulse), 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
