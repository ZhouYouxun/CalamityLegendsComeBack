using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using GlacialEmbraceItem = CalamityLegendsComeBack.Weapons.GlacialEmbrace.GlacialEmbrace;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class GlacialEmbraceAuroraFrostLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "GlacialEmbraceAuroraFrostLegendaryText";

        private static readonly string[] FrostGlyphs = { "❄", "◇", "✦", "°", "◈", "·", "✧" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<GlacialEmbraceItem>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawAuroraFrostLegendaryText(line);
            return false;
        }

        private static void DrawAuroraFrostLegendaryText(DrawableTooltipLine line)
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
            DrawAuroraBackdrop(area, time);
            DrawDriftingIceCrystals(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawAuroraForeground(area, time);
        }

        private static void DrawAuroraBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(4, 8, 22, 228));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(8, 18, 40, 118));

            for (int i = 0; i < 4; i++)
            {
                float bandPhase = time * 0.4f + i * (MathF.PI * 0.5f);
                float yFrac = (MathF.Sin(bandPhase) + 1f) * 0.5f;
                int bandY = area.Y + 3 + (int)(yFrac * (area.Height - 6));
                float hue = (time * 0.12f + i * 0.25f) % 1f;
                Color bandColor = HueToColor(hue, 0.12f);
                DrawRectangle(new Rectangle(area.X + 2, bandY, area.Width - 4, 2), bandColor);
                DrawRectangle(new Rectangle(area.X + 2, Math.Min(bandY + 3, area.Bottom - 4), area.Width - 4, 1), bandColor * 0.4f);
            }
        }

        private static void DrawDriftingIceCrystals(DrawableTooltipLine line, Rectangle area, float time)
        {
            int columns = Math.Clamp(area.Width / 44, 4, 13);
            float travelHeight = area.Height + 40f;
            Vector2 glyphScale = line.BaseScale * 0.5f;

            for (int col = 0; col < columns; col++)
            {
                float startX = area.X + 7f + col * (area.Width - 14f) / Math.Max(1, columns - 1);
                float speed = 10f + col % 4 * 3.5f;
                float topY = area.Y - 22f;
                float currentY = topY + ((time * speed + col * 32f) % travelHeight);
                float swayX = startX + MathF.Sin(time * 1.6f + col * 1.3f) * 9f;

                if (currentY < area.Y + 2f || currentY > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    continue;

                int glyphIndex = ((int)(time * 4f) + col * 7) % FrostGlyphs.Length;
                float hue = (time * 0.2f + col * 0.09f) % 1f;
                Color crystalColor = Color.Lerp(HueToColor(hue, 0.55f), Color.White, 0.35f) * 0.55f;

                ChatManager.DrawColorCodedString(
                    Main.spriteBatch,
                    line.Font,
                    FrostGlyphs[glyphIndex],
                    new Vector2(swayX, currentY),
                    crystalColor,
                    time * 0.3f + col,
                    Vector2.Zero,
                    glyphScale);
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                float sway = MathF.Sin(time * 1.4f - row * 0.55f) * 2.8f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + Vector2.UnitX * sway;

                float hue = (time * 0.15f + row * 0.12f) % 1f;
                Color glowBase = HueToColor(hue, 0.3f);
                Color glowColor = new Color(glowBase.R, glowBase.G, glowBase.B, 0) * 0.26f;
                float glowRadius = 1.4f + MathF.Sin(time * 2.5f - row * 0.4f) * 0.8f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(2, 6, 15, 225));

                float colorT = (MathF.Sin(time * 2f - row * 0.6f) + 1f) * 0.5f;
                Color auroraA = HueToColor((time * 0.15f + row * 0.14f) % 1f, 0.7f);
                Color auroraB = HueToColor((time * 0.15f + row * 0.14f + 0.2f) % 1f, 0.7f);
                Color textColor = Color.Lerp(auroraA, auroraB, colorT);
                textColor = Color.Lerp(textColor, Color.White, 0.35f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawAuroraForeground(Rectangle area, float time)
        {
            float hue = (time * 0.18f) % 1f;
            Color edgeBase = HueToColor(hue, 0.7f);
            Color edgeColor = new Color(edgeBase.R, edgeBase.G, edgeBase.B, 110);
            Color dimEdge = new Color(12, 30, 60, 80);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);
        }

        private static Color HueToColor(float hue, float alpha)
        {
            hue = hue % 1f;
            float h = hue * 6f;
            int i = (int)h;
            float f = h - i;
            float p = 0f;
            float q = 1f - f;
            float t = f;
            float r, g, b;
            switch (i % 6)
            {
                case 0: r = 1f; g = t; b = p; break;
                case 1: r = q; g = 1f; b = p; break;
                case 2: r = p; g = 1f; b = t; break;
                case 3: r = p; g = q; b = 1f; break;
                case 4: r = t; g = p; b = 1f; break;
                default: r = 1f; g = p; b = q; break;
            }
            return new Color(r, g, b, alpha);
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
