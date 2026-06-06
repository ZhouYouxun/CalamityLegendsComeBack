using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    public sealed class HyperdimensionalMatrixCoreTooltipGlobalItem : GlobalItem
    {
        private const int OutlineDrawCount = 20;

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (item.type != ModContent.ItemType<HyperdimensionalMatrixCore>() ||
                line.Mod != "Terraria" ||
                !line.Name.StartsWith("Tooltip", StringComparison.Ordinal) ||
                !int.TryParse(line.Name.AsSpan("Tooltip".Length), out int lineIndex) ||
                lineIndex is < 0 or > 5)
            {
                return true;
            }

            Vector2 position = new(line.X, line.Y);
            float sine = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f / MathHelper.Pi);
            Color dataColor = GetDataColor(lineIndex * 0.15f);

            switch (lineIndex)
            {
                case 0:
                    DrawFlowingText(line, position, dataColor);
                    break;
                case 1:
                    DrawInvertedText(line, position, sine);
                    break;
                case 2:
                    DrawDoubleOutlineText(line, position, dataColor, sine);
                    break;
                case 3:
                    DrawShakyText(line, position, dataColor, sine);
                    break;
                case 4:
                    DrawDarkHorizonText(line, position, dataColor);
                    break;
                case 5:
                    DrawGlitchText(line, position, dataColor, sine);
                    break;
            }

            return false;
        }

        private static Color GetDataColor(float offset)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.24f + offset) % 1f;
            return Main.hslToRgb(hue, 0.95f, 0.7f);
        }

        private static void DrawFlowingText(DrawableTooltipLine line, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedStringWithShadow(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                color,
                line.Rotation,
                line.Origin,
                line.BaseScale,
                line.MaxWidth,
                line.Spread);
        }

        private static void DrawInvertedText(DrawableTooltipLine line, Vector2 position, float sine)
        {
            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 outlinePosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * (1.5f + 0.2f * sine);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    line.Text,
                    outlinePosition,
                    Color.White with { A = 0 },
                    line.Rotation,
                    line.Origin,
                    line.BaseScale,
                    line.MaxWidth,
                    line.Spread);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                Color.Black,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawDoubleOutlineText(DrawableTooltipLine line, Vector2 position, Color color, float sine)
        {
            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 outerPosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * (4.5f + 0.2f * sine);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    line.Text,
                    outerPosition,
                    color with { A = 0 },
                    line.Rotation,
                    line.Origin,
                    line.BaseScale,
                    line.MaxWidth,
                    line.Spread);
            }

            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 innerPosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * (2.5f + 0.2f * sine);
                ChatManager.DrawColorCodedString(
                    Main.spriteBatch,
                    line.Font,
                    line.Text,
                    innerPosition,
                    Color.Black,
                    line.Rotation,
                    line.Origin,
                    line.BaseScale);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                Color.White,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawShakyText(DrawableTooltipLine line, Vector2 position, Color color, float sine)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 shakePosition = position + Main.rand.NextVector2Circular(4.5f, 3f);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    line.Text,
                    shakePosition,
                    Color.Black,
                    line.Rotation,
                    line.Origin,
                    line.BaseScale,
                    line.MaxWidth,
                    line.Spread);
            }

            Color foreground = Color.Lerp(Color.White, color, 0.5f + sine * 0.35f);
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                foreground,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawDarkHorizonText(DrawableTooltipLine line, Vector2 position, Color color)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/Light").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 horizonCenter = position + new Vector2(line.Text.Length * 4f, 10f);

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = Vector2.UnitX * (i % 2 == 0 ? -7f * i : 7f * i);
                Main.EntitySpriteDraw(
                    texture,
                    horizonCenter + offset,
                    null,
                    color with { A = 0 },
                    MathHelper.PiOver2,
                    origin,
                    new Vector2(0.9f - 0.085f * i, 1f + 2.7f * i) * 0.7f,
                    SpriteEffects.None);
                Main.EntitySpriteDraw(
                    texture,
                    horizonCenter + offset,
                    null,
                    Color.Black,
                    MathHelper.PiOver2,
                    origin,
                    new Vector2(0.9f - 0.05f * i, 1f + 4.5f * i) * 0.55f,
                    SpriteEffects.None);
            }

            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 outlinePosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * 1.5f;
                ChatManager.DrawColorCodedString(
                    Main.spriteBatch,
                    line.Font,
                    line.Text,
                    outlinePosition,
                    color with { A = 0 } * 0.6f,
                    line.Rotation,
                    line.Origin,
                    line.BaseScale);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                Color.Black,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawGlitchText(DrawableTooltipLine line, Vector2 position, Color color, float sine)
        {
            Vector2 horizontalGlitch = Vector2.UnitX * (2.5f + Math.Abs(sine) * 2f);
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position - horizontalGlitch,
                Color.Magenta with { A = 0 },
                line.Rotation,
                line.Origin,
                line.BaseScale);
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position + horizontalGlitch,
                Color.Cyan with { A = 0 },
                line.Rotation,
                line.Origin,
                line.BaseScale);
            ChatManager.DrawColorCodedStringWithShadow(
                Main.spriteBatch,
                line.Font,
                line.Text,
                position,
                Color.Lerp(Color.White, color, 0.45f),
                line.Rotation,
                line.Origin,
                line.BaseScale,
                line.MaxWidth,
                line.Spread);
        }
    }
}
