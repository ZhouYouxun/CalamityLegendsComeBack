using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;

namespace CalamityLegendsComeBack
{
    // A draw-only inventory overlay so it remains responsive while single-player auto-pause is active.
    internal sealed class LegendarySupplyBoxSelectionUI : ModSystem
    {
        private enum ShowcaseState
        {
            Browsing,
            Spinning,
            ReadyToClaim
        }

        private const int PanelWidth = 1000;
        private const int PanelHeight = 470;
        private const int SpinDuration = 175;
        private const float CardStep = 142f;

        private static bool isOpen;
        private static ShowcaseState state;
        private static int browseIndex;
        private static int chosenIndex;
        private static int spinTimer;
        private static int lastSpinStep;
        private static float carouselPosition;
        private static float spinStart;
        private static float spinTarget;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Legendary Supply Box Showcase",
                () =>
                {
                    Draw(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI));
        }

        internal static void Open()
        {
            isOpen = true;
            state = ShowcaseState.Browsing;
            browseIndex = 0;
            chosenIndex = 0;
            carouselPosition = 0f;
            spinTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.72f, Pitch = -0.06f });
        }

        private static void Close(bool playSound = true)
        {
            if (!isOpen)
                return;

            isOpen = false;
            if (playSound)
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.06f });
        }

        private static void Draw(SpriteBatch spriteBatch)
        {
            if (Main.netMode == NetmodeID.Server || !isOpen)
                return;

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead || !Main.playerInventory)
            {
                Close(false);
                return;
            }

            int width = Math.Min(PanelWidth, Main.screenWidth - 24);
            int height = Math.Min(PanelHeight, Main.screenHeight - 24);
            Rectangle panel = Utils.CenteredRectangle(Main.ScreenSize.ToVector2() * 0.5f, new Vector2(width, height));
            bool mouseOverPanel = panel.Contains(Main.mouseX, Main.mouseY);
            bool click = Main.mouseLeft && Main.mouseLeftRelease;

            if (state == ShowcaseState.Spinning)
                UpdateSpin();

            DrawBackdrop(spriteBatch, panel);
            DrawHeader(spriteBatch, panel);
            DrawCarousel(spriteBatch, panel);
            DrawFooter(spriteBatch, panel);

            if (mouseOverPanel)
            {
                player.mouseInterface = true;
                Main.blockMouse = true;
            }

            if (click)
                HandleClick(panel, mouseOverPanel);
        }

        private static void UpdateSpin()
        {
            spinTimer++;
            float completion = MathHelper.Clamp(spinTimer / (float)SpinDuration, 0f, 1f);
            float easedCompletion = 1f - MathF.Pow(1f - completion, 3.4f);
            carouselPosition = MathHelper.Lerp(spinStart, spinTarget, easedCompletion);
            int currentStep = (int)MathF.Floor(carouselPosition);
            if (currentStep != lastSpinStep)
            {
                lastSpinStep = currentStep;
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.34f, Pitch = MathHelper.Lerp(0.42f, -0.12f, completion) });
            }

            if (spinTimer < SpinDuration)
                return;

            carouselPosition = spinTarget;
            browseIndex = PositiveMod((int)spinTarget, LegendarySupplyBox.GetMainLegendaryWeapons().Length);
            state = ShowcaseState.ReadyToClaim;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.66f, Pitch = 0.2f });
        }

        private static void HandleClick(Rectangle panel, bool mouseOverPanel)
        {
            if (!mouseOverPanel)
            {
                if (state != ShowcaseState.Spinning)
                    Close();
                return;
            }

            if (state == ShowcaseState.Spinning)
                return;

            Rectangle leftArrow = GetArrowArea(panel, false);
            Rectangle rightArrow = GetArrowArea(panel, true);
            if (state == ShowcaseState.Browsing)
            {
                if (leftArrow.Contains(Main.mouseX, Main.mouseY))
                {
                    browseIndex = PositiveMod(browseIndex - 1, LegendarySupplyBox.GetMainLegendaryWeapons().Length);
                    carouselPosition = browseIndex;
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f, Pitch = 0.03f });
                    return;
                }

                if (rightArrow.Contains(Main.mouseX, Main.mouseY))
                {
                    browseIndex = PositiveMod(browseIndex + 1, LegendarySupplyBox.GetMainLegendaryWeapons().Length);
                    carouselPosition = browseIndex;
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f, Pitch = 0.18f });
                    return;
                }

                int hoveredIndex = GetHoveredCarouselIndex(panel);
                if (hoveredIndex >= 0)
                    BeginSpin(hoveredIndex);

                return;
            }

            Rectangle claimArea = GetClaimArea(panel);
            if (!claimArea.Contains(Main.mouseX, Main.mouseY))
                return;

            LegendarySupplyBoxPackets.RequestClaim(chosenIndex);
            Close(false);
        }

        private static void BeginSpin(int selectionIndex)
        {
            int count = LegendarySupplyBox.GetMainLegendaryWeapons().Length;
            chosenIndex = selectionIndex;
            spinStart = browseIndex;
            int forwardDistance = PositiveMod(selectionIndex - browseIndex, count);
            spinTarget = browseIndex + count * 4 + forwardDistance;
            carouselPosition = spinStart;
            spinTimer = 0;
            lastSpinStep = (int)MathF.Floor(spinStart);
            state = ShowcaseState.Spinning;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.46f, Pitch = 0.34f });
        }

        private static void DrawBackdrop(SpriteBatch spriteBatch, Rectangle panel)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
            DrawRectangle(spriteBatch, panel, new Color(3, 8, 15, 242));
            DrawBorder(spriteBatch, panel, Color.Lerp(new Color(44, 190, 255), new Color(255, 194, 78), pulse), 3);
            DrawBorder(spriteBatch, new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 16, panel.Height - 16), new Color(44, 104, 152, 190), 1);

            for (int x = panel.X + 22; x < panel.Right - 22; x += 32)
                DrawRectangle(spriteBatch, new Rectangle(x, panel.Y + 16, 1, panel.Height - 32), new Color(42, 112, 170, 30));
            for (int y = panel.Y + 22; y < panel.Bottom - 22; y += 28)
                DrawRectangle(spriteBatch, new Rectangle(panel.X + 16, y, panel.Width - 32, 1), new Color(42, 112, 170, 24));
        }

        private static void DrawHeader(SpriteBatch spriteBatch, Rectangle panel)
        {
            DrawCenteredText(spriteBatch, Text("Title"), new Rectangle(panel.X + 38, panel.Y + 24, panel.Width - 76, 42), new Color(224, 246, 255), 1.1f, 0.7f);
            string subtitle = state switch
            {
                ShowcaseState.Browsing => Text("BrowseHint"),
                ShowcaseState.Spinning => Text("SpinningHint"),
                _ => Text("ClaimHint")
            };
            DrawCenteredText(spriteBatch, subtitle, new Rectangle(panel.X + 48, panel.Y + 68, panel.Width - 96, 26), new Color(112, 201, 255), 0.62f, 0.42f);
            DrawCenteredText(spriteBatch, $"{LegendarySupplyBox.GetMainLegendaryWeapons().Length:00}  MAIN LEGENDARY WEAPONS", new Rectangle(panel.X + 48, panel.Y + 98, panel.Width - 96, 20), new Color(255, 200, 100), 0.48f, 0.38f);
        }

        private static void DrawCarousel(SpriteBatch spriteBatch, Rectangle panel)
        {
            Rectangle carousel = new(panel.X + 82, panel.Y + 136, panel.Width - 164, 218);
            DrawRectangle(spriteBatch, carousel, new Color(5, 16, 29, 212));
            DrawBorder(spriteBatch, carousel, new Color(50, 122, 182, 180), 1);

            float fractional = carouselPosition - MathF.Floor(carouselPosition);
            int baseIndex = (int)MathF.Floor(carouselPosition);
            int[] weapons = LegendarySupplyBox.GetMainLegendaryWeapons();
            for (int offset = -4; offset <= 4; offset++)
            {
                float x = carousel.Center.X + (offset - fractional) * CardStep;
                int selectionIndex = PositiveMod(baseIndex + offset, weapons.Length);
                bool center = MathF.Abs(offset - fractional) < 0.5f;
                Rectangle card = GetCardArea(x, carousel.Center.Y, center);
                if (card.Right < carousel.Left || card.Left > carousel.Right)
                    continue;

                bool hovered = state == ShowcaseState.Browsing && card.Contains(Main.mouseX, Main.mouseY);
                DrawWeaponCard(spriteBatch, card, weapons[selectionIndex], center, hovered);
            }

            Rectangle selector = new(carousel.Center.X - 80, carousel.Y - 3, 160, carousel.Height + 6);
            DrawBorder(spriteBatch, selector, state == ShowcaseState.ReadyToClaim ? new Color(255, 210, 102) : new Color(132, 224, 255), 2);
            DrawArrowButton(spriteBatch, GetArrowArea(panel, false), false);
            DrawArrowButton(spriteBatch, GetArrowArea(panel, true), true);

            if (state == ShowcaseState.ReadyToClaim)
            {
                int type = LegendarySupplyBox.GetWeaponType(chosenIndex);
                DrawCenteredText(spriteBatch, Lang.GetItemNameValue(type), new Rectangle(panel.X + 78, panel.Y + 364, panel.Width - 156, 28), Color.White, 0.8f, 0.52f);
                DrawClaimButton(spriteBatch, GetClaimArea(panel));
            }
        }

        private static void DrawWeaponCard(SpriteBatch spriteBatch, Rectangle card, int itemType, bool center, bool hovered)
        {
            Color border = hovered ? new Color(255, 221, 126) : center ? new Color(130, 228, 255) : new Color(55, 113, 168);
            Color fill = hovered ? new Color(27, 51, 66, 242) : new Color(8, 24, 42, 228);
            DrawRectangle(spriteBatch, card, fill);
            DrawBorder(spriteBatch, card, border, center ? 2 : 1);

            Texture2D icon = TextureAssets.Item[itemType].Value;
            float iconScale = Math.Min(0.78f, Math.Min((card.Width - 24f) / icon.Width, (card.Height - 66f) / icon.Height));
            spriteBatch.Draw(icon, new Vector2(card.Center.X, card.Y + 68f), null, Color.White, 0f, icon.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);
            DrawCenteredText(spriteBatch, Lang.GetItemNameValue(itemType), new Rectangle(card.X + 7, card.Bottom - 48, card.Width - 14, 38), center ? Color.White : new Color(175, 220, 248), 0.56f, 0.38f);
        }

        private static void DrawFooter(SpriteBatch spriteBatch, Rectangle panel)
        {
            if (state == ShowcaseState.ReadyToClaim)
                return;

            string footer = state == ShowcaseState.Browsing ? Text("Footer") : Text("SpinFooter");
            DrawCenteredText(spriteBatch, footer, new Rectangle(panel.X + 50, panel.Bottom - 50, panel.Width - 100, 24), new Color(91, 161, 204), 0.52f, 0.38f);
        }

        private static void DrawClaimButton(SpriteBatch spriteBatch, Rectangle area)
        {
            bool hovered = area.Contains(Main.mouseX, Main.mouseY);
            DrawRectangle(spriteBatch, area, hovered ? new Color(76, 115, 38, 244) : new Color(42, 76, 28, 236));
            DrawBorder(spriteBatch, area, hovered ? new Color(255, 238, 150) : new Color(161, 226, 104), 2);
            DrawCenteredText(spriteBatch, Text("ClaimButton"), area, hovered ? Color.White : new Color(232, 255, 200), 0.76f, 0.5f);
        }

        private static void DrawArrowButton(SpriteBatch spriteBatch, Rectangle area, bool right)
        {
            bool hovered = state == ShowcaseState.Browsing && area.Contains(Main.mouseX, Main.mouseY);
            DrawRectangle(spriteBatch, area, hovered ? new Color(38, 83, 122, 238) : new Color(13, 38, 63, 226));
            DrawBorder(spriteBatch, area, hovered ? new Color(214, 247, 255) : new Color(85, 161, 212), 2);
            DrawCenteredText(spriteBatch, right ? ">" : "<", area, hovered ? Color.White : new Color(139, 218, 255), 1.55f, 0.8f);
        }

        private static Rectangle GetArrowArea(Rectangle panel, bool right) => new(right ? panel.Right - 64 : panel.X + 22, panel.Y + 206, 42, 78);

        private static Rectangle GetClaimArea(Rectangle panel) => new(panel.Center.X - 152, panel.Bottom - 68, 304, 40);

        private static Rectangle GetCardArea(float centerX, int centerY, bool center)
        {
            int width = center ? 150 : 124;
            int height = center ? 186 : 162;
            return new Rectangle((int)centerX - width / 2, centerY - height / 2, width, height);
        }

        private static int GetHoveredCarouselIndex(Rectangle panel)
        {
            Rectangle carousel = new(panel.X + 82, panel.Y + 136, panel.Width - 164, 218);
            float fractional = carouselPosition - MathF.Floor(carouselPosition);
            int baseIndex = (int)MathF.Floor(carouselPosition);
            int count = LegendarySupplyBox.GetMainLegendaryWeapons().Length;
            for (int offset = 4; offset >= -4; offset--)
            {
                float x = carousel.Center.X + (offset - fractional) * CardStep;
                bool center = MathF.Abs(offset - fractional) < 0.5f;
                if (GetCardArea(x, carousel.Center.Y, center).Contains(Main.mouseX, Main.mouseY))
                    return PositiveMod(baseIndex + offset, count);
            }

            return -1;
        }

        private static int PositiveMod(int value, int divisor) => (value % divisor + divisor) % divisor;

        private static string Text(string key) => Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Consumables.LegendarySupplyBox.{key}");

        private static void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle area, Color color, float maxScale, float minScale)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y));
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = area.Center.ToVector2() - size * scale * 0.5f;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, position, color, 0f, Vector2.Zero, Vector2.One * scale);
        }

        private static void DrawRectangle(SpriteBatch spriteBatch, Rectangle area, Color color) => spriteBatch.Draw(TextureAssets.MagicPixel.Value, area, color);

        private static void DrawBorder(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness)
        {
            DrawRectangle(spriteBatch, new Rectangle(area.X, area.Y, area.Width, thickness), color);
            DrawRectangle(spriteBatch, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
            DrawRectangle(spriteBatch, new Rectangle(area.X, area.Y, thickness, area.Height), color);
            DrawRectangle(spriteBatch, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
        }
    }
}
