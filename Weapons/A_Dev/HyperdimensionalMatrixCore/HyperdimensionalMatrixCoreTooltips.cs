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
            string text = ExtractTooltipTextAndColor(line.Text, lineIndex, out Color dataColor);

            switch (lineIndex)
            {
                case 0:
                    DrawFlowingText(line, text, position, dataColor);
                    break;
                case 1:
                    DrawInvertedText(line, text, position, dataColor, sine);
                    break;
                case 2:
                    DrawDoubleOutlineText(line, text, position, dataColor, sine);
                    break;
                case 3:
                    DrawShakyText(line, text, position, dataColor, sine);
                    break;
                case 4:
                    DrawDarkHorizonText(line, text, position, dataColor);
                    break;
                case 5:
                    DrawGlitchText(line, text, position, dataColor, sine);
                    break;
            }

            return false;
        }

        private static string ExtractTooltipTextAndColor(string text, int lineIndex, out Color color)
        {
            color = GetFallbackColor(lineIndex);

            if (!text.StartsWith("[c/", StringComparison.Ordinal))
                return text;

            int colorStart = 3;
            int separator = text.IndexOf(':', colorStart);
            if (separator < 0 || separator + 1 >= text.Length)
                return text;

            ReadOnlySpan<char> hex = text.AsSpan(colorStart, separator - colorStart);
            if (hex.Length is 6 or 8 && TryParseHexColor(hex, out Color parsed))
                color = parsed;

            int closingBracket = text.EndsWith("]", StringComparison.Ordinal) ? text.Length - 1 : text.Length;
            return text[(separator + 1)..closingBracket];
        }

        private static Color GetFallbackColor(int lineIndex) => lineIndex switch
        {
            0 => new Color(126, 184, 255),
            1 => new Color(255, 144, 64),
            2 => new Color(255, 236, 64),
            3 => new Color(88, 128, 192),
            4 => new Color(255, 48, 48),
            _ => new Color(120, 120, 136)
        };

        private static bool TryParseHexColor(ReadOnlySpan<char> hex, out Color color)
        {
            color = Color.White;
            if (!TryParseHexByte(hex[0..2], out byte r) ||
                !TryParseHexByte(hex[2..4], out byte g) ||
                !TryParseHexByte(hex[4..6], out byte b))
            {
                return false;
            }

            color = new Color(r, g, b);
            return true;
        }

        private static bool TryParseHexByte(ReadOnlySpan<char> hex, out byte value)
        {
            value = 0;
            if (hex.Length != 2)
                return false;

            int high = HexValue(hex[0]);
            int low = HexValue(hex[1]);
            if (high < 0 || low < 0)
                return false;

            value = (byte)((high << 4) | low);
            return true;
        }

        private static int HexValue(char c)
        {
            if (c is >= '0' and <= '9')
                return c - '0';
            if (c is >= 'a' and <= 'f')
                return c - 'a' + 10;
            if (c is >= 'A' and <= 'F')
                return c - 'A' + 10;
            return -1;
        }

        private static void DrawFlowingText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedStringWithShadow(
                Main.spriteBatch,
                line.Font,
                text,
                position,
                color,
                line.Rotation,
                line.Origin,
                line.BaseScale,
                line.MaxWidth,
                line.Spread);
        }

        private static void DrawInvertedText(DrawableTooltipLine line, string text, Vector2 position, Color color, float sine)
        {
            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 outlinePosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * (1.5f + 0.2f * sine);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    text,
                    outlinePosition,
                    color with { A = 0 },
                    line.Rotation,
                    line.Origin,
                    line.BaseScale,
                    line.MaxWidth,
                    line.Spread);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position,
                Color.Lerp(Color.Black, color, 0.35f),
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawDoubleOutlineText(DrawableTooltipLine line, string text, Vector2 position, Color color, float sine)
        {
            for (int i = 0; i < OutlineDrawCount; i++)
            {
                Vector2 outerPosition = position +
                    (MathHelper.TwoPi * i / OutlineDrawCount).ToRotationVector2() * (4.5f + 0.2f * sine);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    text,
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
                    text,
                    innerPosition,
                    Color.Black,
                    line.Rotation,
                    line.Origin,
                    line.BaseScale);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position,
                Color.White,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawShakyText(DrawableTooltipLine line, string text, Vector2 position, Color color, float sine)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 shakePosition = position + Main.rand.NextVector2Circular(4.5f, 3f);
                ChatManager.DrawColorCodedStringWithShadow(
                    Main.spriteBatch,
                    line.Font,
                    text,
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
                text,
                position,
                foreground,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawDarkHorizonText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/Light").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 horizonCenter = position + new Vector2(text.Length * 4f, 10f);

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
                    text,
                    outlinePosition,
                    color with { A = 0 } * 0.6f,
                    line.Rotation,
                    line.Origin,
                    line.BaseScale);
            }

            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position,
                Color.Black,
                line.Rotation,
                line.Origin,
                line.BaseScale);
        }

        private static void DrawGlitchText(DrawableTooltipLine line, string text, Vector2 position, Color color, float sine)
        {
            Vector2 horizontalGlitch = Vector2.UnitX * (2.5f + Math.Abs(sine) * 2f);
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position - horizontalGlitch,
                Color.Magenta with { A = 0 },
                line.Rotation,
                line.Origin,
                line.BaseScale);
            ChatManager.DrawColorCodedString(
                Main.spriteBatch,
                line.Font,
                text,
                position + horizontalGlitch,
                Color.Cyan with { A = 0 },
                line.Rotation,
                line.Origin,
                line.BaseScale);
            ChatManager.DrawColorCodedStringWithShadow(
                Main.spriteBatch,
                line.Font,
                text,
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
