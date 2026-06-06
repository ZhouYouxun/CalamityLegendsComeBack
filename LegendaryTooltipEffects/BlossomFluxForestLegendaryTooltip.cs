using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class BlossomFluxForestLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "BlossomFluxForestLegendaryText";

        private static readonly string[] ForestGlyphs = { "✿", "☘", "o", "v", "~", "♣" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendBlossomFlux>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawForestLegendaryText(line);
            return false;
        }

        private static void DrawForestLegendaryText(DrawableTooltipLine line)
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
            DrawForestBackdrop(area, time);
            DrawDriftingLeaves(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawForestForeground(area, time);
        }

        private static void DrawForestBackdrop(Rectangle area, float time)
        {
            // Deep forest moss green backdrop
            DrawRectangle(area, new Color(4, 18, 8, 220));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(10, 32, 14, 116));

            // Soft diagonal sunbeams moving across the box
            for (int i = 0; i < 3; i++)
            {
                float beamPos = (time * 30f + i * 120f) % (area.Width + area.Height + 100f);
                float xOffset = beamPos - area.Height;
                for (int y = 0; y < area.Height - 4; y += 4)
                {
                    int x = (int)(area.X + 2 + xOffset + y);
                    if (x >= area.X + 2 && x < area.Right - 4)
                    {
                        DrawRectangle(new Rectangle(x, area.Y + 2 + y, 6, 4), new Color(180, 255, 140, 10));
                    }
                }
            }
        }

        private static void DrawDriftingLeaves(DrawableTooltipLine line, Rectangle area, float time)
        {
            // Leaves drifting down and right
            int columns = Math.Clamp(area.Width / 50, 4, 12);
            float travelHeight = area.Height + 35f;
            Vector2 glyphScale = line.BaseScale * 0.5f;

            for (int col = 0; col < columns; col++)
            {
                // Drift starting position
                float startX = area.X + 8f + col * (area.Width - 16f) / Math.Max(1, columns - 1);
                float speed = 15f + col % 3 * 5.5f;
                float headY = area.Y - 25f + (time * speed + col * 28f) % travelHeight;
                // Add horizontal sway based on depth
                float currentX = startX + MathF.Sin(time * 2f + col) * 12f + (headY - area.Y) * 0.15f;
                int trailLength = 2;

                for (int row = 0; row < trailLength; row++)
                {
                    int glyphIndex = ((int)(time * 6f) + col * 8 + row * 3) % ForestGlyphs.Length;
                    float fade = 1f - row / (float)trailLength;
                    Color color = row == 0
                        ? new Color(110, 255, 150, 60)
                        : new Color(40, 180, 80, 25) * fade;

                    Vector2 position = new(currentX, headY - row * line.Font.LineSpacing * glyphScale.Y);
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f || position.X < area.X + 2f || position.X > area.Right - 8f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        ForestGlyphs[glyphIndex],
                        position,
                        color,
                        time * 0.8f + col, // Rotating leaves
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
                // Forest wind horizontal sway
                float sway = MathF.Sin(time * 1.8f - row * 0.62f) * 3.5f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + Vector2.UnitX * sway;

                // Mossy green outline glow
                Color glowColor = new Color(20, 140, 60, 0) * (0.24f + MathF.Sin(time * 4f - row * 0.5f) * 0.14f);
                float glowRadius = 1.4f + MathF.Sin(time * 2.2f) * 0.8f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                // Shadow
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(2, 12, 5, 230));

                // Pulsing forest/mint green color
                Color textColor = Color.Lerp(
                    new Color(90, 230, 130),
                    new Color(190, 255, 208),
                    0.28f + MathF.Sin(time * 3f - row * 0.6f) * 0.5f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawForestForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 4f) + 1f) * 0.5f;
            Color edgeColor = new Color(60, 240, 120, 100) * (0.55f + pulse * 0.35f);
            Color dimEdge = new Color(15, 90, 35, 80);

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
