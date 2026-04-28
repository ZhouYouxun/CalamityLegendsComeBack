using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Visuals
{
    internal static class HoldoutOutlineHelper
    {
        private static readonly Color[] StarmadaColors =
        {
            new(164, 47, 160),
            new(227, 97, 72),
            new(193, 255, 146)
        };

        public static Color StarmadaCycleColor(float time)
        {
            float wrappedTime = time * 0.05f;
            int colorIndex = (int)(wrappedTime / 2f % StarmadaColors.Length);
            Color first = StarmadaColors[colorIndex];
            Color second = StarmadaColors[(colorIndex + 1) % StarmadaColors.Length];
            float interpolant = wrappedTime % 2f >= 1f ? 1f : wrappedTime % 1f;
            return Color.Lerp(first, second, interpolant);
        }

        public static void DrawStarmadaRainbowOutline(
            Texture2D texture,
            Vector2 drawPosition,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float radius,
            float opacity,
            float time,
            int drawCount = 18,
            bool manageBlendState = true)
        {
            DrawOutline(texture, drawPosition, rotation, origin, scale, effects, radius, opacity, time, drawCount,
                completion => Color.Lerp(StarmadaCycleColor(time * 60f), StarmadaCycleColor(time * 60f + completion * 90f), 0.45f),
                manageBlendState);
        }

        public static void DrawSolidOutline(
            Texture2D texture,
            Vector2 drawPosition,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            Color color,
            float radius,
            float opacity,
            float time,
            int drawCount = 16,
            bool manageBlendState = true)
        {
            DrawOutline(texture, drawPosition, rotation, origin, scale, effects, radius, opacity, time, drawCount,
                completion => Color.Lerp(color, Color.White, 0.16f + completion * 0.16f),
                manageBlendState);
        }

        private static void DrawOutline(
            Texture2D texture,
            Vector2 drawPosition,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float radius,
            float opacity,
            float time,
            int drawCount,
            System.Func<float, Color> colorSelector,
            bool manageBlendState)
        {
            if (texture is null || opacity <= 0f || radius <= 0f || drawCount <= 0)
                return;

            if (manageBlendState)
                Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < drawCount; i++)
            {
                float completion = i / (float)drawCount;
                float angle = MathHelper.TwoPi * completion + time * 1.45f;
                float wave = 0.88f + 0.12f * (float)System.Math.Sin(time * 7.5f + i * 0.73f);
                Vector2 offset = angle.ToRotationVector2() * radius * wave;
                Color color = colorSelector(completion);
                color.A = 0;

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    color * opacity,
                    rotation,
                    origin,
                    scale,
                    effects,
                    0f);
            }

            if (manageBlendState)
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
