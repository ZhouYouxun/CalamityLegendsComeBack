using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityLegendsComeBack.FancyIcon
{
    /// <summary>
    /// Builds five diagonal highlight textures from the icon itself. Each texture keeps the
    /// original alpha channel, so transparent icon pixels can never receive the shine.
    /// </summary>
    internal static class ShineEffectToolkit
    {
        private const int SliceCount = 5;
        private const int SliceStartIntervalFrames = 4;
        private const int SliceFlashFrames = 18;
        private const int CycleFrames = 120;
        private const float BrightnessMultiplier = 1.6f;
        private const float SoftEdge = 0.035f;

        // The pulse is a sine arch whose two halves are stretched outward from the peak: the rise
        // begins 200% earlier and the fall ends 1% later. Peaks still sweep across on the original
        // schedule, so the 34-frame highlight sweep itself is untouched - only its tails grow.
        private const float FadeInStretch = 3f;
        private const float FadeOutStretch = 1.01f;
        private const float HalfFlashFrames = SliceFlashFrames / 2f;
        private const float FadeInFrames = HalfFlashFrames * FadeInStretch;
        private const float FadeOutFrames = HalfFlashFrames * FadeOutStretch;

        private static Texture2D sourceTexture;
        private static Texture2D[] brightSliceTextures;

        public sealed class State
        {
            public int CycleFrame = CycleFrames - 1;
        }

        /// <summary>Call once per frame before Draw.</summary>
        public static void Advance(State state)
        {
            state.CycleFrame = (state.CycleFrame + 1) % CycleFrames;
        }

        /// <summary>
        /// Draws the active brightened slices over the original icon. The diagonal '/' bands use
        /// increasing x + y coordinates, so the scan begins at the upper-left and ends at the
        /// lower-right. Adjacent slices start four frames apart.
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, State state, Texture2D originalTexture, Rectangle destination)
        {
            if (originalTexture is null || state.CycleFrame >= ScanFrames)
                return;

            EnsureBrightSliceTextures(originalTexture);
            if (brightSliceTextures is null)
                return;

            for (int slice = 0; slice < SliceCount; slice++)
            {
                float pulse = GetSlicePulse(state.CycleFrame, slice);
                if (pulse > 0f)
                    spriteBatch.Draw(brightSliceTextures[slice], destination, new Color(255, 255, 255, (byte)(byte.MaxValue * pulse)));
            }
        }

        public static void Unload()
        {
            DisposeBrightSliceTextures();
            sourceTexture = null;
        }

        private static int ScanFrames => (int)MathF.Ceiling(FadeInFrames + SliceStartIntervalFrames * (SliceCount - 1) + FadeOutFrames);

        private static float GetSlicePulse(int cycleFrame, int slice)
        {
            // The first slice peaks once its stretched fade-in has fully played out, so no part of
            // the tail gets clipped against the start of the cycle.
            float peakFrame = FadeInFrames + slice * SliceStartIntervalFrames;
            float offsetFromPeak = cycleFrame + 0.5f - peakFrame;

            float progress = offsetFromPeak <= 0f
                ? 0.5f + 0.5f * (offsetFromPeak / FadeInFrames)
                : 0.5f + 0.5f * (offsetFromPeak / FadeOutFrames);

            if (progress <= 0f || progress >= 1f)
                return 0f;

            return MathF.Sin(progress * MathHelper.Pi);
        }

        private static void EnsureBrightSliceTextures(Texture2D originalTexture)
        {
            if (sourceTexture == originalTexture && brightSliceTextures is not null)
                return;

            DisposeBrightSliceTextures();
            sourceTexture = originalTexture;

            Color[] originalPixels = new Color[originalTexture.Width * originalTexture.Height];
            originalTexture.GetData(originalPixels);
            brightSliceTextures = new Texture2D[SliceCount];

            for (int slice = 0; slice < SliceCount; slice++)
            {
                float minDiagonal = slice * 2f / SliceCount;
                float maxDiagonal = (slice + 1) * 2f / SliceCount;
                Color[] brightPixels = new Color[originalPixels.Length];

                for (int y = 0; y < originalTexture.Height; y++)
                {
                    for (int x = 0; x < originalTexture.Width; x++)
                    {
                        int index = y * originalTexture.Width + x;
                        Color original = originalPixels[index];
                        float diagonal = (x + 0.5f) / originalTexture.Width + (y + 0.5f) / originalTexture.Height;
                        float maskAlpha = MathHelper.Clamp(Math.Min((diagonal - minDiagonal) / SoftEdge, (maxDiagonal - diagonal) / SoftEdge), 0f, 1f);
                        byte alpha = (byte)(original.A * maskAlpha);

                        brightPixels[index] = new Color(
                            IncreaseBrightness(original.R),
                            IncreaseBrightness(original.G),
                            IncreaseBrightness(original.B),
                            alpha);
                    }
                }

                Texture2D brightSlice = new(originalTexture.GraphicsDevice, originalTexture.Width, originalTexture.Height);
                brightSlice.SetData(brightPixels);
                brightSliceTextures[slice] = brightSlice;
            }
        }

        private static byte IncreaseBrightness(byte channel) => (byte)Math.Min(byte.MaxValue, channel * BrightnessMultiplier);

        private static void DisposeBrightSliceTextures()
        {
            if (brightSliceTextures is null)
                return;

            foreach (Texture2D texture in brightSliceTextures)
                texture?.Dispose();

            brightSliceTextures = null;
        }
    }
}
