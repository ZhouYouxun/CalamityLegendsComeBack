using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using MalachiteItem = CalamityLegendsComeBack.Weapons.Malachite.Malachite;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class MalachiteShadowPeacockLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "MalachiteShadowPeacockLegendaryText";

        private static readonly string[] FeatherGlyphs = { "◈", "●", "✦", "◆", "◎", "◇" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<MalachiteItem>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawShadowPeacockLegendaryText(line);
            return false;
        }

        private static void DrawShadowPeacockLegendaryText(DrawableTooltipLine line)
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
            DrawShadowBackdrop(area, time);
            DrawFallingFeathers(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawShadowForeground(area, time);
        }

        private static void DrawShadowBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(4, 12, 6, 232));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(8, 22, 12, 115));

            for (int i = 0; i < 5; i++)
            {
                float seed = i * 2.4f + time * 0.3f;
                int px = area.X + 5 + (int)((MathF.Sin(seed * 1.3f) + 1f) * 0.5f * (area.Width - 10));
                int py = area.Y + 4 + (int)((MathF.Cos(seed * 0.9f) + 1f) * 0.5f * (area.Height - 8));
                int size = 3 + (int)((MathF.Sin(seed * 1.7f) + 1f) * 0.5f * 6f);
                DrawRectangle(new Rectangle(px - size / 2, py, size, 1), new Color(30, 140, 60, 16));
                DrawRectangle(new Rectangle(px, py - size / 2, 1, size), new Color(30, 140, 60, 12));
            }
        }

        private static void DrawFallingFeathers(DrawableTooltipLine line, Rectangle area, float time)
        {
            int columns = Math.Clamp(area.Width / 46, 4, 12);
            float travelDist = (area.Width + area.Height) + 50f;
            Vector2 glyphScale = line.BaseScale * 0.5f;

            for (int col = 0; col < columns; col++)
            {
                float speed = 14f + col % 4 * 4f;
                float progress = (time * speed + col * 36f) % travelDist;

                float angle = MathF.PI * 0.4f + col % 3 * 0.1f;
                float startX = area.X + 6f + col * (area.Width - 12f) / Math.Max(1, columns - 1);
                float startY = area.Y - 18f;

                float cx = startX + MathF.Cos(angle) * progress * 0.6f;
                float cy = startY + MathF.Sin(angle) * progress;
                cx += MathF.Sin(time * 2f + col * 1.5f) * 7f;

                if (cx < area.X + 2f || cx > area.Right - 6f ||
                    cy < area.Y + 2f || cy > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                    continue;

                int glyphIndex = ((int)(time * 5f) + col * 5) % FeatherGlyphs.Length;
                float alpha = 0.4f + MathF.Sin(time * 2.5f + col) * 0.18f;

                int r = (int)(50 + col * 12 % 60);
                int g = (int)(160 + MathF.Sin(time + col) * 40f);
                int b = (int)(80 + col * 9 % 40);
                Color featherColor = new Color(r, g, b, 0) * alpha;

                ChatManager.DrawColorCodedString(
                    Main.spriteBatch,
                    line.Font,
                    FeatherGlyphs[glyphIndex],
                    new Vector2(cx, cy),
                    featherColor,
                    time * 0.7f + col,
                    Vector2.Zero,
                    glyphScale);
            }
        }

        private static void DrawLegendaryLines(DrawableTooltipLine line, string[] textLines, Vector2 basePosition, float lineHeight, float time)
        {
            for (int row = 0; row < textLines.Length; row++)
            {
                string text = textLines[row];
                float shadow = (MathF.Sin(time * 2.2f - row * 0.75f) + 1f) * 0.5f;
                bool lurking = MathF.Sin(time * 11f + row * 4.2f) > 0.88f;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight;

                Color glowColor = new Color(20, 120, 50, 0) * (0.2f + shadow * 0.16f);
                float glowRadius = 1.3f + shadow * 1.1f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                Color shadowColor = new Color(2, 8, 4, 0) * 0.55f;
                DrawText(line, text, position + new Vector2(2f, 3f), shadowColor);
                DrawText(line, text, position + new Vector2(1f, 2f), new Color(4, 14, 8, 225));

                if (lurking)
                {
                    DrawText(line, text, position + new Vector2(-2f, 0f), new Color(10, 80, 30, 0) * 0.4f);
                }

                Color textColor = Color.Lerp(
                    new Color(60, 200, 100),
                    new Color(160, 255, 180),
                    0.22f + shadow * 0.55f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawShadowForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 4f) + 1f) * 0.5f;
            Color edgeColor = new Color(50, 210, 100, 100) * (0.55f + pulse * 0.35f);
            Color dimEdge = new Color(10, 60, 25, 75);

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
