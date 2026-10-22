using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.LegendaryTooltipEffects
{
    internal sealed class AntiMaterielRifleArmorBreakLegendaryTooltip : GlobalItem
    {
        internal const string TooltipLineName = "AMRLegendaryText";

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<NewLegendAntiMaterielRifle>() ||
                line.Mod != Mod.Name ||
                line.Name != TooltipLineName)
                return true;

            DrawArmorBreakText(line);
            return false;
        }

        private static void DrawArmorBreakText(DrawableTooltipLine line)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float width = line.Font.MeasureString(line.Text).X * line.BaseScale.X;
            int height = (int)Math.Ceiling(line.Font.LineSpacing * line.BaseScale.Y) + 10;
            Rectangle panel = new(line.X - 8, line.Y - 5, (int)Math.Ceiling(width) + 16, height);

            DrawRectangle(panel, new Color(9, 16, 23, 210));
            DrawRectangle(new Rectangle(panel.X, panel.Y, panel.Width, 1), new Color(161, 184, 201, 145));
            DrawRectangle(new Rectangle(panel.X, panel.Bottom - 1, panel.Width, 1), new Color(67, 89, 108, 155));

            for (int shard = 0; shard < 6; shard++)
            {
                int x = panel.X + 12 + (shard * 73) % Math.Max(1, panel.Width - 24);
                int y = panel.Y + 3 + shard % 3 * 3;
                DrawRectangle(new Rectangle(x, y, 1, Math.Max(2, panel.Height - 8 - shard % 3 * 3)),
                    new Color(135, 167, 187, 34));
                DrawRectangle(new Rectangle(x - 3, y + 4, 4, 1), new Color(185, 212, 229, 54));
            }

            float sweep = (time * 80f) % (width + 48f) - 24f;
            float advance = 0f;
            for (int index = 0; index < line.Text.Length; index++)
            {
                string glyph = line.Text[index].ToString();
                float glyphWidth = line.Font.MeasureString(glyph).X * line.BaseScale.X;
                Vector2 position = new(line.X + advance, line.Y);
                float impact = MathHelper.Clamp(1f - Math.Abs(sweep - advance - glyphWidth * 0.5f) / 24f, 0f, 1f);

                DrawText(line, glyph, position + new Vector2(1f, 2f), new Color(0, 0, 0, 225));
                DrawText(line, glyph, position - Vector2.UnitX, new Color(90, 164, 198, 0) * (0.18f + impact * 0.55f));
                DrawText(line, glyph, position + Vector2.UnitX, new Color(90, 164, 198, 0) * (0.18f + impact * 0.55f));
                DrawText(line, glyph, position,
                    Color.Lerp(new Color(160, 199, 219), new Color(255, 236, 174), impact));
                advance += glyphWidth;
            }

            int strikeX = line.X + (int)sweep;
            if (strikeX >= panel.X + 3 && strikeX < panel.Right - 3)
                DrawRectangle(new Rectangle(strikeX, panel.Y + 2, 1, panel.Height - 4),
                    new Color(255, 237, 179, 105));
        }

        private static void DrawText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, position, color,
                line.Rotation, line.Origin, line.BaseScale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }
    }
}
