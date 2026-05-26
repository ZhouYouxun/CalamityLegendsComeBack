using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal static class BFChargeArrowVisuals
    {
        internal static void DrawHoldoutChargeBloom(
            Projectile projectile,
            BlossomFluxChloroplastPresetType preset,
            Vector2 gunTipPosition,
            Vector2 aimDirection,
            float chargeGlow)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color presetColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Color bloomColor = (Color.Lerp(presetColor, Color.White, 0.62f) with { A = 0 }) * (0.28f + chargeGlow * 0.62f);
            Color starColor = (Color.Lerp(accentColor, Color.White, 0.74f) with { A = 0 }) * (0.18f + chargeGlow * 0.72f);
            Vector2 bodyCenter = Vector2.Lerp(projectile.Center, gunTipPosition, 0.45f) - Main.screenPosition;
            Vector2 muzzleCenter = gunTipPosition + aimDirection * 3f - Main.screenPosition;
            float pulse = 0.82f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.5f + projectile.identity);

            Main.EntitySpriteDraw(
                bloomTexture,
                bodyCenter,
                null,
                bloomColor,
                projectile.rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.38f + chargeGlow * 0.38f, 0.16f + chargeGlow * 0.14f) * pulse,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                muzzleCenter,
                null,
                bloomColor * (0.72f + chargeGlow * 0.35f),
                projectile.rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.28f + chargeGlow * 0.34f, 0.12f + chargeGlow * 0.18f) * pulse,
                SpriteEffects.None,
                0);

            if (chargeGlow <= 0.03f || preset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
                return;

            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            for (int i = 0; i < 4; i++)
            {
                float rotation = projectile.rotation + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (1.2f + i * 0.2f);
                Main.EntitySpriteDraw(
                    starTexture,
                    muzzleCenter,
                    null,
                    starColor,
                    rotation,
                    starTexture.Size() * 0.5f,
                    new Vector2(0.16f + chargeGlow * 0.18f, 0.85f + chargeGlow * 1.25f) * pulse,
                    SpriteEffects.None,
                    0);
            }
        }

        internal static void DrawRecoveryChargeCore(Projectile projectile, Vector2 gunTipPosition, float chargeCompletion)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Vector2.Lerp(projectile.Center, gunTipPosition, 0.36f) - Main.screenPosition;
            float charge = MathHelper.SmoothStep(0f, 1f, chargeCompletion);
            float pulse = 0.84f + 0.16f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8.5f + projectile.identity * 0.3f);
            const float coreDrawScale = 0.15f;
            Color green = new(98, 255, 142, 210);
            Color pale = new(222, 255, 232, 235);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloomTexture,
                center,
                null,
                green * (0.4f + charge * 0.46f),
                0f,
                bloomTexture.Size() * 0.5f,
                (0.36f + charge * 0.42f) * pulse * coreDrawScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloomTexture,
                center,
                null,
                pale * (0.18f + charge * 0.28f),
                0f,
                bloomTexture.Size() * 0.5f,
                (0.16f + charge * 0.18f) * pulse * coreDrawScale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        internal static void DrawSpecialChargeArrow(
            Projectile projectile,
            Texture2D arrowTexture,
            BlossomFluxChloroplastPresetType preset,
            Vector2 aimDirection,
            float chargeCompletion,
            bool readyBurstPlayed)
        {
            Vector2 chargeArrowOffset = aimDirection * MathHelper.Lerp(20f, 24f, chargeCompletion) + new Vector2(0f, MathHelper.Lerp(-5f, -2f, chargeCompletion));
            Vector2 arrowDrawPosition = projectile.Center + chargeArrowOffset - Main.screenPosition;
            float pulse = readyBurstPlayed ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.05f : 0f;
            float arrowScale = 0.9f + chargeCompletion * 0.2f + pulse;
            Color arrowColor = Color.Lerp(Color.White, BFArrowCommon.GetPresetColor(preset), 0.45f + 0.25f * chargeCompletion);
            float arrowRotation = projectile.rotation + MathHelper.PiOver2 + MathHelper.Pi;

            Main.EntitySpriteDraw(
                arrowTexture,
                arrowDrawPosition,
                null,
                arrowColor,
                arrowRotation,
                arrowTexture.Size() * 0.5f,
                arrowScale,
                SpriteEffects.None,
                0);

            DrawSpecialChargeArrowOverlay(projectile, arrowTexture, preset, aimDirection, chargeCompletion, arrowDrawPosition, arrowRotation, arrowScale);
        }

        internal static void DrawBreakthroughChargedArrows(
            Texture2D arrowTexture,
            Vector2 holdoutCenter,
            Vector2 aimDirection,
            int maxArrows,
            int loadedArrowCount,
            float currentArrowCompletion,
            int loadFlashTimer,
            int loadFlashFrames)
        {
            maxArrows = Math.Max(1, maxArrows);
            int loadedArrows = Utils.Clamp(loadedArrowCount, 0, maxArrows);
            bool fullyLoaded = loadedArrows >= maxArrows;
            int drawCount = fullyLoaded ? loadedArrows : Math.Min(loadedArrows + 1, maxArrows);
            if (drawCount <= 0)
                return;

            Color loadedColor = Color.Lerp(Color.White, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), 0.62f);
            Color loadingColor = Color.Lerp(Color.White, loadedColor, currentArrowCompletion);
            Color outlineColor = new(116, 255, 134, 0);
            Vector2 origin = arrowTexture.Size() * 0.5f;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;

            for (int i = 0; i < drawCount; i++)
            {
                bool loadingArrow = !fullyLoaded && i == loadedArrows;
                float visibility = loadingArrow ? currentArrowCompletion : 1f;
                if (visibility <= 0.02f)
                    continue;

                float drawDistance = MathHelper.Lerp(22f, 32f, visibility);
                Vector2 drawWorld = holdoutCenter + aimDirection * drawDistance;
                float pulse = fullyLoaded ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i * 0.75f) * 0.04f : 0f;
                float arrowScale = MathHelper.Lerp(0.82f, 1.05f, visibility) + pulse;
                Color arrowColor = (loadingArrow ? loadingColor : loadedColor) * visibility;
                float rotation = aimDirection.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

                DrawBreakthroughArrowHelix(bloomTexture, sparkTexture, drawWorld, aimDirection, visibility, i, drawCount);

                bool flashArrow = !loadingArrow && loadFlashTimer > 0 && i == loadedArrows - 1;
                if (flashArrow)
                {
                    float flash = loadFlashTimer / (float)loadFlashFrames;
                    for (int j = 0; j < 10; j++)
                    {
                        Vector2 offset = (MathHelper.TwoPi * j / 10f).ToRotationVector2() * MathHelper.Lerp(1.4f, 3.2f, flash);
                        Main.EntitySpriteDraw(
                            arrowTexture,
                            drawWorld - Main.screenPosition + offset,
                            null,
                            outlineColor * (0.55f * flash),
                            rotation,
                            origin,
                            arrowScale,
                            SpriteEffects.None,
                            0);
                    }
                }

                Main.EntitySpriteDraw(
                    arrowTexture,
                    drawWorld - Main.screenPosition,
                    null,
                    arrowColor,
                    rotation,
                    origin,
                    arrowScale,
                    SpriteEffects.None,
                    0);
            }
        }

        private static void DrawSpecialChargeArrowOverlay(
            Projectile projectile,
            Texture2D arrowTexture,
            BlossomFluxChloroplastPresetType preset,
            Vector2 aimDirection,
            float chargeCompletion,
            Vector2 arrowDrawPosition,
            float arrowRotation,
            float arrowScale)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    DrawReconChargeOverlay(projectile, aimDirection, chargeCompletion, arrowDrawPosition, arrowRotation, arrowScale);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    DrawBombardChargeOverlay(arrowTexture, aimDirection, chargeCompletion, arrowDrawPosition, arrowRotation, arrowScale);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    DrawPlagueChargeOverlay(projectile, arrowTexture, aimDirection, chargeCompletion, arrowDrawPosition, arrowRotation, arrowScale);
                    break;
            }
        }

        private static void DrawReconChargeOverlay(Projectile projectile, Vector2 aimDirection, float chargeCompletion, Vector2 arrowDrawPosition, float arrowRotation, float arrowScale)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            float charge = MathHelper.SmoothStep(0f, 1f, chargeCompletion);
            float scanPulse = 0.72f + 0.28f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 11f + projectile.identity);
            Color blue = new(96, 232, 255, 0);
            Color violet = new(114, 112, 255, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                ring,
                arrowDrawPosition,
                null,
                blue * (0.22f + 0.24f * charge),
                arrowRotation + Main.GlobalTimeWrappedHourly * 1.5f,
                ring.Size() * 0.5f,
                new Vector2(0.105f, 0.07f) * (0.9f + charge * 0.3f) * scanPulse,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 4; i++)
            {
                float angle = aimDirection.ToRotation() + MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 1.8f;
                Vector2 offset = angle.ToRotationVector2() * MathHelper.Lerp(9f, 17f, charge);
                Main.EntitySpriteDraw(
                    spark,
                    arrowDrawPosition + offset,
                    null,
                    Color.Lerp(blue, violet, i / 3f) * (0.28f + charge * 0.3f),
                    angle + MathHelper.PiOver2,
                    spark.Size() * 0.5f,
                    new Vector2(0.025f, 0.12f + charge * 0.08f) * arrowScale,
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawBombardChargeOverlay(Texture2D arrowTexture, Vector2 aimDirection, float chargeCompletion, Vector2 arrowDrawPosition, float arrowRotation, float arrowScale)
        {
            Texture2D streak = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 normal = aimDirection.RotatedBy(MathHelper.PiOver2);
            float charge = MathHelper.SmoothStep(0f, 1f, chargeCompletion);
            Color red = new(255, 54, 42, 0);
            Color gold = new(255, 194, 72, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 offset = normal * i * (5f + 5f * charge);
                Main.EntitySpriteDraw(
                    arrowTexture,
                    arrowDrawPosition + offset,
                    null,
                    Color.Lerp(red, gold, i == 0 ? 0.4f : 0.18f) * (0.18f + charge * 0.18f),
                    arrowRotation,
                    arrowTexture.Size() * 0.5f,
                    arrowScale * (1.04f + charge * 0.08f),
                    SpriteEffects.None,
                    0f);

                Main.EntitySpriteDraw(
                    streak,
                    arrowDrawPosition + offset - aimDirection * 12f,
                    null,
                    Color.Lerp(gold, red, 0.42f) * (0.22f + charge * 0.2f),
                    arrowRotation,
                    streak.Size() * 0.5f,
                    new Vector2(0.18f, 0.42f + charge * 0.18f),
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawPlagueChargeOverlay(Projectile projectile, Texture2D arrowTexture, Vector2 aimDirection, float chargeCompletion, Vector2 arrowDrawPosition, float arrowRotation, float arrowScale)
        {
            Texture2D fog = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/BlightFlames").Value;
            Texture2D noise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/BlobbyNoise").Value;
            float charge = MathHelper.SmoothStep(0f, 1f, chargeCompletion);
            float pulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6.4f + projectile.identity * 0.2f);
            Color acid = new(188, 255, 62, 0);
            Color plague = new(74, 205, 54, 0);
            Color dark = new(20, 72, 28, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                fog,
                arrowDrawPosition,
                null,
                Color.Lerp(dark, plague, 0.55f) * (0.2f + charge * 0.22f),
                -projectile.rotation * 0.72f,
                fog.Size() * 0.5f,
                (0.22f + charge * 0.12f) * pulse,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                arrowTexture,
                arrowDrawPosition,
                null,
                acid * (0.22f + charge * 0.2f),
                arrowRotation,
                arrowTexture.Size() * 0.5f,
                arrowScale * (1.08f + charge * 0.08f),
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                noise,
                arrowDrawPosition + aimDirection * 1.5f,
                null,
                Color.Lerp(plague, acid, 0.35f) * (0.12f + charge * 0.18f),
                projectile.rotation + Main.GlobalTimeWrappedHourly * 0.85f,
                noise.Size() * 0.5f,
                new Vector2(0.22f, 0.08f) * (1f + charge * 0.5f),
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawBreakthroughArrowHelix(Texture2D bloomTexture, Texture2D sparkTexture, Vector2 drawWorld, Vector2 arrowDirection, float visibility, int slotIndex, int drawCount)
        {
            if (visibility <= 0.02f)
                return;

            Vector2 drawPosition = drawWorld - Main.screenPosition;
            Vector2 normal = arrowDirection.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly * 7.8f + slotIndex * 0.82f;
            float stackedOpacity = MathHelper.Clamp(0.62f / MathF.Sqrt(Math.Max(1, drawCount)), 0.28f, 0.62f);
            Color violet = new(128, 72, 255, 0);
            Color magenta = new(255, 74, 216, 0);
            Color leaf = new(112, 255, 134, 0);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition + arrowDirection * 10f,
                null,
                Color.Lerp(leaf, Color.White, 0.22f) * (0.23f * visibility),
                0f,
                bloomTexture.Size() * 0.5f,
                0.055f + 0.015f * visibility,
                SpriteEffects.None,
                0);

            for (int strand = 0; strand < 2; strand++)
            {
                float strandPhase = strand * MathHelper.Pi;
                for (int segment = 0; segment < 7; segment++)
                {
                    float completion = segment / 6f;
                    float along = MathHelper.Lerp(-30f, 20f, completion);
                    float phase = time + strandPhase + completion * MathHelper.TwoPi * 1.38f;
                    float width = MathHelper.Lerp(9.2f, 2.6f, completion);
                    float side = MathF.Sin(phase);
                    float facing = 0.58f + 0.42f * Utils.GetLerpValue(-0.35f, 1f, MathF.Cos(phase), true);
                    Vector2 position = drawPosition + arrowDirection * along + normal * side * width;
                    Color color = Color.Lerp(violet, magenta, completion);
                    color = Color.Lerp(color, leaf, 0.24f + 0.18f * MathF.Sin(time + segment));
                    float scaleX = MathHelper.Lerp(0.026f, 0.052f, facing) * visibility;
                    float scaleY = MathHelper.Lerp(0.11f, 0.23f, 1f - completion) * visibility;

                    Main.EntitySpriteDraw(
                        sparkTexture,
                        position,
                        null,
                        color * (stackedOpacity * facing * visibility),
                        arrowDirection.ToRotation() + MathHelper.PiOver2,
                        sparkTexture.Size() * 0.5f,
                        new Vector2(scaleX, scaleY),
                        SpriteEffects.None,
                        0);
                }
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
