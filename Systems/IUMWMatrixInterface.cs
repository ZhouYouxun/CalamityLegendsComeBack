using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityLegendsComeBack
{
    public static class IUMWMatrixInterface
    {
        public static bool OpenOrClose(Player player, IEntitySource source) => IUMWMatrixPanel.OpenOrClose(player, source);

        public static bool Close(Player player) => IUMWMatrixPanel.TryCloseExistingPanel(player);
    }

    internal sealed class IUMWMatrixPanel : ModProjectile, IScreenOverlayProjectile
    {
        private const int PanelWidth = 640;
        private const int PanelHeight = 390;
        private const int BorderThickness = 3;

        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;

        public override string Texture => "CalamityLegendsComeBack/Assets/UI/IUMWIcon";

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

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = GetClampedPanelTopLeft(Main.MouseScreen - new Vector2(PanelWidth, PanelHeight) * 0.5f);
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
            bool closePressed = (Main.mouseLeft && Main.mouseLeftRelease) || (Main.mouseRight && Main.mouseRightRelease);

            DrawPanel(panelArea, Projectile.Opacity);
            DrawMatrixRain(panelArea, Projectile.Opacity);
            DrawContent(panelArea, Projectile.Opacity);

            if (!mouseOverPanel && !FadeOut && Projectile.Opacity >= 0.95f && closePressed)
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

        public static bool OpenOrClose(Player player, IEntitySource source)
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
                ModContent.ProjectileType<IUMWMatrixPanel>(),
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.38f, Pitch = 0.16f }, player.Center);
            return false;
        }

        internal static bool TryCloseExistingPanel(Player player)
        {
            int panelType = ModContent.ProjectileType<IUMWMatrixPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != panelType)
                    continue;

                if (projectile.ModProjectile is IUMWMatrixPanel panel)
                    panel.FadeOut = true;
                else
                    projectile.ai[0] = 1f;

                return true;
            }

            return false;
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

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            Color back = new(6, 11, 14, 242);
            Color border = new(70, 255, 188);
            DrawRectangle(panelArea, back * opacity);
            DrawBorder(panelArea, border * opacity, BorderThickness);

            Rectangle innerArea = new(panelArea.X + 9, panelArea.Y + 9, panelArea.Width - 18, panelArea.Height - 18);
            DrawBorder(innerArea, new Color(24, 96, 82, 210) * opacity, 1);

            for (int x = panelArea.X + 24; x < panelArea.Right - 24; x += 32)
                DrawRectangle(new Rectangle(x, panelArea.Y + 14, 1, panelArea.Height - 28), new Color(36, 108, 92, 90) * opacity);

            for (int y = panelArea.Y + 24; y < panelArea.Bottom - 24; y += 28)
                DrawRectangle(new Rectangle(panelArea.X + 14, y, panelArea.Width - 28, 1), new Color(36, 108, 92, 90) * opacity);
        }

        private static void DrawMatrixRain(Rectangle panelArea, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Color rainColor = new Color(56, 255, 168) * (opacity * 0.24f);
            const int columns = 18;
            float time = Main.GlobalTimeWrappedHourly * 22f;

            for (int i = 0; i < columns; i++)
            {
                int x = panelArea.X + 28 + i * 33;
                int y = panelArea.Y + 24 + (int)((time + i * 19) % (PanelHeight - 48));
                string glyph = (i % 3) switch
                {
                    0 => "I",
                    1 => "U",
                    _ => "MW"
                };

                ChatManager.DrawColorCodedString(Main.spriteBatch, font, glyph, new Vector2(x, y), rainColor, 0f, Vector2.Zero, Vector2.One * 0.66f);
            }
        }

        private static void DrawContent(Rectangle panelArea, float opacity)
        {
            Rectangle titleArea = new(panelArea.X + 28, panelArea.Y + 24, panelArea.Width - 56, 42);
            DrawFitText(GetText("UI.GuideTitle"), titleArea, new Color(222, 255, 244), 1.06f, 0.62f, opacity);

            Rectangle subtitleArea = new(panelArea.X + 30, panelArea.Y + 66, panelArea.Width - 60, 30);
            DrawFitText(GetText("UI.GuideSubtitle"), subtitleArea, new Color(120, 255, 202), 0.68f, 0.46f, opacity);

            DrawRectangle(new Rectangle(panelArea.X + 30, panelArea.Y + 104, panelArea.Width - 60, 2), new Color(70, 255, 188) * (opacity * 0.8f));

            Rectangle bodyArea = new(panelArea.X + 34, panelArea.Y + 122, panelArea.Width - 68, 120);
            DrawWrappedText(GetText("UI.GuideBody"), bodyArea, new Color(222, 240, 236), 0.74f, opacity);

            Rectangle bossTitleArea = new(panelArea.X + 34, panelArea.Y + 254, panelArea.Width - 68, 24);
            DrawFitText(GetText("UI.BossListTitle"), bossTitleArea, new Color(130, 255, 210), 0.72f, 0.48f, opacity);

            Rectangle bossArea = new(panelArea.X + 34, panelArea.Y + 282, panelArea.Width - 68, 68);
            DrawWrappedText(GetText("UI.BossList"), bossArea, new Color(210, 232, 228), 0.58f, opacity);

            Rectangle hintArea = new(panelArea.X + 34, panelArea.Bottom - 31, panelArea.Width - 68, 18);
            DrawFitText(GetText("UI.CloseHint"), hintArea, new Color(112, 180, 164), 0.5f, 0.4f, opacity);
        }

        private static string GetText(string suffix) => Language.GetTextValue($"Mods.CalamityLegendsComeBack.{suffix}");

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
            Vector2 position = new(area.X, area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
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

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
