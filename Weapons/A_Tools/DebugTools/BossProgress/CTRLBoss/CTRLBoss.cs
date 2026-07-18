using System;
using System.Collections.Generic;
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

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.BossProgress
{
    public class CTRLBoss : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.SuspiciousLookingEye;

        private static int PanelType => ModContent.ProjectileType<CTRLBossPanel>();
        private static int SummonerPanelType => ModContent.ProjectileType<BossSummonerPanel>();

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(230, 70, 70));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
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

        public override bool AltFunctionUse(Player player) => true;

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
            if (player.altFunctionUse == 2)
            {
                if (BossSummonerPanel.TryClose(player))
                {
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                    return false;
                }

                Projectile.NewProjectile(source, player.Center, Vector2.Zero, SummonerPanelType, 0, 0f, player.whoAmI);
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
                return false;
            }

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
                player.whoAmI,
                0f,
                Main.MouseScreen.X,
                Main.MouseScreen.Y);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.42f, Pitch = 0.2f }, player.Center);
            return false;
        }

        private static bool TryCloseExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                CTRLBossPanel.RequestClose(projectile);
                return true;
            }

            return false;
        }
    }

    internal sealed class CTRLBossPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int Columns = 5;
        private const int SlotSize = 52;
        private const int SlotGap = 6;
        private const int PanelPadding = 14;
        private const int BorderThickness = 2;
        private const float MaxIconDrawSize = 40f;
        private const int BulkButtonSize = 58;
        private const int BulkButtonGap = 8;
        private const int BulkPanelGap = 12;

        private static readonly BulkProgressAction[] BulkActions =
        {
            new(BulkProgressActionKind.TogglePhase, BossProgressPhase.PreHardmode, ItemID.SuspiciousLookingEye, "CTRLBossBulkPreHardmode"),
            new(BulkProgressActionKind.TogglePhase, BossProgressPhase.Hardmode, ItemID.MechanicalEye, "CTRLBossBulkHardmode"),
            new(BulkProgressActionKind.TogglePhase, BossProgressPhase.PostMoonLord, ItemID.CelestialSigil, "CTRLBossBulkPostMoonLord"),
            new(BulkProgressActionKind.ToggleAll, null, ItemID.Zenith, "CTRLBossBulkAll"),
            new(BulkProgressActionKind.InvertAll, null, ItemID.Lever, "CTRLBossBulkInvert")
        };

        private readonly int[] clickFeedbackTimers = new int[CTRLBossRegistry.Entries.Length];
        private readonly bool[] hoveredLastFrame = new bool[CTRLBossRegistry.Entries.Length];
        private readonly int[] bulkClickFeedbackTimers = new int[BulkActions.Length];
        private readonly bool[] bulkHoveredLastFrame = new bool[BulkActions.Length];

        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int RowCount => (CTRLBossRegistry.Entries.Length + Columns - 1) / Columns;
        private static int BossGridWidth => PanelPadding * 2 + Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int BossGridHeight => PanelPadding * 2 + RowCount * SlotSize + (RowCount - 1) * SlotGap;
        private static int BulkButtonsHeight => BulkActions.Length * BulkButtonSize + (BulkActions.Length - 1) * BulkButtonGap;
        private static int PanelWidth => BossGridWidth + BulkPanelGap + BulkButtonSize + PanelPadding;
        private static int PanelHeight => Math.Max(BossGridHeight, PanelPadding * 2 + BulkButtonsHeight);
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

            if (owner.HeldItem.type != ModContent.ItemType<CTRLBoss>())
                FadeOut = true;

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 requestedTopLeft = Projectile.ai[1] != 0f || Projectile.ai[2] != 0f
                    ? new Vector2(Projectile.ai[1], Projectile.ai[2]) - new Vector2(PanelWidth, PanelHeight) * 0.5f
                    : Main.MouseScreen;
                panelTopLeft = GetClampedPanelTopLeft(requestedTopLeft);
                panelPositionInitialized = true;
            }

            Vector2 panelCenter = panelTopLeft + new Vector2(PanelWidth, PanelHeight) * 0.5f;
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
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            BossProgressEntry clickedEntry = null;
            int clickedIndex = -1;
            BulkProgressAction clickedBulkAction = null;
            int clickedBulkIndex = -1;

            DrawPanel(panelArea, Projectile.Opacity);

            for (int i = 0; i < CTRLBossRegistry.Entries.Length; i++)
            {
                BossProgressEntry entry = CTRLBossRegistry.Entries[i];
                Rectangle slotArea = GetSlotArea(i);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool downed = entry.Downed;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = GetHoverText(entry, downed);

                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        clickedEntry = entry;
                        clickedIndex = i;
                    }
                }

                DrawBossSlot(entry, slotArea, downed, hovered, clickFeedbackTimers[i], Projectile.Opacity);

                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            for (int i = 0; i < BulkActions.Length; i++)
            {
                BulkProgressAction action = BulkActions[i];
                Rectangle buttonArea = GetBulkButtonArea(i);
                bool hovered = buttonArea.Intersects(MouseRectangle);
                bool fullyDowned = IsBulkActionFullyDowned(action);

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = GetBulkHoverText(action);

                    if (!bulkHoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.16f }, owner.Center);

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        clickedBulkAction = action;
                        clickedBulkIndex = i;
                    }
                }

                DrawBulkButton(action, buttonArea, fullyDowned, hovered, bulkClickFeedbackTimers[i], Projectile.Opacity);

                bulkHoveredLastFrame[i] = hovered;
                if (bulkClickFeedbackTimers[i] > 0)
                    bulkClickFeedbackTimers[i]--;
            }

            if (clickedEntry != null)
            {
                bool newState = !clickedEntry.Downed;
                clickedEntry.SetDowned(newState);
                clickFeedbackTimers[clickedIndex] = 10;
                CTRLBossRegistry.SyncWorldState();
                AnnounceChange(clickedEntry, newState);
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.62f, Pitch = newState ? 0.08f : -0.12f }, owner.Center);
            }
            else if (clickedBulkAction != null)
            {
                int changedCount = ApplyBulkAction(clickedBulkAction);
                bulkClickFeedbackTimers[clickedBulkIndex] = 10;
                CTRLBossRegistry.SyncWorldState();
                AnnounceBulkChange(clickedBulkAction, changedCount);
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.72f, Pitch = clickedBulkAction.Kind == BulkProgressActionKind.InvertAll ? -0.03f : 0.14f }, owner.Center);
            }
            else if (!FadeOut && Projectile.Opacity >= 0.95f && (rightClickPressed || leftClickPressed))
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
            if (projectile.ModProjectile is CTRLBossPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            const float screenMargin = 12f;
            float maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private Rectangle GetSlotArea(int index)
        {
            int column = index % Columns;
            int row = index / Columns;
            int x = (int)panelTopLeft.X + PanelPadding + column * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + row * (SlotSize + SlotGap);

            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private Rectangle GetBulkButtonArea(int index)
        {
            int x = (int)panelTopLeft.X + BossGridWidth + BulkPanelGap;
            int y = (int)panelTopLeft.Y + PanelPadding + index * (BulkButtonSize + BulkButtonGap);

            return new Rectangle(x, y, BulkButtonSize, BulkButtonSize);
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

        private static void DrawBossSlot(BossProgressEntry entry, Rectangle slotArea, bool downed, bool hovered, int clickTimer, float opacity)
        {
            Color phaseColor = CTRLBossRegistry.PhaseColor(entry.Phase);
            Color slotBack = downed ? Color.Lerp(new Color(30, 38, 42), phaseColor, 0.28f) : new Color(46, 48, 56);
            Color slotBorder = downed ? Color.Lerp(phaseColor, Color.White, hovered ? 0.42f : 0.18f) : new Color(124, 128, 142);

            if (hovered)
                slotBack = Color.Lerp(slotBack, new Color(76, 84, 100), 0.48f);

            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(130, 116, 70), 0.35f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 228, 150), 0.5f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.94f));
            DrawBorder(slotArea, slotBorder * opacity, 2);

            Texture2D iconTexture = TryGetIconTexture(entry);
            Vector2 iconCenter = slotArea.Center.ToVector2();
            if (iconTexture != null)
            {
                Vector2 iconSize = iconTexture.Size();
                float fitScale = Math.Min(MaxIconDrawSize / Math.Max(1f, iconSize.X), MaxIconDrawSize / Math.Max(1f, iconSize.Y));
                float drawScale = fitScale * (hovered ? 1.09f : 1f) * (clickTimer > 0 ? 1.08f : 1f);
                Color iconColor = downed ? Color.White : new Color(176, 180, 190);

                if (!downed)
                    iconColor *= 0.78f;

                if (hovered)
                    iconColor = Color.Lerp(iconColor, Color.White, downed ? 0.22f : 0.38f);

                Main.EntitySpriteDraw(
                    iconTexture,
                    iconCenter,
                    null,
                    iconColor * opacity,
                    0f,
                    iconSize * 0.5f,
                    drawScale,
                    SpriteEffects.None,
                    0f);
            }
            else
            {
                DrawMissingIcon(slotArea, phaseColor, opacity, downed);
            }

            if (!downed)
                DrawRectangle(new Rectangle(slotArea.X + 5, slotArea.Bottom - 9, slotArea.Width - 10, 3), new Color(190, 90, 90) * (opacity * 0.82f));
            else
                DrawRectangle(new Rectangle(slotArea.X + 5, slotArea.Bottom - 9, slotArea.Width - 10, 3), phaseColor * (opacity * 0.94f));
        }

        private static void DrawBulkButton(BulkProgressAction action, Rectangle buttonArea, bool fullyDowned, bool hovered, int clickTimer, float opacity)
        {
            Color actionColor = action.Phase.HasValue
                ? CTRLBossRegistry.PhaseColor(action.Phase.Value)
                : action.Kind == BulkProgressActionKind.InvertAll
                    ? new Color(238, 164, 96)
                    : new Color(244, 208, 108);
            Color buttonBack = fullyDowned
                ? Color.Lerp(new Color(30, 38, 42), actionColor, 0.28f)
                : new Color(46, 48, 56);
            Color buttonBorder = fullyDowned
                ? Color.Lerp(actionColor, Color.White, hovered ? 0.42f : 0.18f)
                : new Color(124, 128, 142);

            if (hovered)
                buttonBack = Color.Lerp(buttonBack, new Color(84, 92, 108), 0.54f);

            if (clickTimer > 0)
            {
                buttonBack = Color.Lerp(buttonBack, new Color(130, 116, 70), 0.38f);
                buttonBorder = Color.Lerp(buttonBorder, new Color(255, 228, 150), 0.56f);
            }

            DrawRectangle(buttonArea, buttonBack * (opacity * 0.94f));
            DrawBorder(buttonArea, buttonBorder * opacity, 2);

            Texture2D iconTexture = TryGetVanillaItemTexture(action.VanillaItemType);
            if (iconTexture != null)
            {
                Vector2 iconSize = iconTexture.Size();
                float fitScale = Math.Min(44f / Math.Max(1f, iconSize.X), 44f / Math.Max(1f, iconSize.Y));
                float drawScale = fitScale * (hovered ? 1.08f : 1f) * (clickTimer > 0 ? 1.08f : 1f);
                Color iconColor = fullyDowned ? Color.White : new Color(202, 206, 216);

                Main.EntitySpriteDraw(
                    iconTexture,
                    buttonArea.Center.ToVector2(),
                    null,
                    iconColor * opacity,
                    0f,
                    iconSize * 0.5f,
                    drawScale,
                    SpriteEffects.None,
                    0f);
            }
            else
            {
                DrawMissingIcon(buttonArea, actionColor, opacity, fullyDowned);
            }

            Rectangle statusBar = new(buttonArea.X + 5, buttonArea.Bottom - 9, buttonArea.Width - 10, 3);
            if (action.Kind == BulkProgressActionKind.InvertAll)
            {
                int halfWidth = statusBar.Width / 2;
                DrawRectangle(new Rectangle(statusBar.X, statusBar.Y, halfWidth, statusBar.Height), new Color(112, 224, 142) * (opacity * 0.92f));
                DrawRectangle(new Rectangle(statusBar.X + halfWidth, statusBar.Y, statusBar.Width - halfWidth, statusBar.Height), new Color(224, 112, 112) * (opacity * 0.92f));
            }
            else
            {
                Color statusColor = fullyDowned ? actionColor : new Color(190, 90, 90);
                DrawRectangle(statusBar, statusColor * (opacity * 0.92f));
            }
        }

        private static Texture2D TryGetIconTexture(BossProgressEntry entry)
        {
            try
            {
                return ModContent.Request<Texture2D>(entry.TexturePath).Value;
            }
            catch
            {
                return null;
            }
        }

        private static Texture2D TryGetVanillaItemTexture(int itemType)
        {
            try
            {
                Main.instance.LoadItem(itemType);
                return TextureAssets.Item[itemType].Value;
            }
            catch
            {
                return null;
            }
        }

        private static void DrawMissingIcon(Rectangle slotArea, Color phaseColor, float opacity, bool downed)
        {
            Color color = downed ? phaseColor : new Color(162, 166, 178);
            int centerX = slotArea.Center.X;
            int centerY = slotArea.Center.Y;

            DrawRectangle(new Rectangle(centerX - 13, centerY - 13, 26, 26), color * (opacity * 0.38f));
            DrawBorder(new Rectangle(centerX - 13, centerY - 13, 26, 26), color * (opacity * 0.86f), 2);
            DrawRectangle(new Rectangle(centerX - 2, centerY - 18, 4, 36), color * (opacity * 0.72f));
            DrawRectangle(new Rectangle(centerX - 18, centerY - 2, 36, 4), color * (opacity * 0.72f));
        }

        private static void AnnounceChange(BossProgressEntry entry, bool downed)
        {
            string stateText = Language.GetTextValue(downed
                ? "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossDefeated"
                : "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossUndefeated");
            string message = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossStatusSet",
                entry.ChineseName,
                stateText);
            Color messageColor = downed ? new Color(112, 240, 148) : new Color(255, 132, 118);

            Main.NewText(message, messageColor);
        }

        private static void AnnounceBulkChange(BulkProgressAction action, int changedCount)
        {
            string message = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossBulkChanged",
                GetBulkHoverText(action),
                changedCount);

            Main.NewText(message, new Color(255, 214, 126));
        }

        private static int ApplyBulkAction(BulkProgressAction action)
        {
            int changedCount = 0;
            if (action.Kind == BulkProgressActionKind.InvertAll)
            {
                HashSet<string> invertedProgressKeys = new();
                foreach (BossProgressEntry entry in CTRLBossRegistry.Entries)
                {
                    if (!invertedProgressKeys.Add(entry.ProgressKey))
                        continue;

                    entry.SetDowned(!entry.Downed);
                    changedCount++;
                }

                return changedCount;
            }

            bool setDowned = false;
            foreach (BossProgressEntry entry in CTRLBossRegistry.Entries)
            {
                if (AppliesTo(action, entry) && !entry.Downed)
                {
                    setDowned = true;
                    break;
                }
            }

            foreach (BossProgressEntry entry in CTRLBossRegistry.Entries)
            {
                if (!AppliesTo(action, entry) || entry.Downed == setDowned)
                    continue;

                entry.SetDowned(setDowned);
                changedCount++;
            }

            return changedCount;
        }

        private static bool IsBulkActionFullyDowned(BulkProgressAction action)
        {
            if (action.Kind == BulkProgressActionKind.InvertAll)
                return false;

            bool containsEntry = false;
            foreach (BossProgressEntry entry in CTRLBossRegistry.Entries)
            {
                if (!AppliesTo(action, entry))
                    continue;

                containsEntry = true;
                if (!entry.Downed)
                    return false;
            }

            return containsEntry;
        }

        private static bool AppliesTo(BulkProgressAction action, BossProgressEntry entry)
        {
            return action.Kind != BulkProgressActionKind.TogglePhase || entry.Phase == action.Phase;
        }

        private static string GetBulkHoverText(BulkProgressAction action)
        {
            return Language.GetTextValue($"Mods.CalamityLegendsComeBack.TheSpecialText.{action.HoverTextKey}");
        }

        private static string GetHoverText(BossProgressEntry entry, bool downed)
        {
            string stateText = Language.GetTextValue(downed
                ? "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossDefeated"
                : "Mods.CalamityLegendsComeBack.TheSpecialText.CTRLBossUndefeated");

            return $"{entry.ChineseName} / {entry.EnglishName} - {stateText}";
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

    internal enum BossProgressPhase
    {
        PreHardmode,
        Hardmode,
        PostMoonLord
    }

    internal enum BulkProgressActionKind
    {
        TogglePhase,
        ToggleAll,
        InvertAll
    }

    internal sealed class BulkProgressAction
    {
        public BulkProgressAction(BulkProgressActionKind kind, BossProgressPhase? phase, int vanillaItemType, string hoverTextKey)
        {
            Kind = kind;
            Phase = phase;
            VanillaItemType = vanillaItemType;
            HoverTextKey = hoverTextKey;
        }

        public BulkProgressActionKind Kind { get; }
        public BossProgressPhase? Phase { get; }
        public int VanillaItemType { get; }
        public string HoverTextKey { get; }
    }

    internal sealed class BossProgressEntry
    {
        private readonly Func<bool> getDowned;
        private readonly Action<bool> setDowned;

        public BossProgressEntry(BossProgressPhase phase, string englishName, string chineseName, string texturePath, Func<bool> getDowned, Action<bool> setDowned, string progressKey)
        {
            Phase = phase;
            EnglishName = englishName;
            ChineseName = chineseName;
            TexturePath = texturePath;
            this.getDowned = getDowned;
            this.setDowned = setDowned;
            ProgressKey = progressKey;
        }

        public BossProgressPhase Phase { get; }
        public string EnglishName { get; }
        public string ChineseName { get; }
        public string TexturePath { get; }
        public string ProgressKey { get; }
        public bool Downed => getDowned();

        public void SetDowned(bool downed) => setDowned(downed);
    }

    internal static class CTRLBossRegistry
    {
        public static readonly BossProgressEntry[] Entries =
        {
            Pre("King Slime", "史莱姆王", "史莱姆王-图标", () => NPC.downedSlimeKing, value => NPC.downedSlimeKing = value),
            Pre("Desert Scourge", "荒漠灾虫", "荒漠灾虫-图标", () => DownedBossSystem.downedDesertScourge, value => DownedBossSystem.downedDesertScourge = value),
            Pre("Eye of Cthulhu", "克苏鲁之眼", "克苏鲁之眼第一阶段-图标", () => NPC.downedBoss1, value => NPC.downedBoss1 = value),
            Pre("Crabulon", "菌生蟹", "菌生蟹-图标", () => DownedBossSystem.downedCrabulon, value => DownedBossSystem.downedCrabulon = value),
            Pre("Eater of Worlds", "世界吞噬怪", "世界吞噬怪-图标", () => NPC.downedBoss2, value => NPC.downedBoss2 = value, "NPC.downedBoss2"),
            Pre("Brain of Cthulhu", "克苏鲁之脑", "克苏鲁之脑-图标", () => NPC.downedBoss2, value => NPC.downedBoss2 = value, "NPC.downedBoss2"),
            Pre("The Hive Mind", "腐巢意志", "腐巢意志第二阶段-图标", () => DownedBossSystem.downedHiveMind, value => DownedBossSystem.downedHiveMind = value),
            Pre("The Perforators", "血肉宿主", "血肉宿主本体-图标", () => DownedBossSystem.downedPerforator, value => DownedBossSystem.downedPerforator = value),
            Pre("Queen Bee", "蜂王", "蜂王-图标", () => NPC.downedQueenBee, value => NPC.downedQueenBee = value),
            Pre("Skeletron", "骷髅王", "骷髅王-图标", () => NPC.downedBoss3, value => NPC.downedBoss3 = value),
            Pre("Deerclops", "独眼巨鹿", "独眼巨鹿-图标", () => NPC.downedDeerclops, value => NPC.downedDeerclops = value),
            Pre("The Slime God", "史莱姆之神", "史莱姆之神核心-图标", () => DownedBossSystem.downedSlimeGod, value => DownedBossSystem.downedSlimeGod = value),
            Pre("Wall of Flesh", "肉山墙", "血肉墙-图标", () => Main.hardMode, value => Main.hardMode = value),

            Hard("Queen Slime", "史莱姆皇后", "史莱姆皇后-图标", () => NPC.downedQueenSlime, value => NPC.downedQueenSlime = value),
            Hard("Aquatic Scourge", "渊海灾虫", "渊海灾虫-图标", () => DownedBossSystem.downedAquaticScourge, value => DownedBossSystem.downedAquaticScourge = value),
            Hard("Brimstone Elemental", "硫磺火元素", "硫磺火元素-图标", () => DownedBossSystem.downedBrimstoneElemental, value => DownedBossSystem.downedBrimstoneElemental = value),
            Hard("Cryogen", "极地之灵", "极地之灵-图标", () => DownedBossSystem.downedCryogen, value => DownedBossSystem.downedCryogen = value),
            Hard("The Destroyer", "毁灭者", "毁灭者-图标", () => NPC.downedMechBoss1, value => NPC.downedMechBoss1 = value),
            Hard("The Twins", "双子魔眼", "双子魔眼面具", () => NPC.downedMechBoss2, value => NPC.downedMechBoss2 = value),
            Hard("Skeletron Prime", "机械骷髅王", "机械骷髅王-图标", () => NPC.downedMechBoss3, value => NPC.downedMechBoss3 = value),
            Hard("Calamitas Clone", "灾厄之影", "灾厄之影-图标", () => DownedBossSystem.downedCalamitasClone, value => DownedBossSystem.downedCalamitasClone = value),
            Hard("Plantera", "世纪之花", "世纪之花-图标", () => NPC.downedPlantBoss, value => NPC.downedPlantBoss = value),
            Hard("Leviathan and Anahita", "利维坦和阿娜希塔", "利维坦-图标", () => DownedBossSystem.downedLeviathan, value => DownedBossSystem.downedLeviathan = value),
            Hard("Astrum Aureus", "白金星舰", "白金星舰-图标", () => DownedBossSystem.downedAstrumAureus, value => DownedBossSystem.downedAstrumAureus = value),
            Hard("Golem", "石巨人", "石巨人-图标", () => NPC.downedGolemBoss, value => NPC.downedGolemBoss = value),
            Hard("The Plaguebringer Goliath", "瘟疫使者歌莉娅", "瘟疫使者歌莉娅-图标", () => DownedBossSystem.downedPlaguebringer, value => DownedBossSystem.downedPlaguebringer = value),
            Hard("Duke Fishron", "猪龙鱼公爵", "猪龙鱼公爵-图标", () => NPC.downedFishron, value => NPC.downedFishron = value),
            Hard("Empress of Light", "光之女皇", "光之女皇-图标", () => NPC.downedEmpressOfLight, value => NPC.downedEmpressOfLight = value),
            Hard("Ravager", "毁灭魔像", "毁灭魔像-图标", () => DownedBossSystem.downedRavager, value => DownedBossSystem.downedRavager = value),
            Hard("Lunatic Cultist", "拜月教邪教徒", "拜月教邪教徒-图标", () => NPC.downedAncientCultist, value => NPC.downedAncientCultist = value),
            Hard("Astrum Deus", "星神游龙", "星神游龙-图标", () => DownedBossSystem.downedAstrumDeus, value => DownedBossSystem.downedAstrumDeus = value),
            Hard("Moon Lord", "月亮领主", "月亮领主-图标", () => NPC.downedMoonlord, value => NPC.downedMoonlord = value),

            PostMoon("Profaned Guardians", "亵渎守卫", "亵渎守卫面具", () => DownedBossSystem.downedGuardians, value => DownedBossSystem.downedGuardians = value),
            PostMoon("Dragonfolly", "痴愚金龙", "痴愚金龙-图标", () => DownedBossSystem.downedDragonfolly, value => DownedBossSystem.downedDragonfolly = value),
            PostMoon("Providence, the Profaned Goddess", "亵渎天神，普罗维登斯", "亵渎天神，普罗维登斯-图标", () => DownedBossSystem.downedProvidence, value => DownedBossSystem.downedProvidence = value),
            PostMoon("Storm Weaver", "风暴编织者", "风暴编织者第一阶段地图图标", () => DownedBossSystem.downedStormWeaver, value => DownedBossSystem.downedStormWeaver = value),
            PostMoon("Ceaseless Void", "无尽虚空", "无尽虚空地图图标", () => DownedBossSystem.downedCeaselessVoid, value => DownedBossSystem.downedCeaselessVoid = value),
            PostMoon("Signus, Envoy of the Devourer", "神之使徒西格纳斯", "西格纳斯地图图标", () => DownedBossSystem.downedSignus, value => DownedBossSystem.downedSignus = value),
            PostMoon("Polterghast", "噬魂幽花", "噬魂幽花地图图标（阶段一）", () => DownedBossSystem.downedPolterghast, value => DownedBossSystem.downedPolterghast = value),
            PostMoon("The Old Duke", "硫海遗爵", "硫海遗爵-图标", () => DownedBossSystem.downedBoomerDuke, value => DownedBossSystem.downedBoomerDuke = value),
            PostMoon("The Devourer of Gods", "神明吞噬者", "神明吞噬者-图标", () => DownedBossSystem.downedDoG, value => DownedBossSystem.downedDoG = value),
            PostMoon("Yharon, Dragon of Rebirth", "重生之龙，犽戎", "重生之龙，犽戎-图标", () => DownedBossSystem.downedYharon, value => DownedBossSystem.downedYharon = value),
            PostMoon("Exo Mechs", "星流巨械", "嘉登面具", () => DownedBossSystem.downedExoMechs, value => DownedBossSystem.downedExoMechs = value),
            PostMoon("Supreme Witch, Calamitas", "至尊女巫，灾厄", "至尊女巫，灾厄-图标", () => DownedBossSystem.downedCalamitas, value => DownedBossSystem.downedCalamitas = value)
        };

        public static Color PhaseColor(BossProgressPhase phase)
        {
            return phase switch
            {
                BossProgressPhase.PreHardmode => new Color(104, 196, 132),
                BossProgressPhase.Hardmode => new Color(104, 178, 238),
                BossProgressPhase.PostMoonLord => new Color(224, 140, 238),
                _ => Color.White
            };
        }

        public static void SyncWorldState()
        {
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
        }

        private static BossProgressEntry Pre(string englishName, string chineseName, string textureName, Func<bool> getDowned, Action<bool> setDowned, string progressKey = null)
        {
            return Create(BossProgressPhase.PreHardmode, "PreHardmode", englishName, chineseName, textureName, getDowned, setDowned, progressKey);
        }

        private static BossProgressEntry Hard(string englishName, string chineseName, string textureName, Func<bool> getDowned, Action<bool> setDowned)
        {
            return Create(BossProgressPhase.Hardmode, "Hardmode", englishName, chineseName, textureName, getDowned, setDowned);
        }

        private static BossProgressEntry PostMoon(string englishName, string chineseName, string textureName, Func<bool> getDowned, Action<bool> setDowned)
        {
            return Create(BossProgressPhase.PostMoonLord, "PostMoon", englishName, chineseName, textureName, getDowned, setDowned);
        }

        private static BossProgressEntry Create(BossProgressPhase phase, string folder, string englishName, string chineseName, string textureName, Func<bool> getDowned, Action<bool> setDowned, string progressKey = null)
        {
            return new BossProgressEntry(
                phase,
                englishName,
                chineseName,
                $"CalamityLegendsComeBack/Weapons/A_Tools/DebugTools/BossProgress/Assets/Icons/{folder}/{textureName}",
                getDowned,
                setDowned,
                progressKey ?? englishName);
        }
    }
}
