using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class SHPCMatrixLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "SHPCMatrixLegendaryText";

        private static readonly string[] MatrixGlyphs =
        {
            "0", "1", "01", "10", "::", "//", "SYS", "RUN", "ERR", "SHPC"
        };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendSHPC>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawMatrixLegendaryText(line);
            return false;
        }

        private static void DrawMatrixLegendaryText(DrawableTooltipLine line)
        {
            string plainText = StripChatTags(line.Text);
            string[] textLines = plainText
                .Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (textLines.Length == 0)
                return;

            Vector2 basePosition = new(line.X, line.Y);
            float lineHeight = line.Font.LineSpacing * line.BaseScale.Y;
            float textWidth = textLines.Max(text => line.Font.MeasureString(text).X * line.BaseScale.X);
            float textHeight = lineHeight * textLines.Length;
            Rectangle terminalArea = new(
                (int)basePosition.X - 8,
                (int)basePosition.Y - 5,
                Math.Max(24, (int)Math.Ceiling(textWidth) + 16),
                Math.Max(18, (int)Math.Ceiling(textHeight) + 10));

            float time = Main.GlobalTimeWrappedHourly;
            DrawTerminalBackdrop(terminalArea, time);
            DrawMatrixRain(line, terminalArea, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawTerminalForeground(terminalArea, time);
        }

        private static void DrawTerminalBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(0, 4, 2, 208));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(0, 12, 5, 116));

            for (int y = area.Y + 3; y < area.Bottom - 3; y += 6)
                DrawRectangle(new Rectangle(area.X + 3, y, area.Width - 6, 1), new Color(16, 82, 38, 20));

            int sweepY = area.Y + 2 + (int)((time * 46f) % Math.Max(1, area.Height - 4));
            DrawRectangle(new Rectangle(area.X + 2, sweepY, area.Width - 4, 2), new Color(64, 255, 136, 32));
        }

        private static void DrawMatrixRain(DrawableTooltipLine line, Rectangle area, float time)
        {
            int columns = Math.Clamp(area.Width / 54, 5, 16);
            float travelHeight = area.Height + 42f;
            Vector2 glyphScale = line.BaseScale * 0.48f;

            for (int column = 0; column < columns; column++)
            {
                float x = area.X + 9f + column * (area.Width - 18f) / Math.Max(1, columns - 1);
                float speed = 19f + column % 5 * 4.5f;
                float headY = area.Y - 28f + (time * speed + column * 31f) % travelHeight;
                int trailLength = 2 + column % 4;

                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 8f) + column * 7 + row * 3) % MatrixGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    Color color = row == 0
                        ? new Color(122, 255, 166, 44)
                        : new Color(20, 176, 72, 26) * fade;
                    Vector2 position = new(x, headY - row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        MatrixGlyphs[glyphIndex],
                        position,
                        color,
                        0f,
                        Vector2.Zero,
                        glyphScale);
                }
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                float signal = (MathF.Sin(time * 4.2f - row * 0.82f) + 1f) * 0.5f;
                bool glitching = MathF.Sin(time * 17.5f + row * 4.73f) > 0.92f;
                float glitchDirection = MathF.Sin(time * 31f + row * 9f) >= 0f ? 1f : -1f;
                Vector2 glitchOffset = glitching ? Vector2.UnitX * glitchDirection * (2f + signal * 3f) : Vector2.Zero;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + glitchOffset;

                Color glowColor = new Color(0, 112, 38, 0) * (0.24f + signal * 0.18f);
                float glowRadius = 1.2f + signal * 1.4f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(0, 0, 0, 238));

                if (glitching)
                {
                    DrawText(line, text, position - Vector2.UnitX * glitchDirection * 5f, new Color(0, 72, 22, 0) * 0.56f);
                    DrawText(line, text, position + Vector2.UnitX * glitchDirection * 3f, new Color(166, 255, 196, 0) * 0.24f);
                }

                Color textColor = Color.Lerp(
                    new Color(42, 204, 94),
                    new Color(190, 255, 208),
                    0.18f + signal * 0.58f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawTerminalForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 6.4f) + 1f) * 0.5f;
            Color edgeColor = new Color(52, 255, 124, 112) * (0.58f + pulse * 0.36f);
            Color dimEdge = new Color(12, 112, 48, 92);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);

            if ((int)(time * 4f) % 2 == 0)
                DrawRectangle(new Rectangle(area.Right - 17, area.Bottom - 5, 10, 2), new Color(146, 255, 180, 180));

            if ((int)(time * 9f) % 11 < 2)
            {
                int glitchY = area.Y + 4 + (int)((time * 83f) % Math.Max(1, area.Height - 8));
                int glitchWidth = Math.Max(18, area.Width / 4);
                int glitchX = area.X + (int)((time * 47f) % Math.Max(1, area.Width - glitchWidth));
                DrawRectangle(new Rectangle(glitchX, glitchY, glitchWidth, 1), new Color(110, 255, 154, 86));
            }
        }

        private static void DrawText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position,
                color,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static string StripChatTags(string text)
        {
            return string.Concat(ChatManager.ParseMessage(text, Color.White).Select(snippet => snippet.Text));
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }
    }
}
