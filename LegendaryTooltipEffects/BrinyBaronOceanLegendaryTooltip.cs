using CalamityLegendsComeBack.Weapons.BrinyBaron;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class BrinyBaronOceanLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "BrinyBaronOceanLegendaryText";

        private static readonly string[] WaterGlyphs = { "o", "O", "°", ".", "~" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendBrinyBaron>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawOceanLegendaryText(line);
            return false;
        }

        private static void DrawOceanLegendaryText(DrawableTooltipLine line)
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
            DrawOceanBackdrop(area, time);
            DrawRisingBubbles(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawOceanForeground(area, time);
        }

        private static void DrawOceanBackdrop(Rectangle area, float time)
        {
            // Deep blue abyss background
            DrawRectangle(area, new Color(0, 8, 26, 225));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(4, 20, 52, 116));

            // Caustic ripples crossing
            for (int y = area.Y + 4; y < area.Bottom - 4; y += 6)
            {
                float rippleX = area.X + 4f + (MathF.Sin(time * 2.2f + y * 0.08f) + 1f) * 0.5f * (area.Width - 12f);
                DrawRectangle(new Rectangle((int)rippleX, y, 4, 2), new Color(135, 235, 255, 12));
            }
        }

        private static void DrawRisingBubbles(DrawableTooltipLine line, Rectangle area, float time)
        {
            // Sinusoidally swaying rising bubbles
            int columns = Math.Clamp(area.Width / 44, 5, 14);
            float travelHeight = area.Height + 35f;
            Vector2 glyphScale = line.BaseScale * 0.55f;

            for (int col = 0; col < columns; col++)
            {
                float startX = area.X + 8f + col * (area.Width - 16f) / Math.Max(1, columns - 1);
                float speed = 18f + col % 4 * 4f;
                float bottomY = area.Bottom + 25f;
                float currentY = bottomY - ((time * speed + col * 37f) % travelHeight);
                float currentX = startX + MathF.Sin(time * 3f + col * 1.8f) * 10f;
                int trailLength = 2;

                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 5f) + col * 7 + row * 2) % WaterGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    Color color = row == 0
                        ? new Color(140, 240, 255, 70)
                        : new Color(30, 120, 210, 30) * fade;

                    Vector2 position = new(currentX, currentY + row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        WaterGlyphs[glyphIndex],
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
                // Water wave horizontal ripple refraction
                float rippleX = MathF.Sin(time * 2.8f + row * 0.88f) * 4.2f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + Vector2.UnitX * rippleX;

                // Deep sea cyan glow outline
                Color glowColor = new Color(0, 110, 190, 0) * (0.26f + MathF.Sin(time * 3.6f - row * 0.62f) * 0.14f);
                float glowRadius = 1.3f + MathF.Sin(time * 2f) * 0.6f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                // Shadow
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(0, 3, 12, 220));

                // Pulsing aqua-blue/white gradient
                Color textColor = Color.Lerp(
                    new Color(90, 210, 255),
                    new Color(225, 248, 255),
                    0.35f + MathF.Sin(time * 2.8f - row * 0.5f) * 0.5f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawOceanForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 4.2f) + 1f) * 0.5f;
            Color edgeColor = new Color(80, 220, 255, 110) * (0.6f + pulse * 0.35f);
            Color dimEdge = new Color(10, 60, 130, 85);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);
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
