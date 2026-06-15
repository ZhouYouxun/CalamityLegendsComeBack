using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    internal enum MatrixGeometryShape
    {
        Tetrahedron,
        Icosahedron,
        Cube
    }

    internal static class HyperdimensionalMatrixVisuals
    {
        private const int ProceduralTextureSize = 96;
        private static Texture2D proceduralCoreTexture;
        private static Texture2D proceduralGlyphTexture;
        private static Texture2D proceduralNoiseTexture;
        private static int lastNoiseFrame = -1;

        private sealed class GeometryData
        {
            public GeometryData(Vector3[] vertices, int[,] edges)
            {
                Vertices = vertices;
                Edges = edges;
            }

            public Vector3[] Vertices { get; }
            public int[,] Edges { get; }
        }

        private static readonly GeometryData Tetrahedron = new(
            new Vector3[]
            {
                new(1f, 1f, 1f),
                new(-1f, -1f, 1f),
                new(-1f, 1f, -1f),
                new(1f, -1f, -1f)
            },
            new int[,]
            {
                { 0, 1 }, { 0, 2 }, { 0, 3 },
                { 1, 2 }, { 1, 3 }, { 2, 3 }
            });

        private static readonly GeometryData Cube = new(
            new Vector3[]
            {
                new(-1f, -1f, -1f), new(1f, -1f, -1f), new(1f, 1f, -1f), new(-1f, 1f, -1f),
                new(-1f, -1f, 1f), new(1f, -1f, 1f), new(1f, 1f, 1f), new(-1f, 1f, 1f)
            },
            new int[,]
            {
                { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
                { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
            });

        private static readonly GeometryData Icosahedron = CreateIcosahedron();

        public static Color GetDataColor(float offset, float opacity = 1f)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.18f + offset) % 1f;
            Color color = Main.hslToRgb(hue, 0.88f, 0.62f) * opacity;
            color.A = 0;
            return color;
        }

        public static void UnloadInventoryIconTextures()
        {
            proceduralCoreTexture?.Dispose();
            proceduralGlyphTexture?.Dispose();
            proceduralNoiseTexture?.Dispose();
            proceduralCoreTexture = null;
            proceduralGlyphTexture = null;
            proceduralNoiseTexture = null;
            lastNoiseFrame = -1;
        }

        /// <summary>
        /// Returns the screen-space projected vertex positions for a given geometry shape.
        /// Used by attack modules to spawn projectiles at edge/vertex locations.
        /// </summary>
        internal static Vector2[] GetProjectedVertices(
            MatrixGeometryShape shape,
            Vector2 center,
            float radius,
            float time,
            int identity)
        {
            GeometryData geometry = shape switch
            {
                MatrixGeometryShape.Tetrahedron => Tetrahedron,
                MatrixGeometryShape.Icosahedron => Icosahedron,
                _ => Cube
            };

            Matrix rotation = Matrix.CreateFromYawPitchRoll(
                time * 0.94f + identity * 0.013f,
                time * 0.67f + identity * 0.021f,
                time * 0.43f + identity * 0.008f);

            return ProjectGeometry(center, geometry.Vertices, radius, rotation);
        }

        public static void DrawGeometry(
            Vector2 center,
            MatrixGeometryShape shape,
            float radius,
            float time,
            float opacity,
            int identity,
            bool drawDataFlow = true)
        {
            GeometryData geometry = shape switch
            {
                MatrixGeometryShape.Tetrahedron => Tetrahedron,
                MatrixGeometryShape.Icosahedron => Icosahedron,
                _ => Cube
            };

            Matrix rotation = Matrix.CreateFromYawPitchRoll(
                time * 0.94f + identity * 0.013f,
                time * 0.67f + identity * 0.021f,
                time * 0.43f + identity * 0.008f);

            Vector2[] projected = ProjectGeometry(center, geometry.Vertices, radius, rotation);
            int edgeCount = geometry.Edges.GetLength(0);

            for (int i = 0; i < edgeCount; i++)
            {
                Vector2 start = projected[geometry.Edges[i, 0]];
                Vector2 end = projected[geometry.Edges[i, 1]];
                float pulse = 0.58f + 0.42f * (float)Math.Sin(time * 4.2f + i * 0.73f);
                Color color = GetDataColor(i / (float)Math.Max(1, edgeCount), opacity * pulse);

                Main.spriteBatch.DrawLineBetter(start, end, color * 0.22f, 6f);
                Main.spriteBatch.DrawLineBetter(start, end, color, 2f);

                if (!drawDataFlow || (i & 1) != 0)
                    continue;

                float flow = (time * 0.72f + i * 0.173f) % 1f;
                DrawNode(Vector2.Lerp(start, end, flow), color, 4.5f);
            }

            for (int i = 0; i < projected.Length; i++)
                DrawNode(projected[i], GetDataColor(i / (float)projected.Length, opacity), i % 3 == 0 ? 6f : 4f);

            DrawScanRing(center, radius * 1.18f, time, GetDataColor(0.32f, opacity * 0.42f), 28, 1.2f);
        }

        public static void DrawHypercube(Vector2 center, float radius, float time, float opacity)
        {
            Vector2[] projected = new Vector2[16];
            Matrix rotation3D = Matrix.CreateFromYawPitchRoll(time * 0.71f, time * 0.49f, time * 0.36f);

            for (int i = 0; i < projected.Length; i++)
            {
                Vector4 point = new(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f,
                    (i & 8) == 0 ? -1f : 1f);

                RotatePlane(ref point.X, ref point.W, time * 0.83f);
                RotatePlane(ref point.Y, ref point.Z, time * 0.54f);
                RotatePlane(ref point.Z, ref point.W, time * 0.37f);
                RotatePlane(ref point.X, ref point.Y, time * 0.29f);

                float fourDimensionalPerspective = 2.4f / (3.4f - point.W);
                Vector3 point3D = new Vector3(point.X, point.Y, point.Z) * radius * fourDimensionalPerspective;
                point3D = Vector3.Transform(point3D, rotation3D);
                float perspective = 620f / Math.Max(180f, 620f + point3D.Z);
                projected[i] = center + new Vector2(point3D.X, point3D.Y) * perspective;
            }

            for (int vertex = 0; vertex < 16; vertex++)
            {
                for (int bit = 0; bit < 4; bit++)
                {
                    int other = vertex ^ (1 << bit);
                    if (other < vertex)
                        continue;

                    float offset = bit * 0.21f + vertex * 0.037f;
                    Color color = GetDataColor(offset, opacity);
                    Main.spriteBatch.DrawLineBetter(projected[vertex], projected[other], color * 0.2f, 7f);
                    Main.spriteBatch.DrawLineBetter(projected[vertex], projected[other], color, bit == 3 ? 2.8f : 1.8f);

                    float flow = (time * (0.55f + bit * 0.09f) + vertex * 0.071f) % 1f;
                    DrawNode(Vector2.Lerp(projected[vertex], projected[other], flow), color, 4f);
                }
            }

            for (int i = 0; i < projected.Length; i++)
                DrawNode(projected[i], GetDataColor(i / 16f, opacity), i % 4 == 0 ? 6.5f : 3.5f);

            DrawScanRing(center, radius * 1.5f, -time * 0.8f, GetDataColor(0.68f, opacity * 0.55f), 36, 1.5f);
            DrawScanRing(center, radius * 1.18f, time * 1.2f, GetDataColor(0.18f, opacity * 0.38f), 28, 1f);
        }

        public static void DrawShield(Player owner, float opacity)
        {
            Vector2 center = owner.Center;
            float time = Main.GlobalTimeWrappedHourly;
            const int segments = 48;

            for (int layer = 0; layer < 3; layer++)
            {
                float radiusX = 52f + layer * 5f;
                float radiusY = 72f + layer * 6f;
                float rotation = time * (layer == 1 ? -0.16f : 0.12f) + layer * 0.36f;
                Color color = GetDataColor(0.5f + layer * 0.14f, opacity * (0.42f - layer * 0.08f));

                for (int i = 0; i < segments; i++)
                {
                    float angleA = MathHelper.TwoPi * i / segments;
                    float angleB = MathHelper.TwoPi * (i + 1) / segments;
                    Vector2 a = EllipsePoint(center, radiusX, radiusY, angleA, rotation, time, layer);
                    Vector2 b = EllipsePoint(center, radiusX, radiusY, angleB, rotation, time, layer);
                    Main.spriteBatch.DrawLineBetter(a, b, color, layer == 0 ? 2f : 1f);
                }
            }

            float scanY = center.Y + (float)Math.Sin(time * 2.7f) * 62f;
            float relativeY = MathHelper.Clamp((scanY - center.Y) / 72f, -1f, 1f);
            float halfWidth = 52f * (float)Math.Sqrt(Math.Max(0f, 1f - relativeY * relativeY));
            Main.spriteBatch.DrawLineBetter(
                new Vector2(center.X - halfWidth, scanY),
                new Vector2(center.X + halfWidth, scanY),
                GetDataColor(0.08f, opacity * 0.58f),
                1.4f);

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f + time * 0.35f;
                Vector2 node = center + new Vector2((float)Math.Cos(angle) * 56f, (float)Math.Sin(angle) * 77f);
                DrawNode(node, GetDataColor(i / 8f, opacity * 0.8f), 4f);
            }
        }

        public static void DrawTargetingLine(Vector2 start, Vector2 end, float opacity)
        {
            const int segmentCount = 20;
            float time = Main.GlobalTimeWrappedHourly * 2.2f;

            for (int i = 0; i < segmentCount; i += 2)
            {
                float a = (i + time % 2f) / segmentCount;
                float b = Math.Min(1f, a + 0.6f / segmentCount);
                if (a >= 1f)
                    continue;

                Main.spriteBatch.DrawLineBetter(
                    Vector2.Lerp(start, end, a),
                    Vector2.Lerp(start, end, b),
                    GetDataColor(i / (float)segmentCount, opacity),
                    1.4f);
            }
        }

        public static void DrawInventoryIcon(Vector2 center, float scale)
        {
            float time = Main.GlobalTimeWrappedHourly;
            EnsureProceduralTextures(Main.spriteBatch.GraphicsDevice);
            UpdateProceduralNoise(Main.spriteBatch.GraphicsDevice);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D localStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01").Value;
            float iconScale = MathHelper.Clamp(scale, 0.7f, 1.35f);
            float coreRadius = 27f * iconScale;

            Color primary = GetDataColor(0.02f, 0.92f);
            Color secondary = GetDataColor(0.42f, 0.72f);
            Color tertiary = GetDataColor(0.72f, 0.55f);
            Color flash = Color.White with { A = 0 };

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Main.spriteBatch.Draw(bloom, center, null, primary * 0.72f, time * 0.4f, bloom.Size() * 0.5f, 0.48f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, center, null, secondary * 0.46f, -time * 0.6f, bloom.Size() * 0.5f, 0.78f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, center, null, tertiary * 0.22f, time, bloom.Size() * 0.5f, 1.08f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, center, null, primary * 0.9f, -time * 1.25f, ring.Size() * 0.5f, 0.58f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, center, null, secondary * 0.55f, time * 1.55f, ring.Size() * 0.5f, 0.82f * iconScale, SpriteEffects.None, 0f);

            DrawProceduralTextureLayer(proceduralCoreTexture, center, primary, time * 0.32f, 0.58f * iconScale);
            DrawProceduralTextureLayer(proceduralCoreTexture, center, secondary * 0.66f, -time * 0.46f, 0.76f * iconScale);
            DrawProceduralTextureLayer(proceduralGlyphTexture, center, tertiary * 0.95f, time * 0.75f, 0.72f * iconScale);
            DrawProceduralTextureLayer(proceduralGlyphTexture, center, primary * 0.6f, -time * 1.05f, 0.93f * iconScale);
            DrawProceduralTextureLayer(proceduralNoiseTexture, center, flash * 0.5f, 0f, 0.74f * iconScale);

            DrawInventoryScanRing(center, coreRadius * 1.42f, time * 1.6f, primary * 0.85f, 32, 1.5f * iconScale);
            DrawInventoryScanRing(center, coreRadius * 1.08f, -time * 2.1f, secondary * 0.72f, 28, 1.15f * iconScale);
            DrawInventoryScanRing(center, coreRadius * 0.62f, time * 2.8f, tertiary * 0.65f, 20, 0.9f * iconScale);

            DrawInventoryHypercube(center, coreRadius, time, iconScale);
            DrawInventoryDataStreams(center, coreRadius, time, iconScale);
            DrawInventoryCodeFragments(center, coreRadius, time, iconScale);
            DrawInventorySparkStorm(center, coreRadius, time, iconScale, star, localStar);

            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f + time * (0.72f + i % 4 * 0.08f);
                float radius = coreRadius * (1.02f + 0.33f * (float)Math.Sin(time * 3.2f + i));
                Vector2 node = center + angle.ToRotationVector2() * radius;
                DrawScreenNode(node, GetDataColor(i * 0.09f, 0.95f), (2.4f + i % 4) * iconScale);
                if (i % 3 == 0)
                    Main.spriteBatch.Draw(star, node, null, GetDataColor(i * 0.071f, 0.72f), angle + time, star.Size() * 0.5f, 0.18f * iconScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i + MathHelper.PiOver4 + time * 0.45f;
                Vector2 corner = center + angle.ToRotationVector2() * coreRadius * 1.63f;
                Vector2 tangent = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                Vector2 radial = angle.ToRotationVector2();
                Color bracketColor = GetDataColor(i * 0.17f, 0.72f);
                DrawScreenLine(corner, corner - tangent * 8f * iconScale, bracketColor, 1.2f * iconScale);
                DrawScreenLine(corner, corner - radial * 8f * iconScale, bracketColor, 1.2f * iconScale);
            }

            Rectangle core = new(
                (int)(center.X - 2.2f * iconScale),
                (int)(center.Y - 2.2f * iconScale),
                Math.Max(3, (int)(4.4f * iconScale)),
                Math.Max(3, (int)(4.4f * iconScale)));
            Main.spriteBatch.Draw(pixel, core, Color.White with { A = 0 } * 0.88f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        public static void DrawWorldIcon(Vector2 screenCenter, float scale)
        {
            float time = Main.GlobalTimeWrappedHourly;
            EnsureProceduralTextures(Main.spriteBatch.GraphicsDevice);
            UpdateProceduralNoise(Main.spriteBatch.GraphicsDevice);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D localStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01").Value;
            float iconScale = MathHelper.Clamp(scale * 0.92f, 0.65f, 1.22f);
            float coreRadius = 25f * iconScale;
            Vector2 center = screenCenter + Vector2.UnitY * (float)Math.Sin(time * 2.1f) * 2.4f * iconScale;

            Color primary = GetDataColor(0.02f, 0.82f);
            Color secondary = GetDataColor(0.42f, 0.58f);
            Color tertiary = GetDataColor(0.72f, 0.42f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(bloom, center, null, primary * 0.58f, time * 0.4f, bloom.Size() * 0.5f, 0.34f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, center, null, secondary * 0.32f, -time * 0.6f, bloom.Size() * 0.5f, 0.62f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, center, null, primary * 0.76f, -time * 1.15f, ring.Size() * 0.5f, 0.42f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, center, null, secondary * 0.48f, time * 1.45f, ring.Size() * 0.5f, 0.62f * iconScale, SpriteEffects.None, 0f);
            DrawProceduralTextureLayer(proceduralCoreTexture, center, primary * 0.7f, time * 0.32f, 0.42f * iconScale);
            DrawProceduralTextureLayer(proceduralGlyphTexture, center, tertiary * 0.78f, -time * 0.75f, 0.54f * iconScale);
            DrawProceduralTextureLayer(proceduralNoiseTexture, center, Color.White with { A = 0 } * 0.24f, 0f, 0.48f * iconScale);

            DrawInventoryScanRing(center, coreRadius * 1.34f, time * 1.5f, primary * 0.76f, 28, 1.15f * iconScale);
            DrawInventoryScanRing(center, coreRadius * 0.92f, -time * 2f, secondary * 0.58f, 22, 0.9f * iconScale);
            DrawInventoryHypercube(center, coreRadius, time, iconScale * 0.92f);
            DrawInventoryDataStreams(center, coreRadius, time, iconScale * 0.86f);
            DrawInventoryCodeFragments(center, coreRadius, time, iconScale * 0.72f);
            DrawInventorySparkStorm(center, coreRadius, time, iconScale * 0.84f, star, localStar);

            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f + time * (0.62f + i % 3 * 0.08f);
                float radius = coreRadius * (1.03f + 0.22f * (float)Math.Sin(time * 3.2f + i));
                Vector2 node = center + angle.ToRotationVector2() * radius;
                DrawScreenNode(node, GetDataColor(i * 0.09f, 0.82f), (2.2f + i % 3) * iconScale);
            }

            Rectangle core = new(
                (int)(center.X - 2.1f * iconScale),
                (int)(center.Y - 2.1f * iconScale),
                Math.Max(3, (int)(4.2f * iconScale)),
                Math.Max(3, (int)(4.2f * iconScale)));
            Main.spriteBatch.Draw(pixel, core, Color.White with { A = 0 } * 0.78f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void DrawScanRing(Vector2 center, float radius, float rotation, Color color, int segments, float width)
        {
            for (int i = 0; i < segments; i++)
            {
                if ((i + (int)(Main.GameUpdateCount / 5)) % 5 == 0)
                    continue;

                float angleA = MathHelper.TwoPi * i / segments + rotation;
                float angleB = MathHelper.TwoPi * (i + 0.72f) / segments + rotation;
                Vector2 a = center + new Vector2((float)Math.Cos(angleA) * radius, (float)Math.Sin(angleA) * radius * 0.35f);
                Vector2 b = center + new Vector2((float)Math.Cos(angleB) * radius, (float)Math.Sin(angleB) * radius * 0.35f);
                Main.spriteBatch.DrawLineBetter(a, b, color, width);
            }
        }

        private static void DrawInventoryHypercube(Vector2 center, float radius, float time, float scale)
        {
            Vector2[] projected = new Vector2[16];
            Matrix rotation3D = Matrix.CreateFromYawPitchRoll(time * 1.2f, time * 0.86f, time * 0.67f);

            for (int i = 0; i < projected.Length; i++)
            {
                Vector4 point = new(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f,
                    (i & 8) == 0 ? -1f : 1f);

                RotatePlane(ref point.X, ref point.W, time * 1.18f);
                RotatePlane(ref point.Y, ref point.Z, time * 0.93f);
                RotatePlane(ref point.Z, ref point.W, time * 0.71f);
                RotatePlane(ref point.X, ref point.Y, time * 0.47f);

                float fourDimensionalPerspective = 2.4f / (3.45f - point.W);
                Vector3 point3D = new Vector3(point.X, point.Y, point.Z) * radius * fourDimensionalPerspective;
                point3D = Vector3.Transform(point3D, rotation3D);
                float perspective = 240f / Math.Max(90f, 240f + point3D.Z);
                projected[i] = center + new Vector2(point3D.X, point3D.Y) * perspective;
            }

            for (int vertex = 0; vertex < 16; vertex++)
            {
                for (int bit = 0; bit < 4; bit++)
                {
                    int other = vertex ^ (1 << bit);
                    if (other < vertex)
                        continue;

                    Color color = GetDataColor(bit * 0.22f + vertex * 0.041f, 0.85f);
                    DrawScreenLine(projected[vertex], projected[other], color * 0.28f, 4.2f * scale);
                    DrawScreenLine(projected[vertex], projected[other], color, (bit == 3 ? 1.6f : 1.05f) * scale);

                    float flow = (time * (0.72f + bit * 0.12f) + vertex * 0.083f) % 1f;
                    DrawScreenNode(Vector2.Lerp(projected[vertex], projected[other], flow), color, 2.2f * scale);
                }
            }

            for (int i = 0; i < projected.Length; i++)
                DrawScreenNode(projected[i], GetDataColor(i / 16f, 0.92f), (i % 4 == 0 ? 3.3f : 2f) * scale);
        }

        private static void DrawInventoryDataStreams(Vector2 center, float radius, float time, float scale)
        {
            for (int lane = -4; lane <= 4; lane++)
            {
                float y = center.Y + lane * 4.2f * scale;
                float phase = (time * 58f + lane * 13f) % (radius * 2f);
                for (int segment = 0; segment < 6; segment++)
                {
                    float x = center.X - radius * 1.35f + phase - segment * radius * 0.34f;
                    Vector2 start = new(x, y + (float)Math.Sin(time * 7f + lane) * 1.8f * scale);
                    Vector2 end = start + Vector2.UnitX * (7f + segment * 1.35f) * scale;
                    Color color = GetDataColor(0.18f * segment + lane * 0.07f, 0.62f);
                    DrawScreenLine(start, end, color, 1.05f * scale);
                }
            }
        }

        private static void DrawInventoryCodeFragments(Vector2 center, float radius, float time, float scale)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < 34; i++)
            {
                float seed = i * 17.31f;
                float angle = seed + time * (0.9f + i % 5 * 0.12f);
                float distance = radius * (0.34f + 1.12f * Hash01(i * 91 + (int)(Main.GameUpdateCount / 12)));
                Vector2 position = center + angle.ToRotationVector2() * distance;
                int width = Math.Max(2, (int)((2f + Hash01(i * 29) * 5f) * scale));
                int height = Math.Max(1, (int)((1f + Hash01(i * 37) * 2f) * scale));
                Rectangle fragment = new((int)position.X, (int)position.Y, width, height);
                Color color = GetDataColor(i * 0.031f, 0.4f + 0.42f * Hash01(i * 53));
                Main.spriteBatch.Draw(pixel, fragment, null, color, angle, new Vector2(width, height) * 0.5f, SpriteEffects.None, 0f);
            }
        }

        private static void DrawInventorySparkStorm(
            Vector2 center,
            float radius,
            float time,
            float scale,
            Texture2D star,
            Texture2D localStar)
        {
            for (int i = 0; i < 26; i++)
            {
                float phase = (time * (0.65f + i % 6 * 0.11f) + i * 0.173f) % 1f;
                float angle = i * 2.399963f + time * (i % 2 == 0 ? 1.35f : -1.05f);
                float distance = MathHelper.Lerp(radius * 1.75f, radius * 0.22f, phase);
                Vector2 position = center + angle.ToRotationVector2() * distance;
                float opacity = (float)Math.Sin(phase * MathHelper.Pi);
                Color color = GetDataColor(i * 0.047f + phase, 0.75f * opacity);
                Texture2D texture = i % 3 == 0 ? localStar : star;
                float drawScale = (0.08f + 0.18f * Hash01(i * 113)) * scale * (0.65f + opacity);
                Main.spriteBatch.Draw(texture, position, null, color, angle + time * 2.2f, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);

                if (i % 4 == 0)
                    DrawScreenLine(center, position, color * 0.28f, 0.7f * scale);
            }
        }

        private static void DrawProceduralTextureLayer(Texture2D texture, Vector2 center, Color color, float rotation, float scale)
        {
            if (texture == null)
                return;

            Main.spriteBatch.Draw(texture, center, null, EnsureVisibleAlpha(color), rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private static void DrawInventoryScanRing(Vector2 center, float radius, float rotation, Color color, int segments, float width)
        {
            for (int i = 0; i < segments; i++)
            {
                if ((i + (int)(Main.GameUpdateCount / 4)) % 5 == 0)
                    continue;

                float angleA = MathHelper.TwoPi * i / segments + rotation;
                float angleB = MathHelper.TwoPi * (i + 0.68f) / segments + rotation;
                Vector2 a = center + new Vector2((float)Math.Cos(angleA) * radius, (float)Math.Sin(angleA) * radius * 0.52f);
                Vector2 b = center + new Vector2((float)Math.Cos(angleB) * radius, (float)Math.Sin(angleB) * radius * 0.52f);
                DrawScreenLine(a, b, color, width);
            }
        }

        private static void DrawScreenNode(Vector2 screenPosition, Color color, float size)
        {
            color = EnsureVisibleAlpha(color);
            int width = Math.Max(1, (int)size);
            Rectangle horizontal = new(
                (int)(screenPosition.X - width * 0.5f),
                (int)(screenPosition.Y - 1f),
                width,
                2);
            Rectangle vertical = new(
                (int)(screenPosition.X - 1f),
                (int)(screenPosition.Y - width * 0.5f),
                2,
                width);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, horizontal, color);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, vertical, color);
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float width)
        {
            if (start == end)
                return;

            color = EnsureVisibleAlpha(color);
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Line").Value;
            float rotation = (end - start).ToRotation();
            Vector2 lineScale = new(Vector2.Distance(start, end) / line.Width, width);
            Main.spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, lineScale, SpriteEffects.None, 0f);
        }

        private static Color VisibleDataColor(float offset, float opacity)
        {
            Color color = GetDataColor(offset, opacity);
            color.A = (byte)(MathHelper.Clamp(opacity, 0f, 1f) * 255f);
            return color;
        }

        private static Color EnsureVisibleAlpha(Color color)
        {
            if (color.A == 0 && (color.R != 0 || color.G != 0 || color.B != 0))
                color.A = 255;

            return color;
        }

        private static void EnsureProceduralTextures(GraphicsDevice graphicsDevice)
        {
            if (proceduralCoreTexture == null || proceduralCoreTexture.IsDisposed)
                proceduralCoreTexture = CreateProceduralCoreTexture(graphicsDevice);

            if (proceduralGlyphTexture == null || proceduralGlyphTexture.IsDisposed)
                proceduralGlyphTexture = CreateProceduralGlyphTexture(graphicsDevice);

            if (proceduralNoiseTexture == null || proceduralNoiseTexture.IsDisposed)
                proceduralNoiseTexture = new Texture2D(graphicsDevice, ProceduralTextureSize, ProceduralTextureSize);
        }

        private static Texture2D CreateProceduralCoreTexture(GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new(graphicsDevice, ProceduralTextureSize, ProceduralTextureSize);
            Color[] data = new Color[ProceduralTextureSize * ProceduralTextureSize];
            Vector2 center = new((ProceduralTextureSize - 1) * 0.5f);

            for (int y = 0; y < ProceduralTextureSize; y++)
            {
                for (int x = 0; x < ProceduralTextureSize; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distance = offset.Length() / (ProceduralTextureSize * 0.5f);
                    float angle = offset.ToRotation();
                    float ring = Math.Abs((distance * 7.5f + (float)Math.Sin(angle * 8f) * 0.09f) % 1f - 0.5f);
                    float lattice = Math.Abs((offset.X + offset.Y) * 0.055f % 1f - 0.5f);
                    float radial = MathHelper.Clamp(1f - distance, 0f, 1f);
                    float intensity =
                        radial * radial * 0.78f +
                        MathHelper.Clamp(0.17f - ring, 0f, 0.17f) * 3.2f +
                        MathHelper.Clamp(0.045f - lattice, 0f, 0.045f) * 2.5f;
                    byte alpha = (byte)(MathHelper.Clamp(intensity, 0f, 1f) * 255f);
                    data[y * ProceduralTextureSize + x] = new Color(255, 255, 255, alpha);
                }
            }

            texture.SetData(data);
            return texture;
        }

        private static Texture2D CreateProceduralGlyphTexture(GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new(graphicsDevice, ProceduralTextureSize, ProceduralTextureSize);
            Color[] data = new Color[ProceduralTextureSize * ProceduralTextureSize];
            Vector2 center = new((ProceduralTextureSize - 1) * 0.5f);

            for (int y = 0; y < ProceduralTextureSize; y++)
            {
                for (int x = 0; x < ProceduralTextureSize; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distance = offset.Length() / (ProceduralTextureSize * 0.5f);
                    bool border = distance > 0.34f && distance < 0.38f || distance > 0.61f && distance < 0.65f;
                    bool verticalCode = x % 13 is 0 or 1 && y % 9 < 5 && distance < 0.86f;
                    bool horizontalCode = y % 17 == 0 && x % 11 < 7 && distance < 0.82f;
                    bool diagonalCircuit = Math.Abs(((x + y * 2) % 31) - 15) <= 1 && distance < 0.74f;
                    if (border || verticalCode || horizontalCode || diagonalCircuit)
                        data[y * ProceduralTextureSize + x] = new Color(255, 255, 255, border ? 190 : 120);
                    else
                        data[y * ProceduralTextureSize + x] = Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }

        private static void UpdateProceduralNoise(GraphicsDevice graphicsDevice)
        {
            int frame = (int)(Main.GameUpdateCount / 3);
            if (frame == lastNoiseFrame)
                return;

            lastNoiseFrame = frame;
            EnsureProceduralTextures(graphicsDevice);
            Color[] data = new Color[ProceduralTextureSize * ProceduralTextureSize];
            Vector2 center = new((ProceduralTextureSize - 1) * 0.5f);

            for (int y = 0; y < ProceduralTextureSize; y++)
            {
                for (int x = 0; x < ProceduralTextureSize; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distance = offset.Length() / (ProceduralTextureSize * 0.5f);
                    int hash = HashInt(x * 73856093 ^ y * 19349663 ^ frame * 83492791);
                    float value = (hash & 255) / 255f;
                    bool scanline = (y + frame * 3) % 9 == 0;
                    bool bit = value > 0.79f || scanline && value > 0.45f;
                    float alpha = bit ? MathHelper.Clamp(1f - distance, 0f, 1f) * (0.28f + value * 0.7f) : 0f;
                    data[y * ProceduralTextureSize + x] = new Color(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            proceduralNoiseTexture.SetData(data);
        }

        private static float Hash01(int value)
        {
            int hash = HashInt(value);
            return (hash & 0xFFFFFF) / (float)0xFFFFFF;
        }

        private static int HashInt(int value)
        {
            unchecked
            {
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value;
            }
        }

        public static void DrawNode(Vector2 worldPosition, Color color, float size)
        {
            Vector2 screenPosition = worldPosition - Main.screenPosition;
            int width = Math.Max(1, (int)size);
            Rectangle horizontal = new(
                (int)(screenPosition.X - width * 0.5f),
                (int)(screenPosition.Y - 1f),
                width,
                2);
            Rectangle vertical = new(
                (int)(screenPosition.X - 1f),
                (int)(screenPosition.Y - width * 0.5f),
                2,
                width);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, horizontal, color);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, vertical, color);
        }

        private static GeometryData CreateIcosahedron()
        {
            float phi = (1f + (float)Math.Sqrt(5f)) * 0.5f;
            Vector3[] vertices =
            {
                new(-1f, phi, 0f), new(1f, phi, 0f), new(-1f, -phi, 0f), new(1f, -phi, 0f),
                new(0f, -1f, phi), new(0f, 1f, phi), new(0f, -1f, -phi), new(0f, 1f, -phi),
                new(phi, 0f, -1f), new(phi, 0f, 1f), new(-phi, 0f, -1f), new(-phi, 0f, 1f)
            };

            return new GeometryData(vertices, BuildShortestEdges(vertices));
        }

        private static int[,] BuildShortestEdges(Vector3[] vertices)
        {
            float minimumDistance = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = i + 1; j < vertices.Length; j++)
                    minimumDistance = Math.Min(minimumDistance, Vector3.Distance(vertices[i], vertices[j]));
            }

            List<(int Start, int End)> edges = new();
            float maximumEdgeDistance = minimumDistance * 1.05f;
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = i + 1; j < vertices.Length; j++)
                {
                    if (Vector3.Distance(vertices[i], vertices[j]) <= maximumEdgeDistance)
                        edges.Add((i, j));
                }
            }

            int[,] result = new int[edges.Count, 2];
            for (int i = 0; i < edges.Count; i++)
            {
                result[i, 0] = edges[i].Start;
                result[i, 1] = edges[i].End;
            }

            return result;
        }

        private static Vector2[] ProjectGeometry(Vector2 center, Vector3[] vertices, float radius, Matrix rotation)
        {
            Vector2[] projected = new Vector2[vertices.Length];
            Vector2 projectedCenter = Vector2.Zero;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 normalized = vertices[i];
                normalized.Normalize();
                Vector3 point = Vector3.Transform(normalized * radius, rotation);
                float perspective = 620f / Math.Max(180f, 620f + point.Z);
                projected[i] = center + new Vector2(point.X, point.Y) * perspective;
                projectedCenter += projected[i];
            }

            projectedCenter /= Math.Max(1, projected.Length);
            Vector2 correction = center - projectedCenter;
            for (int i = 0; i < projected.Length; i++)
                projected[i] += correction;

            return projected;
        }

        private static void RotatePlane(ref float a, ref float b, float angle)
        {
            float cosine = (float)Math.Cos(angle);
            float sine = (float)Math.Sin(angle);
            float oldA = a;
            a = oldA * cosine - b * sine;
            b = oldA * sine + b * cosine;
        }

        private static Vector2 EllipsePoint(
            Vector2 center,
            float radiusX,
            float radiusY,
            float angle,
            float rotation,
            float time,
            int layer)
        {
            float interference = 1f + (float)Math.Sin(angle * 6f + time * 3.4f + layer) * 0.025f;
            return center + new Vector2(
                (float)Math.Cos(angle) * radiusX * interference,
                (float)Math.Sin(angle) * radiusY * interference).RotatedBy(rotation);
        }
    }
}
