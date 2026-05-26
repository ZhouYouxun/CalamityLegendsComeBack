using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal static class BFChargeVisuals
    {
        internal static void DrawAirFlow(
            Vector2 gunTipPosition,
            Vector2 aimDirection,
            float projectileScale,
            BlossomFluxChloroplastPresetType preset,
            float chargeCompletion,
            bool chargeReady,
            int identity)
        {
            if (Main.dedServ || chargeCompletion <= 0.02f)
                return;

            aimDirection = aimDirection.SafeNormalize(Vector2.UnitX);
            Vector2 normal = aimDirection.RotatedBy(MathHelper.PiOver2);
            float charge = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(chargeCompletion, 0f, 1f));
            Color mainColor = BFArrowCommon.GetPresetColor(preset) with { A = 0 };
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset) with { A = 0 };
            Texture2D smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D halfStarTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D halfIceStarTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfIceStar").Value;
            Texture2D fullStarTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/FullStar").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawSmearField(smearTexture, gunTipPosition, aimDirection, normal, projectileScale, mainColor, accentColor, charge, chargeReady, identity);
            DrawCompressedAirFlecks(halfStarTexture, halfIceStarTexture, fullStarTexture, gunTipPosition, aimDirection, normal, projectileScale, mainColor, accentColor, charge, identity);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawSmearField(
            Texture2D smearTexture,
            Vector2 gunTipPosition,
            Vector2 aimDirection,
            Vector2 normal,
            float projectileScale,
            Color mainColor,
            Color accentColor,
            float charge,
            bool chargeReady,
            int identity)
        {
            int streamCount = 5 + (int)MathF.Round(charge * 7f) + (chargeReady ? 2 : 0);
            float time = Main.GlobalTimeWrappedHourly;
            int frameSeed = (int)(Main.GameUpdateCount / (ulong)Math.Max(2, 5 - (int)MathF.Round(charge * 3f)));
            float vacuumGap = MathHelper.Lerp(1.5f, 9.5f, charge);
            float angleVariance = MathHelper.Lerp(MathHelper.ToRadians(15f), MathHelper.ToRadians(5f), charge);
            float forwardRange = MathHelper.Lerp(24f, 48f, charge);
            float backSweep = MathHelper.Lerp(12f, 36f, charge);

            for (int i = 0; i < streamCount; i++)
            {
                int seed = identity * 397 + i * 71 + frameSeed * 173;
                float lane = i - (streamCount - 1) * 0.5f;
                float sideSign = lane == 0f ? (HashSigned(seed) >= 0f ? 1f : -1f) : MathF.Sign(lane);
                float sideDistance = sideSign * (vacuumGap + MathF.Abs(lane) * MathHelper.Lerp(1.2f, 2.3f, charge) + Hash01(seed + 1) * MathHelper.Lerp(3f, 12f, charge));
                float flowProgress = Frac(Hash01(seed + 2) + time * MathHelper.Lerp(1.6f, 4.7f, charge) + i * 0.113f);
                float ahead = MathHelper.Lerp(0f, forwardRange, flowProgress);
                float curveAngle = HashSigned(seed + 3) * angleVariance;
                Vector2 flowDirection = (-aimDirection).RotatedBy(curveAngle);
                Vector2 curveOffset = normal * MathF.Sin(time * MathHelper.Lerp(4.2f, 7.6f, charge) + i * 1.37f) * MathHelper.Lerp(0.6f, 2.8f, charge);
                Vector2 drawWorld = gunTipPosition + aimDirection * ahead + normal * sideDistance + flowDirection * (backSweep * (0.18f + 0.28f * Hash01(seed + 4))) + curveOffset;
                float opacity = MathHelper.Lerp(0.28f, 0.84f, charge) * (0.58f + 0.42f * Hash01(seed + 5));
                Color drawColor = Color.Lerp(mainColor, accentColor, Hash01(seed + 6)) * opacity;
                float lengthScale = MathHelper.Lerp(0.24f, 0.92f, charge) * (0.72f + Hash01(seed + 7) * 0.7f);
                Vector2 scale = new(
                    MathHelper.Lerp(0.014f, 0.034f, charge) * projectileScale,
                    lengthScale * projectileScale);

                Main.EntitySpriteDraw(
                    smearTexture,
                    drawWorld - Main.screenPosition,
                    null,
                    drawColor,
                    flowDirection.ToRotation() - MathHelper.PiOver2,
                    new Vector2(smearTexture.Width * 0.5f, smearTexture.Height),
                    scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private static void DrawCompressedAirFlecks(
            Texture2D halfStarTexture,
            Texture2D halfIceStarTexture,
            Texture2D fullStarTexture,
            Vector2 gunTipPosition,
            Vector2 aimDirection,
            Vector2 normal,
            float projectileScale,
            Color mainColor,
            Color accentColor,
            float charge,
            int identity)
        {
            int fleckCount = 2 + (int)MathF.Round(charge * 4f);
            float time = Main.GlobalTimeWrappedHourly;
            float vacuumGap = MathHelper.Lerp(3f, 12f, charge);

            for (int i = 0; i < fleckCount; i++)
            {
                int seed = identity * 613 + i * 97;
                float sideSign = HashSigned(seed) >= 0f ? 1f : -1f;
                float travel = Frac(Hash01(seed + 1) + time * MathHelper.Lerp(1.2f, 3.1f, charge));
                Vector2 position =
                    gunTipPosition +
                    aimDirection * MathHelper.Lerp(48f, -10f, travel) +
                    normal * sideSign * (vacuumGap + Hash01(seed + 2) * MathHelper.Lerp(5f, 18f, charge)) +
                    normal * MathF.Sin(time * 5.4f + i * 1.8f) * MathHelper.Lerp(0.8f, 2.2f, charge);

                Texture2D texture = i % 3 == 0 ? fullStarTexture : i % 3 == 1 ? halfStarTexture : halfIceStarTexture;
                Color color = Color.Lerp(accentColor, mainColor, Hash01(seed + 3)) * MathHelper.Lerp(0.22f, 0.58f, charge);
                float rotation = aimDirection.ToRotation() + MathHelper.PiOver2 + HashSigned(seed + 4) * MathHelper.Lerp(0.35f, 0.12f, charge);
                float scale = MathHelper.Lerp(0.035f, 0.085f, charge) * projectileScale * (0.8f + Hash01(seed + 5) * 0.45f);

                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition,
                    null,
                    color,
                    rotation,
                    texture.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private static float Hash01(int seed)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352dU;
                value ^= value >> 15;
                value *= 0x846ca68bU;
                value ^= value >> 16;
                return (value & 0x00FFFFFF) / 16777216f;
            }
        }

        private static float HashSigned(int seed) => Hash01(seed) * 2f - 1f;

        private static float Frac(float value) => value - MathF.Floor(value);
    }
}
