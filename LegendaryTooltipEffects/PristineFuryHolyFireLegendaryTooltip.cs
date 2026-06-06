using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class PristineFuryHolyFireLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "PristineFuryHolyFireLegendaryText";

        private static readonly string[] EmberGlyphs = { "▲", "☼", "★", "o", "*", ".", "PF" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendPristineFury>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawHolyFireLegendaryText(line);
            return false;
        }

        private static void DrawHolyFireLegendaryText(DrawableTooltipLine line)
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
            DrawFireBackdrop(area, time);
            DrawRisingEmbers(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawFireForeground(area, time);
        }

        private static void DrawFireBackdrop(Rectangle area, float time)
        {
            // Dark obsidian furnace background
            DrawRectangle(area, new Color(22, 6, 2, 220));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(40, 12, 4, 120));

            // Soft glowing horizontal bands inside the box simulating rising heat waves
            for (int y = area.Y + 3; y < area.Bottom - 3; y += 8)
            {
                float waveOffset = MathF.Sin(time * 3f + y * 0.05f) * 10f;
                int pulseY = y - (int)((time * 20f) % 8f);
                if (pulseY >= area.Y + 3 && pulseY < area.Bottom - 3)
                {
                    DrawRectangle(new Rectangle(area.X + 3 + (int)waveOffset, pulseY, area.Width - 6 - (int)MathF.Abs(waveOffset) * 2, 2), new Color(255, 69, 0, 15));
                }
            }
        }

        private static void DrawRisingEmbers(DrawableTooltipLine line, Rectangle area, float time)
        {
            // Sparks rising upwards
            int columns = Math.Clamp(area.Width / 48, 4, 12);
            float travelHeight = area.Height + 35f;
            Vector2 glyphScale = line.BaseScale * 0.45f;

            for (int col = 0; col < columns; col++)
            {
                float x = area.X + 8f + col * (area.Width - 16f) / Math.Max(1, columns - 1);
                // Rising speed
                float speed = 24f + col % 4 * 6f;
                // Move from bottom (area.Bottom + 25) to top (area.Y - 25)
                float bottomY = area.Bottom + 25f;
                float currentY = bottomY - ((time * speed + col * 42f) % travelHeight);
                int trailLength = 3;

                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 10f) + col * 9 + row * 2) % EmberGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    // Embers fade as they trail down from the rising head
                    Color color = row == 0
                        ? new Color(255, 220, 120, 80)
                        : new Color(230, 90, 20, 40) * fade;
                    Vector2 position = new(x + MathF.Sin(time * 2.5f + col * 1.5f + row) * 4f, currentY + row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        EmberGlyphs[glyphIndex],
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
                // Heat shimmer offset (vertical and horizontal waves)
                float wave = time * 4.5f - row * 0.9f;
                Vector2 heatOffset = new(
                    MathF.Sin(wave) * 2.2f,
                    MathF.Cos(wave * 0.8f) * 1.2f
                );
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + heatOffset;

                // Fire outline glow
                Color glowColor = new Color(230, 80, 20, 0) * (0.28f + MathF.Sin(time * 5f - row * 0.7f) * 0.12f);
                float glowRadius = 1.3f + MathF.Sin(time * 3f) * 0.5f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                // Drop shadow
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(15, 3, 0, 220));

                // Pulsing fire gradient color
                Color textColor = Color.Lerp(
                    new Color(255, 90, 30),
                    new Color(255, 225, 130),
                    0.4f + MathF.Sin(time * 3f - row * 0.5f) * 0.45f);

                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawFireForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 5f) + 1f) * 0.5f;
            // Glowing warm borders
            Color edgeColor = new Color(255, 120, 30, 120) * (0.6f + pulse * 0.35f);
            Color dimEdge = new Color(130, 30, 10, 90);

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
