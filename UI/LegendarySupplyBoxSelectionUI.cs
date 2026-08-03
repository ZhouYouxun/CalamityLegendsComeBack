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

namespace CalamityLegendsComeBack
{
    // A draw-only inventory overlay so it remains responsive while single-player auto-pause is active.
    internal sealed class LegendarySupplyBoxSelectionUI : ModSystem
    {
        private const int PanelWidth = 1000;
        private const int PanelHeight = 660;
        private const int ColumnCount = 4;
        private const int CardWidth = 198;
        private const int CardHeight = 122;
        private const int CardGap = 12;

        private static bool isOpen;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Legendary Supply Box Selection",
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

            DrawBackdrop(spriteBatch, panel);
            DrawHeader(spriteBatch, panel);
            DrawWeaponGrid(spriteBatch, panel);
            DrawFooter(spriteBatch, panel);

            if (mouseOverPanel)
            {
                player.mouseInterface = true;
                Main.blockMouse = true;
            }

            if (click)
                HandleClick(panel, mouseOverPanel);
        }

        private static void HandleClick(Rectangle panel, bool mouseOverPanel)
        {
            if (!mouseOverPanel)
            {
                Close();
                return;
            }

            int selectionIndex = GetHoveredWeaponIndex(panel);
            if (selectionIndex < 0)
                return;

            // Selecting a card is the claim itself. There is no random roll, target lock, or second confirmation.
            LegendarySupplyBoxPackets.RequestClaim(selectionIndex);
            Close(false);
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
            DrawCenteredText(spriteBatch, Text("DirectSelectHint"), new Rectangle(panel.X + 48, panel.Y + 68, panel.Width - 96, 26), new Color(112, 201, 255), 0.62f, 0.42f);
            DrawCenteredText(spriteBatch, $"{LegendarySupplyBox.GetMainLegendaryWeapons().Length:00}  MAIN LEGENDARY WEAPONS", new Rectangle(panel.X + 48, panel.Y + 98, panel.Width - 96, 20), new Color(255, 200, 100), 0.48f, 0.38f);
        }

        private static void DrawWeaponGrid(SpriteBatch spriteBatch, Rectangle panel)
        {
            int[] weapons = LegendarySupplyBox.GetMainLegendaryWeapons();
            Rectangle grid = GetGridArea(panel, weapons.Length);
            DrawRectangle(spriteBatch, grid, new Color(5, 16, 29, 212));
            DrawBorder(spriteBatch, grid, new Color(50, 122, 182, 180), 1);

            for (int index = 0; index < weapons.Length; index++)
            {
                Rectangle card = GetCardArea(grid, index);
                bool hovered = card.Contains(Main.mouseX, Main.mouseY);
                DrawWeaponCard(spriteBatch, card, weapons[index], hovered);
            }
        }

        private static void DrawWeaponCard(SpriteBatch spriteBatch, Rectangle card, int itemType, bool hovered)
        {
            Color border = hovered ? new Color(255, 221, 126) : new Color(70, 149, 205);
            Color fill = hovered ? new Color(27, 51, 66, 242) : new Color(8, 24, 42, 228);
            DrawRectangle(spriteBatch, card, fill);
            DrawBorder(spriteBatch, card, border, hovered ? 2 : 1);

            Texture2D icon = TextureAssets.Item[itemType].Value;
            float iconScale = Math.Min(0.72f, Math.Min(76f / icon.Width, 76f / icon.Height));
            spriteBatch.Draw(icon, new Vector2(card.X + 54f, card.Center.Y - 5f), null, Color.White, 0f, icon.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);
            DrawCenteredText(spriteBatch, Lang.GetItemNameValue(itemType), new Rectangle(card.X + 94, card.Y + 14, card.Width - 104, card.Height - 28), hovered ? Color.White : new Color(175, 220, 248), 0.58f, 0.38f);
        }

        private static void DrawFooter(SpriteBatch spriteBatch, Rectangle panel) =>
            DrawCenteredText(spriteBatch, Text("DirectSelectFooter"), new Rectangle(panel.X + 50, panel.Bottom - 50, panel.Width - 100, 24), new Color(91, 161, 204), 0.52f, 0.38f);

        private static Rectangle GetGridArea(Rectangle panel, int weaponCount)
        {
            int rows = (weaponCount + ColumnCount - 1) / ColumnCount;
            int width = ColumnCount * CardWidth + (ColumnCount - 1) * CardGap + 24;
            int height = rows * CardHeight + (rows - 1) * CardGap + 24;
            return new Rectangle(panel.Center.X - width / 2, panel.Y + 132, width, height);
        }

        private static Rectangle GetCardArea(Rectangle grid, int selectionIndex)
        {
            int column = selectionIndex % ColumnCount;
            int row = selectionIndex / ColumnCount;
            return new Rectangle(grid.X + 12 + column * (CardWidth + CardGap), grid.Y + 12 + row * (CardHeight + CardGap), CardWidth, CardHeight);
        }

        private static int GetHoveredWeaponIndex(Rectangle panel)
        {
            int[] weapons = LegendarySupplyBox.GetMainLegendaryWeapons();
            Rectangle grid = GetGridArea(panel, weapons.Length);
            for (int index = 0; index < weapons.Length; index++)
            {
                if (GetCardArea(grid, index).Contains(Main.mouseX, Main.mouseY))
                    return index;
            }

            return -1;
        }

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
