using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.GAMES
{
    public class Tetris : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<TetrisPanel>();

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
            Item.Calamity().devItem = true;
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

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.58f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.26f, Pitch = 0.22f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is TetrisPanel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class TetrisPanel : ModProjectile, ILocalizedModType
    {
        private const int BoardColumns = 10;
        private const int VisibleRows = 20;
        private const int HiddenRows = 2;
        private const int BoardRows = VisibleRows + HiddenRows;
        private const int CellSize = 30;
        private const int PanelPadding = 18;
        private const int HeaderHeight = 44;
        private const int SidebarGap = 16;
        private const int SidebarWidth = 138;
        private const int BorderThickness = 2;
        private const int LockDelay = 28;
        private const int MoveRepeatDelay = 11;
        private const int MoveRepeatRate = 3;
        private const int ClearFlashTime = 14;

        private static readonly Point[][][] PieceCells =
        {
            new Point[][]
            {
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) },
                new[] { new Point(2, 0), new Point(2, 1), new Point(2, 2), new Point(2, 3) },
                new[] { new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(1, 3) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(1, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new[] { new Point(1, 1), new Point(2, 1), new Point(0, 2), new Point(1, 2) },
                new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(2, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(0, 2) }
            },
            new Point[][]
            {
                new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(0, 2), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(2, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2) },
                new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(1, 2) }
            }
        };

        private static readonly Color[] PieceColors =
        {
            new Color(88, 210, 242),
            new Color(244, 214, 74),
            new Color(176, 105, 238),
            new Color(96, 210, 112),
            new Color(238, 82, 90),
            new Color(82, 128, 238),
            new Color(242, 156, 68)
        };

        private static readonly Point[] RotationKicks =
        {
            Point.Zero,
            new Point(-1, 0),
            new Point(1, 0),
            new Point(-2, 0),
            new Point(2, 0),
            new Point(0, -1),
            new Point(-1, -1),
            new Point(1, -1)
        };

        private readonly int[,] board = new int[BoardRows, BoardColumns];
        private readonly List<int> nextQueue = new();
        private int[] clearingRows = Array.Empty<int>();

        private int currentPiece;
        private int currentRotation;
        private int currentX;
        private int currentY;
        private int holdPiece = -1;
        private int fallTimer;
        private int lockTimer;
        private int leftHeldTicks;
        private int rightHeldTicks;
        private int clearTimer;
        private int score;
        private int lines;
        private bool canHold = true;
        private bool initialized;
        private bool gameOver;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BoardPixelWidth => BoardColumns * CellSize;
        private static int BoardPixelHeight => VisibleRows * CellSize;
        private static int PanelWidth => PanelPadding * 2 + BoardPixelWidth + SidebarGap + SidebarWidth;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + BoardPixelHeight;
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

            if (owner.HeldItem.type != ModContent.ItemType<Tetris>())
                FadeOut = true;
            else
                FadeOut = false;

            Rectangle panelArea = GetPanelArea();
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelArea.Center.ToVector2() : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner && !FadeOut && Projectile.Opacity >= 0.92f)
                UpdateGame(owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Rectangle panelArea = GetPanelArea();
            Rectangle boardArea = GetBoardArea(panelArea);
            DrawPanel(panelArea, Projectile.Opacity);
            DrawBoard(boardArea, Projectile.Opacity);
            DrawSidebar(panelArea, boardArea, Projectile.Opacity);

            if (gameOver)
                DrawGameOver(boardArea, Projectile.Opacity);

            if (panelArea.Intersects(MouseRectangle))
            {
                Main.blockMouse = true;
                Main.player[Projectile.owner].mouseInterface = true;
            }

            return false;
        }

        public void RequestStayOpen()
        {
            FadeOut = false;
        }

        private void UpdateGame(Player owner)
        {
            if (!initialized)
                ResetGame(owner);

            if (IsInputPaused())
                return;

            if (gameOver)
            {
                if (JustPressed(Keys.R))
                    ResetGame(owner);

                return;
            }

            if (clearTimer > 0)
            {
                clearTimer--;
                if (clearTimer <= 0)
                    FinishLineClear(owner);

                return;
            }

            HandleHorizontalInput(owner);

            if (JustPressed(Keys.Up) || JustPressed(Keys.W) || JustPressed(Keys.X))
                TryRotate(1, owner);

            if (JustPressed(Keys.Z))
                TryRotate(-1, owner);

            if (JustPressed(Keys.Space))
            {
                HardDrop(owner);
                return;
            }

            if (JustPressed(Keys.C) || JustPressed(Keys.LeftShift) || JustPressed(Keys.RightShift))
                HoldCurrentPiece(owner);

            bool softDrop = Down(Keys.Down) || Down(Keys.S);
            fallTimer++;
            int interval = softDrop ? 2 : GetFallInterval();
            if (fallTimer >= interval)
            {
                fallTimer = 0;
                if (TryMove(0, 1, softDrop ? 1 : 0, owner))
                    return;
            }

            if (!CanPlace(currentPiece, currentX, currentY + 1, currentRotation))
            {
                lockTimer++;
                if (lockTimer >= LockDelay)
                    LockPiece(owner);
            }
            else
                lockTimer = 0;
        }

        private void HandleHorizontalInput(Player owner)
        {
            bool left = Down(Keys.Left) || Down(Keys.A);
            bool right = Down(Keys.Right) || Down(Keys.D);

            if (left && !right)
            {
                leftHeldTicks++;
                rightHeldTicks = 0;
                if (JustPressed(Keys.Left) || JustPressed(Keys.A) || leftHeldTicks > MoveRepeatDelay && leftHeldTicks % MoveRepeatRate == 0)
                    TryMove(-1, 0, 0, owner);
            }
            else if (right && !left)
            {
                rightHeldTicks++;
                leftHeldTicks = 0;
                if (JustPressed(Keys.Right) || JustPressed(Keys.D) || rightHeldTicks > MoveRepeatDelay && rightHeldTicks % MoveRepeatRate == 0)
                    TryMove(1, 0, 0, owner);
            }
            else
            {
                leftHeldTicks = 0;
                rightHeldTicks = 0;
            }
        }

        private bool TryMove(int offsetX, int offsetY, int scoreBonus, Player owner)
        {
            if (!CanPlace(currentPiece, currentX + offsetX, currentY + offsetY, currentRotation))
                return false;

            currentX += offsetX;
            currentY += offsetY;
            if (scoreBonus > 0)
                score += scoreBonus;

            if (offsetX != 0)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.18f, Pitch = 0.34f }, owner.Center);

            if (CanPlace(currentPiece, currentX, currentY + 1, currentRotation))
                lockTimer = 0;

            return true;
        }

        private void TryRotate(int direction, Player owner)
        {
            int nextRotation = WrapRotation(currentRotation + direction);
            for (int i = 0; i < RotationKicks.Length; i++)
            {
                Point kick = RotationKicks[i];
                if (!CanPlace(currentPiece, currentX + kick.X, currentY + kick.Y, nextRotation))
                    continue;

                currentX += kick.X;
                currentY += kick.Y;
                currentRotation = nextRotation;
                lockTimer = 0;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.3f, Pitch = direction > 0 ? 0.12f : -0.08f }, owner.Center);
                return;
            }
        }

        private void HardDrop(Player owner)
        {
            int distance = 0;
            while (CanPlace(currentPiece, currentX, currentY + 1, currentRotation))
            {
                currentY++;
                distance++;
            }

            score += distance * 2;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = -0.18f }, owner.Center);
            LockPiece(owner);
        }

        private void HoldCurrentPiece(Player owner)
        {
            if (!canHold)
                return;

            int held = holdPiece;
            holdPiece = currentPiece;
            canHold = false;

            if (held < 0)
                SpawnNextPiece(owner, false);
            else
                SpawnPiece(held, owner);

            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.28f, Pitch = 0.16f }, owner.Center);
        }

        private void LockPiece(Player owner)
        {
            foreach (Point cell in PieceCells[currentPiece][currentRotation])
            {
                int boardX = currentX + cell.X;
                int boardY = currentY + cell.Y;
                if (!InBoard(boardX, boardY))
                    continue;

                board[boardY, boardX] = currentPiece + 1;
            }

            if (HasBlocksInHiddenRows())
            {
                SetGameOver(owner);
                return;
            }

            clearingRows = FindFullRows();
            if (clearingRows.Length > 0)
            {
                clearTimer = ClearFlashTime;
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.46f, Pitch = 0.26f + clearingRows.Length * 0.05f }, owner.Center);
                return;
            }

            SpawnNextPiece(owner);
        }

        private void FinishLineClear(Player owner)
        {
            int writeRow = BoardRows - 1;
            for (int readRow = BoardRows - 1; readRow >= 0; readRow--)
            {
                if (IsClearingRow(readRow))
                    continue;

                for (int x = 0; x < BoardColumns; x++)
                    board[writeRow, x] = board[readRow, x];

                writeRow--;
            }

            for (int y = writeRow; y >= 0; y--)
            {
                for (int x = 0; x < BoardColumns; x++)
                    board[y, x] = 0;
            }

            int cleared = clearingRows.Length;
            clearingRows = Array.Empty<int>();
            lines += cleared;
            score += GetLineScore(cleared) * GetLevel();
            SpawnNextPiece(owner);
        }

        private void ResetGame(Player owner)
        {
            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                    board[y, x] = 0;
            }

            nextQueue.Clear();
            clearingRows = Array.Empty<int>();
            holdPiece = -1;
            fallTimer = 0;
            lockTimer = 0;
            leftHeldTicks = 0;
            rightHeldTicks = 0;
            clearTimer = 0;
            score = 0;
            lines = 0;
            canHold = true;
            gameOver = false;
            initialized = true;
            RefillQueue();
            SpawnNextPiece(owner, false);
        }

        private void SpawnNextPiece(Player owner, bool resetHold = true)
        {
            EnsureQueue();
            int next = nextQueue[0];
            nextQueue.RemoveAt(0);
            EnsureQueue();
            SpawnPiece(next, owner);
            if (resetHold)
                canHold = true;
        }

        private void SpawnPiece(int piece, Player owner)
        {
            currentPiece = piece;
            currentRotation = 0;
            currentX = 3;
            currentY = 0;
            fallTimer = 0;
            lockTimer = 0;

            if (!CanPlace(currentPiece, currentX, currentY, currentRotation))
                SetGameOver(owner);
        }

        private void SetGameOver(Player owner)
        {
            gameOver = true;
            clearTimer = 0;
            clearingRows = Array.Empty<int>();
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = -0.25f }, owner.Center);
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = -0.12f }, owner.Center);
        }

        private bool CanPlace(int piece, int originX, int originY, int rotation)
        {
            foreach (Point cell in PieceCells[piece][rotation])
            {
                int boardX = originX + cell.X;
                int boardY = originY + cell.Y;

                if (boardX < 0 || boardX >= BoardColumns || boardY >= BoardRows)
                    return false;

                if (boardY >= 0 && board[boardY, boardX] != 0)
                    return false;
            }

            return true;
        }

        private int[] FindFullRows()
        {
            List<int> rows = new();
            for (int y = 0; y < BoardRows; y++)
            {
                bool full = true;
                for (int x = 0; x < BoardColumns; x++)
                {
                    if (board[y, x] != 0)
                    {
                        continue;
                    }

                    full = false;
                    break;
                }

                if (full)
                    rows.Add(y);
            }

            return rows.ToArray();
        }

        private bool HasBlocksInHiddenRows()
        {
            for (int y = 0; y < HiddenRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                {
                    if (board[y, x] != 0)
                        return true;
                }
            }

            return false;
        }

        private bool IsClearingRow(int row)
        {
            for (int i = 0; i < clearingRows.Length; i++)
            {
                if (clearingRows[i] == row)
                    return true;
            }

            return false;
        }

        private void EnsureQueue()
        {
            while (nextQueue.Count < 7)
                RefillQueue();
        }

        private void RefillQueue()
        {
            List<int> bag = new() { 0, 1, 2, 3, 4, 5, 6 };
            while (bag.Count > 0)
            {
                int index = Main.rand.Next(bag.Count);
                nextQueue.Add(bag[index]);
                bag.RemoveAt(index);
            }
        }

        private int GetLevel() => Math.Max(1, lines / 10 + 1);

        private int GetFallInterval()
        {
            int level = GetLevel();
            if (level <= 1)
                return 44;

            if (level <= 9)
                return Math.Max(8, 44 - (level - 1) * 4);

            return Math.Max(4, 10 - (level - 9) / 2);
        }

        private static int GetLineScore(int cleared)
        {
            return cleared switch
            {
                1 => 100,
                2 => 300,
                3 => 500,
                4 => 800,
                _ => 0
            };
        }

        private static bool InBoard(int x, int y)
        {
            return x >= 0 && x < BoardColumns && y >= 0 && y < BoardRows;
        }

        private static int WrapRotation(int rotation)
        {
            rotation %= 4;
            return rotation < 0 ? rotation + 4 : rotation;
        }

        private static bool Down(Keys key) => Main.keyState.IsKeyDown(key);

        private static bool JustPressed(Keys key)
        {
            return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
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
                BoardPixelWidth,
                BoardPixelHeight);
        }

        private static Rectangle GetSidebarArea(Rectangle panelArea, Rectangle boardArea)
        {
            return new Rectangle(
                boardArea.Right + SidebarGap,
                boardArea.Y,
                SidebarWidth,
                boardArea.Height);
        }

        private void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(10, 12, 18, 238) * opacity);
            DrawBorder(panelArea, new Color(116, 132, 156) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(32, 40, 56, 220) * opacity, 1);

            DrawTextWithShadow("TETRIS", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 12), new Color(232, 242, 255) * opacity, 0.92f, opacity);
            string state = gameOver
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisGameOver")
                : Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisLevel", GetLevel());

            Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.62f;
            DrawTextWithShadow(state, new Vector2(panelArea.Right - PanelPadding - stateSize.X, panelArea.Y + 18), new Color(178, 204, 236) * opacity, 0.62f, opacity);
        }

        private void DrawBoard(Rectangle boardArea, float opacity)
        {
            DrawRectangle(boardArea, new Color(4, 6, 10, 245) * opacity);
            DrawBorder(boardArea, new Color(86, 98, 120) * opacity, 2);

            for (int visibleY = 0; visibleY < VisibleRows; visibleY++)
            {
                for (int x = 0; x < BoardColumns; x++)
                {
                    Rectangle cellArea = GetCellArea(boardArea, x, visibleY);
                    DrawRectangle(Shrink(cellArea, 1), new Color(13, 16, 23, 210) * opacity);
                }
            }

            DrawGhostPiece(boardArea, opacity);

            for (int boardY = HiddenRows; boardY < BoardRows; boardY++)
            {
                int visibleY = boardY - HiddenRows;
                for (int x = 0; x < BoardColumns; x++)
                {
                    int value = board[boardY, x];
                    if (value <= 0)
                        continue;

                    float flash = IsClearingRow(boardY) ? 0.45f + 0.35f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 40f) : 0f;
                    DrawBlock(GetCellArea(boardArea, x, visibleY), PieceColors[value - 1], opacity, flash);
                }
            }

            if (!gameOver && clearTimer <= 0)
            {
                foreach (Point cell in PieceCells[currentPiece][currentRotation])
                {
                    int boardX = currentX + cell.X;
                    int boardY = currentY + cell.Y;
                    if (boardY < HiddenRows)
                        continue;

                    DrawBlock(GetCellArea(boardArea, boardX, boardY - HiddenRows), PieceColors[currentPiece], opacity, 0f);
                }
            }
        }

        private void DrawGhostPiece(Rectangle boardArea, float opacity)
        {
            if (gameOver || clearTimer > 0)
                return;

            int ghostY = currentY;
            while (CanPlace(currentPiece, currentX, ghostY + 1, currentRotation))
                ghostY++;

            if (ghostY == currentY)
                return;

            Color color = PieceColors[currentPiece];
            foreach (Point cell in PieceCells[currentPiece][currentRotation])
            {
                int boardX = currentX + cell.X;
                int boardY = ghostY + cell.Y;
                if (boardY < HiddenRows)
                    continue;

                Rectangle area = Shrink(GetCellArea(boardArea, boardX, boardY - HiddenRows), 5);
                DrawBorder(area, color * (opacity * 0.36f), 2);
            }
        }

        private void DrawSidebar(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            Rectangle sidebar = GetSidebarArea(panelArea, boardArea);
            int y = sidebar.Y;
            DrawPieceBox("HOLD", holdPiece, new Rectangle(sidebar.X, y, sidebar.Width, 100), opacity);
            y += 114;
            DrawPieceBox("NEXT", nextQueue.Count > 0 ? nextQueue[0] : -1, new Rectangle(sidebar.X, y, sidebar.Width, 100), opacity);
            y += 114;

            if (nextQueue.Count > 1)
            {
                Rectangle miniBox = new Rectangle(sidebar.X, y, sidebar.Width, 92);
                DrawInfoBox(miniBox, opacity);
                for (int i = 1; i < Math.Min(4, nextQueue.Count); i++)
                    DrawMiniPiece(nextQueue[i], new Vector2(miniBox.X + 24 + (i - 1) * 42, miniBox.Y + 42), opacity * 0.86f, 0.46f);

                y += 106;
            }

            Rectangle scoreBox = new Rectangle(sidebar.X, y, sidebar.Width, 146);
            DrawInfoBox(scoreBox, opacity);
            DrawStatLine("SCORE", score.ToString(), scoreBox.X + 12, scoreBox.Y + 12, opacity);
            DrawStatLine("LINES", lines.ToString(), scoreBox.X + 12, scoreBox.Y + 56, opacity);
            DrawStatLine("LEVEL", GetLevel().ToString(), scoreBox.X + 12, scoreBox.Y + 100, opacity);

            Rectangle rhythmBox = new Rectangle(sidebar.X, scoreBox.Bottom + 14, sidebar.Width, 82);
            DrawInfoBox(rhythmBox, opacity);
            float lockRatio = !CanPlace(currentPiece, currentX, currentY + 1, currentRotation) ? lockTimer / (float)LockDelay : 0f;
            int meterWidth = rhythmBox.Width - 24;
            DrawTextWithShadow("LOCK", new Vector2(rhythmBox.X + 12, rhythmBox.Y + 10), new Color(184, 202, 228) * opacity, 0.52f, opacity);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(20, 24, 32) * opacity);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, (int)(meterWidth * MathHelper.Clamp(lockRatio, 0f, 1f)), 10), new Color(242, 190, 84) * opacity);
            DrawBorder(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(94, 104, 124) * opacity, 1);
        }

        private void DrawPieceBox(string label, int piece, Rectangle box, float opacity)
        {
            DrawInfoBox(box, opacity);
            DrawTextWithShadow(label, new Vector2(box.X + 12, box.Y + 9), new Color(184, 202, 228) * opacity, 0.54f, opacity);
            if (piece >= 0)
                DrawMiniPiece(piece, box.Center.ToVector2() + new Vector2(-6f, 14f), opacity, 0.7f);
        }

        private static void DrawInfoBox(Rectangle box, float opacity)
        {
            DrawRectangle(box, new Color(16, 20, 28, 230) * opacity);
            DrawBorder(box, new Color(70, 82, 104) * opacity, 1);
        }

        private static void DrawStatLine(string label, string value, int x, int y, float opacity)
        {
            DrawTextWithShadow(label, new Vector2(x, y), new Color(152, 170, 198) * opacity, 0.5f, opacity);
            DrawTextWithShadow(value, new Vector2(x, y + 18), Color.White * opacity, 0.68f, opacity);
        }

        private static void DrawMiniPiece(int piece, Vector2 center, float opacity, float scale)
        {
            Color color = PieceColors[piece];
            Point[] cells = PieceCells[piece][0];
            Rectangle bounds = GetPieceBounds(cells);
            float miniCell = CellSize * scale;
            Vector2 origin = center - new Vector2(bounds.Width * miniCell, bounds.Height * miniCell) * 0.5f;

            foreach (Point cell in cells)
            {
                float drawX = origin.X + (cell.X - bounds.X) * miniCell;
                float drawY = origin.Y + (cell.Y - bounds.Y) * miniCell;
                DrawBlock(new Rectangle((int)drawX, (int)drawY, (int)miniCell, (int)miniCell), color, opacity, 0f);
            }
        }

        private static Rectangle GetPieceBounds(Point[] cells)
        {
            int minX = cells[0].X;
            int maxX = cells[0].X;
            int minY = cells[0].Y;
            int maxY = cells[0].Y;

            for (int i = 1; i < cells.Length; i++)
            {
                minX = Math.Min(minX, cells[i].X);
                maxX = Math.Max(maxX, cells[i].X);
                minY = Math.Min(minY, cells[i].Y);
                maxY = Math.Max(maxY, cells[i].Y);
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void DrawGameOver(Rectangle boardArea, float opacity)
        {
            Rectangle overlay = new Rectangle(boardArea.X + 16, boardArea.Y + boardArea.Height / 2 - 56, boardArea.Width - 32, 112);
            DrawRectangle(overlay, new Color(6, 8, 12, 224) * opacity);
            DrawBorder(overlay, new Color(220, 88, 92) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisGameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisRestart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 22, overlay.Width, 28), new Color(255, 212, 212), 0.78f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 60, overlay.Width, 26), new Color(202, 218, 238), 0.52f, opacity);
        }

        private static Rectangle GetCellArea(Rectangle boardArea, int x, int visibleY)
        {
            return new Rectangle(
                boardArea.X + x * CellSize,
                boardArea.Y + visibleY * CellSize,
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

        private static void DrawBlock(Rectangle area, Color color, float opacity, float flash)
        {
            Rectangle inner = Shrink(area, 2);
            Color fill = Color.Lerp(color, Color.White, flash);
            DrawRectangle(inner, fill * (opacity * 0.95f));
            DrawRectangle(new Rectangle(inner.X, inner.Y, inner.Width, 4), Color.Lerp(fill, Color.White, 0.42f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Y, 4, inner.Height), Color.Lerp(fill, Color.White, 0.25f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Bottom - 4, inner.Width, 4), Color.Lerp(fill, Color.Black, 0.32f) * opacity);
            DrawRectangle(new Rectangle(inner.Right - 4, inner.Y, 4, inner.Height), Color.Lerp(fill, Color.Black, 0.28f) * opacity);
            DrawBorder(inner, Color.Lerp(fill, Color.Black, 0.18f) * opacity, 1);
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
            overWiresUI.Add(index);
        }
    }
}
