using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DesertEagleItem = CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.DesertEagle;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    internal sealed class DesertEagleSilverMarksmanTooltip : GlobalItem
    {
        private const string LineName = "DesertEagleSilverMarksman";
        private const string Title = "传奇神枪手同款";

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<DesertEagleItem>())
                tooltips.Add(new TooltipLine(Mod, LineName, Title));
        }

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<DesertEagleItem>() ||
                line.Mod != Mod.Name || line.Name != LineName)
                return true;

            DrawSilverTitle(line);
            return false;
        }

        private static void DrawSilverTitle(DrawableTooltipLine line)
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 origin = new(line.X, line.Y);
            float advance = 0f;
            float sweep = (time * 33f) % (line.Font.MeasureString(line.Text).X * line.BaseScale.X + 34f) - 17f;

            for (int index = 0; index < line.Text.Length; index++)
            {
                string glyph = line.Text[index].ToString();
                float width = line.Font.MeasureString(glyph).X * line.BaseScale.X;
                Vector2 position = origin + Vector2.UnitX * advance;
                float grain = 0.5f + 0.5f * MathF.Sin(time * 1.9f + index * 1.3f);
                float reflection = MathHelper.Clamp(1f - Math.Abs(sweep - advance - width * 0.5f) / 16f, 0f, 1f);

                // Dark engraved edge, a soft metallic halo, and a moving white reflection.
                DrawText(line, glyph, position + new Vector2(1f, 2f), new Color(19, 23, 31, 230));
                Color halo = new Color(191, 210, 225, 0) * (0.12f + reflection * 0.22f);
                DrawText(line, glyph, position + Vector2.UnitX * -1f, halo);
                DrawText(line, glyph, position + Vector2.UnitX, halo);
                DrawText(line, glyph, position - Vector2.UnitY, halo);

                Color steel = Color.Lerp(new Color(121, 133, 148), new Color(215, 223, 231),
                    0.25f + grain * 0.5f);
                steel = Color.Lerp(steel, new Color(250, 253, 255), reflection * 0.85f);
                DrawText(line, glyph, position, steel);
                advance += width;
            }
        }

        private static void DrawText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, position, color,
                line.Rotation, line.Origin, line.BaseScale);
        }
    }
}
