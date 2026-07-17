using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.UI
{
    public class CLCBBossHealthBarSystem : ModSystem
    {
        // ─── layout constants ───────────────────────────────────────────────
        private const int PanelW     = 460;
        private const int PanelH     = 56;
        private const int BarOffX    = 10;   // bar track x-inset from panel edge
        private const int BarOffY    = 39;   // bar track y from panel top
        private const int BarH       = 10;
        private const int PanelGap   = 5;    // vertical gap between stacked bars
        private const int MaxBars    = 4;

        // ─── animation constants ────────────────────────────────────────────
        private const int OpenFrames  = 50;
        private const int CloseFrames = 44;
        private const int ComboFrames = 30;

        // ─── data model ─────────────────────────────────────────────────────
        private sealed class BossBar
        {
            public int   NPCIndex;
            public int   NPCType;
            public int   OpenTimer;
            public int   CloseTimer;
            public bool  Closing;
            public float LifeRatio = 1f;
            public int   ComboCountdown;
            public float LifeRatioAtComboStart;

            public float Opacity => Closing
                ? Math.Max(0f, 1f - CloseTimer / (float)CloseFrames)
                : Math.Min(1f, OpenTimer  / (float)OpenFrames);
        }

        private static readonly List<BossBar> Bars = new();

        private static bool IsEnabled => CLCBClientConfig.Instance?.ShowMatrixBossBar ?? true;

        // ─── lifecycle ──────────────────────────────────────────────────────
        public override void OnWorldUnload() => Bars.Clear();

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (!IsEnabled) { Bars.Clear(); return; }
            if (Main.gameMenu) return;

            // Update existing entries
            for (int i = Bars.Count - 1; i >= 0; i--)
            {
                BossBar bar = Bars[i];

                if (!bar.Closing)
                {
                    NPC npc = Main.npc[bar.NPCIndex];
                    bool alive = npc.active && npc.type == bar.NPCType && !npc.friendly && npc.life > 0;

                    if (!alive)
                    {
                        bar.Closing = true;
                    }
                    else
                    {
                        bar.OpenTimer = Math.Min(bar.OpenTimer + 1, OpenFrames);

                        float newRatio = ComputeLifeRatio(npc);
                        if (newRatio < bar.LifeRatio && bar.ComboCountdown <= 0)
                        {
                            bar.LifeRatioAtComboStart = bar.LifeRatio;
                            bar.ComboCountdown = ComboFrames;
                        }
                        bar.LifeRatio = newRatio;
                        if (bar.ComboCountdown > 0) bar.ComboCountdown--;
                    }
                }
                else
                {
                    bar.CloseTimer++;
                    if (bar.CloseTimer >= CloseFrames)
                        Bars.RemoveAt(i);
                }
            }

            // Register new boss NPCs
            if (Bars.Count >= MaxBars) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Bars.Count >= MaxBars) break;

                NPC npc = Main.npc[i];
                if (!npc.active || !npc.boss || npc.realLife >= 0 || npc.friendly || npc.life <= 0)
                    continue;

                bool alreadyTracked = false;
                foreach (BossBar b in Bars)
                {
                    if (b.NPCType == npc.type && !b.Closing)
                    {
                        alreadyTracked = true;
                        break;
                    }
                }
                if (alreadyTracked) continue;

                Bars.Add(new BossBar
                {
                    NPCIndex  = i,
                    NPCType   = npc.type,
                    LifeRatio = ComputeLifeRatio(npc),
                });
            }
        }

        private static float ComputeLifeRatio(NPC primary)
        {
            long life = primary.life;
            long max  = primary.lifeMax;
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC other = Main.npc[j];
                if (!other.active || other.whoAmI == primary.whoAmI) continue;
                if (other.realLife == primary.whoAmI)
                {
                    life += other.life;
                    max  += other.lifeMax;
                }
            }
            return max <= 0 ? 0f : (float)Math.Clamp(life / (double)max, 0.0, 1.0);
        }

        // ─── interface layer ────────────────────────────────────────────────
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx < 0) return;

            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Matrix Boss Bars",
                () => { DrawAllBars(Main.spriteBatch); return true; },
                InterfaceScaleType.None));
        }

        // ─── drawing ────────────────────────────────────────────────────────
        private static void DrawAllBars(SpriteBatch sb)
        {
            if (!IsEnabled || Main.gameMenu) return;

            int drawn = 0;
            foreach (BossBar bar in Bars)
            {
                if (bar.Opacity > 0.002f)
                    DrawBar(sb, bar, drawn++);
            }
        }

        private static void DrawBar(SpriteBatch sb, BossBar bar, int stackIdx)
        {
            float opacity    = bar.Opacity;
            float animRatio  = Math.Min(1f, bar.OpenTimer / (float)OpenFrames);
            int   px         = (Main.screenWidth - PanelW) / 2;
            int   py         = 8 + stackIdx * (PanelH + PanelGap);

            DrawPanel(sb, px, py, opacity);
            DrawBarFill(sb, bar, px, py, opacity, animRatio);

            NPC npc = Main.npc[bar.NPCIndex];
            if (npc.active && npc.type == bar.NPCType)
                DrawTexts(sb, bar, npc, px, py, opacity);
        }

        // ── panel background + border + scanlines ──────────────────────────
        private static void DrawPanel(SpriteBatch sb, int px, int py, float opacity)
        {
            Texture2D pix = TextureAssets.MagicPixel.Value;

            // Dark background
            sb.Draw(pix, new Rectangle(px, py, PanelW, PanelH), new Color(6, 11, 14, 220) * opacity);

            // Outer teal border (2 px)
            DrawBorderRect(sb, px, py, PanelW, PanelH, new Color(70, 255, 188) * opacity, 2);

            // Corner L-bracket accents (inset 4 px from outer border)
            DrawCornerBrackets(sb, px, py, opacity);

            // Scrolling horizontal scanlines
            float t      = Main.GlobalTimeWrappedHourly;
            float scroll = t * 26f % 14f;
            int   innerX = px + 12;
            int   innerW = PanelW - 24;
            for (float ly = py + 10 + scroll; ly < py + PanelH - 10; ly += 14f)
            {
                float distFrac = Math.Abs(ly - (py + PanelH * 0.5f)) / (PanelH * 0.42f);
                float alpha    = Math.Max(0f, 0.055f - distFrac * 0.042f);
                sb.Draw(pix, new Rectangle(innerX, (int)ly, innerW, 1),
                    new Color(0, 200, 120, 200) * (alpha * opacity));
            }
        }

        // L-brackets at all four corners — targeting-reticle matrix aesthetic
        private static void DrawCornerBrackets(SpriteBatch sb, int px, int py, float opacity)
        {
            Texture2D pix  = TextureAssets.MagicPixel.Value;
            Color     col  = new Color(70, 255, 188) * opacity;
            float     t    = Main.GlobalTimeWrappedHourly;
            // subtle pulse
            float pulse = 0.72f + 0.28f * MathF.Sin(t * 2.8f);
            col *= pulse;

            const int arm  = 9;   // arm length
            const int th   = 2;   // arm thickness
            const int inset = 4;  // inset from outer border

            int r = px + PanelW;
            int b = py + PanelH;

            // top-left
            sb.Draw(pix, new Rectangle(px + inset,        py + inset,         arm, th),  col);
            sb.Draw(pix, new Rectangle(px + inset,        py + inset,         th,  arm), col);
            // top-right
            sb.Draw(pix, new Rectangle(r  - inset - arm,  py + inset,         arm, th),  col);
            sb.Draw(pix, new Rectangle(r  - inset - th,   py + inset,         th,  arm), col);
            // bottom-left
            sb.Draw(pix, new Rectangle(px + inset,        b  - inset - th,    arm, th),  col);
            sb.Draw(pix, new Rectangle(px + inset,        b  - inset - arm,   th,  arm), col);
            // bottom-right
            sb.Draw(pix, new Rectangle(r  - inset - arm,  b  - inset - th,    arm, th),  col);
            sb.Draw(pix, new Rectangle(r  - inset - th,   b  - inset - arm,   th,  arm), col);
        }

        // ── health bar fill + combo bar ─────────────────────────────────────
        private static void DrawBarFill(SpriteBatch sb, BossBar bar, int px, int py, float opacity, float animRatio)
        {
            Texture2D pix    = TextureAssets.MagicPixel.Value;
            int       bx     = px + BarOffX;
            int       by     = py + BarOffY;
            int       trackW = PanelW - BarOffX * 2;

            // Track background
            sb.Draw(pix, new Rectangle(bx, by, trackW, BarH), new Color(8, 18, 14, 200) * opacity);

            // Health fill
            int fillW = Math.Max(0, (int)(trackW * animRatio * bar.LifeRatio));
            if (fillW > 0)
            {
                // Body
                sb.Draw(pix, new Rectangle(bx, by, fillW, BarH), new Color(35, 200, 160) * opacity);

                // Bright top edge (1 px shimmer line)
                sb.Draw(pix, new Rectangle(bx, by, fillW, 1),
                    new Color(130, 255, 222, 220) * (0.70f * opacity));

                // Traveling sweep shimmer
                float t      = Main.GlobalTimeWrappedHourly;
                float sweepT = t * 0.55f % 1f;
                int   sweepX = bx + (int)(fillW * sweepT);
                sb.Draw(pix, new Rectangle(sweepX, by, 3, BarH),
                    new Color(160, 255, 230, 200) * (0.50f * opacity));
            }

            // Combo bar (red, to the right of the fill)
            if (bar.ComboCountdown > 0 && bar.LifeRatioAtComboStart > bar.LifeRatio)
            {
                int comboW = (int)(trackW * animRatio * (bar.LifeRatioAtComboStart - bar.LifeRatio));
                if (bar.ComboCountdown < 8)
                    comboW = (int)(comboW * bar.ComboCountdown / 8f);
                comboW = Math.Max(0, comboW);
                if (comboW > 0)
                    sb.Draw(pix, new Rectangle(bx + fillW, by, comboW, BarH),
                        new Color(220, 55, 45) * opacity);
            }

            // Separator line (1 px, above bar track)
            sb.Draw(pix, new Rectangle(bx, by - 4, trackW, 1),
                new Color(70, 255, 188, 180) * (0.62f * opacity));

            // End-cap accent dots (small 3×3 squares at bar track ends)
            Color capCol = new Color(90, 255, 200) * (0.85f * opacity);
            sb.Draw(pix, new Rectangle(bx - 2,         by - 1, 3, BarH + 2), capCol);
            sb.Draw(pix, new Rectangle(bx + trackW - 1, by - 1, 3, BarH + 2), capCol);
        }

        // ── text: %, name, exact HP ──────────────────────────────────────────
        private static void DrawTexts(SpriteBatch sb, BossBar bar, NPC npc, int px, int py, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            // Aggregate total HP (include attached parts via realLife)
            long totalLife = npc.life;
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC other = Main.npc[j];
                if (!other.active || other.whoAmI == npc.whoAmI) continue;
                if (other.realLife == npc.whoAmI)
                    totalLife += other.life;
            }

            string pctStr  = $"{bar.LifeRatio * 100f:0.0}%";
            string nameStr = npc.GivenOrTypeName;
            string hpStr   = totalLife.ToString();

            float textY = py + 11f;

            // HP% — left, teal-green
            ChatManager.DrawColorCodedStringWithShadow(sb, font, pctStr,
                new Vector2(px + 10, textY),
                new Color(90, 255, 180) * opacity,
                0f, Vector2.Zero, Vector2.One * 0.76f);

            // Boss name — horizontally centered, white
            float nameScale = 0.82f;
            Vector2 nameSz  = font.MeasureString(nameStr) * nameScale;
            ChatManager.DrawColorCodedStringWithShadow(sb, font, nameStr,
                new Vector2(px + PanelW * 0.5f - nameSz.X * 0.5f, textY + 0.5f),
                Color.White * opacity,
                0f, Vector2.Zero, Vector2.One * nameScale);

            // Exact HP — right-aligned, dim cyan
            float hpScale  = 0.65f;
            Vector2 hpSz   = font.MeasureString(hpStr) * hpScale;
            ChatManager.DrawColorCodedStringWithShadow(sb, font, hpStr,
                new Vector2(px + PanelW - 10 - hpSz.X, textY + 3f),
                new Color(130, 190, 175) * opacity,
                0f, Vector2.Zero, Vector2.One * hpScale);
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static void DrawBorderRect(SpriteBatch sb, int x, int y, int w, int h, Color col, int t)
        {
            Texture2D pix = TextureAssets.MagicPixel.Value;
            sb.Draw(pix, new Rectangle(x,         y,         w, t), col);
            sb.Draw(pix, new Rectangle(x,         y + h - t, w, t), col);
            sb.Draw(pix, new Rectangle(x,         y,         t, h), col);
            sb.Draw(pix, new Rectangle(x + w - t, y,         t, h), col);
        }
    }
}
