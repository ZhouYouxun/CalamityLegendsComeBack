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
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    public class Snake : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<SnakePanel>();

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

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.56f, Pitch = 0.05f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.24f, Pitch = 0.18f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is SnakePanel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class SnakePanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int GridSize = 15;
        private const int CellSize = 32;
        private const int PanelPadding = 18;
        private const int HeaderHeight = 42;
        private const int SidebarGap = 14;
        private const int SidebarWidth = 132;
        private const int BorderThickness = 2;
        private const int StartingLength = 4;

        private readonly List<Point> snake = new();
        private Point direction = new(1, 0);
        private Point queuedDirection = new(1, 0);
        private Point apple;
        private int moveTimer;
        private int score;
        private int applesEaten;
        private bool initialized;
        private bool gameOver;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BoardPixelSize => GridSize * CellSize;
        private static int PanelWidth => PanelPadding * 2 + BoardPixelSize + SidebarGap + SidebarWidth;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + BoardPixelSize;
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

            if (owner.HeldItem.type != ModContent.ItemType<Snake>())
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
                ResetGame(owner, false);

            if (IsInputPaused())
                return;

            if (gameOver)
            {
                if (JustPressed(Keys.R))
                    ResetGame(owner, true);

                return;
            }

            ReadDirectionInput(owner);

            moveTimer++;
            if (moveTimer < GetMoveInterval())
                return;

            moveTimer = 0;
            direction = queuedDirection;
            StepSnake(owner);
        }

        private void ReadDirectionInput(Player owner)
        {
            Point wanted = queuedDirection;

            if (JustPressed(Keys.W) || JustPressed(Keys.Up))
                wanted = new Point(0, -1);
            else if (JustPressed(Keys.S) || JustPressed(Keys.Down))
                wanted = new Point(0, 1);
            else if (JustPressed(Keys.A) || JustPressed(Keys.Left))
                wanted = new Point(-1, 0);
            else if (JustPressed(Keys.D) || JustPressed(Keys.Right))
                wanted = new Point(1, 0);

            if (wanted == queuedDirection || IsReverse(wanted, direction))
                return;

            queuedDirection = wanted;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.22f, Pitch = 0.22f }, owner.Center);
        }

        private void StepSnake(Player owner)
        {
            Point head = snake[0];
            Point nextHead = new(head.X + direction.X, head.Y + direction.Y);
            bool eatsApple = nextHead == apple;

            if (!InGrid(nextHead) || OccupiesSnake(nextHead, ignoreTail: !eatsApple))
            {
                SetGameOver(owner);
                return;
            }

            snake.Insert(0, nextHead);

            if (eatsApple)
            {
                applesEaten++;
                score += 100 + Math.Max(0, GetSpeedLevel() - 1) * 25;
                SpawnApple();
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.48f, Pitch = 0.18f }, owner.Center);

                if (snake.Count >= GridSize * GridSize)
                    SetGameOver(owner);
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }
        }

        private void ResetGame(Player owner, bool playSound)
        {
            snake.Clear();
            int center = GridSize / 2;
            for (int i = 0; i < StartingLength; i++)
                snake.Add(new Point(center - i, center));

            direction = new Point(1, 0);
            queuedDirection = direction;
            moveTimer = 0;
            score = 0;
            applesEaten = 0;
            gameOver = false;
            initialized = true;
            SpawnApple();

            if (playSound)
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.45f, Pitch = 0.12f }, owner.Center);
        }

        private void SpawnApple()
        {
            if (snake.Count >= GridSize * GridSize)
                return;

            for (int attempts = 0; attempts < 256; attempts++)
            {
                Point candidate = new(Main.rand.Next(GridSize), Main.rand.Next(GridSize));
                if (!OccupiesSnake(candidate, false))
                {
                    apple = candidate;
                    return;
                }
            }

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Point candidate = new(x, y);
                    if (!OccupiesSnake(candidate, false))
                    {
                        apple = candidate;
                        return;
                    }
                }
            }
        }

        private void SetGameOver(Player owner)
        {
            gameOver = true;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = -0.22f }, owner.Center);
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = -0.08f }, owner.Center);
        }

        private bool OccupiesSnake(Point point, bool ignoreTail)
        {
            int count = ignoreTail ? snake.Count - 1 : snake.Count;
            for (int i = 0; i < count; i++)
            {
                if (snake[i] == point)
                    return true;
            }

            return false;
        }

        private int GetSpeedLevel() => Math.Max(1, applesEaten / 4 + 1);

        private int GetMoveInterval()
        {
            return Math.Max(4, 13 - (GetSpeedLevel() - 1));
        }

        private static bool IsReverse(Point next, Point current)
        {
            return next.X == -current.X && next.Y == -current.Y;
        }

        private static bool InGrid(Point point)
        {
            return point.X >= 0 && point.X < GridSize && point.Y >= 0 && point.Y < GridSize;
        }

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

        private void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(9, 12, 16, 238) * opacity);
            DrawBorder(panelArea, new Color(116, 136, 150) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(30, 44, 48, 220) * opacity, 1);

            DrawTextWithShadow("SNAKE", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 11), new Color(226, 244, 232) * opacity, 0.9f, opacity);
            string state = gameOver
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SnakeGameOver")
                : Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SnakeSpeed", GetSpeedLevel());

            Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.62f;
            DrawTextWithShadow(state, new Vector2(panelArea.Right - PanelPadding - stateSize.X, panelArea.Y + 17), new Color(182, 218, 198) * opacity, 0.62f, opacity);
        }

        private void DrawBoard(Rectangle boardArea, float opacity)
        {
            DrawRectangle(boardArea, new Color(4, 8, 8, 246) * opacity);
            DrawBorder(boardArea, new Color(78, 104, 94) * opacity, 2);

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Rectangle cellArea = GetCellArea(boardArea, x, y);
                    Color cellColor = (x + y) % 2 == 0 ? new Color(13, 24, 21, 214) : new Color(10, 20, 18, 214);
                    DrawRectangle(Shrink(cellArea, 1), cellColor * opacity);
                }
            }

            DrawApple(boardArea, opacity);

            for (int i = snake.Count - 1; i >= 0; i--)
            {
                Point segment = snake[i];
                bool head = i == 0;
                DrawSnakeSegment(GetCellArea(boardArea, segment.X, segment.Y), head, i, opacity);
            }
        }

        private void DrawApple(Rectangle boardArea, float opacity)
        {
            Rectangle area = Shrink(GetCellArea(boardArea, apple.X, apple.Y), 5);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f);
            Color appleColor = Color.Lerp(new Color(220, 52, 70), new Color(255, 124, 92), pulse * 0.35f);

            DrawRectangle(area, appleColor * opacity);
            DrawRectangle(new Rectangle(area.X + 5, area.Y + 3, area.Width - 10, 4), Color.Lerp(appleColor, Color.White, 0.32f) * opacity);
            DrawRectangle(new Rectangle(area.Center.X - 2, area.Y - 4, 4, 7), new Color(92, 188, 94) * opacity);
            DrawBorder(area, Color.Lerp(appleColor, Color.Black, 0.2f) * opacity, 1);
        }

        private static void DrawSnakeSegment(Rectangle cellArea, bool head, int index, float opacity)
        {
            Rectangle area = Shrink(cellArea, head ? 3 : 4);
            Color body = head ? new Color(112, 236, 126) : Color.Lerp(new Color(48, 172, 92), new Color(110, 216, 112), MathHelper.Clamp(1f - index * 0.035f, 0.15f, 0.9f));

            DrawRectangle(area, body * opacity);
            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 4), Color.Lerp(body, Color.White, 0.35f) * opacity);
            DrawRectangle(new Rectangle(area.X, area.Bottom - 4, area.Width, 4), Color.Lerp(body, Color.Black, 0.24f) * opacity);
            DrawRectangle(new Rectangle(area.X, area.Y, 4, area.Height), Color.Lerp(body, Color.White, 0.16f) * opacity);
            DrawRectangle(new Rectangle(area.Right - 4, area.Y, 4, area.Height), Color.Lerp(body, Color.Black, 0.18f) * opacity);
            DrawBorder(area, Color.Lerp(body, Color.Black, 0.18f) * opacity, 1);

            if (!head)
                return;

            Color eye = new Color(8, 18, 10) * opacity;
            DrawRectangle(new Rectangle(area.X + 8, area.Y + 8, 4, 4), eye);
            DrawRectangle(new Rectangle(area.Right - 12, area.Y + 8, 4, 4), eye);
        }

        private void DrawSidebar(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            Rectangle sidebar = GetSidebarArea(panelArea, boardArea);
            Rectangle scoreBox = new(sidebar.X, sidebar.Y, sidebar.Width, 154);
            DrawInfoBox(scoreBox, opacity);
            DrawStatLine("SCORE", score.ToString(), scoreBox.X + 12, scoreBox.Y + 12, opacity);
            DrawStatLine("LENGTH", snake.Count.ToString(), scoreBox.X + 12, scoreBox.Y + 58, opacity);
            DrawStatLine("SPEED", GetSpeedLevel().ToString(), scoreBox.X + 12, scoreBox.Y + 104, opacity);

            Rectangle directionBox = new(sidebar.X, scoreBox.Bottom + 14, sidebar.Width, 118);
            DrawInfoBox(directionBox, opacity);
            DrawTextWithShadow("DIR", new Vector2(directionBox.X + 12, directionBox.Y + 10), new Color(166, 194, 178) * opacity, 0.52f, opacity);
            DrawDirectionGlyph(directionBox, opacity);

            Rectangle appleBox = new(sidebar.X, directionBox.Bottom + 14, sidebar.Width, 96);
            DrawInfoBox(appleBox, opacity);
            DrawTextWithShadow("APPLE", new Vector2(appleBox.X + 12, appleBox.Y + 10), new Color(166, 194, 178) * opacity, 0.52f, opacity);
            DrawApplePreview(new Rectangle(appleBox.X + 48, appleBox.Y + 40, 36, 36), opacity);

            Rectangle rhythmBox = new(sidebar.X, appleBox.Bottom + 14, sidebar.Width, 78);
            DrawInfoBox(rhythmBox, opacity);
            DrawTextWithShadow("STEP", new Vector2(rhythmBox.X + 12, rhythmBox.Y + 10), new Color(166, 194, 178) * opacity, 0.52f, opacity);
            int meterWidth = rhythmBox.Width - 24;
            float stepRatio = MathHelper.Clamp(moveTimer / (float)GetMoveInterval(), 0f, 1f);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(18, 24, 22) * opacity);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, (int)(meterWidth * stepRatio), 10), new Color(104, 216, 116) * opacity);
            DrawBorder(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(78, 98, 88) * opacity, 1);
        }

        private void DrawDirectionGlyph(Rectangle box, float opacity)
        {
            Vector2 center = box.Center.ToVector2() + new Vector2(0f, 12f);
            Color active = new Color(112, 236, 126) * opacity;
            Color idle = new Color(42, 62, 52) * opacity;

            DrawArrowCell(new Rectangle((int)center.X - 12, (int)center.Y - 48, 24, 24), queuedDirection.Y < 0 ? active : idle, "W", opacity);
            DrawArrowCell(new Rectangle((int)center.X - 12, (int)center.Y + 20, 24, 24), queuedDirection.Y > 0 ? active : idle, "S", opacity);
            DrawArrowCell(new Rectangle((int)center.X - 46, (int)center.Y - 14, 24, 24), queuedDirection.X < 0 ? active : idle, "A", opacity);
            DrawArrowCell(new Rectangle((int)center.X + 22, (int)center.Y - 14, 24, 24), queuedDirection.X > 0 ? active : idle, "D", opacity);
        }

        private static void DrawArrowCell(Rectangle area, Color color, string text, float opacity)
        {
            DrawRectangle(area, Color.Lerp(new Color(16, 22, 20), color, 0.28f) * opacity);
            DrawBorder(area, color * opacity, 1);
            DrawCenteredText(text, area, Color.White, 0.44f, opacity);
        }

        private static void DrawApplePreview(Rectangle area, float opacity)
        {
            DrawRectangle(area, new Color(224, 58, 72) * opacity);
            DrawRectangle(new Rectangle(area.X + 7, area.Y + 5, area.Width - 14, 5), new Color(255, 132, 104) * opacity);
            DrawRectangle(new Rectangle(area.Center.X - 2, area.Y - 5, 5, 9), new Color(92, 188, 94) * opacity);
            DrawBorder(area, new Color(134, 24, 42) * opacity, 1);
        }

        private static void DrawInfoBox(Rectangle box, float opacity)
        {
            DrawRectangle(box, new Color(14, 22, 20, 230) * opacity);
            DrawBorder(box, new Color(62, 88, 76) * opacity, 1);
        }

        private static void DrawStatLine(string label, string value, int x, int y, float opacity)
        {
            DrawTextWithShadow(label, new Vector2(x, y), new Color(154, 184, 168) * opacity, 0.5f, opacity);
            DrawTextWithShadow(value, new Vector2(x, y + 19), Color.White * opacity, 0.68f, opacity);
        }

        private static void DrawGameOver(Rectangle boardArea, float opacity)
        {
            Rectangle overlay = new(boardArea.X + 26, boardArea.Y + boardArea.Height / 2 - 56, boardArea.Width - 52, 112);
            DrawRectangle(overlay, new Color(5, 8, 7, 226) * opacity);
            DrawBorder(overlay, new Color(220, 76, 88) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SnakeGameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.SnakeRestart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 22, overlay.Width, 28), new Color(255, 212, 212), 0.78f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 60, overlay.Width, 26), new Color(202, 232, 210), 0.52f, opacity);
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
