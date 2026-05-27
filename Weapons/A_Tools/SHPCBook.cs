using System;
using System.Globalization;
using System.Linq;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
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

namespace CalamityLegendsComeBack.Weapons.A_Tools
{
    public class SHPCBook : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<SHPCBookPanel>();

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
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
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

            Projectile.NewProjectile(
                source,
                player.Center,
                Vector2.Zero,
                PanelType,
                0,
                0f,
                player.whoAmI);

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

                SHPCBookPanel.RequestClose(projectile);
                return true;
            }

            return false;
        }
    }

    internal sealed class SHPCBookPanel : ModProjectile, ILocalizedModType
    {
        private const int Columns = 4;
        private const int SlotWidth = 136;
        private const int SlotHeight = 38;
        private const int SlotGap = 4;
        private const int PanelPadding = 10;
        private const int DetailGap = 6;
        private const int BorderThickness = 2;
        private const float MaxIconDrawSize = 28f;

        private static SHPCBookEntry[] cachedEntries;

        private int selectedIndex = -1;
        private int[] clickFeedbackTimers = Array.Empty<int>();
        private bool[] hoveredLastFrame = Array.Empty<bool>();
        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

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
            Projectile.height = 560;
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

            if (owner.HeldItem.type != ModContent.ItemType<SHPCBook>())
                FadeOut = true;

            SHPCBookEntry[] entries = GetEntries();
            int panelHeight = GetPanelHeight(entries.Length);
            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = GetClampedPanelTopLeft(Main.MouseScreen + new Vector2(14f, 14f), panelHeight);
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
            SHPCBookEntry[] entries = GetEntries();
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
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SHPCBookNoRecords"),
                    new Rectangle(panelArea.X + 16, panelArea.Y + 16, panelArea.Width - 32, panelArea.Height - 32),
                    new Color(190, 210, 232),
                    0.8f,
                    0.5f,
                    Projectile.Opacity);
            }

            for (int i = 0; i < entries.Length; i++)
            {
                SHPCBookEntry entry = entries[i];
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

                DrawBookSlot(entry, slotArea, hovered, selected, clickFeedbackTimers[i], Projectile.Opacity);

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
            if (projectile.ModProjectile is SHPCBookPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static SHPCBookEntry[] GetEntries()
        {
            if (cachedEntries is { Length: > 0 })
                return cachedEntries;

            cachedEntries = EffectRegistry.GetRegisteredEffects()
                .Where(effect => effect.EffectID > 0 && effect.AmmoType > ItemID.None)
                .GroupBy(effect => effect.EffectID)
                .Select(group => group.OrderBy(effect => effect.AmmoType).First())
                .OrderBy(effect => GetBookSortOrder(effect.EffectID))
                .Select(effect => new SHPCBookEntry(effect.EffectID, effect.AmmoType))
                .ToArray();

            return cachedEntries;
        }

        private static float GetBookSortOrder(int effectID)
        {
            return effectID switch
            {
                31 => 25.5f,
                40 => 30.5f,
                _ => effectID
            };
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
            int totalWidth = PanelWidth + DetailGap + SlotWidth;
            float maxX = Math.Max(screenMargin, Main.screenWidth - totalWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - panelHeight - screenMargin);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
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
            return new Rectangle(panelArea.Right + DetailGap, selectedSlotArea.Y, SlotWidth, SlotHeight);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(12, 14, 20, 232) * opacity);
            DrawBorder(panelArea, new Color(94, 110, 132) * opacity, BorderThickness);

            Rectangle innerArea = new(
                panelArea.X + BorderThickness,
                panelArea.Y + BorderThickness,
                panelArea.Width - BorderThickness * 2,
                panelArea.Height - BorderThickness * 2);

            DrawBorder(innerArea, new Color(31, 41, 58, 210) * opacity, 1);
        }

        private static void DrawBookSlot(SHPCBookEntry entry, Rectangle slotArea, bool hovered, bool selected, int clickTimer, float opacity)
        {
            Color effectColor = SHPCAmmoSelectionPanel.GetEffectColor(entry.EffectID);
            Color slotBack = Color.Lerp(new Color(24, 28, 36), effectColor, selected ? 0.24f : 0.12f);
            Color slotBorder = Color.Lerp(new Color(112, 126, 150), effectColor, selected ? 0.68f : 0.38f);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(78, 88, 104), 0.5f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.34f);
            }

            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(132, 116, 70), 0.35f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 228, 150), 0.5f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.94f));
            DrawBorder(slotArea, slotBorder * opacity, selected ? 2 : 1);

            Rectangle iconFrame = new(slotArea.X + 4, slotArea.Y + 4, 30, 30);
            DrawRectangle(iconFrame, Color.Lerp(new Color(10, 12, 18), effectColor, 0.08f) * (opacity * 0.82f));
            DrawBorder(iconFrame, slotBorder * (opacity * 0.62f), 1);
            DrawAmmoIcon(entry, iconFrame.Center.ToVector2(), hovered, selected, clickTimer, opacity);

            string itemName = Lang.GetItemNameValue(entry.AmmoType);
            Rectangle nameArea = new(slotArea.X + 40, slotArea.Y + 3, slotArea.Width - 44, 17);
            DrawFitText(itemName, nameArea, Color.White, 0.62f, 0.42f, opacity);

            Rectangle idArea = new(slotArea.X + 40, slotArea.Y + 20, slotArea.Width - 44, 14);
            DrawFitText(GetSlotStatsText(entry.EffectID), idArea, new Color(190, 218, 255), 0.52f, 0.38f, opacity);
        }

        private static string GetSlotStatsText(int effectID)
        {
            int capacity = SHPCAmmoCapacity.GetCapacity(effectID);
            string multiplierText = new BalanceSHPC()
                .GetLeftClickMaterialDamageMultiplier(effectID)
                .ToString("0.##", CultureInfo.InvariantCulture);

            return $"#{capacity} / {multiplierText}x";
        }

        private static void DrawDetailBox(SHPCBookEntry entry, Rectangle detailArea, float opacity)
        {
            Color effectColor = SHPCAmmoSelectionPanel.GetEffectColor(entry.EffectID);
            Color backColor = Color.Lerp(new Color(18, 22, 30), effectColor, 0.18f);
            Color borderColor = Color.Lerp(effectColor, Color.White, 0.28f);

            DrawRectangle(detailArea, backColor * (opacity * 0.96f));
            DrawBorder(detailArea, borderColor * opacity, 2);

            Rectangle textArea = new(detailArea.X + 6, detailArea.Y + 4, detailArea.Width - 12, detailArea.Height - 8);
            DrawFitText(GetAmmoEffectText(entry.EffectID), textArea, Color.White, 0.62f, 0.4f, opacity);
        }

        private static void DrawAmmoIcon(SHPCBookEntry entry, Vector2 iconCenter, bool hovered, bool selected, int clickTimer, float opacity)
        {
            Texture2D texture = SHPCAmmoSelectionPanel.TryGetAmmoTexture(entry.EffectID, entry.AmmoType);
            if (texture == null)
                return;

            Rectangle source = SHPCAmmoSelectionPanel.GetCurrentFrame(texture, SHPCAmmoSelectionPanel.GetFrameCount(entry.EffectID));
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(MaxIconDrawSize / Math.Max(1f, sourceSize.X), MaxIconDrawSize / Math.Max(1f, sourceSize.Y));
            float hoverScale = hovered ? 1.08f : 1f;
            float selectedScale = selected ? 1.04f : 1f;
            float clickScale = clickTimer > 0 ? 1.08f : 1f;

            Main.EntitySpriteDraw(
                texture,
                iconCenter,
                source,
                Color.White * opacity,
                hovered ? 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f,
                sourceSize * 0.5f,
                fitScale * hoverScale * selectedScale * clickScale,
                SpriteEffects.None,
                0f);
        }

        private static string GetHoverText(SHPCBookEntry entry)
        {
            string itemName = Lang.GetItemNameValue(entry.AmmoType);
            string effectText = GetAmmoEffectText(entry.EffectID);
            int capacity = SHPCAmmoCapacity.GetCapacity(entry.EffectID);
            string multiplierText = new BalanceSHPC()
                .GetLeftClickMaterialDamageMultiplier(entry.EffectID)
                .ToString("0.##", CultureInfo.InvariantCulture);
            string panelText = Language.GetTextValue("Mods.CalamityLegendsComeBack.AMMO.SHPCAmmoPanel", multiplierText, capacity);

            return $"{itemName}\n{effectText}\n{panelText}";
        }

        private static string GetAmmoEffectText(int effectID)
        {
            string key = $"Mods.CalamityLegendsComeBack.AMMO.SHPCAmmo{effectID}";
            string text = Language.GetTextValue(key);
            if (text == key)
                return Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SHPCBookMissingEffect");

            return text;
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            var font = FontAssets.MouseText.Value;
            string displayText = TrimTextToFit(text.Replace('\n', ' '), area.Width, minScale);
            Vector2 size = font.MeasureString(displayText);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = maxScale;
            if (size.X * scale > area.Width)
                scale = area.Width / size.X;
            if (size.Y * scale > area.Height)
                scale = Math.Min(scale, area.Height / size.Y);

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = new(area.X, area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
            DrawTextWithShadow(displayText, position, color * opacity, scale, opacity);
        }

        private static string TrimTextToFit(string text, int width, float scale)
        {
            var font = FontAssets.MouseText.Value;
            if (font.MeasureString(text).X * scale <= width)
                return text;

            const string suffix = "...";
            string trimmed = text;
            while (trimmed.Length > 0 && font.MeasureString(trimmed + suffix).X * scale > width)
                trimmed = trimmed[..^1];

            return trimmed.Length > 0 ? trimmed + suffix : suffix;
        }

        private static void DrawTextWithShadow(string text, Vector2 position, Color color, float scale, float opacity)
        {
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color,
                Color.Black * (0.75f * opacity),
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

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles, System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles, System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
    }

    internal readonly struct SHPCBookEntry
    {
        public SHPCBookEntry(int effectID, int ammoType)
        {
            EffectID = effectID;
            AmmoType = ammoType;
        }

        public int EffectID { get; }
        public int AmmoType { get; }
    }
}
