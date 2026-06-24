using CalamityLegendsComeBack.Weapons.CosmicDischarge;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    public sealed class CosmicDischargeRiftVoidLegendaryTooltip : GlobalItem
    {
        public const string TooltipLineName = "CosmicDischargeRiftVoidLegendaryText";

        private static readonly string[] RiftGlyphs = { "▮", "//", "≋", "∿", "DoG", "::", "▪" };

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendCosmicDischarge>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName ||
                !Main.keyState.PressingShift())
            {
                return true;
            }

            DrawRiftVoidLegendaryText(line);
            return false;
        }

        private static void DrawRiftVoidLegendaryText(DrawableTooltipLine line)
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
            DrawVoidBackdrop(area, time);
            DrawRiftCracks(line, area, time);
            DrawLegendaryLines(line, textLines, basePosition, lineHeight, time);
            DrawVoidForeground(area, time);
        }

        private static void DrawVoidBackdrop(Rectangle area, float time)
        {
            DrawRectangle(area, new Color(2, 0, 6, 235));
            DrawRectangle(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(8, 2, 20, 120));

            for (int i = 0; i < 5; i++)
            {
                float seed = time * 0.6f + i * 1.8f;
                float yFrac = (MathF.Sin(seed) + 1f) * 0.5f;
                int crackY = area.Y + 4 + (int)(yFrac * (area.Height - 8));
                int crackLen = 12 + (int)((MathF.Cos(seed * 2.3f) + 1f) * 0.5f * 24f);
                int crackX = area.X + 4 + (int)((MathF.Sin(seed * 0.7f + 1f) + 1f) * 0.5f * Math.Max(1, area.Width - 8 - crackLen));
                DrawRectangle(new Rectangle(crackX, crackY, crackLen, 1), new Color(90, 20, 180, 35));
                DrawRectangle(new Rectangle(crackX + 4, crackY - 1, crackLen / 2, 1), new Color(50, 10, 120, 20));
            }
        }

        private static void DrawRiftCracks(DrawableTooltipLine line, Rectangle area, float time)
        {
            int rows = Math.Clamp(area.Height / 40, 3, 10);
            Vector2 glyphScale = line.BaseScale * 0.48f;

            for (int row = 0; row < rows; row++)
            {
                float y = area.Y + 6f + row * (area.Height - 12f) / Math.Max(1, rows - 1);
                float speed = 28f + row % 3 * 8f;
                float travelWidth = area.Width + 60f;

                bool leftToRight = row % 2 == 0;
                float rawPos = (time * speed + row * 41f) % travelWidth;
                float x = leftToRight
                    ? area.X - 30f + rawPos
                    : area.Right + 30f - rawPos;

                int trailLength = 3;
                for (int t = 0; t < trailLength; t++)
                {
                    int glyphIndex = ((int)(time * 9f) + row * 5 + t * 2) % RiftGlyphs.Length;
                    float fade = 1f - t / (float)trailLength;
                    Color color = t == 0
                        ? new Color(130, 40, 255, 65)
                        : new Color(60, 10, 180, 28) * fade;

                    float trailOffset = leftToRight ? -t * 14f : t * 14f;
                    Vector2 position = new(x + trailOffset, y);
                    if (position.X < area.X + 2f || position.X > area.Right - 8f)
                        continue;
                    if (position.Y < area.Y + 2f || position.Y > area.Bottom - line.Font.LineSpacing * glyphScale.Y - 2f)
                        continue;

                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch,
                        line.Font,
                        RiftGlyphs[glyphIndex],
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
                float signal = (MathF.Sin(time * 4.8f - row * 0.88f) + 1f) * 0.5f;
                bool glitching = MathF.Sin(time * 21f + row * 5.1f) > 0.91f;
                float glitchDir = MathF.Sin(time * 33f + row * 7.7f) >= 0f ? 1f : -1f;
                Vector2 glitchOffset = glitching ? Vector2.UnitX * glitchDir * (1.5f + signal * 3f) : Vector2.Zero;
                Vector2 position = basePosition + Vector2.UnitY * row * lineHeight + glitchOffset;

                Color glowColor = new Color(60, 10, 160, 0) * (0.26f + signal * 0.18f);
                float glowRadius = 1.2f + signal * 1.3f;
                for (int draw = 0; draw < 6; draw++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * draw / 6f).ToRotationVector2() * glowRadius;
                    DrawText(line, text, position + glowOffset, glowColor);
                }

                DrawText(line, text, position + new Vector2(1f, 2f), new Color(2, 0, 8, 230));

                if (glitching)
                {
                    DrawText(line, text, position - Vector2.UnitX * glitchDir * 5f, new Color(0, 60, 200, 0) * 0.5f);
                    DrawText(line, text, position + Vector2.UnitX * glitchDir * 3f, new Color(160, 80, 255, 0) * 0.3f);
                }

                Color textColor = Color.Lerp(
                    new Color(100, 30, 220),
                    new Color(160, 110, 255),
                    0.2f + signal * 0.6f);
                DrawText(line, text, position, textColor);
            }
        }

        private static void DrawVoidForeground(Rectangle area, float time)
        {
            float pulse = (MathF.Sin(time * 6.2f) + 1f) * 0.5f;
            Color edgeColor = new Color(110, 30, 255, 110) * (0.58f + pulse * 0.36f);
            Color dimEdge = new Color(40, 8, 100, 80);

            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 1), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 1, area.Width, 1), dimEdge);
            DrawRectangle(new Rectangle(area.X, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.Right - 1, area.Y, 1, 8), edgeColor);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 8, 1, 8), dimEdge);
            DrawRectangle(new Rectangle(area.Right - 1, area.Bottom - 8, 1, 8), dimEdge);

            if ((int)(time * 5f) % 7 < 2)
            {
                int glitchY = area.Y + 3 + (int)((time * 91f) % Math.Max(1, area.Height - 6));
                int glitchW = Math.Max(16, area.Width / 5);
                int glitchX = area.X + (int)((time * 53f) % Math.Max(1, area.Width - glitchW));
                DrawRectangle(new Rectangle(glitchX, glitchY, glitchW, 1), new Color(80, 20, 200, 70));
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
