using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
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
        private const int SlotWidth = 44;
        private const int SlotHeight = 38;
        private const int SlotGap = 4;
        private const int PanelPadding = 5;
        private const int FeedLipHeight = 7;
        private const int PanelRightOffset = 76;
        private const int ScreenMargin = 12;
        private const float MaxIconDrawSize = 24f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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

            int slotCount = weapon.GetActiveMagazineCount(owner);
            if (slotCount <= 0)
                return false;

            int columns = slotCount > 4 ? 2 : 1;
            int rows = (slotCount + columns - 1) / columns;
            int panelWidth = PanelPadding * 2 + columns * SlotWidth + (columns - 1) * SlotGap;
            int panelHeight = PanelPadding * 2 + FeedLipHeight + rows * SlotHeight + (rows - 1) * SlotGap;
            Rectangle panelArea = GetPanelArea(owner, panelWidth, panelHeight);
            float opacity = Projectile.Opacity;

            NewLegendSHPC.SHPCMagazineSlot selectedSlot = weapon.GetMagazineSlot(weapon.CurrentMagazineIndex, owner);
            Color themeColor = selectedSlot.HasAmmo ? SHPCAmmoSelectionPanel.GetEffectColor(selectedSlot.EffectID) : new Color(112, 154, 190);

            DrawMagazineBody(panelArea, themeColor, opacity);

            for (int i = 0; i < slotCount; i++)
            {
                NewLegendSHPC.SHPCMagazineSlot slot = weapon.GetMagazineSlot(i, owner);
                int row = i / columns;
                int column = i % columns;
                Rectangle slotArea = new(
                    panelArea.X + PanelPadding + column * (SlotWidth + SlotGap),
                    panelArea.Y + PanelPadding + FeedLipHeight + row * (SlotHeight + SlotGap),
                    SlotWidth,
                    SlotHeight);

                DrawMagazineSlot(slot, owner, slotArea, opacity);
            }

            return false;
        }

        private static Rectangle GetPanelArea(Player owner, int panelWidth, int panelHeight)
        {
            Vector2 ownerScreen = owner.Center - Main.screenPosition + new Vector2(0f, owner.gfxOffY - 8f);
            int x = (int)(ownerScreen.X - PanelRightOffset - panelWidth);
            int y = (int)(ownerScreen.Y - panelHeight * 0.5f);

            x = Utils.Clamp(x, ScreenMargin, Main.screenWidth - panelWidth - ScreenMargin);
            y = Utils.Clamp(y, ScreenMargin, Main.screenHeight - panelHeight - ScreenMargin);
            return new Rectangle(x, y, panelWidth, panelHeight);
        }

        private static void DrawMagazineBody(Rectangle area, Color themeColor, float opacity)
        {
            Color body = Color.Lerp(new Color(9, 11, 16), themeColor, 0.08f);
            Color edge = Color.Lerp(new Color(74, 86, 104), themeColor, 0.36f);
            Color lip = Color.Lerp(new Color(38, 45, 56), themeColor, 0.24f);

            DrawRectangle(area, body * (0.82f * opacity));
            DrawBorder(area, edge * (0.82f * opacity), 1);
            DrawBorder(new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), Color.Black * (0.32f * opacity), 1);

            Rectangle feedLip = new(area.X + 8, area.Y + 3, area.Width - 16, 3);
            DrawRectangle(feedLip, lip * (0.8f * opacity));
            DrawRectangle(new Rectangle(area.X + 5, area.Bottom - 3, area.Width - 10, 2), edge * (0.48f * opacity));
        }

        private static void DrawMagazineSlot(NewLegendSHPC.SHPCMagazineSlot slot, Player owner, Rectangle slotArea, float opacity)
        {
            Color effectColor = slot.HasAmmo ? SHPCAmmoSelectionPanel.GetEffectColor(slot.EffectID) : new Color(82, 88, 102);
            Color back = Color.Lerp(new Color(18, 21, 28), effectColor, slot.HasAmmo ? 0.16f : 0.04f);
            Color border = Color.Lerp(new Color(88, 102, 124), effectColor, slot.HasAmmo ? 0.44f : 0.12f);
            float selectedPulse = slot.Selected ? 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.5f) : 0f;

            if (slot.Selected)
            {
                back = Color.Lerp(back, effectColor, 0.16f + selectedPulse * 0.06f);
                border = Color.Lerp(border, Color.White, 0.28f + selectedPulse * 0.18f);
                DrawRectangle(new Rectangle(slotArea.X - 3, slotArea.Y + 5, 2, slotArea.Height - 10), effectColor * (0.88f * opacity));
            }

            DrawRectangle(slotArea, back * (0.9f * opacity));
            DrawBorder(slotArea, border * opacity, slot.Selected ? 2 : 1);

            Rectangle innerArea = new(slotArea.X + 5, slotArea.Y + 5, slotArea.Width - 10, slotArea.Height - 12);
            DrawRectangle(innerArea, Color.Lerp(new Color(6, 8, 12), effectColor, 0.08f) * (0.82f * opacity));
            DrawBorder(innerArea, border * (0.42f * opacity), 1);

            if (!slot.HasAmmo)
            {
                DrawCenteredText((slot.Index + 1).ToString(), innerArea, Color.Gray * opacity, 0.58f);
                return;
            }

            Texture2D texture = SHPCAmmoSelectionPanel.TryGetAmmoTexture(slot.EffectID, slot.AmmoType);
            if (texture != null)
            {
                Rectangle source = SHPCAmmoSelectionPanel.GetCurrentFrame(texture, SHPCAmmoSelectionPanel.GetFrameCount(slot.EffectID));
                Vector2 sourceSize = source.Size();
                float fitScale = Math.Min(MaxIconDrawSize / Math.Max(1f, sourceSize.X), MaxIconDrawSize / Math.Max(1f, sourceSize.Y));
                float selectedScale = slot.Selected ? 1.07f + selectedPulse * 0.03f : 1f;
                Vector2 iconCenter = innerArea.Center.ToVector2() - new Vector2(0f, 1f);

                Main.EntitySpriteDraw(
                    texture,
                    iconCenter,
                    source,
                    Color.Lerp(Color.White, effectColor, 0.1f) * opacity,
                    slot.Selected ? 0.02f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f) : 0f,
                    sourceSize * 0.5f,
                    fitScale * selectedScale,
                    SpriteEffects.None,
                    0f);
            }

            DrawAmmoBar(slot, owner, slotArea, effectColor, opacity);
            DrawPowerText(slot.Power.ToString(), slotArea, opacity);
        }

        private static void DrawAmmoBar(NewLegendSHPC.SHPCMagazineSlot slot, Player owner, Rectangle slotArea, Color effectColor, float opacity)
        {
            int capacity = NewLegendSHPC.GetAdjustedAmmoCapacity(owner, slot.EffectID);
            float fillRatio = capacity <= 0 ? 0f : MathHelper.Clamp(slot.Power / (float)capacity, 0f, 1f);
            Rectangle barBack = new(slotArea.X + 5, slotArea.Bottom - 6, slotArea.Width - 10, 3);
            Rectangle barFill = new(barBack.X, barBack.Y, (int)(barBack.Width * fillRatio), barBack.Height);

            DrawRectangle(barBack, Color.Black * (0.5f * opacity));
            if (barFill.Width > 0)
                DrawRectangle(barFill, effectColor * (0.86f * opacity));
        }

        private static void DrawPowerText(string text, Rectangle slotArea, float opacity)
        {
            float scale = text.Length >= 3 ? 0.38f : 0.44f;
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 position = new(slotArea.Right - textSize.X - 4f, slotArea.Bottom - textSize.Y - 5f);

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                Color.White * opacity,
                Color.Black * opacity,
                scale);
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 position = area.Center.ToVector2() - size * 0.5f;

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color,
                Color.Black * (color.A / 255f),
                scale);
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
