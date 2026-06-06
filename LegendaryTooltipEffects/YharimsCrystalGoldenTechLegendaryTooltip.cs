using CalamityLegendsComeBack.Weapons.YharimsCrystal;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class YharimsCrystalGoldenTechLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "YharimsCrystalGoldenTechLegendaryText";

        private static readonly string[] TechGlyphs = { "0", "1", "A", "F", "7", "9", "▲", "■", "▶", "//" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendYharimsCrystal>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawGoldenTechLegendaryText(line);
            return false;
        }

        private static void DrawGoldenTechLegendaryText(DrawableTooltipLine line)
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
            Rectangle area = new(
                (int)basePosition.X - 8,
                (int)basePosition.Y - 5,
                Math.Max(24, (int)Math.Ceiling(textWidth) + 16),
                Math.Max(18, (int)Math.Ceiling(textHeight) + 10));

            float time = Main.GlobalTimeWrappedHourly;
            DrawTechBackdrop(area, time);
            DrawDigitalStreams(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawTechForeground(area, time);
        }

        private static void DrawTechBackdrop(Rectangle area, float time)
        {
            // Dark futuristic circuit board background
            DrawRectangle(area, new Color(18, 14, 2, 230));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(32, 24, 4, 116));

            // Grid lines overlay
            for (int x = area.X + 6; x < area.Right - 4; x += 20)
                DrawRectangle(new Rectangle(x, area.Y + 3, 1, area.Height - 6), new Color(255, 215, 0, 10));

            for (int y = area.Y + 6; y < area.Bottom - 4; y += 14)
                DrawRectangle(new Rectangle(area.X + 3, y, area.Width - 6, 1), new Color(255, 215, 0, 10));

            // High-tech golden sweep bar
            int sweepY = area.Y + 2 + (int)((time * 62f) % Math.Max(1, area.Height - 4));
            DrawRectangle(new Rectangle(area.X + 2, sweepY, area.Width - 4, 1), new Color(255, 240, 150, 45));
        }

        private static void DrawDigitalStreams(DrawableTooltipLine line, Rectangle area, float time)
        {
            // Falling binary / hex streams
            int columns = Math.Clamp(area.Width / 50, 5, 15);
            float travelHeight = area.Height + 35f;
            Vector2 glyphScale = line.BaseScale * 0.46f;

            for (int col = 0; col < columns; col++)
            {
                float x = area.X + 9f + col * (area.Width - 18f) / Math.Max(1, columns - 1);
                float speed = 25f + col % 4 * 5.5f;
                float headY = area.Y - 25f + (time * speed + col * 33f) % travelHeight;
                int trailLength = 3;

                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 12f) + col * 8 + row * 2) % TechGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    Color color = row == 0
                        ? new Color(255, 245, 180, 80)
                        : new Color(200, 160, 20, 32) * fade;

                    Vector2 position = new(x, headY - row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        TechGlyphs[glyphIndex],
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

                // Digital glitch offsets
                float signal = (MathF.Sin(time * 5.2f - row * 0.75f) + 1f) * 0.5f;
                bool glitching = MathF.Sin(time * 19f + row * 3.44f) > 0.94f;
                float glitchDirection = MathF.Sin(time * 28f + row * 7f) >= 0f ? 1f : -1f;
                Vector2 glitchOffset = glitching ? Vector2.UnitX * glitchDirection * (2f + signal * 2.5f) : Vector2.Zero;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + glitchOffset;

                // Auric golden outline glow
                Color glowColor = new Color(220, 170, 10, 0) * (0.24f + signal * 0.16f);
                float glowRadius = 1.2f + signal * 1.2f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                // Shadow
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(15, 12, 2, 230));

                // Chromatic Aberration under tech glitch
                if (glitching)
                {
                    // Gold / Orange aberration
                    DrawText(line, text, position - Vector2.UnitX * glitchDirection * 4f, new Color(230, 110, 10, 0) * 0.5f);
                    // Light gold / Amber aberration
                    DrawText(line, text, position + Vector2.UnitX * glitchDirection * 2f, new Color(255, 230, 180, 0) * 0.3f);
                }

                // Text core color pulsing
                Color textColor = Color.Lerp(
                    new Color(255, 190, 20),
                    new Color(255, 250, 205),
                    0.2f + signal * 0.65f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawTechForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 7f) + 1f) * 0.5f;
            Color edgeColor = new Color(255, 205, 50, 120) * (0.6f + pulse * 0.35f);
            Color dimEdge = new Color(140, 100, 10, 80);

            // Techy corner brackets
            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);

            // Futuristic corner markers
            DrawRectangle(new Rectangle(area.X, area.Y, 8, 1), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 8, area.Y, 8, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, 8, 1), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 8, area.Bottom - 1, 8, 1), dimEdge);

            // Bounded digital cursor blink in bottom right
            if ((int)(time * 3.5f) % 2 == 0)
                DrawRectangle(new Rectangle(area.Right - 14, area.Bottom - 5, 8, 2), new Color(255, 220, 100, 160));
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
