using System;
using System.Collections.Generic;
using System.IO;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using CalamityMod.Schematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.StructureExtractor
{
    public sealed class StructureExtractor : ModItem, ILocalizedModType
    {
        private static int OverlayType => ModContent.ProjectileType<StructureExtractorOverlay>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_2622"; // 利刃台风 Blade Tsunami

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(120, 220, 255));
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
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            if (TryKeepExistingOverlay(player))
                return;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                OverlayType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.56f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.28f, Pitch = 0.18f }, player.Center);
        }

        private static bool TryKeepExistingOverlay(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != OverlayType)
                    continue;

                if (projectile.ModProjectile is StructureExtractorOverlay overlay)
                    overlay.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class StructureExtractorOverlay : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int ModalWidth = 560;
        private const int ModalHeight = 238;
        private const int ModalPadding = 18;
        private const int ButtonWidth = 156;
        private const int ButtonHeight = 42;
        private const int ButtonGap = 16;
        private const int BorderThickness = 3;

        private SelectionState selectionState;
        private Point dragStart;
        private Point dragEnd;
        private Rectangle selectedArea;
        private bool wasLeftMouseDown;
        private int buttonPulseTimer;

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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
            Projectile.width = 2;
            Projectile.height = 2;
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

            if (owner.HeldItem.type != ModContent.ItemType<StructureExtractor>())
                FadeOut = true;

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner && !FadeOut)
                UpdateSelection(owner);

            if (buttonPulseTimer > 0)
                buttonPulseTimer--;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];

            if (selectionState == SelectionState.Dragging || selectionState == SelectionState.Confirming)
                DrawSelectionArea(selectedArea, selectionState == SelectionState.Confirming ? 0.82f : 1f);

            if (selectionState == SelectionState.Confirming)
                DrawConfirmationModal(owner);

            return false;
        }

        public void RequestStayOpen()
        {
            FadeOut = false;
        }

        private void UpdateSelection(Player owner)
        {
            if (selectionState == SelectionState.Confirming)
            {
                wasLeftMouseDown = Main.mouseLeft;
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    Main.mouseRightRelease = false;
                    CancelSelection(owner);
                }
                return;
            }

            bool canSelect = CanSelectWorldArea(owner);
            bool leftDown = Main.mouseLeft && canSelect;

            if (leftDown)
            {
                Point mouseTile = GetClampedMouseTile();
                if (!wasLeftMouseDown || selectionState != SelectionState.Dragging)
                {
                    dragStart = mouseTile;
                    dragEnd = mouseTile;
                    selectedArea = CreateSelectionArea(dragStart, dragEnd);
                    selectionState = SelectionState.Dragging;
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.22f }, owner.Center);
                }
                else
                {
                    dragEnd = mouseTile;
                    selectedArea = CreateSelectionArea(dragStart, dragEnd);
                }
            }
            else if (wasLeftMouseDown && selectionState == SelectionState.Dragging)
            {
                dragEnd = GetClampedMouseTile();
                selectedArea = CreateSelectionArea(dragStart, dragEnd);
                selectionState = SelectionState.Confirming;
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.52f, Pitch = 0.12f }, owner.Center);
            }

            wasLeftMouseDown = Main.mouseLeft;
        }

        private static bool CanSelectWorldArea(Player owner)
        {
            return owner.HeldItem.type == ModContent.ItemType<StructureExtractor>() &&
                !Main.mapFullscreen &&
                !Main.drawingPlayerChat &&
                !Main.gameMenu &&
                !Main.playerInventory &&
                !Main.blockMouse &&
                !owner.mouseInterface;
        }

        private static Point GetClampedMouseTile()
        {
            Point mouseTile = Main.MouseWorld.ToTileCoordinates();
            int maxX = Math.Max(0, Main.maxTilesX - 2);
            int maxY = Math.Max(0, Main.maxTilesY - 2);
            return new Point(Clamp(mouseTile.X, 0, maxX), Clamp(mouseTile.Y, 0, maxY));
        }

        private static Rectangle CreateSelectionArea(Point first, Point second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X, second.X);
            int bottom = Math.Max(first.Y, second.Y);

            return new Rectangle(left, top, right - left + 1, bottom - top + 1);
        }

        private void DrawSelectionArea(Rectangle tileArea, float opacity)
        {
            Rectangle screenArea = TileAreaToScreenArea(tileArea);
            Color accent = new(70, 255, 188);
            Color coldAccent = new(62, 192, 255);
            float pulse = 0.72f + MathF.Sin(Main.GlobalTimeWrappedHourly * 7.2f) * 0.18f;

            DrawRectangle(screenArea, new Color(4, 18, 16, 86) * opacity);

            for (int x = screenArea.X; x <= screenArea.Right; x += 16)
                DrawRectangle(new Rectangle(x, screenArea.Y, 1, screenArea.Height), accent * (opacity * 0.16f));

            for (int y = screenArea.Y; y <= screenArea.Bottom; y += 16)
                DrawRectangle(new Rectangle(screenArea.X, y, screenArea.Width, 1), coldAccent * (opacity * 0.12f));

            int sweepY = screenArea.Y + (int)((Main.GlobalTimeWrappedHourly * 72f) % Math.Max(1, screenArea.Height));
            DrawRectangle(new Rectangle(screenArea.X, sweepY, screenArea.Width, 2), accent * (opacity * 0.26f));

            DrawBorder(screenArea, accent * (opacity * pulse), 2);
            DrawBorder(new Rectangle(screenArea.X - 2, screenArea.Y - 2, screenArea.Width + 4, screenArea.Height + 4), coldAccent * (opacity * 0.46f), 1);
            DrawCornerBrackets(screenArea, accent * opacity);

            string label = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorSelectionLabel",
                tileArea.Width,
                tileArea.Height,
                tileArea.X,
                tileArea.Y,
                tileArea.Width * tileArea.Height);

            Vector2 labelPosition = new(screenArea.X + 8, screenArea.Y - 26);
            if (labelPosition.Y < 8f)
                labelPosition.Y = screenArea.Bottom + 8;
            DrawTextWithShadow(label, labelPosition, new Color(202, 255, 238) * opacity, 0.72f, opacity);
        }

        private void DrawConfirmationModal(Player owner)
        {
            Rectangle panelArea = new(
                (Main.screenWidth - ModalWidth) / 2,
                (Main.screenHeight - ModalHeight) / 2,
                ModalWidth,
                ModalHeight);

            Rectangle cancelButton = new(
                panelArea.Center.X - ButtonWidth - ButtonGap / 2,
                panelArea.Bottom - ModalPadding - ButtonHeight,
                ButtonWidth,
                ButtonHeight);

            Rectangle confirmButton = new(
                panelArea.Center.X + ButtonGap / 2,
                panelArea.Bottom - ModalPadding - ButtonHeight,
                ButtonWidth,
                ButtonHeight);

            bool cancelHovered = cancelButton.Intersects(MouseRectangle);
            bool confirmHovered = confirmButton.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;

            DrawMatrixPanel(panelArea, Projectile.Opacity);

            Rectangle titleArea = new(panelArea.X + ModalPadding, panelArea.Y + 16, panelArea.Width - ModalPadding * 2, 32);
            DrawFitText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorConfirmTitle"), titleArea, new Color(222, 255, 244), 0.86f, 0.5f, Projectile.Opacity);

            Rectangle bodyArea = new(panelArea.X + ModalPadding, panelArea.Y + 58, panelArea.Width - ModalPadding * 2, 96);
            string bodyText = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorConfirmBody",
                selectedArea.X,
                selectedArea.Y,
                selectedArea.Width,
                selectedArea.Height,
                selectedArea.Width * selectedArea.Height,
                GetExportDirectory());
            DrawWrappedText(bodyText, bodyArea, new Color(210, 238, 232), 0.64f, Projectile.Opacity);

            DrawButton(cancelButton, Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorCancel"), new Color(255, 116, 108), cancelHovered, false, Projectile.Opacity);
            DrawButton(confirmButton, Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorConfirm"), new Color(70, 255, 188), confirmHovered, buttonPulseTimer > 0, Projectile.Opacity);

            if (leftClickPressed && Projectile.Opacity >= 0.94f)
            {
                if (cancelHovered)
                {
                    Main.mouseLeftRelease = false;
                    CancelSelection(owner);
                }
                else if (confirmHovered)
                {
                    Main.mouseLeftRelease = false;
                    ExportSelectedArea(owner);
                }
            }

            if (panelArea.Intersects(MouseRectangle))
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }
        }

        private void ExportSelectedArea(Player owner)
        {
            DateTime exportStart = DateTime.UtcNow.AddSeconds(-1);
            ExportResult result;

            try
            {
                result = CalamitySchematicIO.ExportSchematic(selectedArea);
            }
            catch (Exception exception)
            {
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorExportError", exception.Message), new Color(255, 116, 108));
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = -0.18f }, owner.Center);
                ResetSelection();
                return;
            }

            if (result != ExportResult.Success)
            {
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorExportFailed", result), new Color(255, 168, 110));
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = -0.12f }, owner.Center);
                ResetSelection();
                return;
            }

            string exportedPath = MoveNewestCalamityExport(exportStart);
            string details = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorExportDetails",
                selectedArea.X,
                selectedArea.Y,
                selectedArea.Width,
                selectedArea.Height,
                selectedArea.Width * selectedArea.Height);

            Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorExportSaved", exportedPath), new Color(112, 255, 190));
            Main.NewText(details, new Color(164, 224, 255));
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.62f, Pitch = 0.22f }, owner.Center);
            buttonPulseTimer = 10;
            ResetSelection();
        }

        private void CancelSelection(Player owner)
        {
            Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.StructureExtractorCanceled"), new Color(255, 160, 126));
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.02f }, owner.Center);
            ResetSelection();
        }

        private void ResetSelection()
        {
            selectionState = SelectionState.Idle;
            selectedArea = Rectangle.Empty;
            dragStart = Point.Zero;
            dragEnd = Point.Zero;
            wasLeftMouseDown = Main.mouseLeft;
        }

        private static string MoveNewestCalamityExport(DateTime exportStartUtc)
        {
            string sourcePath = FindNewestCalamityExport(exportStartUtc);
            if (string.IsNullOrEmpty(sourcePath))
                return Main.SavePath;

            string exportDirectory = GetExportDirectory();
            Directory.CreateDirectory(exportDirectory);

            string fileName = $"{SanitizeFileName(Main.worldName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csch";
            string targetPath = GetUniquePath(Path.Combine(exportDirectory, fileName));

            File.Move(sourcePath, targetPath);
            return targetPath;
        }

        private static string FindNewestCalamityExport(DateTime exportStartUtc)
        {
            if (!Directory.Exists(Main.SavePath))
                return null;

            string newestPath = null;
            DateTime newestWriteTime = DateTime.MinValue;

            foreach (string path in Directory.GetFiles(Main.SavePath, "schematic_*.csch", SearchOption.TopDirectoryOnly))
            {
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime < exportStartUtc || writeTime <= newestWriteTime)
                    continue;

                newestPath = path;
                newestWriteTime = writeTime;
            }

            return newestPath;
        }

        private static string GetExportDirectory() => Path.Combine(Main.SavePath, "CalamityLegendsComeBack", "ExtractedStructures");

        private static string GetUniquePath(string basePath)
        {
            if (!File.Exists(basePath))
                return basePath;

            string directory = Path.GetDirectoryName(basePath);
            string fileName = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);

            for (int i = 2; i < 10000; i++)
            {
                string candidate = Path.Combine(directory, $"{fileName}_{i}{extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(directory, $"{fileName}_{DateTime.Now.Ticks}{extension}");
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "World";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            fileName = fileName.Trim();
            return fileName.Length <= 0 ? "World" : fileName;
        }

        private static Rectangle TileAreaToScreenArea(Rectangle tileArea)
        {
            return new Rectangle(
                (int)(tileArea.X * 16f - Main.screenPosition.X),
                (int)(tileArea.Y * 16f - Main.screenPosition.Y),
                tileArea.Width * 16,
                tileArea.Height * 16);
        }

        private static void DrawMatrixPanel(Rectangle panelArea, float opacity)
        {
            Color accent = new(70, 255, 188);
            DrawRectangle(panelArea, new Color(6, 11, 14, 242) * opacity);
            DrawBorder(panelArea, accent * opacity, BorderThickness);

            Rectangle innerArea = new(panelArea.X + 9, panelArea.Y + 9, panelArea.Width - 18, panelArea.Height - 18);
            DrawBorder(innerArea, new Color(24, 96, 82, 210) * opacity, 1);

            for (int x = panelArea.X + 22; x < panelArea.Right - 22; x += 28)
                DrawRectangle(new Rectangle(x, panelArea.Y + 13, 1, panelArea.Height - 26), new Color(36, 108, 92, 70) * opacity);

            for (int y = panelArea.Y + 22; y < panelArea.Bottom - 22; y += 24)
                DrawRectangle(new Rectangle(panelArea.X + 13, y, panelArea.Width - 26, 1), new Color(36, 108, 92, 70) * opacity);

            int sweepY = panelArea.Y + 14 + (int)((Main.GlobalTimeWrappedHourly * 68f) % Math.Max(1, panelArea.Height - 28));
            DrawRectangle(new Rectangle(panelArea.X + 12, sweepY, panelArea.Width - 24, 2), accent * (opacity * 0.2f));
        }

        private static void DrawButton(Rectangle area, string text, Color accent, bool hovered, bool clicked, float opacity)
        {
            Color back = hovered ? Color.Lerp(new Color(12, 26, 28), accent, 0.22f) : new Color(10, 18, 22);
            if (clicked)
                back = Color.Lerp(back, Color.White, 0.14f);

            DrawRectangle(area, back * (opacity * 0.96f));
            DrawBorder(area, Color.Lerp(accent, Color.White, hovered ? 0.26f : 0f) * opacity, hovered ? 3 : 2);
            DrawFitText(text, new Rectangle(area.X + 10, area.Y + 7, area.Width - 20, area.Height - 14), Color.Lerp(accent, Color.White, 0.38f), 0.72f, 0.44f, opacity);
        }

        private static void DrawCornerBrackets(Rectangle area, Color color)
        {
            const int length = 28;
            const int thickness = 3;

            DrawRectangle(new Rectangle(area.X, area.Y, length, thickness), color);
            DrawRectangle(new Rectangle(area.X, area.Y, thickness, length), color);
            DrawRectangle(new Rectangle(area.Right - length, area.Y, length, thickness), color);
            DrawRectangle(new Rectangle(area.Right - thickness, area.Y, thickness, length), color);
            DrawRectangle(new Rectangle(area.X, area.Bottom - thickness, length, thickness), color);
            DrawRectangle(new Rectangle(area.X, area.Bottom - length, thickness, length), color);
            DrawRectangle(new Rectangle(area.Right - length, area.Bottom - thickness, length, thickness), color);
            DrawRectangle(new Rectangle(area.Right - thickness, area.Bottom - length, thickness, length), color);
        }

        private static void DrawWrappedText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            List<string> lines = WrapText(text, area.Width, scale);
            int maxLines = Math.Max(1, (int)(area.Height / (font.LineSpacing * scale)));

            for (int i = 0; i < lines.Count && i < maxLines; i++)
                DrawTextWithShadow(lines[i], new Vector2(area.X, area.Y + i * font.LineSpacing * scale), color * opacity, scale, opacity);
        }

        private static List<string> WrapText(string text, int width, float scale)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            List<string> lines = new();
            string currentLine = string.Empty;

            foreach (char character in text.Replace("\r", string.Empty))
            {
                if (character == '\n')
                {
                    lines.Add(currentLine.TrimEnd());
                    currentLine = string.Empty;
                    continue;
                }

                string candidate = currentLine + character;
                if (font.MeasureString(candidate).X * scale <= width)
                {
                    currentLine = candidate;
                    continue;
                }

                lines.Add(currentLine.TrimEnd());
                currentLine = character.ToString();
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine.TrimEnd());

            return lines;
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = maxScale;
            if (size.X * scale > area.Width)
                scale = area.Width / size.X;
            if (size.Y * scale > area.Height)
                scale = Math.Min(scale, area.Height / size.Y);

            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = new(area.X + Math.Max(0f, (area.Width - size.X * scale) * 0.5f), area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
        }

        private static void DrawTextWithShadow(string text, Vector2 position, Color color, float scale, float opacity)
        {
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, position, color, 0f, Vector2.Zero, Vector2.One * scale);
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

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }

        private enum SelectionState
        {
            Idle,
            Dragging,
            Confirming
        }
    }
}
