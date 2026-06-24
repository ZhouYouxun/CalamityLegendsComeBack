using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using SeasSearingItem = CalamityLegendsComeBack.Weapons.SeasSearing.SeasSearing;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class SeasSearingAbyssalPollutionLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "SeasSearingAbyssalPollutionLegendaryText";

        private static readonly string[] BubbleGlyphs = { "○", "°", "•", "·", "◦" };
        private static readonly string[] AcidGlyphs = { "~", "≋", "∿", "-" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<SeasSearingItem>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawAbyssalPollutionLegendaryText(line);
            return false;
        }

        private static void DrawAbyssalPollutionLegendaryText(DrawableTooltipLine line)
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
            DrawAbyssBackdrop(area, time);
            DrawPollutionParticles(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawAbyssForeground(area, time);
        }

        private static void DrawAbyssBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(2, 8, 14, 232));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(4, 16, 26, 115));

            for (int i = 0; i < 4; i++)
            {
                float seed = time * 0.5f + i * 2.1f;
                float yFrac = (MathF.Sin(seed) + 1f) * 0.5f;
                int y = area.Y + 4 + (int)(yFrac * (area.Height - 8));
                int len = 10 + (int)((MathF.Cos(seed * 2f) + 1f) * 0.5f * 20f);
                int startX = i % 2 == 0 ? area.X + 3 : area.Right - 3 - len;
                DrawRectangle(new Rectangle(startX, y, len, 1), new Color(30, 180, 100, 22));
                DrawRectangle(new Rectangle(startX, y + 2, len / 2, 1), new Color(20, 120, 70, 14));
            }
        }

        private static void DrawPollutionParticles(DrawableTooltipLine line, Rectangle area, float time)
        {
            int bubbleCount = Math.Clamp(area.Width / 40, 5, 14);
            float travelHeight = area.Height + 40f;
            Vector2 glyphScale = line.BaseScale * 0.52f;

            for (int col = 0; col < bubbleCount; col++)
            {
                float x = area.X + 7f + col * (area.Width - 14f) / Math.Max(1, bubbleCount - 1);

                if (col % 3 == 2)
                {
                    float speed = 8f + col * 2.5f;
                    float topY = area.Y - 20f;
                    float currentY = topY + ((time * speed + col * 19f) % travelHeight);
                    float drX = x + MathF.Cos(time * 1.2f + col) * 4f;

                    if (currentY >= area.Y + 2f && currentY <= area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    {
                        int gIdx = ((int)(time * 3f) + col) % AcidGlyphs.Length;
                        ChatManager.DrawColorCodedString(
                            Main.spriteBatch,
                            line.Font,
                            AcidGlyphs[gIdx],
                            new Vector2(drX, currentY),
                            new Color(50, 200, 120, 38),
                            0f,
                            Vector2.Zero,
                            glyphScale);
                    }
                }
                else
                {
                    float speed = 12f + col % 4 * 3.5f;
                    float bottomY = area.Bottom + 26f;
                    float currentY = bottomY - ((time * speed + col * 27f) % travelHeight);
                    float bubX = x + MathF.Sin(time * 2.4f + col * 1.7f) * 8f;

                    if (currentY >= area.Y + 2f && currentY <= area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    {
                        int gIdx = ((int)(time * 4f) + col * 3) % BubbleGlyphs.Length;
                        float tox = (col % 2 == 0) ? 0.55f : 0.4f;
                        Color bubColor = Color.Lerp(new Color(40, 220, 150), new Color(100, 255, 80), tox) * 0.45f;

                        ChatManager.DrawColorCodedString(
                            Main.spriteBatch,
                            line.Font,
                            BubbleGlyphs[gIdx],
                            new Vector2(bubX, currentY),
                            bubColor,
                            0f,
                            Vector2.Zero,
                            glyphScale);
                    }
                }
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                float rippleX = MathF.Sin(time * 2.2f + row * 1.1f) * 3.5f;
                float rippleY = MathF.Cos(time * 1.7f + row * 0.8f) * 1.2f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + new Vector2(rippleX, rippleY);

                float pulse = (MathF.Sin(time * 3.8f - row * 0.7f) + 1f) * 0.5f;
                Color glowColor = new Color(20, 160, 90, 0) * (0.22f + pulse * 0.16f);
                float glowRadius = 1.3f + pulse * 0.9f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                Color radGlow = new Color(60, 255, 140, 0) * (0.08f + pulse * 0.07f);
                for (int draw = 0; draw < 4; draw++)
                {
                    Vector2 radOffset = (MathHelper.TwoPi * draw / 4f).ToRotationVector2() * (glowRadius + 1.8f);
                    DrawText(line, text, position + radOffset, radGlow);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(2, 8, 10, 228));

                Color textColor = Color.Lerp(
                    new Color(40, 200, 160),
                    new Color(120, 255, 180),
                    0.28f + pulse * 0.55f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawAbyssForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 5f) + 1f) * 0.5f;
            Color edgeColor = new Color(40, 220, 140, 108) * (0.58f + pulse * 0.36f);
            Color dimEdge = new Color(8, 70, 40, 78);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);

            if ((int)(time * 3f) % 5 == 0)
            {
                int flashY = area.Y + 3 + (int)((time * 77f) % Math.Max(1, area.Height - 6));
                DrawRectangle(new Rectangle(area.X + 2, flashY, area.Width - 4, 1), new Color(30, 200, 120, 55));
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
