using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.FancyIcon
{
    /// <summary>
    /// STATUS: reference only, not called from anywhere in the mod right now.
    /// See FancyIcon/README.md for the full story and how to wire this back in.
    ///
    /// Reusable "holo-tech shine" overlay - draw it centered on any point (an item icon, a menu
    /// logo, a mod-list thumbnail, whatever) and it spins in place. Built from the same technique
    /// StarsAbove uses on its main menu logo (ModSources/StarsAbove/Menu/StarsAboveMainMenu.cs,
    /// PreDrawLogo): several independent rotation timers advancing every draw call, one shine
    /// texture drawn twice at different speeds/scales so the two copies drift in and out of phase
    /// (which reads as a flicker/flash), plus small extra pieces popping in and fading. No shaders,
    /// no blend-state changes - same plain SpriteBatch.Draw calls StarsAbove uses, recolored to
    /// this mod's matrix-green palette instead of stars.
    ///
    /// Usage:
    ///   private readonly ShineEffectToolkit.State shine = new();
    ///   // each frame, wherever you're already drawing something at `center` with a given `radius`:
    ///   ShineEffectToolkit.Advance(shine);
    ///   ShineEffectToolkit.Draw(spriteBatch, shine, center, radius);
    /// </summary>
    internal static class ShineEffectToolkit
    {
        public const string ShineTexturePath = "CalamityLegendsComeBack/Texture/Myown/ShineFX";

        private static readonly string[] Glyphs = { "0", "1", "::", "//", "{}", "01" };

        public sealed class State
        {
            // Randomized starting angles so that two instances of this effect (two item stacks,
            // two UI elements, whatever) don't spin in perfect lockstep.
            public float RingRotationA = Main.rand.NextFloat(MathHelper.TwoPi);
            public float RingRotationB = Main.rand.NextFloat(MathHelper.TwoPi);
            public float FlareRotationA = Main.rand.NextFloat(MathHelper.TwoPi);
            public float FlareRotationB = Main.rand.NextFloat(MathHelper.TwoPi);
            public float CometAngle = Main.rand.NextFloat(MathHelper.TwoPi);

            public int BurstTimer = Main.rand.Next(50, 120);
            public float BurstRadius01;
            public bool BurstActive;
            public float FlashBoost;

            public int SparkTimer = Main.rand.Next(20, 50);
            public float SparkAngle;
            public float SparkLife;
            public string SparkGlyph = "";
        }

        private const float SparkMaxLife = 24f;

        /// <summary>Call once per frame before Draw. Advances every timer in the state.</summary>
        public static void Advance(State state)
        {
            state.RingRotationA += 0.02f;
            state.RingRotationB -= 0.014f;
            state.FlareRotationA += 0.03f;
            state.FlareRotationB -= 0.024f;
            state.CometAngle += 0.1f;

            if (state.BurstActive)
            {
                state.BurstRadius01 += 0.05f;
                if (state.BurstRadius01 >= 1f)
                    state.BurstActive = false;
            }
            else if (--state.BurstTimer <= 0)
            {
                state.BurstActive = true;
                state.BurstRadius01 = 0f;
                state.BurstTimer = Main.rand.Next(140, 300);
            }

            // Peaks halfway through the burst then eases back to 0, so the ring/flare/dash
            // brighten in step with the burst ring instead of just ambient shimmering - the whole
            // thing flashes together for a moment instead of one part popping alone.
            state.FlashBoost = state.BurstActive ? MathF.Sin(state.BurstRadius01 * MathHelper.Pi) * 0.9f : 0f;

            if (state.SparkLife > 0f)
            {
                state.SparkLife -= 1f;
            }
            else if (--state.SparkTimer <= 0)
            {
                state.SparkTimer = Main.rand.Next(35, 70);
                state.SparkAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                state.SparkLife = SparkMaxLife;
                state.SparkGlyph = Glyphs[Main.rand.Next(Glyphs.Length)];
            }
        }

        /// <summary>Call once per frame after Advance, centered on whatever you're decorating.</summary>
        public static void Draw(SpriteBatch spriteBatch, State state, Vector2 center, float radius)
        {
            DrawRing(spriteBatch, state, center, radius * 1.3f, state.RingRotationA, 6, new Color(70, 255, 190));
            DrawDashRing(spriteBatch, state, center, radius * 1.62f, state.RingRotationB, 4, new Color(40, 190, 130));
            DrawComet(spriteBatch, state, center, radius * 1.85f);
            DrawFlareCross(spriteBatch, state, center, radius);
            DrawSpark(spriteBatch, state, center);
            DrawBurst(spriteBatch, state, center, radius);
        }

        private static void DrawRing(SpriteBatch spriteBatch, State state, Vector2 center, float radius, float rotation, int nodeCount, Color color)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                float angle = rotation + i * (MathHelper.TwoPi / nodeCount);
                Vector2 nodePosition = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                DrawPixelRect(spriteBatch, nodePosition, new Vector2(3f, 3f), angle, color * (0.85f + state.FlashBoost));
            }
        }

        private static void DrawDashRing(SpriteBatch spriteBatch, State state, Vector2 center, float radius, float rotation, int dashCount, Color color)
        {
            for (int i = 0; i < dashCount; i++)
            {
                float angle = rotation + i * (MathHelper.TwoPi / dashCount);
                Vector2 dashPosition = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                DrawPixelRect(spriteBatch, dashPosition, new Vector2(9f, 2f), angle + MathHelper.PiOver2, color * (0.6f + state.FlashBoost));
            }
        }

        private static void DrawComet(SpriteBatch spriteBatch, State state, Vector2 center, float radius)
        {
            // A single fast point orbiting the center, with a handful of afterimages sampled a
            // little earlier on the same orbit - a cheap motion-blur trail with no extra state
            // beyond the one angle, since the path is fully deterministic.
            const int trailCount = 5;
            for (int i = 0; i < trailCount; i++)
            {
                float angle = state.CometAngle - i * 0.14f;
                float fade = 1f - i / (float)trailCount;
                Vector2 position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                float size = MathHelper.Lerp(1.5f, 4.5f, fade);
                Color color = Color.Lerp(new Color(70, 255, 180), Color.White, 0.5f) * (fade * fade);
                DrawPixelRect(spriteBatch, position, new Vector2(size, size), angle, color);
            }
        }

        private static void DrawFlareCross(SpriteBatch spriteBatch, State state, Vector2 center, float radius)
        {
            // The core trick, straight from StarsAboveMainMenu.PreDrawLogo: one shine texture
            // drawn twice, spinning at two different speeds. When the two copies' angles line up
            // the overlap reads brighter for a frame - that beat is the "flash", not a scripted
            // brightness curve. We recolor it matrix green instead of leaving it white.
            Texture2D shine = ModContent.Request<Texture2D>(ShineTexturePath).Value;
            float alignment = MathF.Abs(MathF.Cos(state.FlareRotationA - state.FlareRotationB));
            float intensity = 0.4f + alignment * 0.55f + state.FlashBoost;
            float baseScale = radius * 2.2f / shine.Width;

            spriteBatch.Draw(shine, center, null, new Color(110, 255, 185) * intensity, state.FlareRotationA, shine.Size() * 0.5f, baseScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(shine, center, null, new Color(190, 255, 225) * (intensity * 0.7f), state.FlareRotationB, shine.Size() * 0.5f, baseScale * 0.68f, SpriteEffects.None, 0f);
        }

        private static void DrawSpark(SpriteBatch spriteBatch, State state, Vector2 center)
        {
            if (state.SparkLife <= 0f)
                return;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float progress = 1f - state.SparkLife / SparkMaxLife;
            float fade = MathF.Sin(progress * MathHelper.Pi);
            Vector2 sparkPosition = center + new Vector2(MathF.Cos(state.SparkAngle), MathF.Sin(state.SparkAngle)) * 26f;
            Color color = new Color(150, 255, 210) * fade;
            ChatManager.DrawColorCodedString(spriteBatch, font, state.SparkGlyph, sparkPosition, color, 0f, font.MeasureString(state.SparkGlyph) * 0.5f, Vector2.One * 0.42f);
        }

        private static void DrawBurst(SpriteBatch spriteBatch, State state, Vector2 center, float radius)
        {
            if (!state.BurstActive)
                return;

            const int points = 10;
            float burstRadius = radius * MathHelper.Lerp(0.3f, 2.1f, state.BurstRadius01);
            float alpha = 1f - state.BurstRadius01;
            Color color = new Color(200, 255, 235) * (alpha * alpha);

            for (int i = 0; i < points; i++)
            {
                float angle = i * (MathHelper.TwoPi / points) + state.BurstRadius01 * 1.5f;
                Vector2 position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * burstRadius;
                DrawPixelRect(spriteBatch, position, new Vector2(3.5f, 3.5f), angle, color);
            }
        }

        private static void DrawPixelRect(SpriteBatch spriteBatch, Vector2 center, Vector2 size, float rotation, Color color)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, center, null, color, rotation, new Vector2(0.5f, 0.5f), size, SpriteEffects.None, 0f);
        }
    }
}
