using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.BossProgress;
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
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.SHPC.SHPCBook
{
    public class SHPCBook : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<SHPCBookPanel>();
        private static int RightPanelType => ModContent.ProjectileType<SHPCBookRightPanel>();

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

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Glass, 50)
                .AddIngredient<CalamityMod.Items.Materials.MysteriousCircuitry>()
                .AddIngredient<CalamityMod.Items.Materials.DubiousPlating>()
                .Register();
        }

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int panelType = player.altFunctionUse == 2 ? RightPanelType : PanelType;
            if (TryCloseExistingPanel(player))
                return false;

            Projectile.NewProjectile(
                source,
                player.Center,
                Vector2.Zero,
                panelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.38f, Pitch = 0.16f }, player.Center);
            return false;
        }

        private static bool TryCloseExistingPanel(Player player)
        {
            bool closedAnyPanel = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || (projectile.type != PanelType && projectile.type != RightPanelType))
                    continue;

                if (projectile.type == PanelType)
                    SHPCBookPanel.RequestClose(projectile);
                else
                    SHPCBookRightPanel.RequestClose(projectile);

                closedAnyPanel = true;
            }

            if (closedAnyPanel)
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);

            return closedAnyPanel;
        }
    }

    internal sealed class SHPCBookPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int Columns = 4;
        private const int SlotWidth = 204;
        private const int SlotHeight = 57;
        private const int SlotGap = 6;
        private const int PanelPadding = 15;
        private const int DetailGap = 9;
        private const int DetailHeight = SlotHeight * 2;
        private const int HeaderHeight = 30;
        private const int CloseButtonSize = 22;
        private const int BorderThickness = 3;
        private const int ScreenMargin = 12;
        private const float SafeScreenFill = 0.82f;
        private const float ReferenceGameZoom = 1f;
        private const float MaxIconDrawSize = 42f;

        private static SHPCBookEntry[] cachedEntries;

        private int selectedIndex = -1;
        private int[] clickFeedbackTimers = Array.Empty<int>();
        private bool[] hoveredLastFrame = Array.Empty<bool>();
        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;
        private int lastPanelWidth;
        private int lastPanelHeight;
        private int openedSelectedItem = -1;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BasePanelWidth => PanelPadding * 2 + Columns * SlotWidth + (Columns - 1) * SlotGap;
        private static int BaseDetailWidth => SlotWidth * 2;
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = BasePanelWidth;
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
            if (Main.myPlayer == Projectile.owner)
            {
                if (openedSelectedItem < 0)
                    openedSelectedItem = owner.selectedItem;
                else if (owner.selectedItem != openedSelectedItem)
                    FadeOut = true;
            }

            float layoutScale = GetLayoutScale(entries.Length);
            int panelWidth = GetPanelWidth(layoutScale);
            int panelHeight = GetPanelHeight(entries.Length, layoutScale);
            int detailWidth = GetDetailWidth(layoutScale, panelWidth);
            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = GetClampedPanelTopLeftFromCenter(Main.MouseScreen, panelWidth, panelHeight, detailWidth, layoutScale);
                panelPositionInitialized = true;
            }
            else if (Main.myPlayer == Projectile.owner)
            {
                int previousPanelWidth = lastPanelWidth > 0 ? lastPanelWidth : panelWidth;
                int previousPanelHeight = lastPanelHeight > 0 ? lastPanelHeight : panelHeight;
                Vector2 anchoredCenter = panelTopLeft + new Vector2(previousPanelWidth, previousPanelHeight) * 0.5f;
                panelTopLeft = GetClampedPanelTopLeftFromCenter(anchoredCenter, panelWidth, panelHeight, detailWidth, layoutScale);
            }

            Vector2 panelCenter = panelTopLeft + new Vector2(panelWidth, panelHeight) * 0.5f;
            lastPanelWidth = panelWidth;
            lastPanelHeight = panelHeight;
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

            float layoutScale = GetLayoutScale(entries.Length);
            int panelWidth = GetPanelWidth(layoutScale);
            int panelHeight = GetPanelHeight(entries.Length, layoutScale);
            int detailWidth = GetDetailWidth(layoutScale, panelWidth);
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, panelWidth, panelHeight);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool closePressed = leftClickPressed || rightClickPressed;
            Rectangle detailArea = default;
            bool hasDetail = selectedIndex >= 0 && selectedIndex < entries.Length;
            if (hasDetail)
                detailArea = GetDetailArea(panelArea, GetSlotArea(selectedIndex, layoutScale), detailWidth, layoutScale);

            Rectangle closeButtonArea = GetCloseButtonArea(panelArea, layoutScale);
            bool closeHovered = closeButtonArea.Intersects(MouseRectangle);
            bool mouseOverDetail = hasDetail && detailArea.Intersects(MouseRectangle);
            bool mouseOverUi = panelArea.Intersects(MouseRectangle) || mouseOverDetail;
            bool readyForInput = !FadeOut && Projectile.Opacity >= 0.95f;
            int clickedIndex = -1;

            if (readyForInput && closeHovered && closePressed)
                CloseWithSound(owner);

            DrawPanel(panelArea, Projectile.Opacity, layoutScale);
            DrawCloseButton(closeButtonArea, closeHovered, Projectile.Opacity, layoutScale);

            if (entries.Length == 0)
            {
                DrawFitText(
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SHPCBookNoRecords"),
                    new Rectangle(
                        panelArea.X + ScaleOffset(16, layoutScale),
                        panelArea.Y + ScaleOffset(PanelPadding + HeaderHeight, layoutScale),
                        panelArea.Width - ScaleOffset(32, layoutScale),
                        panelArea.Height - ScaleOffset(PanelPadding + HeaderHeight + 16, layoutScale)),
                    new Color(190, 210, 232),
                    0.8f * layoutScale,
                    0.5f * layoutScale,
                    Projectile.Opacity);
            }

            for (int i = 0; i < entries.Length; i++)
            {
                SHPCBookEntry entry = entries[i];
                Rectangle slotArea = GetSlotArea(i, layoutScale);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool selected = i == selectedIndex;

                if (hovered)
                {
                    mouseOverUi = true;
                    Main.hoverItemName = GetHoverText(entry);

                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);

                    if (leftClickPressed && readyForInput)
                        clickedIndex = i;
                }

                DrawBookSlot(entry, slotArea, hovered, selected, clickFeedbackTimers[i], Projectile.Opacity, layoutScale);

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
                Rectangle selectedSlotArea = GetSlotArea(selectedIndex, layoutScale);
                detailArea = GetDetailArea(panelArea, selectedSlotArea, detailWidth, layoutScale);
                mouseOverDetail = detailArea.Intersects(MouseRectangle);

                DrawDetailBox(entries[selectedIndex], detailArea, Projectile.Opacity, layoutScale);
                if (mouseOverDetail)
                {
                    mouseOverUi = true;
                    Main.hoverItemName = GetHoverText(entries[selectedIndex]);
                }
            }

            if (!mouseOverUi && readyForInput && closePressed)
                CloseWithSound(owner);

            if (mouseOverUi)
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

        private void CloseWithSound(Player owner)
        {
            FadeOut = true;
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
        }

        private static Rectangle GetCloseButtonArea(Rectangle panelArea, float scale)
        {
            int inset = ScaleOffset(8, scale);
            int size = ScaleLength(CloseButtonSize, scale);
            return new Rectangle(
                panelArea.Right - size - inset,
                panelArea.Y + inset,
                size,
                size);
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
                19 => 16.5f,
                31 => 25.5f,
                40 => 30.5f,
                41 => 2.5f,    // 珍珠碎片放在风暴之颚(2)和硫磺鳞片(3)之间
                44 => 20f,     // calamity核心放在瘟疫细胞罐(18)后面，日耀碎片(21)前面
                42 => 35.5f,   // 黑日碎片放在恒温能量(34)和梦魇燃料(35)后面，Ascendant灵魂碎片(36)前面
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

        private static int GetBasePanelHeight(int entryCount)
        {
            int rows = GetRowCount(entryCount);
            return PanelPadding * 2 + HeaderHeight + rows * SlotHeight + Math.Max(0, rows - 1) * SlotGap;
        }

        private static float GetLayoutScale(int entryCount)
        {
            int basePanelHeight = GetBasePanelHeight(entryCount);
            int baseTotalWidth = BasePanelWidth + DetailGap + BaseDetailWidth;
            float gameZoom = GetGameZoom();
            float zoomCompensation = ReferenceGameZoom / gameZoom;
            float safeHeightScale = Main.screenHeight * SafeScreenFill / Math.Max(1f, basePanelHeight * gameZoom);
            float safeWidthScale = (Main.screenWidth - ScreenMargin * 2f) * SafeScreenFill / Math.Max(1f, baseTotalWidth * gameZoom);
            float targetScale = zoomCompensation;
            float safeScale = Math.Min(safeHeightScale, safeWidthScale);
            return safeScale <= 0.2f ? Math.Max(0.05f, safeScale) : MathHelper.Clamp(targetScale, 0.2f, safeScale);
        }

        private static float GetGameZoom()
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            return MathHelper.Clamp(Math.Max(zoom.X, zoom.Y), 0.25f, 4f);
        }

        private static int GetPanelWidth(float scale)
        {
            return ScaleLength(BasePanelWidth, scale);
        }

        private static int GetPanelHeight(int entryCount, float scale)
        {
            return ScaleLength(GetBasePanelHeight(entryCount), scale);
        }

        private static int GetDetailWidth(float scale, int panelWidth)
        {
            int requestedWidth = ScaleLength(BaseDetailWidth, scale);
            int minimumWidth = ScaleLength(SlotWidth, scale);
            int maximumWidth = Math.Max(
                minimumWidth,
                Main.screenWidth - ScreenMargin * 2 - panelWidth - ScaleLength(DetailGap, scale));

            return Math.Clamp(requestedWidth, minimumWidth, maximumWidth);
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft, int panelWidth, int panelHeight, int detailWidth, float scale)
        {
            int totalWidth = panelWidth + ScaleLength(DetailGap, scale) + detailWidth;
            float maxX = Math.Max(ScreenMargin, Main.screenWidth - totalWidth - ScreenMargin);
            float maxY = Math.Max(0f, Main.screenHeight - panelHeight);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, ScreenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, 0f, maxY));
        }

        private static Vector2 GetClampedPanelTopLeftFromCenter(Vector2 desiredCenter, int panelWidth, int panelHeight, int detailWidth, float scale)
        {
            return GetClampedPanelTopLeft(desiredCenter - new Vector2(panelWidth, panelHeight) * 0.5f, panelWidth, panelHeight, detailWidth, scale);
        }

        private Rectangle GetSlotArea(int index, float scale)
        {
            int column = index % Columns;
            int row = index / Columns;
            int x = (int)panelTopLeft.X + ScaleOffset(PanelPadding + column * (SlotWidth + SlotGap), scale);
            int y = (int)panelTopLeft.Y + ScaleOffset(PanelPadding + HeaderHeight + row * (SlotHeight + SlotGap), scale);

            return new Rectangle(x, y, ScaleLength(SlotWidth, scale), ScaleLength(SlotHeight, scale));
        }

        private static Rectangle GetDetailArea(Rectangle panelArea, Rectangle selectedSlotArea, int detailWidth, float scale)
        {
            int detailGap = ScaleLength(DetailGap, scale);
            int detailHeight = ScaleLength(DetailHeight, scale);
            int minY = panelArea.Top;
            int maxY = Math.Max(minY, panelArea.Bottom - detailHeight);
            int y = Math.Clamp(selectedSlotArea.Center.Y - detailHeight / 2, minY, maxY);
            return new Rectangle(panelArea.Right + detailGap, y, detailWidth, detailHeight);
        }

        private static int ScaleLength(int value, float scale)
        {
            return Math.Max(1, (int)MathF.Round(value * scale));
        }

        private static int ScaleOffset(int value, float scale)
        {
            return (int)MathF.Round(value * scale);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity, float scale)
        {
            DrawRectangle(panelArea, new Color(12, 14, 20, 232) * opacity);
            DrawBorder(panelArea, new Color(94, 110, 132) * opacity, ScaleLength(BorderThickness, scale));

            Rectangle innerArea = new(
                panelArea.X + ScaleLength(BorderThickness, scale),
                panelArea.Y + ScaleLength(BorderThickness, scale),
                panelArea.Width - ScaleLength(BorderThickness, scale) * 2,
                panelArea.Height - ScaleLength(BorderThickness, scale) * 2);

            DrawBorder(innerArea, new Color(31, 41, 58, 210) * opacity, 1);
            DrawRectangle(
                new Rectangle(
                    panelArea.X + ScaleLength(BorderThickness, scale),
                    panelArea.Y + ScaleOffset(PanelPadding + HeaderHeight - 7, scale),
                    panelArea.Width - ScaleLength(BorderThickness, scale) * 2,
                    ScaleLength(1, scale)),
                new Color(94, 110, 132, 148) * opacity);
        }

        private static void DrawCloseButton(Rectangle area, bool hovered, float opacity, float scale)
        {
            Color fill = hovered ? new Color(84, 46, 54) : new Color(34, 30, 38);
            Color border = hovered ? new Color(255, 166, 178) : new Color(148, 104, 116);
            DrawRectangle(area, fill * (opacity * 0.92f));
            DrawBorder(area, border * opacity, ScaleLength(hovered ? 2 : 1, scale));
            DrawFitText("X", area, hovered ? Color.White : new Color(238, 190, 198), 0.72f * scale, 0.44f * scale, opacity);
        }

        private static void DrawBookSlot(SHPCBookEntry entry, Rectangle slotArea, bool hovered, bool selected, int clickTimer, float opacity, float scale)
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
            DrawBorder(slotArea, slotBorder * opacity, ScaleLength(selected ? 2 : 1, scale));

            Rectangle iconFrame = new(
                slotArea.X + ScaleOffset(4, scale),
                slotArea.Y + ScaleOffset(4, scale),
                ScaleLength(30, scale),
                ScaleLength(30, scale));
            DrawRectangle(iconFrame, Color.Lerp(new Color(10, 12, 18), effectColor, 0.08f) * (opacity * 0.82f));
            DrawBorder(iconFrame, slotBorder * (opacity * 0.62f), 1);
            DrawAmmoIcon(entry, iconFrame.Center.ToVector2(), hovered, selected, clickTimer, opacity, scale);

            string itemName = Lang.GetItemNameValue(entry.AmmoType);
            Rectangle nameArea = new(
                slotArea.X + ScaleOffset(40, scale),
                slotArea.Y + ScaleOffset(3, scale),
                slotArea.Width - ScaleOffset(44, scale),
                ScaleLength(17, scale));
            DrawFitText(itemName, nameArea, Color.White, 0.62f * scale, 0.42f * scale, opacity);

            Rectangle idArea = new(
                slotArea.X + ScaleOffset(40, scale),
                slotArea.Y + ScaleOffset(20, scale),
                slotArea.Width - ScaleOffset(44, scale),
                ScaleLength(14, scale));
            DrawFitText(GetSlotStatsText(entry.EffectID), idArea, new Color(190, 218, 255), 0.52f * scale, 0.38f * scale, opacity);
        }

        private static string GetSlotStatsText(int effectID)
        {
            int capacity = SHPCAmmoCapacity.GetCapacity(effectID);
            string multiplierText = new BalanceSHPC()
                .GetLeftClickMaterialDamageMultiplier(effectID)
                .ToString("0.##", CultureInfo.InvariantCulture);

            return $"#{capacity} / {multiplierText}x";
        }

        private static void DrawDetailBox(SHPCBookEntry entry, Rectangle detailArea, float opacity, float scale)
        {
            Color effectColor = SHPCAmmoSelectionPanel.GetEffectColor(entry.EffectID);
            Color backColor = Color.Lerp(new Color(18, 22, 30), effectColor, 0.18f);
            Color borderColor = Color.Lerp(effectColor, Color.White, 0.28f);

            DrawRectangle(detailArea, backColor * (opacity * 0.96f));
            DrawBorder(detailArea, borderColor * opacity, ScaleLength(2, scale));

            Rectangle textArea = new(
                detailArea.X + ScaleOffset(12, scale),
                detailArea.Y + ScaleOffset(8, scale),
                detailArea.Width - ScaleOffset(24, scale),
                detailArea.Height - ScaleOffset(16, scale));
            DrawFitText(GetAmmoEffectText(entry.EffectID), textArea, Color.White, 1.24f * scale, 0.8f * scale, opacity);
        }

        private static void DrawAmmoIcon(SHPCBookEntry entry, Vector2 iconCenter, bool hovered, bool selected, int clickTimer, float opacity, float scale)
        {
            Texture2D texture = SHPCAmmoSelectionPanel.TryGetAmmoTexture(entry.EffectID, entry.AmmoType);
            if (texture == null)
                return;

            Rectangle source = SHPCAmmoSelectionPanel.GetCurrentFrame(texture, SHPCAmmoSelectionPanel.GetFrameCount(entry.EffectID));
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(MaxIconDrawSize * scale / Math.Max(1f, sourceSize.X), MaxIconDrawSize * scale / Math.Max(1f, sourceSize.Y));
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
        }
    }

    internal sealed class SHPCBookRightPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int BasePanelWidth = 822;
        private const int PanelPadding = 15;
        private const int RowHeight = 87;
        private const int RowGap = 8;
        private const int HeaderHeight = 30;
        private const int CloseButtonSize = 22;
        private const int BorderThickness = 3;
        private const int ScreenMargin = 12;
        private const float SafeScreenFill = 0.82f;
        private const float ReferenceGameZoom = 1f;
        private const float NumberIconSize = 51f;
        private const float BossIconSize = 45f;

        private static readonly RightHeatEntry[] HeatEntries =
        {
            new(1, ItemID.AlphabetStatue1, null, null, "Initial"),
            new(2, ItemID.AlphabetStatue2, "Wall of Flesh", "Hardmode", "Wall of Flesh"),
            new(3, ItemID.AlphabetStatue3, "Plantera", null, "Plantera"),
            new(4, ItemID.AlphabetStatue4, "Moon Lord", null, "Moon Lord"),
            new(5, ItemID.AlphabetStatue5, "The Devourer of Gods", null, "Devourer of Gods")
        };

        private readonly int[] clickFeedbackTimers = new int[HeatEntries.Length];
        private readonly bool[] hoveredLastFrame = new bool[HeatEntries.Length];
        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;
        private int lastPanelWidth;
        private int lastPanelHeight;
        private int openedSelectedItem = -1;

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
            Projectile.width = BasePanelWidth;
            Projectile.height = GetBasePanelHeight();
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

            if (Main.myPlayer == Projectile.owner)
            {
                if (openedSelectedItem < 0)
                    openedSelectedItem = owner.selectedItem;
                else if (owner.selectedItem != openedSelectedItem)
                    FadeOut = true;
            }

            float layoutScale = GetLayoutScale();
            int panelWidth = GetPanelWidth(layoutScale);
            int panelHeight = GetPanelHeight(layoutScale);
            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = GetClampedPanelTopLeftFromCenter(Main.MouseScreen, panelWidth, panelHeight);
                panelPositionInitialized = true;
            }
            else if (Main.myPlayer == Projectile.owner)
            {
                int previousPanelWidth = lastPanelWidth > 0 ? lastPanelWidth : panelWidth;
                int previousPanelHeight = lastPanelHeight > 0 ? lastPanelHeight : panelHeight;
                Vector2 anchoredCenter = panelTopLeft + new Vector2(previousPanelWidth, previousPanelHeight) * 0.5f;
                panelTopLeft = GetClampedPanelTopLeftFromCenter(anchoredCenter, panelWidth, panelHeight);
            }

            Vector2 panelCenter = panelTopLeft + new Vector2(panelWidth, panelHeight) * 0.5f;
            lastPanelWidth = panelWidth;
            lastPanelHeight = panelHeight;
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
            float layoutScale = GetLayoutScale();
            int panelWidth = GetPanelWidth(layoutScale);
            int panelHeight = GetPanelHeight(layoutScale);
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, panelWidth, panelHeight);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool closePressed = leftClickPressed || rightClickPressed;
            Rectangle closeButtonArea = GetCloseButtonArea(panelArea, layoutScale);
            bool closeHovered = closeButtonArea.Intersects(MouseRectangle);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool readyForInput = !FadeOut && Projectile.Opacity >= 0.95f;
            int maxHeat = new BalanceSHPC().GetRightClickMaxHeatLevel();

            if (readyForInput && closeHovered && closePressed)
                CloseWithSound(owner);

            DrawPanel(panelArea, Projectile.Opacity, layoutScale);
            DrawCloseButton(closeButtonArea, closeHovered, Projectile.Opacity, layoutScale);

            for (int i = 0; i < HeatEntries.Length; i++)
            {
                RightHeatEntry entry = HeatEntries[i];
                Rectangle rowArea = GetRowArea(i, layoutScale);
                bool hovered = rowArea.Intersects(MouseRectangle);
                bool unlocked = entry.Level <= maxHeat;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = GetHoverText(entry, unlocked);

                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);
                }

                DrawHeatRow(entry, rowArea, unlocked, hovered, clickFeedbackTimers[i], Projectile.Opacity, layoutScale);
                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            if (!mouseOverPanel && readyForInput && closePressed)
                CloseWithSound(owner);

            if (mouseOverPanel)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public static void RequestClose(Projectile projectile)
        {
            if (projectile.ModProjectile is SHPCBookRightPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private void CloseWithSound(Player owner)
        {
            FadeOut = true;
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
        }

        private static int GetBasePanelHeight()
        {
            return PanelPadding * 2 + HeaderHeight + HeatEntries.Length * RowHeight + Math.Max(0, HeatEntries.Length - 1) * RowGap;
        }

        private static float GetLayoutScale()
        {
            float gameZoom = GetGameZoom();
            float zoomCompensation = ReferenceGameZoom / gameZoom;
            float safeHeightScale = Main.screenHeight * SafeScreenFill / Math.Max(1f, GetBasePanelHeight() * gameZoom);
            float safeWidthScale = (Main.screenWidth - ScreenMargin * 2f) * SafeScreenFill / Math.Max(1f, BasePanelWidth * gameZoom);
            float targetScale = zoomCompensation;
            float safeScale = Math.Min(safeHeightScale, safeWidthScale);
            return safeScale <= 0.2f ? Math.Max(0.05f, safeScale) : MathHelper.Clamp(targetScale, 0.2f, safeScale);
        }

        private static float GetGameZoom()
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            return MathHelper.Clamp(Math.Max(zoom.X, zoom.Y), 0.25f, 4f);
        }

        private static int GetPanelWidth(float scale)
        {
            return ScaleLength(BasePanelWidth, scale);
        }

        private static int GetPanelHeight(float scale)
        {
            return ScaleLength(GetBasePanelHeight(), scale);
        }

        private static Vector2 GetClampedPanelTopLeftFromCenter(Vector2 desiredCenter, int panelWidth, int panelHeight)
        {
            return GetClampedPanelTopLeft(desiredCenter - new Vector2(panelWidth, panelHeight) * 0.5f, panelWidth, panelHeight);
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft, int panelWidth, int panelHeight)
        {
            float maxX = Math.Max(ScreenMargin, Main.screenWidth - panelWidth - ScreenMargin);
            float maxY = Math.Max(0f, Main.screenHeight - panelHeight);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, ScreenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, 0f, maxY));
        }

        private static Rectangle GetCloseButtonArea(Rectangle panelArea, float scale)
        {
            int inset = ScaleOffset(8, scale);
            int size = ScaleLength(CloseButtonSize, scale);
            return new Rectangle(
                panelArea.Right - size - inset,
                panelArea.Y + inset,
                size,
                size);
        }

        private Rectangle GetRowArea(int index, float scale)
        {
            return new Rectangle(
                (int)panelTopLeft.X + ScaleOffset(PanelPadding, scale),
                (int)panelTopLeft.Y + ScaleOffset(PanelPadding + HeaderHeight + index * (RowHeight + RowGap), scale),
                ScaleLength(BasePanelWidth - PanelPadding * 2, scale),
                ScaleLength(RowHeight, scale));
        }

        private static int ScaleLength(int value, float scale)
        {
            return Math.Max(1, (int)MathF.Round(value * scale));
        }

        private static int ScaleOffset(int value, float scale)
        {
            return (int)MathF.Round(value * scale);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity, float scale)
        {
            DrawRectangle(panelArea, new Color(12, 14, 20, 232) * opacity);
            DrawBorder(panelArea, new Color(94, 110, 132) * opacity, ScaleLength(BorderThickness, scale));

            Rectangle innerArea = new(
                panelArea.X + ScaleLength(BorderThickness, scale),
                panelArea.Y + ScaleLength(BorderThickness, scale),
                panelArea.Width - ScaleLength(BorderThickness, scale) * 2,
                panelArea.Height - ScaleLength(BorderThickness, scale) * 2);

            DrawBorder(innerArea, new Color(31, 41, 58, 210) * opacity, 1);
            DrawRectangle(
                new Rectangle(
                    panelArea.X + ScaleLength(BorderThickness, scale),
                    panelArea.Y + ScaleOffset(PanelPadding + HeaderHeight - 7, scale),
                    panelArea.Width - ScaleLength(BorderThickness, scale) * 2,
                    ScaleLength(1, scale)),
                new Color(94, 110, 132, 148) * opacity);
        }

        private static void DrawCloseButton(Rectangle area, bool hovered, float opacity, float scale)
        {
            Color fill = hovered ? new Color(84, 46, 54) : new Color(34, 30, 38);
            Color border = hovered ? new Color(255, 166, 178) : new Color(148, 104, 116);
            DrawRectangle(area, fill * (opacity * 0.92f));
            DrawBorder(area, border * opacity, ScaleLength(hovered ? 2 : 1, scale));
            DrawFitText("X", area, hovered ? Color.White : new Color(238, 190, 198), 0.72f * scale, 0.44f * scale, opacity);
        }

        private static void DrawHeatRow(RightHeatEntry entry, Rectangle rowArea, bool unlocked, bool hovered, int clickTimer, float opacity, float scale)
        {
            Color heatColor = GetHeatColor(entry.Level);
            Color muted = new(126, 130, 140);
            Color rowColor = unlocked ? heatColor : muted;
            Color slotBack = Color.Lerp(new Color(24, 28, 36), rowColor, unlocked ? 0.18f : 0.08f);
            Color slotBorder = Color.Lerp(new Color(112, 126, 150), rowColor, unlocked ? 0.58f : 0.22f);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(78, 88, 104), 0.45f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.3f);
            }

            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(132, 116, 70), 0.35f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 228, 150), 0.5f);
            }

            DrawRectangle(rowArea, slotBack * (opacity * 0.94f));
            DrawBorder(rowArea, slotBorder * opacity, ScaleLength(unlocked ? 2 : 1, scale));

            Rectangle numberFrame = new(
                rowArea.X + ScaleOffset(9, scale),
                rowArea.Y + ScaleOffset(12, scale),
                ScaleLength(63, scale),
                ScaleLength(63, scale));
            DrawRectangle(numberFrame, Color.Lerp(new Color(10, 12, 18), rowColor, 0.08f) * (opacity * 0.82f));
            DrawBorder(numberFrame, slotBorder * (opacity * 0.62f), 1);
            DrawItemIcon(entry.NumberItemID, numberFrame.Center.ToVector2(), NumberIconSize * scale, unlocked ? Color.White : Color.Gray, opacity);

            string description = GetHeatDescription(entry.Level);
            Rectangle descriptionArea = new(
                rowArea.X + ScaleOffset(84, scale),
                rowArea.Y + ScaleOffset(8, scale),
                rowArea.Width - ScaleOffset(189, scale),
                ScaleLength(72, scale));
            DrawWrappedFitText(description, descriptionArea, unlocked ? Color.White : new Color(170, 174, 184), 0.73f * scale, 0.49f * scale, opacity);

            Rectangle unlockFrame = new(
                rowArea.Right - ScaleOffset(87, scale),
                rowArea.Y + ScaleOffset(12, scale),
                ScaleLength(63, scale),
                ScaleLength(63, scale));
            DrawRectangle(unlockFrame, Color.Lerp(new Color(10, 12, 18), rowColor, unlocked ? 0.12f : 0.04f) * (opacity * 0.82f));
            DrawBorder(unlockFrame, slotBorder * (opacity * 0.62f), 1);
            DrawUnlockIcon(entry, unlockFrame, unlocked, opacity, scale);
        }

        private static void DrawUnlockIcon(RightHeatEntry entry, Rectangle frame, bool unlocked, float opacity, float scale)
        {
            Texture2D icon = TryGetUnlockTexture(entry);
            if (icon == null)
            {
                DrawFitText(entry.Level == 1 ? "OK" : "?", frame, unlocked ? Color.White : Color.Gray, 0.62f * scale, 0.42f * scale, opacity);
                return;
            }

            Rectangle source = icon.Frame();
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(BossIconSize * scale / Math.Max(1f, sourceSize.X), BossIconSize * scale / Math.Max(1f, sourceSize.Y));
            Color color = unlocked ? Color.White : Color.Gray;
            Main.EntitySpriteDraw(icon, frame.Center.ToVector2(), source, color * opacity, 0f, sourceSize * 0.5f, fitScale, SpriteEffects.None, 0f);
        }

        private static Texture2D TryGetUnlockTexture(RightHeatEntry entry)
        {
            if (entry.UnlockBossEnglishName == null)
                return null;

            BossProgressEntry bossEntry = CTRLBossRegistry.Entries.FirstOrDefault(boss => boss.EnglishName == entry.UnlockBossEnglishName);
            if (bossEntry == null)
                return null;

            try
            {
                return ModContent.Request<Texture2D>(bossEntry.TexturePath).Value;
            }
            catch
            {
                return null;
            }
        }

        private static void DrawItemIcon(int itemID, Vector2 center, float maxSize, Color color, float opacity)
        {
            Texture2D texture = TextureAssets.Item[itemID].Value;
            Rectangle source = texture.Frame();
            Vector2 sourceSize = source.Size();
            float fitScale = Math.Min(maxSize / Math.Max(1f, sourceSize.X), maxSize / Math.Max(1f, sourceSize.Y));
            Main.EntitySpriteDraw(texture, center, source, color * opacity, 0f, sourceSize * 0.5f, fitScale, SpriteEffects.None, 0f);
        }

        private static Color GetHeatColor(int level)
        {
            return level switch
            {
                1 => new Color(78, 190, 255),
                2 => new Color(255, 226, 82),
                3 => new Color(255, 132, 38),
                4 => new Color(255, 58, 34),
                _ => new Color(255, 38, 70)
            };
        }

        private static string GetHeatDescription(int level)
        {
            string key = $"Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendSHPC.SHPC_RightIntro{level}";
            string text = Language.GetTextValue(key);
            if (text == key)
                return level switch
                {
                    1 => "Heat I: precise beam fire. Sustained fire builds heat until forced shutdown.",
                    2 => "Heat II: adds a secondary beam and improves piercing.",
                    3 => "Heat III: heat blasts can trigger on hit.",
                    4 => "Heat IV: adds another secondary beam and applies overheat pressure.",
                    _ => "Heat V: beams gain infinite piercing and severe overheat risk."
                };

            return StripColorTags(text).Replace('\n', ' ').Replace("  ", " ").Trim();
        }

        private static string GetHoverText(RightHeatEntry entry, bool unlocked)
        {
            string state = unlocked ? "已解锁 / Unlocked" : $"未解锁 / Unlock: {entry.UnlockText}";
            return $"Heat {entry.Level}\n{state}\n{GetHeatDescription(entry.Level)}";
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

        private static void DrawWrappedFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            string[] lines = WrapTextToArea(text, area.Width, area.Height, minScale);
            DrawMultilineFitText(lines, area, color, maxScale, minScale, opacity);
        }

        private static void DrawMultilineFitText(string[] lines, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (lines.Length == 0)
                return;

            var font = FontAssets.MouseText.Value;
            float scale = maxScale;
            float lineHeight = font.LineSpacing * scale;
            float widest = 0f;

            foreach (string line in lines)
                widest = Math.Max(widest, font.MeasureString(line).X);

            if (widest * scale > area.Width)
                scale = area.Width / Math.Max(1f, widest);

            if (lineHeight * lines.Length > area.Height)
                scale = Math.Min(scale, area.Height / Math.Max(1f, font.LineSpacing * lines.Length));

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            lineHeight = font.LineSpacing * scale;
            Vector2 position = new(area.X, area.Y + Math.Max(0f, (area.Height - lineHeight * lines.Length) * 0.5f));

            for (int i = 0; i < lines.Length; i++)
                DrawTextWithShadow(lines[i], position + new Vector2(0f, i * lineHeight), color * opacity, scale, opacity);
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
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
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

        private static string[] WrapTextToArea(string text, int width, int height, float scale)
        {
            var font = FontAssets.MouseText.Value;
            int maxLines = Math.Max(1, (int)Math.Floor(height / Math.Max(1f, font.LineSpacing * scale)));
            string[] words = text.Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<string> lines = new();
            string currentLine = string.Empty;

            foreach (string word in words)
            {
                string candidate = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                if (font.MeasureString(candidate).X * scale <= width)
                {
                    currentLine = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);

                currentLine = TrimTextToFit(word, width, font.LineSpacing, scale);
                if (lines.Count >= maxLines)
                    break;
            }

            if (!string.IsNullOrEmpty(currentLine) && lines.Count < maxLines)
                lines.Add(currentLine);

            if (lines.Count > maxLines)
                lines.RemoveRange(maxLines, lines.Count - maxLines);

            if (lines.Count == maxLines && words.Length > 0)
                lines[^1] = TrimTextToFit(lines[^1], width, font.LineSpacing, scale);

            return lines.ToArray();
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
        }
    }

    internal readonly struct RightHeatEntry
    {
        public RightHeatEntry(int level, int numberItemID, string unlockBossEnglishName, string unlockTextOverride, string unlockText)
        {
            Level = level;
            NumberItemID = numberItemID;
            UnlockBossEnglishName = unlockBossEnglishName;
            UnlockText = unlockTextOverride ?? unlockText;
        }

        public int Level { get; }
        public int NumberItemID { get; }
        public string UnlockBossEnglishName { get; }
        public string UnlockText { get; }
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
