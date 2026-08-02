using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 战术同步率的头顶「矩阵条」。跟随玩家、常驻显示（手持 M4A1 时），
    /// 分段单元格随同步率点亮、阶段阈值有刻度、满同步时脉冲提示大招就绪。
    /// </summary>
    public class M4A1SyncBarProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int BarWidth = 132;
        private const int BarHeight = 10;
        private const int Segments = 22;
        private const float HeadOffset = -44f;

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || !M4A1Player.Get(Owner).HoldingM4A1)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Top + new Vector2(0f, HeadOffset);
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.14f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.owner != Main.myPlayer)
                return false;

            M4A1Player mp = M4A1Player.Get(Owner);
            float sync = MathHelper.Clamp(mp.SyncRate, 0f, BalanceM4A1.MaxSyncRate);
            float fill = sync / BalanceM4A1.MaxSyncRate;
            int stage = mp.SyncStage;
            float op = Projectile.Opacity;

            float gainFlash = mp.GainFlashTimer / 10f;
            float lossFlash = mp.LossFlashTimer / 14f;
            float fullPulse = stage >= 3 ? 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color stageColor = M4A1Visuals.StageColor(stage);

            Vector2 barTL = new(Owner.Center.X - Main.screenPosition.X - BarWidth / 2f, Owner.Top.Y - Main.screenPosition.Y + HeadOffset);
            int left = (int)barTL.X;
            int top = (int)barTL.Y;

            // ── 外发光边框 ──
            Color border = Color.Lerp(stageColor, Color.White, 0.25f + fullPulse * 0.4f);
            border = Color.Lerp(border, new Color(255, 60, 55), lossFlash);
            border *= op * (0.75f + fullPulse * 0.25f);
            DrawRectBorder(pixel, new Rectangle(left - 2, top - 2, BarWidth + 4, BarHeight + 4), 1, border);

            // ── 内底 ──
            Main.spriteBatch.Draw(pixel, new Rectangle(left, top, BarWidth, BarHeight), new Color(12, 12, 16) * (op * 0.85f));

            // ── 分段单元格 ──
            float cellW = (float)BarWidth / Segments;
            for (int i = 0; i < Segments; i++)
            {
                float cellStart = (float)i / Segments;
                bool lit = cellStart < fill - 0.0001f;
                int cx = left + (int)(i * cellW) + 1;
                int cw = Math.Max(1, (int)cellW - 1);

                Color cell = lit
                    ? Color.Lerp(stageColor, Color.White, gainFlash * 0.6f) * op
                    : new Color(40, 40, 48) * (op * 0.8f);
                Main.spriteBatch.Draw(pixel, new Rectangle(cx, top + 1, cw, BarHeight - 2), cell);
            }

            // ── 阶段阈值刻度（30 / 70）──
            DrawThresholdTick(pixel, left, top, BalanceM4A1.Stage_TacticalLock / 100f, op);
            DrawThresholdTick(pixel, left, top, BalanceM4A1.Stage_CommandOverride / 100f, op);

            // ── 文字：阶段名 + 数值 ──
            string label = M4A1Visuals.StageName(stage);
            if (stage >= 3)
                label = "» " + label + " «";
            float textScale = 0.6f + fullPulse * 0.05f;
            Vector2 size = FontAssets.MouseText.Value.MeasureString(label) * textScale;
            Vector2 labelPos = new(Owner.Center.X - Main.screenPosition.X - size.X / 2f, top - size.Y - 2f);
            Color textColor = Color.Lerp(stageColor, Color.White, 0.3f + fullPulse * 0.4f) * op;
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, label, labelPos, textColor, Color.Black * op, textScale);

            string num = $"{(int)sync}";
            float numScale = 0.55f;
            Vector2 numSize = FontAssets.MouseText.Value.MeasureString(num) * numScale;
            Vector2 numPos = new(left + BarWidth - numSize.X, top + BarHeight + 1f);
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, num, numPos, Color.White * (op * 0.85f), Color.Black * op, numScale);

            return false;
        }

        private void DrawThresholdTick(Texture2D pixel, int left, int top, float t, float op)
        {
            int x = left + (int)(BarWidth * t);
            Main.spriteBatch.Draw(pixel, new Rectangle(x, top - 2, 1, BarHeight + 4), new Color(230, 230, 240) * (op * 0.7f));
        }

        private static void DrawRectBorder(Texture2D pixel, Rectangle r, int thick, Color color)
        {
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, thick), color);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - thick, r.Width, thick), color);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, thick, r.Height), color);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.Right - thick, r.Y, thick, r.Height), color);
        }
    }
}
