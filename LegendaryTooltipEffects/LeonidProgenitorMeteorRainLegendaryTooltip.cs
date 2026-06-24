using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using LeonidProgenitorItem = CalamityLegendsComeBack.Weapons.LeonidProgenitor.LeonidProgenitor;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class LeonidProgenitorMeteorRainLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "LeonidProgenitorMeteorRainLegendaryText";

        private static readonly string[] StarGlyphs = { "★", "✦", "*", "·", "✧", "°" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<LeonidProgenitorItem>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawMeteorRainLegendaryText(line);
            return false;
        }

        private static void DrawMeteorRainLegendaryText(DrawableTooltipLine line)
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
            DrawNightSkyBackdrop(area, time);
            DrawFallingMeteors(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawNightSkyForeground(area, time);
        }

        private static void DrawNightSkyBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(2, 4, 14, 232));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(6, 10, 28, 118));

            int starCount = Math.Clamp(area.Width * area.Height / 600, 8, 30);
            for (int i = 0; i < starCount; i++)
            {
                float seed = i * 1.618f;
                int sx = area.X + 4 + (int)((seed * 73.1f) % (area.Width - 8));
                int sy = area.Y + 3 + (int)((seed * 41.7f) % (area.Height - 6));
                float twinkle = (MathF.Sin(time * (1.8f + seed * 0.5f) + seed) + 1f) * 0.5f;
                int alpha = (int)(12f + twinkle * 22f);
                DrawRectangle(new Rectangle(sx, sy, 1, 1), new Color(200, 210, 255, alpha));
            }
        }

        private static void DrawFallingMeteors(DrawableTooltipLine line, Rectangle area, float time)
        {
            int columns = Math.Clamp(area.Width / 50, 4, 11);
            float travelDist = (area.Width + area.Height) + 60f;
            Vector2 glyphScale = line.BaseScale * 0.47f;

            for (int col = 0; col < columns; col++)
            {
                float speed = 22f + col % 4 * 6.5f;
                float progress = (time * speed + col * 44f) % travelDist;

                float angle = MathF.PI * 0.3f + col % 3 * 0.12f;
                float startX = area.X + 8f + col * (area.Width - 16f) / Math.Max(1, columns - 1);
                float startY = area.Y - 20f;

                float cx = startX + MathF.Cos(angle) * progress;
                float cy = startY + MathF.Sin(angle) * progress;

                int trailLength = 3 + col % 2;
                for (int t = 0; t < trailLength; t++)
                {
                    float fade = 1f - t / (float)trailLength;
                    float trailX = cx - MathF.Cos(angle) * t * 10f;
                    float trailY = cy - MathF.Sin(angle) * t * 10f;

                    if (trailX < area.X + 2f || trailX > area.Right - 6f ||
                        trailY < area.Y + 2f || trailY > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    int glyphIndex = t == 0
                        ? ((int)(time * 5f) + col) % StarGlyphs.Length
                        : ((col + t) % 3 + 2) % StarGlyphs.Length;

                    Color color = t == 0
                        ? new Color(230, 230, 255, 80)
                        : new Color(160, 170, 220, 35) * fade;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        StarGlyphs[glyphIndex],
                        new Vector2(trailX, trailY),
                        color,
                        0f,
                        Vector2.Zero,
                        glyphScale * (t == 0 ? 1f : 0.75f));
                }
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                float shimmer = (MathF.Sin(time * 3.5f - row * 0.65f) + 1f) * 0.5f;
                bool flash = MathF.Sin(time * 14f + row * 3.7f) > 0.95f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight;

                Color glowColor = new Color(160, 170, 220, 0) * (0.2f + shimmer * 0.18f);
                float glowRadius = 1.2f + shimmer * 1.2f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(2, 4, 12, 225));

                if (flash)
                {
                    Color flashColor = new Color(255, 245, 200, 0) * 0.45f;
                    DrawText(line, text, position + new Vector2(-1f, 0f), flashColor);
                    DrawText(line, text, position + new Vector2(1f, 0f), flashColor);
                }

                Color textColor = Color.Lerp(
                    new Color(190, 200, 240),
                    new Color(255, 250, 220),
                    0.2f + shimmer * 0.65f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawNightSkyForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 4.5f) + 1f) * 0.5f;
            Color edgeColor = new Color(180, 190, 240, 100) * (0.55f + pulse * 0.35f);
            Color dimEdge = new Color(20, 25, 60, 75);

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
