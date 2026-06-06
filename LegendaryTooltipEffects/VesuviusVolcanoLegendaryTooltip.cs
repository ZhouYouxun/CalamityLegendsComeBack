using CalamityLegendsComeBack.Weapons.Vesuvius;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class VesuviusVolcanoLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "VesuviusVolcanoLegendaryText";

        private static readonly string[] AshGlyphs = { "▒", "░", "▓", "*", ".", "▲" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewVesuvius>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawVolcanoLegendaryText(line);
            return false;
        }

        private static void DrawVolcanoLegendaryText(DrawableTooltipLine line)
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
            DrawVolcanoBackdrop(area, time);
            DrawAshAndLavaSparks(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawVolcanoForeground(area, time);
        }

        private static void DrawVolcanoBackdrop(Rectangle area, float time)
        {
            // Dark basalt rock background
            DrawRectangle(area, new Color(16, 16, 16, 230));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(30, 24, 20, 128));

            // Animated magma crack lines glowing randomly inside the background
            for (int i = 0; i < 4; i++)
            {
                float seed = time * 0.8f + i * 2.3f;
                int startX = area.X + 10 + (int)((MathF.Sin(seed) + 1f) * 0.5f * (area.Width - 20));
                int startY = area.Y + 2 + (int)((MathF.Cos(seed * 1.5f) + 1f) * 0.5f * (area.Height - 6));
                int len = 8 + (int)(MathF.Sin(seed * 2.5f) * 6f);
                Color magmaColor = new Color(255, 60, 0, 30);
                DrawRectangle(new Rectangle(startX, startY, len, 1), magmaColor);
                DrawRectangle(new Rectangle(startX, startY, 1, len), magmaColor);
            }
        }

        private static void DrawAshAndLavaSparks(DrawableTooltipLine line, Rectangle area, float time)
        {
            // Falling ash and rising embers mixed together
            int columns = Math.Clamp(area.Width / 46, 4, 12);
            float travelHeight = area.Height + 35f;
            Vector2 glyphScale = line.BaseScale * 0.5f;

            for (int col = 0; col < columns; col++)
            {
                float x = area.X + 8f + col * (area.Width - 16f) / Math.Max(1, columns - 1);
                
                // Even columns: rising magma sparks (upwards)
                if (col % 2 == 0)
                {
                    float speed = 20f + col * 4f;
                    float bottomY = area.Bottom + 25f;
                    float currentY = bottomY - ((time * speed + col * 23f) % travelHeight);
                    Vector2 position = new(x + MathF.Sin(time * 3f + col) * 6f, currentY);

                    if (position.Y >= area.Y + 2f && position.Y <= area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    {
                        ChatManager.DrawColorCodedString(
                            Main.spriteBatch,
                            line.Font,
                            "▲",
                            position,
                            new Color(255, 100, 20, 60),
                            0f,
                            Vector2.Zero,
                            glyphScale);
                    }
                }
                // Odd columns: falling volcanic ash (downwards)
                else
                {
                    float speed = 12f + col * 3f;
                    float topY = area.Y - 25f;
                    float currentY = topY + ((time * speed + col * 17f) % travelHeight);
                    Vector2 position = new(x + MathF.Cos(time * 2f + col) * 8f, currentY);

                    if (position.Y >= area.Y + 2f && position.Y <= area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    {
                        int glyphIndex = ((int)(time * 4f) + col) % AshGlyphs.Length;
                        ChatManager.DrawColorCodedString(
                            Main.spriteBatch,
                            line.Font,
                            AshGlyphs[glyphIndex],
                            position,
                            new Color(110, 95, 90, 45),
                            time * 0.5f,
                            Vector2.Zero,
                            glyphScale);
                    }
                }
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            // Quake effect: random high frequency shake
            bool quaking = Main.rand.NextFloat() < 0.12f;
            Vector2 quakeOffset = quaking ? new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)) * 2.8f : Vector2.Zero;

            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + quakeOffset;

                // Lava glow outline
                Color glowColor = new Color(220, 50, 0, 0) * (0.28f + MathF.Sin(time * 4.5f - row * 0.8f) * 0.15f);
                float glowRadius = 1.3f + (quaking ? 0.8f : 0f);
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                // Shadow
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(10, 5, 0, 230));

                // Pulsing magma orange/red color
                Color textColor = Color.Lerp(
                    new Color(255, 70, 20),
                    new Color(255, 160, 40),
                    0.3f + MathF.Sin(time * 3.5f - row * 0.55f) * 0.5f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawVolcanoForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 5.5f) + 1f) * 0.5f;
            Color edgeColor = new Color(255, 80, 20, 110) * (0.6f + pulse * 0.35f);
            Color dimEdge = new Color(100, 30, 5, 80);

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
