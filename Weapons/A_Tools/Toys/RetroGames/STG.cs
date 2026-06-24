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
    public class STG : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<STGPanel>();

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

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.58f, Pitch = 0.02f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.3f, Pitch = 0.18f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is STGPanel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class STGPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int PlayWidth = 448;
        private const int PlayHeight = 640;
        private const int PanelPadding = 14;
        private const int HeaderHeight = 40;
        private const int BorderThickness = 2;
        private const int StartingLives = 3;
        private const int MaxPowerLevel = 3;
        private const float PlayerRadius = 13f;

        private readonly List<ArcadeShot> playerShots = new();
        private readonly List<ArcadeShot> enemyShots = new();
        private readonly List<ArcadeEnemy> enemies = new();
        private readonly List<ArcadePowerUp> powerUps = new();
        private readonly List<ArcadeExplosion> explosions = new();

        private Vector2 playerPosition;
        private float playerTilt;
        private int score;
        private int lives;
        private int powerLevel;
        private int fireTimer;
        private int spawnTimer;
        private int invulnerableTimer;
        private int shakeTimer;
        private int survivalTimer;
        private float scrollOffset;
        private bool initialized;
        private bool gameOver;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int PanelWidth => PanelPadding * 2 + PlayWidth;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + PlayHeight;
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

            if (owner.HeldItem.type != ModContent.ItemType<STG>())
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
            Rectangle playArea = GetPlayArea(panelArea);
            DrawPanel(panelArea, playArea, Projectile.Opacity);
            DrawPlayfield(playArea, Projectile.Opacity);
            DrawActors(playArea, Projectile.Opacity);
            DrawHud(playArea, Projectile.Opacity);

            if (gameOver)
                DrawGameOver(playArea, Projectile.Opacity);

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

            scrollOffset += 2.4f;
            if (scrollOffset >= 64f)
                scrollOffset -= 64f;

            if (shakeTimer > 0)
                shakeTimer--;

            UpdateExplosions();

            if (IsInputPaused())
                return;

            if (gameOver)
            {
                UpdateEnemyShots();
                UpdatePowerUps(owner);
                if (JustPressed(Keys.R))
                    ResetGame(owner, true);

                return;
            }

            survivalTimer++;
            if (invulnerableTimer > 0)
                invulnerableTimer--;

            HandlePlayerInput(owner);
            FirePlayerWeapons(owner);
            SpawnEnemies();
            UpdatePlayerShots();
            UpdateEnemies(owner);
            UpdateEnemyShots();
            UpdatePowerUps(owner);
            HandleCollisions(owner);
        }

        private void HandlePlayerInput(Player owner)
        {
            Vector2 movement = Vector2.Zero;
            if (Down(Keys.A) || Down(Keys.Left))
                movement.X -= 1f;
            if (Down(Keys.D) || Down(Keys.Right))
                movement.X += 1f;
            if (Down(Keys.W) || Down(Keys.Up))
                movement.Y -= 1f;
            if (Down(Keys.S) || Down(Keys.Down))
                movement.Y += 1f;

            if (movement.LengthSquared() > 1f)
                movement.Normalize();

            playerPosition += movement * 5.2f;
            playerPosition.X = MathHelper.Clamp(playerPosition.X, 24f, PlayWidth - 24f);
            playerPosition.Y = MathHelper.Clamp(playerPosition.Y, 42f, PlayHeight - 28f);

            float targetTilt = movement.X * 0.18f;
            playerTilt = MathHelper.Lerp(playerTilt, targetTilt, 0.22f);

            if (movement.LengthSquared() > 0f && Main.GameUpdateCount % 10UL == 0UL)
                AddExhaust(playerPosition + new Vector2(0f, 18f));
        }

        private void FirePlayerWeapons(Player owner)
        {
            fireTimer++;
            int interval = powerLevel >= 3 ? 7 : 8;
            if (fireTimer < interval)
                return;

            fireTimer = 0;
            Vector2 muzzle = playerPosition + new Vector2(0f, -18f);
            if (powerLevel <= 1)
            {
                AddPlayerShot(muzzle, new Vector2(0f, -9.4f), 1);
            }
            else if (powerLevel == 2)
            {
                AddPlayerShot(muzzle, new Vector2(0f, -9.6f), 1);
                AddPlayerShot(muzzle + new Vector2(-10f, 1f), new Vector2(-1.15f, -9.05f), 1);
                AddPlayerShot(muzzle + new Vector2(10f, 1f), new Vector2(1.15f, -9.05f), 1);
            }
            else
            {
                AddPlayerShot(muzzle, new Vector2(0f, -10.2f), 1);
                AddPlayerShot(muzzle + new Vector2(-10f, 0f), new Vector2(-1.1f, -9.55f), 1);
                AddPlayerShot(muzzle + new Vector2(10f, 0f), new Vector2(1.1f, -9.55f), 1);
                AddPlayerShot(muzzle + new Vector2(-18f, 7f), new Vector2(-2.15f, -8.75f), 1);
                AddPlayerShot(muzzle + new Vector2(18f, 7f), new Vector2(2.15f, -8.75f), 1);
            }

            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.13f, Pitch = 0.36f, MaxInstances = 6 }, owner.Center);
        }

        private void AddPlayerShot(Vector2 position, Vector2 velocity, int damage)
        {
            playerShots.Add(new ArcadeShot(position, velocity, 4.5f, damage));
        }

        private void SpawnEnemies()
        {
            spawnTimer--;
            if (spawnTimer > 0)
                return;

            float difficulty = MathHelper.Clamp(survivalTimer / (60f * 95f), 0f, 1f);
            spawnTimer = Math.Max(14, Main.rand.Next(28, 54) - (int)(difficulty * 14f));

            int roll = Main.rand.Next(100);
            if (roll < 45)
                SpawnEnemy(ArcadeEnemyKind.Scout);
            else if (roll < 68)
                SpawnEnemy(ArcadeEnemyKind.Sidewinder);
            else if (roll < 87)
                SpawnEnemy(ArcadeEnemyKind.Bomber);
            else
                SpawnEnemy(ArcadeEnemyKind.Looper);

            if (difficulty > 0.45f && Main.rand.NextBool(5))
                SpawnEnemy(ArcadeEnemyKind.Scout);
        }

        private void SpawnEnemy(ArcadeEnemyKind kind)
        {
            ArcadeEnemy enemy = new(kind);
            switch (kind)
            {
                case ArcadeEnemyKind.Scout:
                    enemy.Position = new Vector2(Main.rand.NextFloat(28f, PlayWidth - 28f), -24f);
                    enemy.Velocity = new Vector2(Main.rand.NextFloat(-0.72f, 0.72f), Main.rand.NextFloat(2.5f, 3.45f));
                    enemy.Life = 1;
                    enemy.Score = 100;
                    enemy.Radius = 14f;
                    enemy.ShootInterval = Main.rand.Next(72, 106);
                    break;

                case ArcadeEnemyKind.Sidewinder:
                    bool fromLeft = Main.rand.NextBool();
                    enemy.Position = new Vector2(fromLeft ? -32f : PlayWidth + 32f, Main.rand.NextFloat(92f, 345f));
                    enemy.Velocity = new Vector2(fromLeft ? Main.rand.NextFloat(2.9f, 3.65f) : Main.rand.NextFloat(-3.65f, -2.9f), Main.rand.NextFloat(0.55f, 1.15f));
                    enemy.Life = 2;
                    enemy.Score = 220;
                    enemy.Radius = 16f;
                    enemy.ShootInterval = Main.rand.Next(86, 122);
                    break;

                case ArcadeEnemyKind.Bomber:
                    enemy.Position = new Vector2(Main.rand.NextFloat(44f, PlayWidth - 44f), -36f);
                    enemy.Velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(1.45f, 2f));
                    enemy.Life = 5;
                    enemy.Score = 520;
                    enemy.Radius = 22f;
                    enemy.ShootInterval = Main.rand.Next(62, 82);
                    break;

                default:
                    enemy.Position = new Vector2(Main.rand.NextFloat(72f, PlayWidth - 72f), -30f);
                    enemy.Velocity = new Vector2(0f, 2.15f);
                    enemy.Life = 2;
                    enemy.Score = 300;
                    enemy.Radius = 17f;
                    enemy.BaseX = enemy.Position.X;
                    enemy.TurnDirection = Main.rand.NextBool() ? 1f : -1f;
                    enemy.ShootInterval = Main.rand.Next(74, 110);
                    break;
            }

            enemy.MaxLife = enemy.Life;
            enemy.ShootTimer = Main.rand.Next(enemy.ShootInterval / 2);
            enemies.Add(enemy);
        }

        private void UpdatePlayerShots()
        {
            for (int i = playerShots.Count - 1; i >= 0; i--)
            {
                ArcadeShot shot = playerShots[i];
                shot.Position += shot.Velocity;
                if (shot.Position.Y < -20f || shot.Position.X < -28f || shot.Position.X > PlayWidth + 28f)
                    playerShots.RemoveAt(i);
            }
        }

        private void UpdateEnemies(Player owner)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                ArcadeEnemy enemy = enemies[i];
                enemy.Age++;

                switch (enemy.Kind)
                {
                    case ArcadeEnemyKind.Looper:
                        UpdateLooper(enemy);
                        break;

                    case ArcadeEnemyKind.Sidewinder:
                        enemy.Position += enemy.Velocity;
                        enemy.Rotation = MathHelper.Lerp(enemy.Rotation, enemy.Velocity.X > 0f ? 1.48f : -1.48f, 0.08f);
                        enemy.Velocity.Y += 0.006f;
                        break;

                    case ArcadeEnemyKind.Bomber:
                        enemy.Position += enemy.Velocity;
                        enemy.Position.X += (float)Math.Sin(enemy.Age * 0.035f) * 0.45f;
                        enemy.Rotation = MathHelper.Lerp(enemy.Rotation, enemy.Velocity.X * 0.04f, 0.08f);
                        break;

                    default:
                        enemy.Position += enemy.Velocity;
                        enemy.Rotation = MathHelper.Lerp(enemy.Rotation, enemy.Velocity.X * 0.13f, 0.1f);
                        break;
                }

                enemy.ShootTimer++;
                if (enemy.Position.Y > 38f && enemy.ShootTimer >= enemy.ShootInterval)
                {
                    enemy.ShootTimer = 0;
                    FireEnemyShot(enemy, owner);
                }

                if (enemy.Position.Y > PlayHeight + 46f || enemy.Position.X < -64f || enemy.Position.X > PlayWidth + 64f)
                    enemies.RemoveAt(i);
            }
        }

        private static void UpdateLooper(ArcadeEnemy enemy)
        {
            if (enemy.Age < 96)
            {
                float phase = enemy.Age / 96f * MathHelper.TwoPi;
                enemy.Position.X = enemy.BaseX + (float)Math.Sin(phase) * 48f * enemy.TurnDirection;
                enemy.Position.Y += 1.9f + (float)Math.Sin(phase * 0.5f) * 0.28f;
                enemy.Rotation += 0.14f * enemy.TurnDirection;
                return;
            }

            enemy.Position.Y += 3.05f;
            enemy.Position.X += (float)Math.Sin(enemy.Age * 0.05f) * 1.2f;
            enemy.Rotation = MathHelper.Lerp(enemy.Rotation, 0f, 0.05f);
        }

        private void FireEnemyShot(ArcadeEnemy enemy, Player owner)
        {
            Vector2 aim = playerPosition - enemy.Position;
            if (aim.LengthSquared() < 1f)
                aim = Vector2.UnitY;

            aim.Normalize();
            float speed = enemy.Kind == ArcadeEnemyKind.Bomber ? 3.1f : 3.45f;

            if (enemy.Kind == ArcadeEnemyKind.Bomber)
            {
                AddEnemyShot(enemy.Position + new Vector2(0f, 14f), aim.RotatedBy(-0.2f) * speed);
                AddEnemyShot(enemy.Position + new Vector2(0f, 14f), aim * (speed + 0.25f));
                AddEnemyShot(enemy.Position + new Vector2(0f, 14f), aim.RotatedBy(0.2f) * speed);
                SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.13f, Pitch = -0.18f, MaxInstances = 4 }, owner.Center);
                return;
            }

            AddEnemyShot(enemy.Position + new Vector2(0f, 12f), aim * speed);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.09f, Pitch = 0.22f, MaxInstances = 5 }, owner.Center);
        }

        private void AddEnemyShot(Vector2 position, Vector2 velocity)
        {
            enemyShots.Add(new ArcadeShot(position, velocity, 5.5f, 1));
        }

        private void UpdateEnemyShots()
        {
            for (int i = enemyShots.Count - 1; i >= 0; i--)
            {
                ArcadeShot shot = enemyShots[i];
                shot.Position += shot.Velocity;
                if (shot.Position.Y > PlayHeight + 26f || shot.Position.Y < -32f || shot.Position.X < -32f || shot.Position.X > PlayWidth + 32f)
                    enemyShots.RemoveAt(i);
            }
        }

        private void UpdatePowerUps(Player owner)
        {
            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                ArcadePowerUp powerUp = powerUps[i];
                powerUp.Age++;
                powerUp.Position += powerUp.Velocity;
                powerUp.Position.X += (float)Math.Sin(powerUp.Age * 0.08f) * 0.45f;

                if (!gameOver && Vector2.DistanceSquared(powerUp.Position, playerPosition) <= 28f * 28f)
                {
                    score += 150;
                    if (powerLevel < MaxPowerLevel)
                        powerLevel++;

                    powerUps.RemoveAt(i);
                    AddExplosion(powerUp.Position, 24f, new Color(92, 240, 255), 20);
                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.42f, Pitch = 0.32f }, owner.Center);
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.26f, Pitch = 0.38f }, owner.Center);
                    continue;
                }

                if (powerUp.Position.Y > PlayHeight + 28f)
                    powerUps.RemoveAt(i);
            }
        }

        private void HandleCollisions(Player owner)
        {
            for (int i = playerShots.Count - 1; i >= 0; i--)
            {
                ArcadeShot shot = playerShots[i];
                bool consumed = false;
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    ArcadeEnemy enemy = enemies[j];
                    float radius = shot.Radius + enemy.Radius;
                    if (Vector2.DistanceSquared(shot.Position, enemy.Position) > radius * radius)
                        continue;

                    playerShots.RemoveAt(i);
                    consumed = true;
                    enemy.Life -= shot.Damage;
                    AddExplosion(shot.Position, 12f, new Color(255, 238, 124), 10);
                    SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.12f, Pitch = 0.38f, MaxInstances = 8 }, owner.Center);

                    if (enemy.Life <= 0)
                        DestroyEnemy(j, owner);

                    break;
                }

                if (consumed)
                    continue;
            }

            if (invulnerableTimer > 0)
                return;

            for (int i = enemyShots.Count - 1; i >= 0; i--)
            {
                ArcadeShot shot = enemyShots[i];
                float radius = shot.Radius + PlayerRadius;
                if (Vector2.DistanceSquared(shot.Position, playerPosition) > radius * radius)
                    continue;

                enemyShots.RemoveAt(i);
                DamagePlayer(owner, shot.Position);
                return;
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                ArcadeEnemy enemy = enemies[i];
                float radius = enemy.Radius + PlayerRadius;
                if (Vector2.DistanceSquared(enemy.Position, playerPosition) > radius * radius)
                    continue;

                DestroyEnemy(i, owner, false);
                DamagePlayer(owner, enemy.Position);
                return;
            }
        }

        private void DestroyEnemy(int index, Player owner, bool dropPower = true)
        {
            ArcadeEnemy enemy = enemies[index];
            score += enemy.Score;
            AddExplosion(enemy.Position, enemy.Radius * 2.2f, GetEnemyColor(enemy.Kind), 24);
            enemies.RemoveAt(index);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.32f, Pitch = Main.rand.NextFloat(-0.18f, 0.16f), MaxInstances = 6 }, owner.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.14f, Pitch = Main.rand.NextFloat(0.1f, 0.36f), MaxInstances = 4 }, owner.Center);

            if (!dropPower)
                return;

            int dropChance = powerLevel < MaxPowerLevel ? 5 : 12;
            if (!Main.rand.NextBool(dropChance))
                return;

            powerUps.Add(new ArcadePowerUp(enemy.Position, new Vector2(0f, 2.15f)));
        }

        private void DamagePlayer(Player owner, Vector2 impactPosition)
        {
            lives--;
            invulnerableTimer = 96;
            shakeTimer = 20;
            powerLevel = Math.Max(1, powerLevel - 1);
            AddExplosion(impactPosition, 34f, new Color(255, 94, 62), 24);
            AddExplosion(playerPosition, 28f, new Color(255, 214, 86), 22);

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.48f, Pitch = -0.22f }, owner.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = -0.16f }, owner.Center);

            if (lives <= 0)
                SetGameOver(owner);
        }

        private void SetGameOver(Player owner)
        {
            gameOver = true;
            invulnerableTimer = 0;
            shakeTimer = 34;
            for (int i = 0; i < 7; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(42f, 42f);
                AddExplosion(playerPosition + offset, Main.rand.NextFloat(20f, 48f), new Color(255, 104, 70), Main.rand.Next(22, 34));
            }

            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f, Pitch = -0.18f }, owner.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.68f, Pitch = -0.3f }, owner.Center);
        }

        private void ResetGame(Player owner, bool playSound)
        {
            playerShots.Clear();
            enemyShots.Clear();
            enemies.Clear();
            powerUps.Clear();
            explosions.Clear();

            playerPosition = new Vector2(PlayWidth * 0.5f, PlayHeight - 58f);
            playerTilt = 0f;
            score = 0;
            lives = StartingLives;
            powerLevel = 1;
            fireTimer = 0;
            spawnTimer = 36;
            invulnerableTimer = 120;
            shakeTimer = 0;
            survivalTimer = 0;
            scrollOffset = 0f;
            gameOver = false;
            initialized = true;

            if (playSound)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.48f, Pitch = 0.08f }, owner.Center);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.32f, Pitch = 0.28f }, owner.Center);
            }
        }

        private void AddExplosion(Vector2 position, float radius, Color color, int duration)
        {
            explosions.Add(new ArcadeExplosion(position, radius, color, duration));
        }

        private void AddExhaust(Vector2 position)
        {
            explosions.Add(new ArcadeExplosion(position, 10f, new Color(110, 190, 255), 9));
        }

        private void UpdateExplosions()
        {
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                explosions[i].Timer++;
                if (explosions[i].Timer >= explosions[i].Duration)
                    explosions.RemoveAt(i);
            }
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

        private static Rectangle GetPlayArea(Rectangle panelArea)
        {
            return new Rectangle(
                panelArea.X + PanelPadding,
                panelArea.Y + PanelPadding + HeaderHeight,
                PlayWidth,
                PlayHeight);
        }

        private Vector2 GetShakeOffset()
        {
            if (shakeTimer <= 0)
                return Vector2.Zero;

            float strength = shakeTimer / 20f * 4f;
            return new Vector2(
                (float)Math.Sin(Main.GlobalTimeWrappedHourly * 91f) * strength,
                (float)Math.Cos(Main.GlobalTimeWrappedHourly * 73f) * strength);
        }

        private void DrawPanel(Rectangle panelArea, Rectangle playArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(11, 13, 18, 240) * opacity);
            DrawBorder(panelArea, new Color(126, 136, 150) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(38, 48, 58, 220) * opacity, 1);

            DrawTextWithShadow("STG ARCADE", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 11), new Color(238, 244, 232) * opacity, 0.78f, opacity);
            string scoreText = $"SCORE {score:000000}";
            Vector2 scoreSize = FontAssets.MouseText.Value.MeasureString(scoreText) * 0.56f;
            DrawTextWithShadow(scoreText, new Vector2(playArea.Right - scoreSize.X, panelArea.Y + 17), new Color(255, 226, 130) * opacity, 0.56f, opacity);
        }

        private void DrawPlayfield(Rectangle playArea, float opacity)
        {
            DrawRectangle(playArea, new Color(9, 22, 35, 246) * opacity);
            DrawScrollingBackdrop(playArea, opacity);
            DrawBorder(playArea, new Color(84, 104, 118) * opacity, 2);
        }

        private void DrawScrollingBackdrop(Rectangle playArea, float opacity)
        {
            for (int i = -1; i < 12; i++)
            {
                int y = playArea.Y + (int)((i * 64 + scrollOffset) % (PlayHeight + 64));
                DrawRectangle(new Rectangle(playArea.X, y, PlayWidth, 2), new Color(22, 48, 68, 140) * opacity);
            }

            for (int i = 0; i < 42; i++)
            {
                int x = playArea.X + (i * 83 + 31) % PlayWidth;
                int y = playArea.Y + (int)((i * 137 + scrollOffset * (1.2f + i % 4 * 0.25f)) % PlayHeight);
                int size = i % 5 == 0 ? 3 : 2;
                Color color = i % 3 == 0 ? new Color(110, 172, 196) : new Color(78, 126, 158);
                DrawRectangle(new Rectangle(x, y, size, size), color * (opacity * 0.72f));
            }

            for (int i = 0; i < 4; i++)
            {
                int x = playArea.X + (i * 127 + 46) % (PlayWidth - 72);
                int y = playArea.Y + (int)((i * 181 + scrollOffset * 0.55f) % (PlayHeight + 100)) - 60;
                DrawCloudPatch(new Rectangle(x, y, 72, 28), opacity * 0.34f);
            }
        }

        private static void DrawCloudPatch(Rectangle area, float opacity)
        {
            DrawRectangle(area, new Color(86, 112, 132, 90) * opacity);
            DrawRectangle(new Rectangle(area.X + 10, area.Y - 8, area.Width - 26, area.Height + 4), new Color(102, 130, 148, 76) * opacity);
            DrawRectangle(new Rectangle(area.X + 24, area.Y + 9, area.Width - 20, 12), new Color(52, 78, 98, 86) * opacity);
        }

        private void DrawActors(Rectangle playArea, float opacity)
        {
            Vector2 shake = GetShakeOffset();

            for (int i = powerUps.Count - 1; i >= 0; i--)
                DrawPowerUp(playArea, powerUps[i], shake, opacity);

            for (int i = enemyShots.Count - 1; i >= 0; i--)
                DrawEnemyShot(playArea, enemyShots[i], shake, opacity);

            for (int i = playerShots.Count - 1; i >= 0; i--)
                DrawPlayerShot(playArea, playerShots[i], shake, opacity);

            for (int i = enemies.Count - 1; i >= 0; i--)
                DrawEnemy(playArea, enemies[i], shake, opacity);

            bool visible = invulnerableTimer <= 0 || Main.GameUpdateCount / 5UL % 2UL == 0UL;
            if (!gameOver && visible)
                DrawPlayer(playArea, shake, opacity);

            for (int i = explosions.Count - 1; i >= 0; i--)
                DrawExplosion(playArea, explosions[i], shake, opacity);
        }

        private void DrawHud(Rectangle playArea, float opacity)
        {
            Rectangle scoreBack = new(playArea.Right - 140, playArea.Y + 8, 128, 24);
            DrawRectangle(scoreBack, new Color(7, 10, 14, 160) * opacity);
            DrawBorder(scoreBack, new Color(76, 92, 106) * opacity, 1);
            DrawTextWithShadow(score.ToString("000000"), new Vector2(scoreBack.Right - 78, scoreBack.Y + 4), new Color(255, 226, 126) * opacity, 0.5f, opacity);

            for (int i = 0; i < StartingLives; i++)
            {
                Rectangle lifeBox = new(playArea.X + 10 + i * 22, playArea.Y + 9, 16, 14);
                Color lifeColor = i < lives ? new Color(98, 232, 255) : new Color(48, 56, 64);
                DrawMiniPlane(lifeBox, lifeColor * opacity);
            }

            Rectangle powerBack = new(playArea.X + 10, playArea.Bottom - 25, 90, 15);
            DrawRectangle(powerBack, new Color(7, 10, 14, 150) * opacity);
            DrawBorder(powerBack, new Color(76, 92, 106) * opacity, 1);
            for (int i = 0; i < MaxPowerLevel; i++)
            {
                Rectangle pip = new(powerBack.X + 7 + i * 26, powerBack.Y + 5, 18, 5);
                DrawRectangle(pip, (i < powerLevel ? new Color(255, 212, 92) : new Color(44, 50, 58)) * opacity);
            }
        }

        private void DrawPlayer(Rectangle playArea, Vector2 shake, float opacity)
        {
            Vector2 center = ToScreen(playArea, playerPosition, shake);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, 12f), playerTilt), new Vector2(22f, 26f), playerTilt, new Color(28, 72, 116) * opacity);
            DrawRotatedRectangle(center, new Vector2(12f, 42f), playerTilt, new Color(138, 216, 242) * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, -19f), playerTilt), new Vector2(7f, 12f), playerTilt, new Color(238, 248, 255) * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(-18f, 7f), playerTilt), new Vector2(30f, 8f), playerTilt + 0.05f, new Color(92, 168, 210) * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(18f, 7f), playerTilt), new Vector2(30f, 8f), playerTilt - 0.05f, new Color(92, 168, 210) * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, -4f), playerTilt), new Vector2(7f, 12f), playerTilt, new Color(18, 36, 58) * (opacity * 0.9f));
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(-8f, 18f), playerTilt), new Vector2(5f, 11f), playerTilt, new Color(255, 158, 70) * (opacity * 0.9f));
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(8f, 18f), playerTilt), new Vector2(5f, 11f), playerTilt, new Color(255, 158, 70) * (opacity * 0.9f));
        }

        private static void DrawMiniPlane(Rectangle area, Color color)
        {
            DrawRectangle(new Rectangle(area.Center.X - 2, area.Y, 4, area.Height), color);
            DrawRectangle(new Rectangle(area.X, area.Y + 6, area.Width, 4), color);
            DrawRectangle(new Rectangle(area.Center.X - 5, area.Bottom - 3, 10, 3), color);
        }

        private void DrawEnemy(Rectangle playArea, ArcadeEnemy enemy, Vector2 shake, float opacity)
        {
            Vector2 center = ToScreen(playArea, enemy.Position, shake);
            Color body = GetEnemyColor(enemy.Kind);
            Color wing = Color.Lerp(body, Color.White, enemy.Kind == ArcadeEnemyKind.Bomber ? 0.16f : 0.08f);
            float scale = enemy.Kind == ArcadeEnemyKind.Bomber ? 1.25f : 1f;

            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, 2f * scale), enemy.Rotation), new Vector2(12f * scale, 34f * scale), enemy.Rotation, body * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(-14f * scale, 0f), enemy.Rotation), new Vector2(28f * scale, 8f * scale), enemy.Rotation + 0.04f, wing * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(14f * scale, 0f), enemy.Rotation), new Vector2(28f * scale, 8f * scale), enemy.Rotation - 0.04f, wing * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, 17f * scale), enemy.Rotation), new Vector2(24f * scale, 6f * scale), enemy.Rotation, Color.Lerp(body, Color.Black, 0.18f) * opacity);
            DrawRotatedRectangle(RotatedOffset(center, new Vector2(0f, -12f * scale), enemy.Rotation), new Vector2(7f * scale, 11f * scale), enemy.Rotation, new Color(245, 236, 172) * opacity);

            if (enemy.MaxLife > 2 && enemy.Life < enemy.MaxLife)
                DrawEnemyHealthBar(center, enemy, opacity);
        }

        private static void DrawEnemyHealthBar(Vector2 center, ArcadeEnemy enemy, float opacity)
        {
            Rectangle bar = new((int)center.X - 18, (int)center.Y - 31, 36, 4);
            DrawRectangle(bar, new Color(18, 18, 22, 180) * opacity);
            DrawRectangle(new Rectangle(bar.X, bar.Y, (int)(bar.Width * MathHelper.Clamp(enemy.Life / (float)enemy.MaxLife, 0f, 1f)), bar.Height), new Color(255, 100, 76) * opacity);
        }

        private void DrawPlayerShot(Rectangle playArea, ArcadeShot shot, Vector2 shake, float opacity)
        {
            Vector2 position = ToScreen(playArea, shot.Position, shake);
            DrawRectangle(new Rectangle((int)position.X - 2, (int)position.Y - 9, 4, 18), new Color(176, 248, 255) * opacity);
            DrawRectangle(new Rectangle((int)position.X - 1, (int)position.Y - 13, 2, 7), Color.White * opacity);
        }

        private void DrawEnemyShot(Rectangle playArea, ArcadeShot shot, Vector2 shake, float opacity)
        {
            Vector2 position = ToScreen(playArea, shot.Position, shake);
            DrawRectangle(new Rectangle((int)position.X - 4, (int)position.Y - 4, 8, 8), new Color(255, 92, 66) * opacity);
            DrawRectangle(new Rectangle((int)position.X - 2, (int)position.Y - 2, 4, 4), new Color(255, 220, 96) * opacity);
        }

        private void DrawPowerUp(Rectangle playArea, ArcadePowerUp powerUp, Vector2 shake, float opacity)
        {
            Vector2 position = ToScreen(playArea, powerUp.Position, shake);
            Rectangle box = new((int)position.X - 11, (int)position.Y - 11, 22, 22);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(powerUp.Age * 0.18f);
            Color border = Color.Lerp(new Color(255, 210, 78), Color.White, pulse * 0.35f);
            DrawRectangle(box, new Color(30, 46, 58, 230) * opacity);
            DrawBorder(box, border * opacity, 2);
            DrawCenteredText("P", box, new Color(255, 238, 126), 0.52f, opacity);
        }

        private void DrawExplosion(Rectangle playArea, ArcadeExplosion explosion, Vector2 shake, float opacity)
        {
            float progress = explosion.Timer / (float)Math.Max(1, explosion.Duration);
            float radius = MathHelper.Lerp(4f, explosion.Radius, progress);
            float alpha = 1f - progress;
            Vector2 center = ToScreen(playArea, explosion.Position, shake);
            Color color = Color.Lerp(explosion.Color, new Color(255, 238, 174), 0.35f * (1f - progress)) * (opacity * alpha);

            DrawRectangle(new Rectangle((int)(center.X - radius), (int)center.Y - 2, (int)(radius * 2f), 4), color);
            DrawRectangle(new Rectangle((int)center.X - 2, (int)(center.Y - radius), 4, (int)(radius * 2f)), color);
            DrawRectangle(new Rectangle((int)(center.X - radius * 0.55f), (int)(center.Y - radius * 0.55f), (int)(radius * 1.1f), (int)(radius * 1.1f)), color * 0.45f);
        }

        private static void DrawGameOver(Rectangle playArea, float opacity)
        {
            Rectangle overlay = new(playArea.X + 46, playArea.Y + playArea.Height / 2 - 62, playArea.Width - 92, 124);
            DrawRectangle(overlay, new Color(5, 7, 10, 228) * opacity);
            DrawBorder(overlay, new Color(226, 78, 78) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.STGGameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.STGRestart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 25, overlay.Width, 30), new Color(255, 220, 210), 0.78f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 67, overlay.Width, 26), new Color(214, 232, 238), 0.52f, opacity);
        }

        private static Color GetEnemyColor(ArcadeEnemyKind kind)
        {
            return kind switch
            {
                ArcadeEnemyKind.Bomber => new Color(178, 70, 66),
                ArcadeEnemyKind.Sidewinder => new Color(202, 154, 72),
                ArcadeEnemyKind.Looper => new Color(136, 202, 116),
                _ => new Color(184, 188, 198)
            };
        }

        private static Vector2 ToScreen(Rectangle playArea, Vector2 localPosition, Vector2 shake)
        {
            return new Vector2(playArea.X + localPosition.X, playArea.Y + localPosition.Y) + shake;
        }

        private static Vector2 RotatedOffset(Vector2 center, Vector2 offset, float rotation)
        {
            return center + offset.RotatedBy(rotation);
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

        private static void DrawRotatedRectangle(Vector2 center, Vector2 size, float rotation, Color color)
        {
            Main.spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                center,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0.5f, 0.5f),
                size,
                SpriteEffects.None,
                0f);
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

        private enum ArcadeEnemyKind
        {
            Scout,
            Sidewinder,
            Bomber,
            Looper
        }

        private sealed class ArcadeEnemy
        {
            public ArcadeEnemy(ArcadeEnemyKind kind)
            {
                Kind = kind;
            }

            public ArcadeEnemyKind Kind { get; }
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float BaseX;
            public float TurnDirection = 1f;
            public float Radius;
            public int Life;
            public int MaxLife;
            public int Score;
            public int Age;
            public int ShootTimer;
            public int ShootInterval;
        }

        private sealed class ArcadeShot
        {
            public ArcadeShot(Vector2 position, Vector2 velocity, float radius, int damage)
            {
                Position = position;
                Velocity = velocity;
                Radius = radius;
                Damage = damage;
            }

            public Vector2 Position;
            public Vector2 Velocity;
            public float Radius;
            public int Damage;
        }

        private sealed class ArcadePowerUp
        {
            public ArcadePowerUp(Vector2 position, Vector2 velocity)
            {
                Position = position;
                Velocity = velocity;
            }

            public Vector2 Position;
            public Vector2 Velocity;
            public int Age;
        }

        private sealed class ArcadeExplosion
        {
            public ArcadeExplosion(Vector2 position, float radius, Color color, int duration)
            {
                Position = position;
                Radius = radius;
                Color = color;
                Duration = duration;
            }

            public Vector2 Position;
            public float Radius;
            public Color Color;
            public int Timer;
            public int Duration;
        }
    }
}
