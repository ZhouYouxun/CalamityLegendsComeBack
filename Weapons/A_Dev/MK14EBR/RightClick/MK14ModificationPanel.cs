using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14ModificationPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int PanelPadding = 16;
        private const int BorderThickness = 2;
        private const int ButtonHeight = 38;
        private const int ButtonGap = 6;
        private const int OptionHeight = 34;
        private const float OptionTextScale = 0.58f;
        private const float EffectTextScale = 0.95f;

        private static readonly MK14AttachmentSlot[] SlotOrder =
        {
            MK14AttachmentSlot.Barrel,
            MK14AttachmentSlot.Muzzle,
            MK14AttachmentSlot.Underbarrel,
            MK14AttachmentSlot.Stock,
            MK14AttachmentSlot.Sight
        };

        private readonly bool[] hoveredButtonsLastFrame = new bool[SlotOrder.Length];
        private readonly bool[] hoveredOptionsLastFrame = new bool[6];
        private readonly int[] clickFeedbackTimers = new int[SlotOrder.Length];

        private int openSlot = -1;

        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);
        private bool InputReady => Projectile.localAI[0] > 8f && Projectile.Opacity >= 0.92f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 640;
            Projectile.height = 360;
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

            if (owner.HeldItem.ModItem is not NewLegendMK14EBR)
                FadeOut = true;

            Rectangle panelArea = GetPanelArea();
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelArea.Center.ToVector2() : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.localAI[0]++;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem.ModItem is not NewLegendMK14EBR weapon)
                return false;

            Rectangle panelArea = GetPanelArea();
            bool leftClickPressed = InputReady && Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = InputReady && Main.mouseRight && Main.mouseRightRelease;
            bool consumedLeftClick = false;

            DrawPanel(panelArea, Projectile.Opacity);
            DrawHeader(panelArea, weapon);
            DrawGunPreview(panelArea, weapon);
            DrawEffectsList(panelArea, weapon);
            DrawButtons(panelArea, weapon, leftClickPressed, ref consumedLeftClick);

            if (openSlot >= 0 && openSlot < SlotOrder.Length)
                DrawDropdown(panelArea, weapon, SlotOrder[openSlot], GetButtonArea(panelArea, openSlot), leftClickPressed, ref consumedLeftClick);

            if (rightClickPressed)
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.55f, Pitch = 0.04f }, owner.Center);
            }
            else if (leftClickPressed && !consumedLeftClick && !panelArea.Intersects(MouseRectangle))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.48f, Pitch = 0.02f }, owner.Center);
            }

            if (Projectile.Opacity > 0.08f)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public static void RequestClose(Projectile projectile)
        {
            if (projectile.ModProjectile is MK14ModificationPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static Rectangle GetPanelArea()
        {
            int maxWidth = Math.Min(Main.screenWidth - 80, 1140);
            int maxHeight = Math.Min(Main.screenHeight - 80, 650);
            int minWidth = Math.Min(760, maxWidth);
            int minHeight = Math.Min(480, maxHeight);
            int width = Utils.Clamp((int)(Main.screenWidth * 0.495f), minWidth, maxWidth);
            int height = Utils.Clamp((int)(Main.screenHeight * 0.495f), minHeight, maxHeight);
            return new Rectangle((Main.screenWidth - width) / 2, (Main.screenHeight - height) / 2, width, height);
        }

        private void DrawHeader(Rectangle panelArea, NewLegendMK14EBR weapon)
        {
            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.MK14EBR.UI.Title");
            string stage = BalanceMK14EBR.GetLocalizedStageName(new BalanceMK14EBR().GetCompletedStageIndex());
            DrawText(title, new Vector2(panelArea.X + PanelPadding, panelArea.Y + 10f), new Color(225, 238, 255) * Projectile.Opacity, 0.82f);
            Vector2 stageSize = FontAssets.MouseText.Value.MeasureString(stage) * 0.62f;
            DrawText(stage, new Vector2(panelArea.Right - PanelPadding - stageSize.X, panelArea.Y + 12f), new Color(150, 210, 255) * Projectile.Opacity, 0.62f);
        }

        private void DrawGunPreview(Rectangle panelArea, NewLegendMK14EBR weapon)
        {
            Vector2 gunCenter = new(panelArea.X + panelArea.Width * 0.36f, panelArea.Y + panelArea.Height * 0.36f);
            float fitScale = Math.Min(panelArea.Width * 0.52f / 186f, panelArea.Height * 0.22f / 48f);
            fitScale = MathHelper.Clamp(fitScale, 1.65f, 2.85f);

            Rectangle rail = new(
                (int)(gunCenter.X - 186f * fitScale * 0.5f) - 8,
                (int)(gunCenter.Y + 36f),
                (int)(186f * fitScale) + 16,
                2);
            DrawRectangle(rail, new Color(80, 112, 140) * (Projectile.Opacity * 0.42f));

            MK14TextureComposer.DrawComposite(
                Main.spriteBatch,
                weapon,
                gunCenter,
                Color.White * Projectile.Opacity,
                0f,
                fitScale,
                SpriteEffects.None);
        }

        private void DrawEffectsList(Rectangle panelArea, NewLegendMK14EBR weapon)
        {
            Rectangle listArea = new(
                panelArea.X + (int)(panelArea.Width * 0.62f),
                panelArea.Y + 54,
                panelArea.Width - (int)(panelArea.Width * 0.62f) - PanelPadding,
                panelArea.Height - 126);

            DrawRectangle(listArea, new Color(10, 14, 20, 208) * (Projectile.Opacity * 0.86f));
            DrawBorder(listArea, new Color(74, 104, 132) * Projectile.Opacity, 1);

            string header = Language.GetTextValue("Mods.CalamityLegendsComeBack.MK14EBR.UI.EffectHeader");
            DrawText(header, new Vector2(listArea.X + 8f, listArea.Y + 8f), new Color(255, 232, 170) * Projectile.Opacity, 0.62f);

            float y = listArea.Y + 30f;
            foreach (MK14AttachmentDefinition definition in weapon.GetSelectedDefinitions())
            {
                if (definition == null)
                    continue;

                string name = Language.GetTextValue(definition.NameKey);
                string effect = Language.GetTextValue(definition.EffectKey);
                foreach (string line in WrapText($"{name}: {effect}", listArea.Width - 16f, EffectTextScale))
                {
                    if (y + 30f > listArea.Bottom - 8f)
                    {
                        DrawText("...", new Vector2(listArea.X + 8f, y), Color.White * Projectile.Opacity, EffectTextScale);
                        return;
                    }

                    DrawText(line, new Vector2(listArea.X + 8f, y), Color.White * Projectile.Opacity, EffectTextScale);
                    y += 30f;
                }

                y += 6f;
            }
        }

        private void DrawButtons(Rectangle panelArea, NewLegendMK14EBR weapon, bool leftClickPressed, ref bool consumedLeftClick)
        {
            for (int i = 0; i < SlotOrder.Length; i++)
            {
                Rectangle buttonArea = GetButtonArea(panelArea, i);
                MK14AttachmentSlot slot = SlotOrder[i];
                bool hovered = buttonArea.Intersects(MouseRectangle);
                bool open = openSlot == i;
                bool clicked = false;

                if (hovered)
                {
                    Main.hoverItemName = GetCategoryName(slot);
                    if (!hoveredButtonsLastFrame[i] && InputReady)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.36f, Pitch = 0.12f }, Main.LocalPlayer.Center);

                    if (leftClickPressed)
                    {
                        openSlot = open ? -1 : i;
                        clickFeedbackTimers[i] = 8;
                        consumedLeftClick = true;
                        clicked = true;
                        SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.48f, Pitch = 0.1f + i * 0.03f }, Main.LocalPlayer.Center);
                    }
                }

                DrawButton(buttonArea, GetCategoryName(slot), open, hovered, clickFeedbackTimers[i], Projectile.Opacity);

                hoveredButtonsLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0 && !clicked)
                    clickFeedbackTimers[i]--;
            }
        }

        private void DrawDropdown(Rectangle panelArea, NewLegendMK14EBR weapon, MK14AttachmentSlot slot, Rectangle buttonArea, bool leftClickPressed, ref bool consumedLeftClick)
        {
            MK14AttachmentDefinition[] entries = MK14AttachmentDatabase.GetEntries(slot);
            int dropdownHeight = entries.Length * OptionHeight;
            Rectangle dropdownArea = new(buttonArea.X, buttonArea.Y - dropdownHeight - 6, buttonArea.Width, dropdownHeight);

            if (dropdownArea.Y < panelArea.Y + 42)
                dropdownArea.Y = buttonArea.Bottom + 6;

            DrawRectangle(dropdownArea, new Color(12, 18, 26, 240) * Projectile.Opacity);
            DrawBorder(dropdownArea, new Color(94, 130, 162) * Projectile.Opacity, 1);

            for (int i = 0; i < entries.Length; i++)
            {
                MK14AttachmentDefinition entry = entries[i];
                Rectangle optionArea = new(dropdownArea.X, dropdownArea.Y + i * OptionHeight, dropdownArea.Width, OptionHeight);
                bool hovered = optionArea.Intersects(MouseRectangle);
                bool selected = weapon.GetSelectedValue(slot) == entry.Value;
                bool unlocked = MK14AttachmentDatabase.IsUnlocked(entry);

                if (hovered)
                {
                    Main.hoverItemName = GetHoverText(entry, unlocked);
                    if (!hoveredOptionsLastFrame[i] && InputReady)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.34f, Pitch = 0.2f }, Main.LocalPlayer.Center);

                    if (leftClickPressed)
                    {
                        consumedLeftClick = true;
                        if (unlocked && weapon.TrySetAttachment(slot, entry.Value))
                        {
                            openSlot = -1;
                            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.58f, Pitch = 0.08f + i * 0.02f }, Main.LocalPlayer.Center);
                        }
                        else
                            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.38f, Pitch = -0.18f }, Main.LocalPlayer.Center);
                    }
                }

                DrawOption(optionArea, weapon, slot, entry, selected, unlocked, hovered);
                hoveredOptionsLastFrame[i] = hovered;
            }
        }

        private Rectangle GetButtonArea(Rectangle panelArea, int index)
        {
            int totalGap = (SlotOrder.Length - 1) * ButtonGap;
            int buttonWidth = (panelArea.Width - PanelPadding * 2 - totalGap) / SlotOrder.Length;
            int x = panelArea.X + PanelPadding + index * (buttonWidth + ButtonGap);
            int y = panelArea.Bottom - PanelPadding - ButtonHeight;
            return new Rectangle(x, y, buttonWidth, ButtonHeight);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(8, 11, 16, 238) * opacity);
            DrawBorder(panelArea, new Color(82, 106, 128) * opacity, BorderThickness);

            Rectangle inner = new(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6);
            DrawBorder(inner, new Color(24, 36, 50, 220) * opacity, 1);
        }

        private static void DrawButton(Rectangle area, string text, bool open, bool hovered, int clickTimer, float opacity)
        {
            Color back = open ? new Color(38, 64, 86) : new Color(24, 31, 42);
            Color border = open ? new Color(140, 210, 255) : new Color(90, 112, 136);

            if (hovered)
            {
                back = Color.Lerp(back, new Color(70, 88, 108), 0.48f);
                border = Color.Lerp(border, Color.White, 0.32f);
            }

            if (clickTimer > 0)
            {
                back = Color.Lerp(back, new Color(124, 104, 54), 0.34f);
                border = Color.Lerp(border, new Color(255, 226, 142), 0.5f);
            }

            DrawRectangle(area, back * (opacity * 0.95f));
            DrawBorder(area, border * opacity, 1);
            DrawTextCentered(text, area, Color.White * opacity, 0.58f);
        }

        private void DrawOption(Rectangle area, NewLegendMK14EBR weapon, MK14AttachmentSlot slot, MK14AttachmentDefinition entry, bool selected, bool unlocked, bool hovered)
        {
            Color back = selected ? new Color(38, 58, 74) : new Color(18, 24, 32);
            Color border = selected ? new Color(126, 196, 244) : new Color(64, 82, 104);
            if (!unlocked)
            {
                back = new Color(30, 30, 34);
                border = new Color(70, 70, 78);
            }

            if (hovered)
            {
                back = Color.Lerp(back, new Color(74, 82, 96), 0.45f);
                border = Color.Lerp(border, Color.White, 0.3f);
            }

            DrawRectangle(area, back * (Projectile.Opacity * 0.96f));
            DrawBorder(area, border * Projectile.Opacity, 1);

            string previewPath = GetPreviewPath(weapon, slot, entry.Value);
            if (!string.IsNullOrEmpty(previewPath))
                DrawPreviewIcon(previewPath, new Rectangle(area.X + 4, area.Y + 4, 38, area.Height - 8), unlocked ? Color.White : new Color(110, 110, 116));

            string name = Language.GetTextValue(entry.NameKey);
            Color textColor = unlocked ? Color.White : new Color(155, 155, 160);
            DrawText(FitText(name, area.Width - 48f, OptionTextScale), new Vector2(area.X + 46f, area.Y + 7f), textColor * Projectile.Opacity, OptionTextScale);
        }

        private static string GetPreviewPath(NewLegendMK14EBR weapon, MK14AttachmentSlot slot, int value)
        {
            return slot switch
            {
                MK14AttachmentSlot.Barrel => MK14TextureComposer.BarrelPath((MK14Barrel)value, weapon.Muzzle),
                MK14AttachmentSlot.Muzzle => MK14TextureComposer.BarrelPath(weapon.Barrel, (MK14Muzzle)value),
                MK14AttachmentSlot.Underbarrel => MK14TextureComposer.UnderbarrelPath((MK14Underbarrel)value),
                MK14AttachmentSlot.Stock => MK14TextureComposer.StockPath((MK14Stock)value),
                MK14AttachmentSlot.Sight => MK14TextureComposer.SightPath((MK14Sight)value),
                _ => null
            };
        }

        private void DrawPreviewIcon(string path, Rectangle area, Color color)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            Vector2 sourceSize = new(texture.Width, texture.Height);
            float scale = Math.Min(area.Width / Math.Max(1f, sourceSize.X), area.Height / Math.Max(1f, sourceSize.Y));
            Main.EntitySpriteDraw(texture, area.Center.ToVector2(), null, color * Projectile.Opacity, 0f, sourceSize * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private string GetHoverText(MK14AttachmentDefinition entry, bool unlocked)
        {
            string name = Language.GetTextValue(entry.NameKey);
            string effect = Language.GetTextValue(entry.EffectKey);

            if (!unlocked)
            {
                string unlockStage = BalanceMK14EBR.GetLocalizedStageName(entry.UnlockStage);
                string lockedFormat = Language.GetTextValue("Mods.CalamityLegendsComeBack.MK14EBR.UI.LockedWithStage");
                return string.Format(lockedFormat, name, unlockStage, effect);
            }

            return $"{name} - {effect}";
        }

        private static string GetCategoryName(MK14AttachmentSlot slot)
        {
            return Language.GetTextValue($"Mods.CalamityLegendsComeBack.MK14EBR.UI.Category.{slot}");
        }

        private static string FitText(string text, float maxWidth, float scale)
        {
            var font = FontAssets.MouseText.Value;
            if (font.MeasureString(text).X * scale <= maxWidth)
                return text;

            const string suffix = "...";
            int length = text.Length;
            while (length > 0)
            {
                string candidate = text.Substring(0, length) + suffix;
                if (font.MeasureString(candidate).X * scale <= maxWidth)
                    return candidate;

                length--;
            }

            return suffix;
        }

        private static List<string> WrapText(string text, float maxWidth, float scale)
        {
            List<string> lines = new();
            if (string.IsNullOrEmpty(text))
                return lines;

            var font = FontAssets.MouseText.Value;
            string current = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '\n')
                {
                    if (current.Length > 0)
                        lines.Add(current);

                    current = string.Empty;
                    continue;
                }

                string candidate = current + character;
                if (current.Length == 0 || font.MeasureString(candidate).X * scale <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = character.ToString();
            }

            if (current.Length > 0)
                lines.Add(current);

            return lines;
        }

        private static void DrawText(string text, Vector2 position, Color color, float scale)
        {
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, text, position, color, Color.Black * (color.A / 255f), scale);
        }

        private static void DrawTextCentered(string text, Rectangle area, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            DrawText(text, area.Center.ToVector2() - size * 0.5f, color, scale);
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
