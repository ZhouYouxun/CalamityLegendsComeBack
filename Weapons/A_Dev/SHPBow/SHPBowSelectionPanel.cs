using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowSelectionPanel : ModProjectile, ILocalizedModType
    {
        private const int PanelWidth = 386;
        private const int PanelHeight = 188;
        private const int PanelPadding = 16;
        private const int HeaderHeight = 34;
        private const int ModeSlotWidth = 78;
        private const int ModeSlotHeight = 76;
        private const int ModeSlotGap = 10;
        private const int SequenceSlotWidth = 58;
        private const int SequenceSlotHeight = 42;
        private const int SequenceSlotGap = 12;
        private const int BorderThickness = 2;

        private static readonly SHPBowMode[] Modes =
        {
            SHPBowMode.Pierce,
            SHPBowMode.Ricochet,
            SHPBowMode.Scatter,
            SHPBowMode.Homing
        };

        private readonly int[] clickFeedbackTimers = new int[SHPBowModeHelpers.Count];
        private readonly bool[] hoveredLastFrame = new bool[SHPBowModeHelpers.Count];

        private Vector2 playerOffset;
        private bool offsetInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = PanelWidth;
            Projectile.height = PanelHeight;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem.type != ModContent.ItemType<SHPBow>())
                FadeOut = true;

            if (!offsetInitialized && Main.myPlayer == Projectile.owner)
            {
                playerOffset = Main.MouseWorld - owner.Center;
                offsetInitialized = true;
            }

            Projectile.Center = owner.Center + playerOffset;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.16f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            SHPBowPlayer bowPlayer = owner.GetModPlayer<SHPBowPlayer>();
            Rectangle panelArea = GetPanelArea((owner.Center + playerOffset - Main.screenPosition).Floor());

            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool panelHovered = panelArea.Intersects(MouseRectangle);
            bool hoveringOverModeSlot = false;
            SHPBowMode? appendMode = null;
            SHPBowMode? resetMode = null;

            DrawPanel(panelArea, bowPlayer, Projectile.Opacity);

            for (int i = 0; i < Modes.Length; i++)
            {
                SHPBowMode mode = Modes[i];
                Rectangle slotArea = GetModeSlotArea(panelArea, i);
                bool hovered = slotArea.Intersects(MouseRectangle);
                int coreCount = bowPlayer.CountMode(mode);
                bool selected = coreCount > 0;

                if (hovered)
                {
                    hoveringOverModeSlot = true;
                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.Item55 with { Volume = 0.36f, Pitch = 0.12f }, owner.Center);

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        appendMode = mode;
                        clickFeedbackTimers[i] = 10;
                    }
                    else if (rightClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        resetMode = mode;
                        clickFeedbackTimers[i] = 10;
                    }
                }

                DrawModeSlot(slotArea, mode, coreCount, selected, hovered, clickFeedbackTimers[i], Projectile.Opacity);

                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            DrawSequenceSlots(panelArea, bowPlayer, Projectile.Opacity);

            if (appendMode.HasValue)
            {
                bowPlayer.AppendMode(appendMode.Value);
                string modeName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.SHPBow.ModeName{(int)appendMode.Value}");
                CombatText.NewText(owner.Hitbox, SHPBowModeHelpers.MainColor(appendMode.Value), modeName, dramatic: false, dot: false);
                SoundEngine.PlaySound(SoundID.Item23 with { Volume = 0.55f, Pitch = 0.02f }, owner.Center);
            }
            else if (resetMode.HasValue)
            {
                bowPlayer.ResetSequence(resetMode.Value);
                string modeName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.SHPBow.ModeName{(int)resetMode.Value}");
                CombatText.NewText(owner.Hitbox, SHPBowModeHelpers.AccentColor(resetMode.Value), modeName, dramatic: false, dot: false);
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.58f, Pitch = -0.1f }, owner.Center);
            }
            else if (Projectile.Opacity >= 0.95f && (leftClickPressed && !panelHovered || rightClickPressed && !hoveringOverModeSlot))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f }, owner.Center);
            }

            if (panelHovered)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        private static Rectangle GetPanelArea(Vector2 wantedCenter)
        {
            const int screenMargin = 16;
            int x = (int)(wantedCenter.X - PanelWidth * 0.5f);
            int y = (int)(wantedCenter.Y - PanelHeight * 0.5f);
            int maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            int maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            x = Math.Min(Math.Max(x, screenMargin), maxX);
            y = Math.Min(Math.Max(y, screenMargin), maxY);
            return new Rectangle(x, y, PanelWidth, PanelHeight);
        }

        private static Rectangle GetModeSlotArea(Rectangle panelArea, int index)
        {
            int rowWidth = Modes.Length * ModeSlotWidth + (Modes.Length - 1) * ModeSlotGap;
            int startX = panelArea.X + (panelArea.Width - rowWidth) / 2;
            return new Rectangle(
                startX + index * (ModeSlotWidth + ModeSlotGap),
                panelArea.Y + HeaderHeight + 12,
                ModeSlotWidth,
                ModeSlotHeight);
        }

        private static Rectangle GetSequenceSlotArea(Rectangle panelArea, int index)
        {
            int rowWidth = SHPBowModeHelpers.MaxSequenceLength * SequenceSlotWidth + (SHPBowModeHelpers.MaxSequenceLength - 1) * SequenceSlotGap;
            int startX = panelArea.X + (panelArea.Width - rowWidth) / 2;
            return new Rectangle(
                startX + index * (SequenceSlotWidth + SequenceSlotGap),
                panelArea.Y + PanelHeight - PanelPadding - SequenceSlotHeight,
                SequenceSlotWidth,
                SequenceSlotHeight);
        }

        private static void DrawPanel(Rectangle panelArea, SHPBowPlayer bowPlayer, float opacity)
        {
            SHPBowMode accentMode = bowPlayer.SequenceAccentMode;
            Color mainColor = SHPBowModeHelpers.MainColor(accentMode);
            Color accentColor = SHPBowModeHelpers.AccentColor(accentMode);

            DrawRectangle(panelArea, new Color(8, 10, 15, 240) * opacity);
            DrawBorder(panelArea, Color.Lerp(new Color(96, 106, 126), mainColor, 0.35f) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(28, 34, 46, 220) * opacity, 1);

            Rectangle headerArea = new(panelArea.X + PanelPadding, panelArea.Y + 7, panelArea.Width - PanelPadding * 2, 24);
            DrawTextWithShadow("SHPB CORE STACK", new Vector2(headerArea.X, headerArea.Y - 1), Color.Lerp(Color.White, accentColor, 0.22f) * opacity, 0.62f, opacity);

            string countText = $"{bowPlayer.SequenceLength}/{SHPBowModeHelpers.MaxSequenceLength}";
            Vector2 countSize = FontAssets.MouseText.Value.MeasureString(countText) * 0.58f;
            DrawTextWithShadow(countText, new Vector2(headerArea.Right - countSize.X, headerArea.Y + 1), new Color(178, 204, 228) * opacity, 0.58f, opacity);

            DrawRectangle(new Rectangle(panelArea.X + PanelPadding, panelArea.Y + HeaderHeight, panelArea.Width - PanelPadding * 2, 1), new Color(58, 70, 88, 190) * opacity);

            Rectangle sequenceBack = new(
                panelArea.X + PanelPadding,
                panelArea.Y + PanelHeight - PanelPadding - SequenceSlotHeight - 8,
                panelArea.Width - PanelPadding * 2,
                SequenceSlotHeight + 8);
            DrawRectangle(sequenceBack, new Color(13, 17, 24, 178) * opacity);
            DrawBorder(sequenceBack, new Color(52, 64, 82, 160) * opacity, 1);
        }

        private static void DrawModeSlot(Rectangle slotArea, SHPBowMode mode, int coreCount, bool selected, bool hovered, int clickTimer, float opacity)
        {
            Texture2D iconTexture = ModContent.Request<Texture2D>(SHPBowModeHelpers.IconTexturePath(mode)).Value;
            Color mainColor = SHPBowModeHelpers.MainColor(mode);
            Color accentColor = SHPBowModeHelpers.AccentColor(mode);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + (int)mode);

            Color fill = selected
                ? Color.Lerp(new Color(16, 21, 30), mainColor, 0.18f + pulse * 0.04f)
                : new Color(15, 19, 28);
            Color border = hovered
                ? Color.Lerp(mainColor, Color.White, 0.26f)
                : selected
                    ? Color.Lerp(new Color(72, 84, 104), mainColor, 0.48f)
                    : new Color(56, 64, 78);

            if (clickTimer > 0)
                fill = Color.Lerp(fill, accentColor, 0.18f);

            DrawRectangle(slotArea, fill * (opacity * 0.94f));
            DrawRectangle(new Rectangle(slotArea.X, slotArea.Y, slotArea.Width, 4), Color.Lerp(fill, Color.White, hovered ? 0.34f : 0.18f) * opacity);
            DrawRectangle(new Rectangle(slotArea.X, slotArea.Bottom - 4, slotArea.Width, 4), Color.Lerp(fill, Color.Black, 0.26f) * opacity);
            DrawBorder(slotArea, border * opacity, hovered || clickTimer > 0 ? 2 : 1);

            Rectangle iconArea = new(slotArea.X + 12, slotArea.Y + 8, slotArea.Width - 24, 38);
            Vector2 iconCenter = iconArea.Center.ToVector2();
            float iconScale = GetFitScale(iconTexture, iconArea.Width, iconArea.Height);
            iconScale *= 0.92f + (selected ? 0.08f : 0f) + (hovered ? 0.1f : 0f) + clickTimer * 0.008f;

            DrawIconGlow(iconTexture, iconCenter, mode, iconScale, selected, hovered, clickTimer, opacity);
            Main.EntitySpriteDraw(
                iconTexture,
                iconCenter,
                null,
                (selected ? Color.Lerp(Color.White, accentColor, 0.18f) : Color.White) * opacity,
                selected ? Main.GlobalTimeWrappedHourly * 0.28f : 0f,
                iconTexture.Size() * 0.5f,
                iconScale,
                SpriteEffects.None,
                0);

            string modeName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.SHPBow.ModeName{(int)mode}");
            DrawCenteredTextFitted(modeName, new Rectangle(slotArea.X + 5, slotArea.Bottom - 24, slotArea.Width - 10, 18), Color.Lerp(Color.White, accentColor, selected ? 0.26f : 0.06f), 0.42f, opacity);

            if (coreCount > 0)
                DrawCountPips(slotArea, mode, coreCount, opacity);
        }

        private static void DrawSequenceSlots(Rectangle panelArea, SHPBowPlayer bowPlayer, float opacity)
        {
            for (int i = 0; i < SHPBowModeHelpers.MaxSequenceLength; i++)
            {
                Rectangle slotArea = GetSequenceSlotArea(panelArea, i);
                bool filled = i < bowPlayer.SequenceLength;
                Color border = new Color(58, 70, 90);
                Color fill = new Color(10, 13, 19);

                if (filled)
                {
                    SHPBowMode mode = bowPlayer.GetSequenceMode(i);
                    border = Color.Lerp(border, SHPBowModeHelpers.MainColor(mode), 0.58f);
                    fill = Color.Lerp(fill, SHPBowModeHelpers.MainColor(mode), 0.13f);
                }

                DrawRectangle(slotArea, fill * (opacity * 0.96f));
                DrawRectangle(new Rectangle(slotArea.X, slotArea.Y, slotArea.Width, 3), Color.Lerp(fill, Color.White, 0.18f) * opacity);
                DrawBorder(slotArea, border * opacity, 1);

                DrawCenteredText((i + 1).ToString(), new Rectangle(slotArea.X + 4, slotArea.Y + 4, 12, 12), new Color(142, 156, 176), 0.32f, opacity * 0.78f);

                if (!filled)
                {
                    DrawEmptySlotGlyph(slotArea, opacity);
                    continue;
                }

                SHPBowMode slotMode = bowPlayer.GetSequenceMode(i);
                Texture2D iconTexture = ModContent.Request<Texture2D>(SHPBowModeHelpers.IconTexturePath(slotMode)).Value;
                Vector2 iconCenter = slotArea.Center.ToVector2() + new Vector2(4f, 2f);
                float pulse = 0.94f + 0.06f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f + i);
                float iconScale = GetFitScale(iconTexture, slotArea.Width - 22, slotArea.Height - 12) * pulse;

                Main.EntitySpriteDraw(
                    iconTexture,
                    iconCenter,
                    null,
                    Color.Lerp(SHPBowModeHelpers.MainColor(slotMode), SHPBowModeHelpers.AccentColor(slotMode), 0.28f) * opacity,
                    0f,
                    iconTexture.Size() * 0.5f,
                    iconScale,
                    SpriteEffects.None,
                    0);
            }
        }

        private static void DrawEmptySlotGlyph(Rectangle slotArea, float opacity)
        {
            Rectangle line = new(slotArea.Center.X - 10, slotArea.Center.Y, 20, 2);
            DrawRectangle(line, new Color(58, 66, 78, 190) * opacity);
            DrawRectangle(new Rectangle(line.X + 9, line.Y - 9, 2, 20), new Color(58, 66, 78, 120) * opacity);
        }

        private static void DrawCountPips(Rectangle slotArea, SHPBowMode mode, int count, float opacity)
        {
            Color color = SHPBowModeHelpers.AccentColor(mode);
            int visibleCount = Math.Min(count, SHPBowModeHelpers.MaxSequenceLength);
            for (int i = 0; i < visibleCount; i++)
            {
                Rectangle pip = new(slotArea.Right - 10 - i * 8, slotArea.Y + 7, 5, 5);
                DrawRectangle(pip, color * opacity);
                DrawBorder(pip, SHPBowModeHelpers.MainColor(mode) * opacity, 1);
            }
        }

        private static void DrawIconGlow(Texture2D texture, Vector2 iconCenter, SHPBowMode mode, float scale, bool selected, bool hovered, int clickTimer, float opacity)
        {
            if (!selected && !hovered && clickTimer <= 0)
                return;

            Color mainColor = SHPBowModeHelpers.MainColor(mode);
            Color accentColor = SHPBowModeHelpers.AccentColor(mode);
            float intensity = 0.14f + (selected ? 0.12f : 0f) + (hovered ? 0.22f : 0f) + clickTimer * 0.018f;
            float radius = 1.6f + (hovered ? 1.2f : 0f) + clickTimer * 0.1f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 8; i++)
            {
                float completion = i / 8f;
                Vector2 offset = (MathHelper.TwoPi * completion + Main.GlobalTimeWrappedHourly * 1.2f).ToRotationVector2() * radius;
                Main.EntitySpriteDraw(
                    texture,
                    iconCenter + offset,
                    null,
                    MakeAdditive(Color.Lerp(mainColor, accentColor, completion)) * (opacity * intensity),
                    0f,
                    texture.Size() * 0.5f,
                    scale * 1.04f,
                    SpriteEffects.None,
                    0);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static float GetFitScale(Texture2D texture, int maxWidth, int maxHeight)
        {
            float widthScale = maxWidth / Math.Max(1f, texture.Width);
            float heightScale = maxHeight / Math.Max(1f, texture.Height);
            return Math.Min(widthScale, heightScale);
        }

        private static Color MakeAdditive(Color color)
        {
            color.A = 0;
            return color;
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 position = new(area.Center.X - size.X * 0.5f, area.Center.Y - size.Y * 0.5f);
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
        }

        private static void DrawCenteredTextFitted(string text, Rectangle area, Color color, float desiredScale, float opacity)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
            float scale = size.X > 0f ? Math.Min(desiredScale, area.Width / size.X) : desiredScale;
            scale = Math.Min(scale, area.Height / Math.Max(1f, size.Y));
            Vector2 position = new(area.Center.X - size.X * scale * 0.5f, area.Center.Y - size.Y * scale * 0.5f);
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
        }

        private static void DrawTextWithShadow(string text, Vector2 position, Color color, float scale, float opacity)
        {
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color,
                Color.Black * (0.76f * opacity),
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
            overWiresUI.Add(index);
        }
    }
}
