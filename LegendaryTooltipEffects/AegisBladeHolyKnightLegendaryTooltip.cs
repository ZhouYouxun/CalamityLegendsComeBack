using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using AegisBladeItem = CalamityLegendsComeBack.Weapons.AegisBlade.AegisBlade;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class AegisBladeHolyKnightLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "AegisBladeHolyKnightLegendaryText";

        private static readonly string[] SacredGlyphs = { "†", "✦", "★", "◈", "*", "▲", "│" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<AegisBladeItem>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawHolyKnightLegendaryText(line);
            return false;
        }

        private static void DrawHolyKnightLegendaryText(DrawableTooltipLine line)
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
            DrawHolyBackdrop(area, time);
            DrawRisingSacredLight(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawHolyForeground(area, time);
        }

        private static void DrawHolyBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(8, 6, 2, 228));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(22, 16, 4, 120));

            for (int y = area.Y + 4; y < area.Bottom - 4; y += 10)
            {
                float phase = MathF.Sin(time * 1.6f + y * 0.04f);
                int lineAlpha = (int)(8f + phase * 5f);
                DrawRectangle(new Rectangle(area.X + 3, y, area.Width - 6, 1), new Color(255, 215, 50, lineAlpha));
            }

            float sweepX = area.X + 2f + (time * 38f) % Math.Max(1, area.Width - 4);
            DrawRectangle(new Rectangle((int)sweepX, area.Y + 2, 2, area.Height - 4), new Color(255, 220, 100, 28));
        }

        private static void DrawRisingSacredLight(DrawableTooltipLine line, Rectangle area, float time)
        {
            int columns = Math.Clamp(area.Width / 48, 4, 12);
            float travelHeight = area.Height + 40f;
            Vector2 glyphScale = line.BaseScale * 0.46f;

            for (int col = 0; col < columns; col++)
            {
                float x = area.X + 9f + col * (area.Width - 18f) / Math.Max(1, columns - 1);
                float speed = 20f + col % 4 * 5f;
                float bottomY = area.Bottom + 28f;
                float currentY = bottomY - ((time * speed + col * 38f) % travelHeight);
                float swayX = x + MathF.Sin(time * 1.4f + col * 2.1f) * 5f;

                int trailLength = 2 + col % 2;
                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 7f) + col * 6 + row * 2) % SacredGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    Color color = row == 0
                        ? new Color(255, 220, 100, 70)
                        : new Color(200, 160, 30, 30) * fade;

                    Vector2 position = new(swayX, currentY + row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        SacredGlyphs[glyphIndex],
                        position,
                        color,
                        row == 0 ? time * 0.5f : 0f,
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
                float breath = (MathF.Sin(time * 2.4f - row * 0.7f) + 1f) * 0.5f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight;

                Color glowColor = new Color(200, 150, 20, 0) * (0.22f + breath * 0.18f);
                float glowRadius = 1.3f + breath * 1.5f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                Color outerGlow = new Color(255, 235, 160, 0) * (0.1f + breath * 0.08f);
                for (int draw = 0; draw < 4; draw++)
                {
                    Vector2 outerOffset = (MathHelper.TwoPi * draw / 4f).ToRotationVector2() * (glowRadius + 1.5f);
                    DrawText(line, text, position + outerOffset, outerGlow);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(10, 8, 2, 220));

                Color textColor = Color.Lerp(
                    new Color(255, 195, 60),
                    new Color(255, 245, 190),
                    0.25f + breath * 0.6f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawHolyForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 5f) + 1f) * 0.5f;
            Color edgeColor = new Color(255, 210, 80, 120) * (0.6f + pulse * 0.35f);
            Color dimEdge = new Color(120, 80, 10, 80);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);

            DrawRectangle(new Rectangle(area.X, area.Y, 8, 1), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 8, area.Y, 8, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, 8, 1), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 8, area.Bottom - 1, 8, 1), dimEdge);
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
