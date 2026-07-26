using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal sealed class BrinyBaronRightClickDashCooldownBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>().IsCoolingDown;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            BrinyBaronRightClickDashCooldownPlayer dashCooldown = player.GetModPlayer<BrinyBaronRightClickDashCooldownPlayer>();
            if (!dashCooldown.IsCoolingDown)
                return;

            float progress = dashCooldown.CooldownCompletion;
            float time = Main.GlobalTimeWrappedHourly;

            // Position Head Matrix HUD Bar above player head
            Vector2 sc = player.Center - Main.screenPosition + new Vector2(0f, player.gfxOffY - 48f);

            const float halfW = 30f;
            const float halfH = 6f;

            Color oceanBlue = new Color(40, 180, 255);
            Color hydroCyan = new Color(130, 240, 255);
            Color darkBg = new Color(6, 24, 48, 220);

            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 4.2f + player.whoAmI * 0.5f);
            float alpha = 0.85f + 0.15f * pulse;

            Color mainCol = oceanBlue * alpha;
            Color accentCol = hydroCyan * alpha;
            Color brightMain = mainCol * 1.7f;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            List<DrawData> drawDataCache = drawInfo.DrawDataCache;

            Vector2 tl = sc + new Vector2(-halfW, -halfH);
            Vector2 tr = sc + new Vector2(halfW, -halfH);
            Vector2 bl = sc + new Vector2(-halfW, halfH);
            Vector2 br = sc + new Vector2(halfW, halfH);

            // 1. Dark Abyssal Background Box
            Rectangle bgRect = new((int)(sc.X - halfW), (int)(sc.Y - halfH), (int)(halfW * 2f), (int)(halfH * 2f));
            drawDataCache.Add(new DrawData(pixel, bgRect, darkBg * 0.65f));

            // 2. Animated Dashed Edge Borders (Top, Right, Bottom, Left)
            float edgeW = 1.4f;
            DrawDashedEdge(drawDataCache, pixel, tl, tr, mainCol, edgeW, time * 0.55f);
            DrawDashedEdge(drawDataCache, pixel, tr, br, accentCol, edgeW, time * 0.55f + 0.5f);
            DrawDashedEdge(drawDataCache, pixel, br, bl, mainCol, edgeW, time * 0.55f + 1.0f);
            DrawDashedEdge(drawDataCache, pixel, bl, tl, accentCol, edgeW, time * 0.55f + 1.5f);

            // 3. Corner L-Brackets
            float bSize = 6f;
            float bW = 2.2f;
            DrawCornerBracket(drawDataCache, pixel, tl, 1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(drawDataCache, pixel, tr, -1f, 1f, brightMain, bSize, bW);
            DrawCornerBracket(drawDataCache, pixel, bl, 1f, -1f, brightMain, bSize, bW);
            DrawCornerBracket(drawDataCache, pixel, br, -1f, -1f, brightMain, bSize, bW);

            // 4. Corner Cross-Nodes & Mid-Nodes
            float nSize = 4.5f;
            DrawNode(drawDataCache, pixel, tl, brightMain, nSize);
            DrawNode(drawDataCache, pixel, tr, brightMain, nSize);
            DrawNode(drawDataCache, pixel, bl, brightMain, nSize);
            DrawNode(drawDataCache, pixel, br, brightMain, nSize);

            // 5. Internal Matrix Segmented Progress Meter
            float fillWidth = (halfW * 2f - 4f) * progress;
            if (fillWidth > 0f)
            {
                const int totalSegments = 8;
                float segWidth = (halfW * 2f - 4f) / totalSegments;
                float activeSegmentsF = totalSegments * progress;

                for (int i = 0; i < totalSegments; i++)
                {
                    if (i >= activeSegmentsF)
                        break;

                    float segFrac = Math.Min(1f, activeSegmentsF - i);
                    float segX = sc.X - halfW + 2f + i * segWidth;
                    float segW = (segWidth - 1.5f) * segFrac;

                    Color segCol = Color.Lerp(mainCol, accentCol, i / (float)totalSegments) * pulse;

                    Rectangle segRect = new((int)segX, (int)(sc.Y - halfH + 2f), (int)Math.Max(1f, segW), (int)(halfH * 2f - 4f));
                    drawDataCache.Add(new DrawData(pixel, segRect, segCol));
                }

                // Moving Hydro Data Dot traversing along top dashed edge
                float flowT = (time * 2.2f) % 1f;
                float flowX = sc.X - halfW + (halfW * 2f) * flowT;
                DrawNode(drawDataCache, pixel, new Vector2(flowX, sc.Y - halfH), brightMain * 1.8f, 4f);
            }
        }

        private static void DrawDashedEdge(List<DrawData> drawDataCache, Texture2D pixel, Vector2 a, Vector2 b, Color color, float width, float animOffset)
        {
            float totalLen = Vector2.Distance(a, b);
            if (totalLen < 1f) return;

            Vector2 dir = (b - a) / totalLen;
            const float segLen = 8f;
            const float gapLen = 4f;
            const float cycle = segLen + gapLen;
            float phase = (animOffset * 26f) % cycle;

            for (float pos = -phase; pos < totalLen; pos += cycle)
            {
                float s = Math.Max(0f, pos);
                float e = Math.Min(totalLen, pos + segLen);
                if (e <= s) continue;
                DrawLineSegment(drawDataCache, pixel, a + dir * s, a + dir * e, color, width);
            }
        }

        private static void DrawLineSegment(List<DrawData> drawDataCache, Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            if (start == end) return;
            float dist = Vector2.Distance(start, end);
            if (dist <= 0.01f) return;

            float rotation = (end - start).ToRotation();
            Vector2 scale = new(dist, width);
            Vector2 origin = new(0f, 0.5f);

            drawDataCache.Add(new DrawData(
                pixel,
                start,
                null,
                color,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0));
        }

        private static void DrawCornerBracket(List<DrawData> drawDataCache, Texture2D pixel, Vector2 corner, float dx, float dy, Color color, float size, float width)
        {
            DrawLineSegment(drawDataCache, pixel, corner, corner + new Vector2(dx * size, 0f), color, width);
            DrawLineSegment(drawDataCache, pixel, corner, corner + new Vector2(0f, dy * size), color, width);
        }

        private static void AddNode(List<DrawData> drawDataCache, Texture2D pixel, Vector2 pos, Color color, float size)
        {
            DrawNode(drawDataCache, pixel, pos, color, size);
        }

        private static void DrawNode(List<DrawData> drawDataCache, Texture2D pixel, Vector2 pos, Color color, float size)
        {
            int w = Math.Max(1, (int)size);
            drawDataCache.Add(new DrawData(pixel, new Rectangle((int)(pos.X - w * 0.5f), (int)(pos.Y - 1f), w, 2), color));
            drawDataCache.Add(new DrawData(pixel, new Rectangle((int)(pos.X - 1f), (int)(pos.Y - w * 0.5f), 2, w), color));
        }
    }
}
