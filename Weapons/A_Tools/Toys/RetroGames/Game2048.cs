using System;
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
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    public class Game2048 : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/Toys/RetroGames/Game2048";

        private static int PanelType => ModContent.ProjectileType<Game2048Panel>();

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
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.22f, Pitch = 0.18f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is Game2048Panel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class Game2048Panel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int GridSize = 4;
        private const int CellSize = 92;
        private const int PanelPadding = 18;
        private const int HeaderHeight = 56;
        private const int FooterHeight = 76;
        private const int BorderThickness = 2;

        private readonly int[,] board = new int[GridSize, GridSize];

        private int score;
        private int bestTile;
        private bool initialized;
        private bool gameOver;
        private bool won2048;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BoardPixelSize => GridSize * CellSize;
        private static int PanelWidth => PanelPadding * 2 + BoardPixelSize;
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

            FadeOut = owner.HeldItem.type != ModContent.ItemType<Game2048>();

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
            DrawPanel(panelArea, boardArea, Projectile.Opacity);
            DrawBoard(boardArea, Projectile.Opacity);

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

            if (JustPressed(Keys.Left) || JustPressed(Keys.A))
                TrySlide(new Point(-1, 0), owner);
            else if (JustPressed(Keys.Right) || JustPressed(Keys.D))
                TrySlide(new Point(1, 0), owner);
            else if (JustPressed(Keys.Up) || JustPressed(Keys.W))
                TrySlide(new Point(0, -1), owner);
            else if (JustPressed(Keys.Down) || JustPressed(Keys.S))
                TrySlide(new Point(0, 1), owner);
            else if (JustPressed(Keys.R))
                ResetGame(owner);
        }

        private void ResetGame(Player owner)
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                    board[y, x] = 0;
            }

            score = 0;
            bestTile = 0;
            gameOver = false;
            won2048 = false;
            initialized = true;
            SpawnTile();
            SpawnTile();
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.42f, Pitch = 0.15f }, owner.Center);
        }

        private void TrySlide(Point direction, Player owner)
        {
            int[,] previous = CopyBoard();
            int gainedScore = 0;

            for (int line = 0; line < GridSize; line++)
            {
                int[] values = ReadLine(line, direction);
                int[] merged = MergeLine(values, ref gainedScore);
                WriteLine(line, direction, merged);
            }

            if (!BoardChanged(previous))
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.16f, Pitch = -0.38f }, owner.Center);
                return;
            }

            score += gainedScore;
            RefreshBestTile();
            SpawnTile();
            RefreshBestTile();

            if (bestTile >= 2048)
                won2048 = true;

            if (!CanMove())
                gameOver = true;

            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.34f, Pitch = 0.08f }, owner.Center);
        }

        private int[] ReadLine(int line, Point direction)
        {
            int[] values = new int[GridSize];
            for (int i = 0; i < GridSize; i++)
            {
                Point cell = GetLineCell(line, i, direction);
                values[i] = board[cell.Y, cell.X];
            }

            return values;
        }

        private void WriteLine(int line, Point direction, int[] values)
        {
            for (int i = 0; i < GridSize; i++)
            {
                Point cell = GetLineCell(line, i, direction);
                board[cell.Y, cell.X] = values[i];
            }
        }

        private static Point GetLineCell(int line, int index, Point direction)
        {
            if (direction.X < 0)
                return new Point(index, line);

            if (direction.X > 0)
                return new Point(GridSize - 1 - index, line);

            if (direction.Y < 0)
                return new Point(line, index);

            return new Point(line, GridSize - 1 - index);
        }

        private static int[] MergeLine(int[] values, ref int gainedScore)
        {
            int[] compacted = new int[GridSize];
            int count = 0;
            for (int i = 0; i < GridSize; i++)
            {
                if (values[i] > 0)
                    compacted[count++] = values[i];
            }

            int[] merged = new int[GridSize];
            int write = 0;
            for (int i = 0; i < count; i++)
            {
                int value = compacted[i];
                if (i + 1 < count && compacted[i + 1] == value)
                {
                    value *= 2;
                    gainedScore += value;
                    i++;
                }

                merged[write++] = value;
            }

            return merged;
        }

        private void SpawnTile()
        {
            Span<Point> emptyCells = stackalloc Point[GridSize * GridSize];
            int emptyCount = 0;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] == 0)
                        emptyCells[emptyCount++] = new Point(x, y);
                }
            }

            if (emptyCount <= 0)
                return;

            Point chosen = emptyCells[Main.rand.Next(emptyCount)];
            board[chosen.Y, chosen.X] = Main.rand.NextFloat() < 0.9f ? 2 : 4;
        }

        private bool CanMove()
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] == 0)
                        return true;

                    if (x + 1 < GridSize && board[y, x] == board[y, x + 1])
                        return true;

                    if (y + 1 < GridSize && board[y, x] == board[y + 1, x])
                        return true;
                }
            }

            return false;
        }

        private int[,] CopyBoard()
        {
            int[,] copy = new int[GridSize, GridSize];
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                    copy[y, x] = board[y, x];
            }

            return copy;
        }

        private bool BoardChanged(int[,] previous)
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (board[y, x] != previous[y, x])
                        return true;
                }
            }

            return false;
        }

        private void RefreshBestTile()
        {
            bestTile = 0;
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                    bestTile = Math.Max(bestTile, board[y, x]);
            }
        }

        private static bool Down(Keys key) => Main.keyState.IsKeyDown(key);

        private static bool JustPressed(Keys key)
        {
            return Down(key) && !Main.oldKeyState.IsKeyDown(key);
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

        private void DrawPanel(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(20, 22, 26, 240) * opacity);
            DrawBorder(panelArea, new Color(142, 150, 160) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(58, 64, 72, 210) * opacity, 1);

            DrawTextWithShadow("2048", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 12), new Color(245, 244, 236) * opacity, 1f, opacity);

            string scoreText = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Score", score);
            string bestText = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Best", bestTile);
            DrawStatPill(scoreText, new Rectangle(panelArea.Right - PanelPadding - 180, panelArea.Y + 10, 84, 36), opacity);
            DrawStatPill(bestText, new Rectangle(panelArea.Right - PanelPadding - 88, panelArea.Y + 10, 88, 36), opacity);

            Rectangle footer = new(boardArea.X, boardArea.Bottom + 16, boardArea.Width, FooterHeight - 16);
            DrawRectangle(footer, new Color(26, 29, 34, 230) * opacity);
            DrawBorder(footer, new Color(86, 92, 102) * opacity, 1);

            string status = gameOver
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Restart")
                : won2048
                    ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Won")
                    : Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Slide");

            DrawCenteredText(status, footer, new Color(210, 218, 226), 0.58f, opacity);
        }

        private static void DrawStatPill(string text, Rectangle area, float opacity)
        {
            DrawRectangle(area, new Color(38, 42, 48, 235) * opacity);
            DrawBorder(area, new Color(108, 116, 128) * opacity, 1);
            DrawCenteredText(text, area, new Color(238, 240, 235), 0.48f, opacity);
        }

        private void DrawBoard(Rectangle boardArea, float opacity)
        {
            DrawRectangle(boardArea, new Color(34, 37, 42, 245) * opacity);
            DrawBorder(boardArea, new Color(96, 104, 112) * opacity, 2);

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Rectangle cellArea = Shrink(GetCellArea(boardArea, x, y), 6);
                    int value = board[y, x];
                    DrawTile(cellArea, value, opacity);
                }
            }
        }

        private static void DrawTile(Rectangle area, int value, float opacity)
        {
            Color fill = GetTileColor(value);
            DrawRectangle(area, fill * opacity);
            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 5), Color.Lerp(fill, Color.White, 0.22f) * opacity);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 5, area.Width, 5), Color.Lerp(fill, Color.Black, 0.18f) * opacity);
            DrawBorder(area, Color.Lerp(fill, Color.Black, value <= 0 ? 0.05f : 0.24f) * opacity, 1);

            if (value <= 0)
                return;

            string text = value.ToString();
            float scale = value < 100 ? 0.92f : value < 1000 ? 0.76f : 0.62f;
            Color textColor = value <= 4 ? new Color(58, 64, 70) : Color.White;
            DrawCenteredText(text, area, textColor, scale, opacity);
        }

        private static Color GetTileColor(int value)
        {
            return value switch
            {
                0 => new Color(48, 52, 58),
                2 => new Color(226, 224, 216),
                4 => new Color(216, 212, 198),
                8 => new Color(232, 152, 82),
                16 => new Color(230, 124, 78),
                32 => new Color(220, 92, 78),
                64 => new Color(212, 70, 66),
                128 => new Color(216, 178, 78),
                256 => new Color(206, 164, 66),
                512 => new Color(196, 150, 52),
                1024 => new Color(88, 168, 184),
                2048 => new Color(62, 142, 206),
                _ => new Color(80, 90, 104)
            };
        }

        private static void DrawGameOver(Rectangle boardArea, float opacity)
        {
            Rectangle overlay = new(boardArea.X + 20, boardArea.Y + boardArea.Height / 2 - 58, boardArea.Width - 40, 116);
            DrawRectangle(overlay, new Color(8, 10, 12, 226) * opacity);
            DrawBorder(overlay, new Color(220, 88, 92) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048GameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.Game2048Restart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 22, overlay.Width, 30), new Color(255, 218, 218), 0.8f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 62, overlay.Width, 26), new Color(218, 226, 236), 0.54f, opacity);
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

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles, System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles, System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
        }
    }
}
