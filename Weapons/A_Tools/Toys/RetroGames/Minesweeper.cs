using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    public class Minesweeper : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<MinesweeperPanel>();

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            if (TryKeepExistingPanel(player))
                return;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                PanelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.56f, Pitch = 0.04f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.24f, Pitch = 0.14f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is MinesweeperPanel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class MinesweeperPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int GridSize = 16;
        private const int MineCount = 40;
        private const int CellSize = 30;
        private const int PanelPadding = 18;
        private const int HeaderHeight = 42;
        private const int FooterHeight = 38;
        private const int SidebarGap = 14;
        private const int SidebarWidth = 134;
        private const int BorderThickness = 2;

        private static readonly Color[] NumberColors =
        {
            Color.Transparent,
            new Color(86, 154, 255),
            new Color(82, 190, 112),
            new Color(242, 90, 90),
            new Color(164, 112, 240),
            new Color(230, 150, 72),
            new Color(70, 204, 204),
            new Color(214, 214, 224),
            new Color(152, 162, 176)
        };

        private readonly bool[,] mines = new bool[GridSize, GridSize];
        private readonly bool[,] revealed = new bool[GridSize, GridSize];
        private readonly bool[,] flagged = new bool[GridSize, GridSize];

        private Point explodedCell = new(-1, -1);
        private int revealedSafeCells;
        private int flaggedCells;
        private bool initialized;
        private bool gameOver;
        private bool won;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BoardPixelSize => GridSize * CellSize;
        private static int PanelWidth => PanelPadding * 2 + BoardPixelSize + SidebarGap + SidebarWidth;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + BoardPixelSize + FooterHeight;
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

            if (owner.HeldItem.type != ModContent.ItemType<Minesweeper>())
                FadeOut = true;
            else
                FadeOut = false;

            Rectangle panelArea = GetPanelArea();
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelArea.Center.ToVector2() : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            if (!initialized)
                ResetGame(Main.player[Projectile.owner], false);

            Player owner = Main.player[Projectile.owner];
            Rectangle panelArea = GetPanelArea();
            Rectangle boardArea = GetBoardArea(panelArea);

            if (!FadeOut && Projectile.Opacity >= 0.92f && !IsInputPaused())
                HandleMouseInput(owner, panelArea, boardArea);

            DrawPanel(panelArea, Projectile.Opacity);
            DrawBoard(boardArea, Projectile.Opacity);
            DrawSidebar(panelArea, boardArea, Projectile.Opacity);
            DrawFooter(panelArea, boardArea, Projectile.Opacity);

            if (gameOver)
                DrawGameOver(boardArea, Projectile.Opacity);

            if (panelArea.Intersects(MouseRectangle))
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public void RequestStayOpen()
        {
            FadeOut = false;
        }

        private void HandleMouseInput(Player owner, Rectangle panelArea, Rectangle boardArea)
        {
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            if (!leftClickPressed && !rightClickPressed)
                return;

            if (won || gameOver)
            {
                if (leftClickPressed && panelArea.Intersects(MouseRectangle))
                    ResetGame(owner, true);

                return;
            }

            if (!boardArea.Intersects(MouseRectangle))
                return;

            Point cell = GetMouseCell(boardArea);
            if (!InGrid(cell))
                return;

            if (rightClickPressed)
            {
                ToggleFlag(cell, owner);
                return;
            }

            if (leftClickPressed)
                RevealCell(cell, owner);
        }

        private void ToggleFlag(Point cell, Player owner)
        {
            if (revealed[cell.X, cell.Y])
                return;

            flagged[cell.X, cell.Y] = !flagged[cell.X, cell.Y];
            flaggedCells += flagged[cell.X, cell.Y] ? 1 : -1;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.28f, Pitch = flagged[cell.X, cell.Y] ? 0.2f : -0.08f }, owner.Center);
            CheckWin(owner);
        }

        private void RevealCell(Point cell, Player owner)
        {
            if (flagged[cell.X, cell.Y] || revealed[cell.X, cell.Y])
                return;

            if (mines[cell.X, cell.Y])
            {
                revealed[cell.X, cell.Y] = true;
                explodedCell = cell;
                gameOver = true;
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = -0.22f }, owner.Center);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.4f, Pitch = -0.12f }, owner.Center);
                return;
            }

            RevealSafeFlood(cell);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.32f, Pitch = 0.1f }, owner.Center);
            CheckWin(owner);
        }

        private void RevealSafeFlood(Point start)
        {
            Queue<Point> queue = new();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Point cell = queue.Dequeue();
                if (!InGrid(cell) || revealed[cell.X, cell.Y] || flagged[cell.X, cell.Y] || mines[cell.X, cell.Y])
                    continue;

                revealed[cell.X, cell.Y] = true;
                revealedSafeCells++;

                if (CountAdjacentMines(cell) != 0)
                    continue;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        queue.Enqueue(new Point(cell.X + x, cell.Y + y));
                    }
                }
            }
        }

        private void ResetGame(Player owner, bool playSound)
        {
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    mines[x, y] = false;
                    revealed[x, y] = false;
                    flagged[x, y] = false;
                }
            }

            int placed = 0;
            while (placed < MineCount)
            {
                int x = Main.rand.Next(GridSize);
                int y = Main.rand.Next(GridSize);
                if (mines[x, y])
                    continue;

                mines[x, y] = true;
                placed++;
            }

            explodedCell = new Point(-1, -1);
            revealedSafeCells = 0;
            flaggedCells = 0;
            gameOver = false;
            won = false;
            initialized = true;

            if (playSound)
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.44f, Pitch = 0.1f }, owner.Center);
        }

        private void CheckWin(Player owner)
        {
            bool allMinesFlagged = true;
            bool hasWrongFlag = false;
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    if (mines[x, y] && !flagged[x, y])
                        allMinesFlagged = false;
                    else if (!mines[x, y] && flagged[x, y])
                        hasWrongFlag = true;
                }
            }

            bool allSafeRevealed = revealedSafeCells >= GridSize * GridSize - MineCount;
            if ((!hasWrongFlag && allMinesFlagged) || allSafeRevealed)
            {
                won = true;
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.58f, Pitch = 0.28f }, owner.Center);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.38f, Pitch = 0.14f }, owner.Center);
            }
        }

        private int CountAdjacentMines(Point cell)
        {
            int count = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Point neighbor = new(cell.X + x, cell.Y + y);
                    if (InGrid(neighbor) && mines[neighbor.X, neighbor.Y])
                        count++;
                }
            }

            return count;
        }

        private int GetCorrectFlagCount()
        {
            int count = 0;
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    if (mines[x, y] && flagged[x, y])
                        count++;
                }
            }

            return count;
        }

        private static bool InGrid(Point cell)
        {
            return cell.X >= 0 && cell.X < GridSize && cell.Y >= 0 && cell.Y < GridSize;
        }

        private static bool IsInputPaused()
        {
            return Main.mapFullscreen || Main.drawingPlayerChat || Main.gameMenu;
        }

        private static Rectangle GetPanelArea()
        {
            const int screenMargin = 16;
            int x = (Main.screenWidth - PanelWidth) / 2;
            int y = (Main.screenHeight - PanelHeight) / 2;
            int maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            int maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            x = Math.Min(Math.Max(x, screenMargin), maxX);
            y = Math.Min(Math.Max(y, screenMargin), maxY);
            return new Rectangle(x, y, PanelWidth, PanelHeight);
        }

        private static Rectangle GetBoardArea(Rectangle panelArea)
        {
            return new Rectangle(
                panelArea.X + PanelPadding,
                panelArea.Y + PanelPadding + HeaderHeight,
                BoardPixelSize,
                BoardPixelSize);
        }

        private static Rectangle GetSidebarArea(Rectangle panelArea, Rectangle boardArea)
        {
            return new Rectangle(
                boardArea.Right + SidebarGap,
                boardArea.Y,
                SidebarWidth,
                boardArea.Height);
        }

        private static Point GetMouseCell(Rectangle boardArea)
        {
            return new Point(
                (int)((Main.MouseScreen.X - boardArea.X) / CellSize),
                (int)((Main.MouseScreen.Y - boardArea.Y) / CellSize));
        }

        private void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(11, 12, 16, 238) * opacity);
            DrawBorder(panelArea, new Color(126, 132, 144) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(42, 44, 52, 220) * opacity, 1);

            DrawTextWithShadow("MINESWEEPER", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 11), new Color(238, 240, 244) * opacity, 0.82f, opacity);
            string state = won
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperWin")
                : gameOver
                    ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperGameOver")
                    : Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperMines", Math.Max(0, MineCount - flaggedCells));

            Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.62f;
            DrawTextWithShadow(state, new Vector2(panelArea.Right - PanelPadding - stateSize.X, panelArea.Y + 17), new Color(206, 214, 226) * opacity, 0.62f, opacity);
        }

        private void DrawBoard(Rectangle boardArea, float opacity)
        {
            DrawRectangle(boardArea, new Color(7, 8, 11, 246) * opacity);
            DrawBorder(boardArea, new Color(84, 90, 104) * opacity, 2);

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Point cell = new(x, y);
                    Rectangle cellArea = GetCellArea(boardArea, x, y);
                    bool showMine = gameOver && mines[x, y] || won && mines[x, y] && flagged[x, y];

                    if (revealed[x, y] && !mines[x, y])
                        DrawRevealedCell(cellArea, cell, opacity);
                    else
                        DrawCoveredCell(cellArea, cell, opacity);

                    if (showMine)
                        DrawMine(cellArea, cell == explodedCell, opacity);
                    else if (flagged[x, y])
                        DrawFlag(cellArea, opacity);
                }
            }
        }

        private void DrawRevealedCell(Rectangle area, Point cell, float opacity)
        {
            DrawRectangle(Shrink(area, 1), new Color(50, 54, 62, 232) * opacity);
            DrawBorder(Shrink(area, 1), new Color(86, 92, 106) * (opacity * 0.55f), 1);

            int adjacent = CountAdjacentMines(cell);
            if (adjacent <= 0)
                return;

            Color numberColor = NumberColors[Math.Min(adjacent, NumberColors.Length - 1)];
            DrawCenteredText(adjacent.ToString(), area, numberColor, 0.74f, opacity);
        }

        private static void DrawCoveredCell(Rectangle area, Point cell, float opacity)
        {
            Color tileColor = (cell.X + cell.Y) % 2 == 0 ? new Color(32, 36, 46, 238) : new Color(28, 32, 42, 238);
            Rectangle inner = Shrink(area, 1);
            DrawRectangle(inner, tileColor * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Y, inner.Width, 4), Color.Lerp(tileColor, Color.White, 0.24f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Y, 4, inner.Height), Color.Lerp(tileColor, Color.White, 0.18f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Bottom - 4, inner.Width, 4), Color.Lerp(tileColor, Color.Black, 0.24f) * opacity);
            DrawRectangle(new Rectangle(inner.Right - 4, inner.Y, 4, inner.Height), Color.Lerp(tileColor, Color.Black, 0.2f) * opacity);
            DrawBorder(inner, new Color(12, 14, 18, 120) * opacity, 1);
        }

        private static void DrawFlag(Rectangle area, float opacity)
        {
            Rectangle inner = Shrink(area, 6);
            int poleX = inner.X + inner.Width / 2 - 2;
            DrawRectangle(new Rectangle(poleX, inner.Y + 3, 4, inner.Height - 2), new Color(220, 220, 226) * opacity);
            DrawRectangle(new Rectangle(poleX - 9, inner.Y + 4, 13, 8), new Color(230, 60, 74) * opacity);
            DrawRectangle(new Rectangle(poleX - 7, inner.Y + 12, 10, 5), new Color(184, 44, 58) * opacity);
            DrawRectangle(new Rectangle(inner.X + 3, inner.Bottom - 3, inner.Width - 6, 4), new Color(116, 122, 136) * opacity);
        }

        private static void DrawMine(Rectangle area, bool exploded, float opacity)
        {
            Rectangle inner = Shrink(area, 7);
            Color back = exploded ? new Color(174, 40, 50) : new Color(40, 42, 48);
            DrawRectangle(Shrink(area, 2), Color.Lerp(new Color(54, 58, 68), back, 0.72f) * opacity);
            DrawRectangle(new Rectangle(inner.Center.X - 2, inner.Y - 4, 4, inner.Height + 8), new Color(18, 18, 22) * opacity);
            DrawRectangle(new Rectangle(inner.X - 4, inner.Center.Y - 2, inner.Width + 8, 4), new Color(18, 18, 22) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Y, inner.Width, inner.Height), new Color(10, 10, 14) * opacity);
            DrawRectangle(new Rectangle(inner.X + 5, inner.Y + 4, 5, 4), new Color(226, 226, 232) * (opacity * 0.74f));
        }

        private void DrawSidebar(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            Rectangle sidebar = GetSidebarArea(panelArea, boardArea);
            Rectangle statBox = new(sidebar.X, sidebar.Y, sidebar.Width, 156);
            DrawInfoBox(statBox, opacity);
            DrawStatLine("MINES", MineCount.ToString(), statBox.X + 12, statBox.Y + 12, opacity);
            DrawStatLine("FLAGS", flaggedCells.ToString(), statBox.X + 12, statBox.Y + 58, opacity);
            DrawStatLine("FOUND", GetCorrectFlagCount().ToString(), statBox.X + 12, statBox.Y + 104, opacity);

            Rectangle sampleBox = new(sidebar.X, statBox.Bottom + 14, sidebar.Width, 128);
            DrawInfoBox(sampleBox, opacity);
            DrawTextWithShadow("MARK", new Vector2(sampleBox.X + 12, sampleBox.Y + 10), new Color(174, 184, 198) * opacity, 0.52f, opacity);
            DrawFlag(new Rectangle(sampleBox.X + 18, sampleBox.Y + 46, 38, 38), opacity);
            DrawMine(new Rectangle(sampleBox.X + 76, sampleBox.Y + 46, 38, 38), false, opacity);

            Rectangle progressBox = new(sidebar.X, sampleBox.Bottom + 14, sidebar.Width, 86);
            DrawInfoBox(progressBox, opacity);
            DrawTextWithShadow("CLEAR", new Vector2(progressBox.X + 12, progressBox.Y + 10), new Color(174, 184, 198) * opacity, 0.52f, opacity);
            int meterWidth = progressBox.Width - 24;
            float clearRatio = revealedSafeCells / (float)(GridSize * GridSize - MineCount);
            DrawRectangle(new Rectangle(progressBox.X + 12, progressBox.Y + 50, meterWidth, 10), new Color(24, 26, 32) * opacity);
            DrawRectangle(new Rectangle(progressBox.X + 12, progressBox.Y + 50, (int)(meterWidth * MathHelper.Clamp(clearRatio, 0f, 1f)), 10), new Color(90, 174, 230) * opacity);
            DrawBorder(new Rectangle(progressBox.X + 12, progressBox.Y + 50, meterWidth, 10), new Color(88, 96, 112) * opacity, 1);
        }

        private void DrawFooter(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            Rectangle footerArea = new(boardArea.X, boardArea.Bottom + 8, panelArea.Width - PanelPadding * 2, 24);
            if (won)
            {
                DrawRectangle(footerArea, new Color(16, 32, 24, 210) * opacity);
                DrawBorder(footerArea, new Color(90, 224, 126) * opacity, 1);
                DrawCenteredText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperWin"), footerArea, new Color(178, 255, 196), 0.62f, opacity);
            }
            else
            {
                DrawRectangle(footerArea, new Color(14, 16, 20, 160) * opacity);
                DrawBorder(footerArea, new Color(46, 52, 62) * opacity, 1);
            }
        }

        private static void DrawInfoBox(Rectangle box, float opacity)
        {
            DrawRectangle(box, new Color(17, 20, 27, 230) * opacity);
            DrawBorder(box, new Color(70, 78, 92) * opacity, 1);
        }

        private static void DrawStatLine(string label, string value, int x, int y, float opacity)
        {
            DrawTextWithShadow(label, new Vector2(x, y), new Color(166, 174, 190) * opacity, 0.5f, opacity);
            DrawTextWithShadow(value, new Vector2(x, y + 19), Color.White * opacity, 0.68f, opacity);
        }

        private static void DrawGameOver(Rectangle boardArea, float opacity)
        {
            Rectangle overlay = new(boardArea.X + 30, boardArea.Y + boardArea.Height / 2 - 56, boardArea.Width - 60, 112);
            DrawRectangle(overlay, new Color(6, 7, 10, 226) * opacity);
            DrawBorder(overlay, new Color(218, 72, 82) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperGameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.MinesweeperRestart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 22, overlay.Width, 28), new Color(255, 210, 210), 0.78f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 60, overlay.Width, 26), new Color(210, 218, 232), 0.52f, opacity);
        }

        private static Rectangle GetCellArea(Rectangle boardArea, int x, int y)
        {
            return new Rectangle(
                boardArea.X + x * CellSize,
                boardArea.Y + y * CellSize,
                CellSize,
                CellSize);
        }

        private static Rectangle Shrink(Rectangle rectangle, int amount)
        {
            return new Rectangle(
                rectangle.X + amount,
                rectangle.Y + amount,
                rectangle.Width - amount * 2,
                rectangle.Height - amount * 2);
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 position = new(area.Center.X - size.X * 0.5f, area.Center.Y - size.Y * 0.5f);
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
        }
    }
}
