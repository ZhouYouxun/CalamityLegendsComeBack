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

        private static int ScanFrames => SliceFlashFrames + SliceStartIntervalFrames * (SliceCount - 1);

        private static float GetSlicePulse(int cycleFrame, int slice)
        {
            int age = cycleFrame - slice * SliceStartIntervalFrames;
            if (age < 0 || age >= SliceFlashFrames)
                return 0f;

            float progress = (age + 0.5f) / SliceFlashFrames;
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
