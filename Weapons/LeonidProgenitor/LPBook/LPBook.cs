using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.LPBook
{
    public class LPBook : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/SHPCBook/SHPCBook";
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<LPBookPanel>();

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = PanelType;
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.value = Item.sellPrice(gold: 25);
            Item.rare = ItemRarityID.Cyan;
        }

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (TryCloseExistingPanel(player))
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return false;
            }

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, PanelType, 0, 0f, player.whoAmI);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.38f, Pitch = 0.16f }, player.Center);
            return false;
        }

        private static bool TryCloseExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                LPBookPanel.RequestClose(projectile);
                return true;
            }

            return false;
        }
    }

    internal sealed class LPBookPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int Columns = 3;
        private const int SlotWidth = 206;
        private const int SlotHeight = 48;
        private const int SlotGap = 6;
        private const int PanelPadding = 14;
        private const int DetailGap = 9;
        private const int DetailWidth = 292;
        private const int DetailHeight = 164;
        private const int BorderThickness = 3;
        private const float MaxIconDrawSize = 36f;

        private static LeonidMetalEntry[] cachedEntries;

        private int selectedIndex = -1;
        private int[] clickFeedbackTimers = Array.Empty<int>();
        private bool[] hoveredLastFrame = Array.Empty<bool>();
        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int PanelWidth => PanelPadding * 2 + Columns * SlotWidth + (Columns - 1) * SlotGap;
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = PanelWidth;
            Projectile.height = 620;
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

            if (owner.HeldItem.type != ModContent.ItemType<LPBook>())
                FadeOut = true;

            LeonidMetalEntry[] entries = GetEntries();
            int panelHeight = GetPanelHeight(entries.Length);
            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = GetClampedPanelTopLeftFromCenter(Main.MouseScreen, panelHeight);
                panelPositionInitialized = true;
            }

            Vector2 panelCenter = panelTopLeft + new Vector2(PanelWidth, panelHeight) * 0.5f;
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelCenter : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            LeonidMetalEntry[] entries = GetEntries();
            EnsureStateSize(entries.Length);

            if (selectedIndex >= entries.Length)
                selectedIndex = -1;

            int panelHeight = GetPanelHeight(entries.Length);
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, panelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            int clickedIndex = -1;

            DrawPanel(panelArea, Projectile.Opacity);

            if (entries.Length == 0)
            {
                DrawFitText(
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.LPBookNoRecords"),
                    new Rectangle(panelArea.X + 16, panelArea.Y + 16, panelArea.Width - 32, panelArea.Height - 32),
                    new Color(190, 218, 255),
                    0.8f,
                    0.5f,
                    Projectile.Opacity);
            }

            for (int i = 0; i < entries.Length; i++)
            {
                LeonidMetalEntry entry = entries[i];
                Rectangle slotArea = GetSlotArea(i);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool selected = i == selectedIndex;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = GetHoverText(entry);

                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                        clickedIndex = i;
                }

                DrawMetalSlot(entry, slotArea, hovered, selected, clickFeedbackTimers[i], Projectile.Opacity);
                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            if (clickedIndex >= 0)
            {
                selectedIndex = clickedIndex;
                clickFeedbackTimers[clickedIndex] = 10;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.56f, Pitch = 0.08f }, owner.Center);
            }

            if (selectedIndex >= 0 && selectedIndex < entries.Length)
            {
                Rectangle selectedSlotArea = GetSlotArea(selectedIndex);
                Rectangle detailArea = GetDetailArea(panelArea, selectedSlotArea);
                bool mouseOverDetail = detailArea.Intersects(MouseRectangle);

                DrawDetailBox(entries[selectedIndex], detailArea, Projectile.Opacity);
                if (mouseOverDetail)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = GetHoverText(entries[selectedIndex]);
                }
            }

            if (clickedIndex < 0 && !FadeOut && Projectile.Opacity >= 0.95f && (rightClickPressed || leftClickPressed))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
            }

            if (mouseOverPanel)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public static void RequestClose(Projectile projectile)
        {
            if (projectile.ModProjectile is LPBookPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static LeonidMetalEntry[] GetEntries()
        {
            if (cachedEntries is { Length: > 0 })
                return cachedEntries;

            cachedEntries = LeonidMetalRegistry.Entries
                .OrderBy(entry => entry.EffectID)
                .ToArray();

            return cachedEntries;
        }

        private void EnsureStateSize(int entryCount)
        {
            if (clickFeedbackTimers.Length == entryCount && hoveredLastFrame.Length == entryCount)
                return;

            clickFeedbackTimers = new int[entryCount];
            hoveredLastFrame = new bool[entryCount];
            selectedIndex = -1;
        }

        private static int GetRowCount(int entryCount)
        {
            return Math.Max(1, (entryCount + Columns - 1) / Columns);
        }

        private static int GetPanelHeight(int entryCount)
        {
            int rows = GetRowCount(entryCount);
            return PanelPadding * 2 + rows * SlotHeight + (rows - 1) * SlotGap;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft, int panelHeight)
        {
            const float screenMargin = 12f;
            int totalWidth = PanelWidth + DetailGap + DetailWidth;
            float maxX = Math.Max(screenMargin, Main.screenWidth - totalWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - panelHeight - screenMargin);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private static Vector2 GetClampedPanelTopLeftFromCenter(Vector2 desiredCenter, int panelHeight)
        {
            return GetClampedPanelTopLeft(desiredCenter - new Vector2(PanelWidth, panelHeight) * 0.5f, panelHeight);
        }

        private Rectangle GetSlotArea(int index)
        {
            int column = index % Columns;
            int row = index / Columns;
            int x = (int)panelTopLeft.X + PanelPadding + column * (SlotWidth + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + row * (SlotHeight + SlotGap);

            return new Rectangle(x, y, SlotWidth, SlotHeight);
        }

        private static Rectangle GetDetailArea(Rectangle panelArea, Rectangle selectedSlotArea)
        {
            int minY = panelArea.Top;
            int maxY = Math.Max(minY, panelArea.Bottom - DetailHeight);
            int y = Math.Clamp(selectedSlotArea.Center.Y - DetailHeight / 2, minY, maxY);
            return new Rectangle(panelArea.Right + DetailGap, y, DetailWidth, DetailHeight);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            Color back = Color.Lerp(new Color(10, 14, 24), LeonidVisualUtils.DeepStratusBlue, 0.14f);
            DrawRectangle(panelArea, back * (opacity * 0.96f));
            DrawBorder(panelArea, Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.24f) * opacity, BorderThickness);

            Rectangle innerArea = new(
                panelArea.X + BorderThickness,
                panelArea.Y + BorderThickness,
                panelArea.Width - BorderThickness * 2,
                panelArea.Height - BorderThickness * 2);

            DrawBorder(innerArea, Color.Lerp(LeonidVisualUtils.MoonViolet, LeonidVisualUtils.DeepStratusBlue, 0.35f) * (opacity * 0.7f), 1);
        }

        private static void DrawMetalSlot(LeonidMetalEntry entry, Rectangle slotArea, bool hovered, bool selected, int clickTimer, float opacity)
        {
            Color metalColor = entry.ThemeColor;
            Color slotBack = Color.Lerp(new Color(20, 25, 36), metalColor, selected ? 0.22f : 0.1f);
            Color slotBorder = Color.Lerp(LeonidVisualUtils.StratusBlue, metalColor, selected ? 0.54f : 0.32f);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(70, 86, 108), 0.45f);
                slotBorder = Color.Lerp(slotBorder, LeonidVisualUtils.MoonWhite, 0.36f);
            }

            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, LeonidVisualUtils.StarGold, 0.22f);
                slotBorder = Color.Lerp(slotBorder, LeonidVisualUtils.StarGold, 0.46f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.94f));
            DrawBorder(slotArea, slotBorder * opacity, selected ? 2 : 1);

            Rectangle iconFrame = new(slotArea.X + 5, slotArea.Y + 5, 38, 38);
            DrawRectangle(iconFrame, Color.Lerp(new Color(8, 11, 18), metalColor, 0.1f) * (opacity * 0.88f));
            DrawBorder(iconFrame, slotBorder * (opacity * 0.62f), 1);
            DrawItemIcon(entry.ItemType, iconFrame.Center.ToVector2(), hovered, selected, clickTimer, opacity);

            string itemName = Lang.GetItemNameValue(entry.ItemType);
            Rectangle nameArea = new(slotArea.X + 49, slotArea.Y + 4, slotArea.Width - 55, 17);
            DrawFitText(itemName, nameArea, Color.White, 0.62f, 0.38f, opacity);

            Rectangle idArea = new(slotArea.X + 49, slotArea.Y + 22, slotArea.Width - 55, 16);
            DrawFitText($"#{entry.EffectID:00} / {entry.EffectGroup}", idArea, new Color(190, 222, 255), 0.5f, 0.34f, opacity);
        }

        private static void DrawDetailBox(LeonidMetalEntry entry, Rectangle detailArea, float opacity)
        {
            Color metalColor = entry.ThemeColor;
            Color backColor = Color.Lerp(new Color(16, 21, 32), metalColor, 0.14f);
            Color borderColor = Color.Lerp(metalColor, LeonidVisualUtils.MoonWhite, 0.28f);

            DrawRectangle(detailArea, backColor * (opacity * 0.96f));
            DrawBorder(detailArea, borderColor * opacity, 2);

            Rectangle iconArea = new(detailArea.X + 12, detailArea.Y + 12, 48, 48);
            DrawRectangle(iconArea, Color.Lerp(new Color(8, 11, 18), metalColor, 0.14f) * (opacity * 0.88f));
            DrawBorder(iconArea, borderColor * (opacity * 0.72f), 1);
            DrawItemIcon(entry.ItemType, iconArea.Center.ToVector2(), false, true, 0, opacity, 44f);

            Rectangle titleArea = new(detailArea.X + 70, detailArea.Y + 10, detailArea.Width - 84, 22);
            DrawFitText(Lang.GetItemNameValue(entry.ItemType), titleArea, Color.White, 0.7f, 0.42f, opacity);

            Rectangle metaArea = new(detailArea.X + 70, detailArea.Y + 36, detailArea.Width - 84, 18);
            DrawFitText($"Effect #{entry.EffectID:00} / {entry.EffectGroup}", metaArea, new Color(192, 224, 255), 0.52f, 0.34f, opacity);

            Rectangle descriptionArea = new(detailArea.X + 12, detailArea.Y + 68, detailArea.Width - 24, detailArea.Height - 80);
            DrawWrappedFitText(GetMetalEffectText(entry.EffectID), descriptionArea, Color.White, 0.68f, 0.38f, opacity);
        }

        private static void DrawItemIcon(int itemType, Vector2 iconCenter, bool hovered, bool selected, int clickTimer, float opacity, float maxSize = MaxIconDrawSize)
        {
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Rectangle source = Main.itemAnimations[itemType]?.GetFrame(texture) ?? texture.Frame();
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(maxSize / Math.Max(1f, sourceSize.X), maxSize / Math.Max(1f, sourceSize.Y));
            float scale = fitScale * (hovered ? 1.08f : 1f) * (selected ? 1.04f : 1f) * (clickTimer > 0 ? 1.08f : 1f);

            Main.EntitySpriteDraw(
                texture,
                iconCenter,
                source,
                Color.White * opacity,
                hovered ? 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f,
                sourceSize * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        private static string GetHoverText(LeonidMetalEntry entry)
        {
            return $"[i:{entry.ItemType}] {Lang.GetItemNameValue(entry.ItemType)}\n#{entry.EffectID:00} / {entry.EffectGroup}\n{GetMetalEffectText(entry.EffectID)}";
        }

        private static string GetMetalEffectText(int effectID)
        {
            string key = $"Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.MetalDesc{effectID}";
            string text = Language.GetTextValue(key);
            if (text == key)
                return Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.LPBookMissingEffect");

            return StripColorTags(text).Replace('\n', ' ').Replace("  ", " ").Trim();
        }

        private static string StripColorTags(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string output = text;
            int start;
            while ((start = output.IndexOf("[c/", StringComparison.Ordinal)) >= 0)
            {
                int colon = output.IndexOf(':', start);
                if (colon < 0)
                    break;

                output = output.Remove(start, colon - start + 1);
            }

            return output.Replace("]", string.Empty);
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = maxScale;
            if (size.X * scale > area.Width)
                scale = area.Width / size.X;
            if (size.Y * scale > area.Height)
                scale = Math.Min(scale, area.Height / size.Y);

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = new(area.X, area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color * opacity,
                Color.Black * (0.75f * opacity),
                scale);
        }

        private static void DrawWrappedFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            string[] lines = WrapTextToArea(text, area.Width, area.Height, minScale);
            if (lines.Length == 0)
                return;

            var font = FontAssets.MouseText.Value;
            float scale = maxScale;
            float widest = 0f;

            foreach (string line in lines)
                widest = Math.Max(widest, font.MeasureString(line).X);

            if (widest * scale > area.Width)
                scale = area.Width / Math.Max(1f, widest);
            if (font.LineSpacing * scale * lines.Length > area.Height)
                scale = Math.Min(scale, area.Height / Math.Max(1f, font.LineSpacing * lines.Length));

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            float lineHeight = font.LineSpacing * scale;
            Vector2 position = new(area.X, area.Y + Math.Max(0f, (area.Height - lineHeight * lines.Length) * 0.5f));

            for (int i = 0; i < lines.Length; i++)
            {
                CalamityUtils.DrawBorderStringEightWay(
                    Main.spriteBatch,
                    FontAssets.MouseText.Value,
                    lines[i],
                    position + new Vector2(0f, i * lineHeight),
                    color * opacity,
                    Color.Black * (0.75f * opacity),
                    scale);
            }
        }

        private static string[] WrapTextToArea(string text, int width, int height, float scale)
        {
            var font = FontAssets.MouseText.Value;
            int maxLines = Math.Max(1, (int)Math.Floor(height / Math.Max(1f, font.LineSpacing * scale)));
            bool splitByWords = text.Contains(' ');
            string[] tokens = splitByWords
                ? text.Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : text.Replace('\n', ' ').Select(character => character.ToString()).ToArray();
            string separator = splitByWords ? " " : string.Empty;
            List<string> lines = new();
            string currentLine = string.Empty;

            foreach (string token in tokens)
            {
                string candidate = string.IsNullOrEmpty(currentLine) ? token : currentLine + separator + token;
                if (font.MeasureString(candidate).X * scale <= width)
                {
                    currentLine = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);

                currentLine = TrimTextToFit(token, width, font.LineSpacing, scale);
                if (lines.Count >= maxLines)
                    break;
            }

            if (!string.IsNullOrEmpty(currentLine) && lines.Count < maxLines)
                lines.Add(currentLine);

            if (lines.Count == maxLines)
                lines[^1] = TrimTextToFit(lines[^1], width, font.LineSpacing, scale);

            return lines.ToArray();
        }

        private static string TrimTextToFit(string text, int width, int height, float scale)
        {
            var font = FontAssets.MouseText.Value;
            if (font.MeasureString(text).X * scale <= width && font.MeasureString(text).Y * scale <= height)
                return text;

            const string suffix = "...";
            string trimmed = text;
            while (trimmed.Length > 0)
            {
                Vector2 size = font.MeasureString(trimmed + suffix);
                if (size.X * scale <= width && size.Y * scale <= height)
                    break;

                trimmed = trimmed[..^1];
            }

            return trimmed.Length > 0 ? trimmed + suffix : suffix;
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
