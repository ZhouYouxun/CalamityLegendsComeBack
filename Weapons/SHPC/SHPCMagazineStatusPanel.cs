using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal sealed class SHPCMagazineStatusPanel : ModProjectile, IScreenOverlayProjectile
    {
        // 和青霆剑地剑矩阵保持相近占地：整体约 24x55~70 像素，只做轻量弹夹状态提示。
        private const int MagazineWidth = 24;
        private const int MagazinePadding = 5;
        private const int FeedLipHeight = 5;
        private const int BulletWidth = 12;
        private const int BulletHeight = 5;
        private const int BulletGap = 3;
        private const int CenterLeftOffset = 92;
        private const int ScreenMargin = 12;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 0 = 正常，1 = 完全悬停透明。指数lerp产生非线性过渡。
        private float _hoverFade;

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            FadeOut = owner.HeldItem?.ModItem is not NewLegendSHPC;
            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.12f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem?.ModItem is not NewLegendSHPC weapon)
                return false;

            int bulletCount = weapon.GetActiveMagazineCount(owner);
            if (bulletCount <= 0)
                return false;

            int panelHeight = MagazinePadding * 2 + FeedLipHeight + bulletCount * BulletHeight + Math.Max(0, bulletCount - 1) * BulletGap;
            Rectangle panelArea = GetPanelArea(owner, MagazineWidth, panelHeight);
            float opacity = Projectile.Opacity;

            // 鼠标悬停检测 → 非线性过渡（指数 lerp：进快 0.18，退慢 0.10）
            bool hovered = panelArea.Contains(Main.MouseScreen.ToPoint());
            _hoverFade += ((hovered ? 1f : 0f) - _hoverFade) * (hovered ? 0.18f : 0.10f);
            float effectiveOpacity = opacity * (1f - _hoverFade * 0.90f);

            NewLegendSHPC.SHPCMagazineSlot selectedSlot = weapon.GetMagazineSlot(weapon.CurrentMagazineIndex, owner);
            Color themeColor = GetSlotThemeColor(selectedSlot);

            DrawMagazineBody(panelArea, themeColor, effectiveOpacity);

            int firstBulletY = panelArea.Y + MagazinePadding + FeedLipHeight;
            for (int i = 0; i < bulletCount; i++)
            {
                NewLegendSHPC.SHPCMagazineSlot slot = weapon.GetMagazineSlot(i, owner);
                Rectangle bulletArea = new(
                    panelArea.X + (panelArea.Width - BulletWidth) / 2,
                    firstBulletY + i * (BulletHeight + BulletGap),
                    BulletWidth,
                    BulletHeight);

                DrawBullet(slot, bulletArea, effectiveOpacity);
            }

            return false;
        }

        private static Rectangle GetPanelArea(Player owner, int panelWidth, int panelHeight)
        {
            Vector2 panelCenter = owner.Center - Main.screenPosition + new Vector2(-CenterLeftOffset, owner.gfxOffY - 8f);
            int x = (int)(panelCenter.X - panelWidth * 0.5f);
            int y = (int)(panelCenter.Y - panelHeight * 0.5f);

            x = Utils.Clamp(x, ScreenMargin, Main.screenWidth - panelWidth - ScreenMargin);
            y = Utils.Clamp(y, ScreenMargin, Main.screenHeight - panelHeight - ScreenMargin);
            return new Rectangle(x, y, panelWidth, panelHeight);
        }

        private static void DrawMagazineBody(Rectangle area, Color themeColor, float opacity)
        {
            Color body = Color.Lerp(new Color(8, 12, 18), themeColor, 0.08f);
            Color edge = Color.Lerp(new Color(68, 84, 104), themeColor, 0.35f);
            Color lip = Color.Lerp(new Color(36, 45, 56), themeColor, 0.22f);

            DrawRectangle(area, body * (0.78f * opacity));
            DrawBorder(area, edge * (0.82f * opacity), 1);
            DrawBorder(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), Color.Black * (0.28f * opacity), 1);

            DrawRectangle(new Rectangle(area.X + 7, area.Y + 3, area.Width - 14, 2), lip * (0.84f * opacity));
            DrawRectangle(new Rectangle(area.X + 5, area.Bottom - 3, area.Width - 10, 2), edge * (0.45f * opacity));
        }

        private static void DrawBullet(NewLegendSHPC.SHPCMagazineSlot slot, Rectangle area, float opacity)
        {
            Color themeColor = GetSlotThemeColor(slot);
            Color emptyFill = new(30, 36, 46, 210);
            Color emptyEdge = new(74, 88, 108, 170);
            Color fill = slot.HasAmmo ? Color.Lerp(themeColor, Color.White, 0.08f) : emptyFill;
            Color edge = slot.IsConfigured ? Color.Lerp(themeColor, Color.White, slot.HasAmmo ? 0.24f : 0.04f) : emptyEdge;

            float selectedPulse = slot.Selected ? 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.2f) : 0f;
            if (slot.Selected)
            {
                Rectangle glowArea = new(area.X - 3, area.Y - 2, area.Width + 6, area.Height + 4);
                DrawBorder(glowArea, Color.Lerp(themeColor, Color.White, 0.35f + selectedPulse * 0.25f) * (0.66f * opacity), 1);
                DrawRectangle(new Rectangle(area.X - 5, area.Y + 1, 2, area.Height - 2), themeColor * (0.85f * opacity));
            }

            Rectangle casing = new(area.X, area.Y, area.Width - 3, area.Height);
            Rectangle nose = new(area.Right - 3, area.Y + 1, 2, area.Height - 2);
            Rectangle tip = new(area.Right - 1, area.Y + 2, 1, Math.Max(1, area.Height - 4));

            DrawRectangle(casing, fill * (0.86f * opacity));
            DrawRectangle(nose, Color.Lerp(fill, Color.White, slot.HasAmmo ? 0.18f : 0.05f) * (0.82f * opacity));
            DrawRectangle(tip, edge * (0.8f * opacity));
            DrawBorder(area, edge * (0.42f * opacity), 1);
        }

        private static Color GetSlotThemeColor(NewLegendSHPC.SHPCMagazineSlot slot)
        {
            if (!slot.IsConfigured)
                return new Color(82, 92, 108);

            return SHPCAmmoSelectionPanel.GetEffectColor(slot.EffectID);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
