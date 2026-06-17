using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.CoreOfCalamity
{
    internal static class MagicCorePalette
    {
        internal static readonly Color Cyan = new(72, 230, 255);
        internal static readonly Color Fuchsia = new(255, 48, 210);
        internal static readonly Color Twilight = new(126, 58, 255);

        internal static Color SpecialMoveColor(float offset = 0f)
        {
            float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 4.6f + offset) * 0.5f + 0.5f;
            Color color = Color.Lerp(Fuchsia, Cyan, 0.16f + pulse * 0.72f);
            color = Color.Lerp(Twilight, color, 0.76f);
            color.A = 0;
            return color;
        }

        internal static Color RiftColor(float offset = 0f)
        {
            Color color = SpecialMoveColor(offset);
            return Color.Lerp(color, Color.White, 0.12f);
        }
    }

    internal static class TheNewEnforcerMagicCoreDraw
    {
        internal static void Draw(Vector2 drawPosition, float rotation, float visualScale, float opacity, Color primaryColor, int identity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magicRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D runeStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_05").Value;
            Texture2D perfectGlow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;

            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.92f + 0.08f * MathF.Sin(time * 12f + identity * 0.41f);
            float spin = time * 1.7f + identity * 0.23f;
            Color primary = primaryColor with { A = 0 };
            Color cyan = Color.Lerp(primary, MagicCorePalette.Cyan, 0.52f) with { A = 0 };
            Color fuchsia = Color.Lerp(primary, MagicCorePalette.Fuchsia, 0.46f) with { A = 0 };
            Color white = Color.Lerp(primary, Color.White, 0.72f) with { A = 0 };

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                primary * 0.58f * opacity,
                0f,
                bloom.Size() * 0.5f,
                0.2f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                magicRing,
                drawPosition,
                null,
                cyan * 0.34f * opacity,
                rotation * 0.2f + spin,
                magicRing.Size() * 0.5f,
                0.058f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                fuchsia * 0.3f * opacity,
                -rotation * 0.16f - spin * 0.78f,
                reticle.Size() * 0.5f,
                0.052f * visualScale,
                SpriteEffects.FlipHorizontally,
                0f);

            Main.EntitySpriteDraw(
                runeStar,
                drawPosition,
                null,
                Color.Lerp(cyan, fuchsia, 0.5f) * 0.28f * opacity,
                rotation + spin * 0.34f,
                runeStar.Size() * 0.5f,
                0.052f * visualScale * pulse,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 3; i++)
            {
                float layerRotation = rotation + spin * (i % 2 == 0 ? 0.62f : -0.54f) + MathHelper.TwoPi * i / 3f;
                Color layerColor = i == 0 ? white : Color.Lerp(cyan, fuchsia, i * 0.5f);
                Main.EntitySpriteDraw(
                    perfectGlow,
                    drawPosition,
                    null,
                    layerColor * (0.5f - i * 0.09f) * opacity,
                    layerRotation,
                    perfectGlow.Size() * 0.5f,
                    (0.2f + i * 0.045f) * visualScale * pulse,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}
