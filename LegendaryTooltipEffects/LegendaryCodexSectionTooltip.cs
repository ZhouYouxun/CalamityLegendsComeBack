using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    // Use the final vanilla line positions so localization, UI scale and other tooltip
    // lines keep their normal layout. Each contiguous color section shares one effect.
    public sealed class LegendaryCodexSectionTooltip : GlobalItem
    {
        private enum SectionStyle { Welcome, Warning, Features, Farewell }

        private readonly record struct TextRow(DrawableTooltipLine Line, string Text, Vector2 Size);

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            return item.ModItem is not LegendaryCodex || GetSectionColor(line) < 0;
        }

        public override void PostDrawTooltip(Item item, ReadOnlyCollection<DrawableTooltipLine> lines)
        {
            if (item.ModItem is not LegendaryCodex)
                return;

            var rows = new List<TextRow>();
            float time = Main.GlobalTimeWrappedHourly;
            bool passedOpening = false;

            for (int index = 0; index < lines.Count;)
            {
                int color = GetSectionColor(lines[index]);
                if (color < 0)
                {
                    index++;
                    continue;
                }

                SectionStyle style = color == 1 ? SectionStyle.Warning
                    : color == 2 ? SectionStyle.Features
                    : passedOpening ? SectionStyle.Farewell : SectionStyle.Welcome;
                rows.Clear();

                do
                {
                    DrawableTooltipLine line = lines[index++];
                    string text = PlainText(line.Text);
                    Vector2 size = ChatManager.GetStringSize(line.Font, text, line.BaseScale);
                    rows.Add(new TextRow(line, text, size));
                }
                while (index < lines.Count && GetSectionColor(lines[index]) == color);

                // Leave space before the following section, price or mod attribution.
                float nextY = index < lines.Count ? lines[index].Y : float.MaxValue;
                DrawSection(rows, style, time, nextY);
                passedOpening = true;
            }
        }

        private static int GetSectionColor(DrawableTooltipLine line)
        {
            // Only the item's own numbered description lines, never name/value lines
            // or another mod's appended tooltip. Keep the localized color tags intact.
            if (line.Mod != "Terraria" || !line.Name.StartsWith("Tooltip", StringComparison.Ordinal) ||
                !int.TryParse(line.Name.AsSpan(7), out _) || string.IsNullOrEmpty(line.Text))
                return -1;

            if (line.Text.StartsWith("[c/55DFFF:", StringComparison.OrdinalIgnoreCase)) return 0;
            if (line.Text.StartsWith("[c/FF5555:", StringComparison.OrdinalIgnoreCase)) return 1;
            if (line.Text.StartsWith("[c/70E0A0:", StringComparison.OrdinalIgnoreCase)) return 2;
            return -1;
        }

        private static string PlainText(string text)
        {
            var snippets = ChatManager.ParseMessage(text, Color.White);
            return string.Concat(snippets.ConvertAll(snippet => snippet.Text));
        }

        private static void DrawSection(List<TextRow> rows, SectionStyle style, float time, float nextY)
        {
            DrawableTooltipLine first = rows[0].Line;
            float scale = Math.Max(0.5f, first.BaseScale.Y);
            float left = first.X;
            float right = left;
            float bottom = first.Y;
            foreach (TextRow row in rows)
            {
                left = Math.Min(left, row.Line.X);
                right = Math.Max(right, row.Line.X + row.Size.X);
                bottom = Math.Max(bottom, row.Line.Y + row.Size.Y);
            }

            bottom = Math.Min(bottom, nextY - 2f * scale);
            Rectangle area = new((int)(left - 5f * scale), first.Y,
                Math.Max(20, (int)Math.Ceiling(right - left + 10f * scale)),
                Math.Max(8, (int)Math.Ceiling(bottom - first.Y)));

            Color theme = style switch
            {
                SectionStyle.Warning => new Color(255, 85, 85),
                SectionStyle.Features => new Color(112, 224, 160),
                _ => new Color(85, 223, 255)
            };

            // Shared dark glass, fine corner marks and a restrained colored glow.
            Fill(area, Color.Lerp(new Color(3, 7, 16), theme, 0.035f) * 0.72f);
            Fill(new Rectangle(area.X, area.Y, Math.Max(1, (int)scale), area.Height), theme * 0.22f);
            Rectangle inner = area;
            inner.Inflate(-3, -2);
            if (inner.Width > 8 && inner.Height > 6)
            {
                switch (style)
                {
                    case SectionStyle.Welcome: DrawWelcome(inner, theme, time, scale); break;
                    case SectionStyle.Warning: DrawWarning(inner, theme, time, scale); break;
                    case SectionStyle.Features: DrawFeatures(inner, theme, time, scale); break;
                    case SectionStyle.Farewell: DrawFarewell(inner, theme, time, scale); break;
                }
            }

            DrawFrame(area, theme, style, time, scale);
            for (int row = 0; row < rows.Count; row++)
                DrawTextRow(rows[row], style, theme, time, row, rows.Count);
        }

        private static void DrawWelcome(Rectangle area, Color theme, float time, float scale)
        {
            // Fast, narrow beacon crossing a sparse constellation.
            float sweep = Fraction(time * 0.19f) * (area.Width + 70f * scale) - 35f * scale;
            for (int band = -3; band <= 3; band++)
            {
                float strength = 1f - Math.Abs(band) / 4f;
                FillClipped(area, new Rectangle((int)(area.X + sweep + band * 4f * scale), area.Y,
                    Math.Max(1, (int)(4f * scale)), area.Height), theme * (0.055f * strength));
            }

            int count = Math.Clamp(area.Width / 70, 4, 11);
            Vector2 previous = Vector2.Zero;
            for (int i = 0; i < count; i++)
            {
                float x = area.X + (i + 0.5f) * area.Width / count;
                float y = area.Y + area.Height * (0.5f + 0.25f * MathF.Sin(i * 2.4f + time * 0.35f));
                Vector2 point = new(x, y);
                if (i > 0) Stroke(previous, point, theme * 0.07f, scale);
                Spark(point, theme, (0.14f + 0.2f * Wave(time * 2.2f + i * 2f)), scale);
                previous = point;
            }
        }

        private static void DrawWarning(Rectangle area, Color theme, float time, float scale)
        {
            // Two soft signal pulses, with a slow scan and paired edge ticks.
            float pulse = WarningPulse(time);
            Fill(area, theme * (0.012f + pulse * 0.035f));
            float scan = area.X + Fraction(time * 0.13f) * area.Width;
            FillClipped(area, new Rectangle((int)scan, area.Y, Math.Max(1, (int)(2f * scale)), area.Height),
                theme * 0.12f);
            for (int i = 0; i < 7; i++)
            {
                float x = area.X + (i + 0.5f) * area.Width / 7f;
                Color tick = theme * (0.12f + pulse * 0.25f);
                Stroke(new Vector2(x - 3f * scale, area.Y), new Vector2(x, area.Y + 3f * scale), tick, scale);
                Stroke(new Vector2(x, area.Bottom - 3f * scale), new Vector2(x + 3f * scale, area.Bottom), tick, scale);
            }
        }

        private static void DrawFeatures(Rectangle area, Color theme, float time, float scale)
        {
            // Living filaments along the sides, with gently falling diamond leaves.
            for (int side = 0; side < 2; side++)
            {
                Vector2 previous = Vector2.Zero;
                for (int step = 0; step <= 24; step++)
                {
                    float t = step / 24f;
                    float inset = (2f + 1.5f * MathF.Sin(t * 9f - time * 0.8f + side)) * scale;
                    Vector2 point = new(side == 0 ? area.X + inset : area.Right - inset, area.Y + t * area.Height);
                    if (step > 0) Stroke(previous, point, theme * 0.16f, scale);
                    previous = point;
                }
            }

            int count = Math.Clamp(area.Width / 65, 5, 11);
            for (int i = 0; i < count; i++)
            {
                float progress = Fraction(time * (0.045f + i % 3 * 0.008f) + i * 0.618f);
                float x = area.X + (0.08f + 0.84f * Fraction(i * 0.618f + 0.2f)) * area.Width
                    + MathF.Sin(time * 0.8f + i * 2f) * 3f * scale;
                float y = MathHelper.Lerp(area.Y + 4f * scale, area.Bottom - 4f * scale, progress);
                Vector2 center = new(x, y);
                float fade = MathF.Sin(progress * MathHelper.Pi);
                Vector2 stem = new Vector2(2.5f, 2f) * scale;
                Vector2 leaf = new Vector2(1.5f, -1.5f) * scale;
                Color color = theme * (fade * 0.3f);
                Stroke(center - stem, center + leaf, color, scale);
                Stroke(center + leaf, center + stem, color, scale);
                Stroke(center + stem, center - leaf, color, scale);
                Stroke(center - leaf, center - stem, color, scale);
            }
        }

        private static void DrawFarewell(Rectangle area, Color theme, float time, float scale)
        {
            // Broad, quiet ribbons and rising dust distinguish this cyan from the beacon.
            for (int ribbon = 0; ribbon < 2; ribbon++)
            {
                Vector2 previous = Vector2.Zero;
                for (int step = 0; step <= 36; step++)
                {
                    float t = step / 36f;
                    float y = area.Y + area.Height * (0.32f + ribbon * 0.35f
                        + 0.12f * MathF.Sin(t * 7f - time * 0.65f + ribbon * 2f));
                    Vector2 point = new(area.X + t * area.Width, y);
                    if (step > 0)
                    {
                        Stroke(previous, point, theme * 0.025f, 5f * scale);
                        Stroke(previous, point, theme * 0.08f, scale);
                    }
                    previous = point;
                }
            }

            int count = Math.Clamp(area.Width / 55, 6, 12);
            for (int i = 0; i < count; i++)
            {
                float progress = Fraction(time * (0.028f + i % 3 * 0.006f) + i * 0.618f);
                float x = area.X + area.Width * (0.06f + 0.88f * Fraction(i * 0.7549f + 0.12f))
                    + MathF.Sin(time * 0.45f + i) * 3f * scale;
                float y = MathHelper.Lerp(area.Bottom - 3f * scale, area.Y + 3f * scale, progress);
                Spark(new Vector2(x, y), theme, MathF.Sin(progress * MathHelper.Pi) * 0.25f, scale * 0.7f);
            }
        }

        private static void DrawFrame(Rectangle area, Color theme, SectionStyle style, float time, float scale)
        {
            float pulse = style == SectionStyle.Warning ? WarningPulse(time) : Wave(time * 1.4f);
            Color edge = theme * (0.2f + pulse * 0.14f);
            int length = Math.Max(4, (int)(7f * scale));
            int thickness = Math.Max(1, (int)scale);
            Fill(new Rectangle(area.X, area.Y, length, thickness), edge);
            Fill(new Rectangle(area.Right - length, area.Y, length, thickness), edge);
            Fill(new Rectangle(area.X, area.Bottom - thickness, length, thickness), edge);
            Fill(new Rectangle(area.Right - length, area.Bottom - thickness, length, thickness), edge);
            Fill(new Rectangle(area.Right - thickness, area.Y, thickness, length), edge);
            Fill(new Rectangle(area.Right - thickness, area.Bottom - length, thickness, length), edge);
        }

        private static void DrawTextRow(TextRow row, SectionStyle style, Color theme, float time, int index, int count)
        {
            DrawableTooltipLine line = row.Line;
            float signal = style switch
            {
                SectionStyle.Welcome => MathF.Pow(Wave(time * 3.2f - index * 0.5f), 3f),
                SectionStyle.Warning => WarningPulse(time - index * 0.06f),
                SectionStyle.Features => Wave(time * 1.7f - index * 0.55f),
                _ => Wave(time * 0.95f - index * 0.4f)
            };
            bool blessing = style == SectionStyle.Farewell && index == count - 1;
            float radius = (1.1f + signal * 0.45f) * line.BaseScale.Y;
            Color glow = theme * (0.055f + signal * (blessing ? 0.075f : 0.045f));
            glow.A = 0;
            Vector2 position = new(line.X, line.Y);

            // Stable letter positions and the original hue keep the long text readable.
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * radius;
                DrawText(line, row.Text, position + offset, glow);
            }
            DrawText(line, row.Text, position + new Vector2(1f, 1f) * line.BaseScale, new Color(0, 0, 0, 235));
            Color ink = Color.Lerp(theme, Color.White, signal * (blessing ? 0.16f : 0.09f));
            DrawText(line, row.Text, position, ink);
        }

        private static void DrawText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, position,
                color, line.Rotation, line.Origin, line.BaseScale);
        }

        private static float Fraction(float value) => value - MathF.Floor(value);
        private static float Wave(float phase) => (MathF.Sin(phase) + 1f) * 0.5f;

        private static float WarningPulse(float time)
        {
            float phase = Fraction(time / 3.4f);
            float first = Math.Max(0f, 1f - Math.Abs(phase - 0.18f) / 0.13f);
            float second = Math.Max(0f, 1f - Math.Abs(phase - 0.45f) / 0.13f);
            return MathHelper.SmoothStep(0f, 1f, Math.Max(first, second * 0.75f));
        }

        private static void Spark(Vector2 point, Color theme, float strength, float scale)
        {
            Stroke(point - Vector2.UnitX * 2f * scale, point + Vector2.UnitX * 2f * scale, theme * strength, scale);
            Stroke(point - Vector2.UnitY * 2f * scale, point + Vector2.UnitY * 2f * scale, theme * strength, scale);
        }

        private static void Stroke(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            // Length and width are in pixels, not multiples of the full texture size.
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color,
                delta.ToRotation(), new Vector2(0f, 0.5f), new Vector2(delta.Length(), Math.Max(1f, width)),
                SpriteEffects.None, 0f);
        }

        private static void FillClipped(Rectangle clip, Rectangle rectangle, Color color) => Fill(Rectangle.Intersect(clip, rectangle), color);

        private static void Fill(Rectangle rectangle, Color color)
        {
            if (rectangle.Width > 0 && rectangle.Height > 0)
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }
    }
}
