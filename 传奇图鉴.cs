using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.Vesuvius;

namespace CalamityLegendsComeBack
{
    public class LegendaryCodex : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/LegendarySupplyBox";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 1);
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<LegendaryCodexPanel>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
        }

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
            return LegendaryCodexPanel.OpenOrClose(player, source);
        }
    }

    public class LegendaryCodexPanel : ModProjectile, ILocalizedModType
    {
        private const int PreferredPanelWidth = 900;
        private const int PreferredPanelHeight = 560;
        private const int ScreenMargin = 12;
        private const int BorderThickness = 3;
        private const int TransitionDuration = 18;

        private static readonly LegendaryEntry[] Entries =
        {
            new("SHPC", () => ModContent.ItemType<NewLegendSHPC>(), true, "SYSTEM // SHPC", "Legendary weapon record online.\nEnergy lattice, material feed, heat sweep and tactical computer are readable."),
            new("BlossomFlux", () => ModContent.ItemType<NewLegendBlossomFlux>(), false, "???", "???\n???\n???"),
            new("BrinyBaron", () => ModContent.ItemType<NewLegendBrinyBaron>(), false, "???", "???\n???\n???"),
            new("PristineFury", () => ModContent.ItemType<NewLegendPristineFury>(), false, "???", "???\n???\n???"),
            new("Vesuvius", () => ModContent.ItemType<NewVesuvius>(), false, "???", "???\n???\n???"),
        };

        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;
        private int entryIndex;
        private int previousEntryIndex;
        private int transitionTimer = TransitionDuration;
        private int transitionDirection = 1;
        private int lastHoveredControl = -1;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/LegendarySupplyBox";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);
        private static int PanelWidth => Math.Min(PreferredPanelWidth, Math.Max(620, Main.screenWidth - ScreenMargin * 2));
        private static int PanelHeight => Math.Min(PreferredPanelHeight, Math.Max(430, Main.screenHeight - ScreenMargin * 2));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = PreferredPanelWidth;
            Projectile.height = PreferredPanelHeight;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
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

            if (owner.HeldItem.type != ModContent.ItemType<LegendaryCodex>())
                FadeOut = true;

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 playerScreenCenter = owner.Center - Main.screenPosition;
                panelTopLeft = GetClampedPanelTopLeft(playerScreenCenter - new Vector2(PanelWidth, PanelHeight) * 0.5f);
                panelPositionInitialized = true;
            }

            if (transitionTimer < TransitionDuration)
                transitionTimer++;

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
            Rectangle contentArea = new(panelArea.X + 38, panelArea.Y + 92, panelArea.Width - 76, panelArea.Height - 132);
            Rectangle leftArrowArea = new(contentArea.X + 10, contentArea.Y + contentArea.Height / 2 - 34, 54, 68);
            Rectangle rightArrowArea = new(contentArea.Right - 64, contentArea.Y + contentArea.Height / 2 - 34, 54, 68);
            Rectangle cardArea = new(contentArea.X + 82, contentArea.Y + 12, contentArea.Width - 164, contentArea.Height - 24);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool leftHovered = leftArrowArea.Intersects(MouseRectangle);
            bool rightHovered = rightArrowArea.Intersects(MouseRectangle);
            int hoveredControl = -1;

            DrawPanel(panelArea, Projectile.Opacity);
            DrawMatrixRain(panelArea, Projectile.Opacity);
            DrawHeader(panelArea, Projectile.Opacity);
            DrawContentFrame(contentArea, Projectile.Opacity);

            if (leftHovered)
            {
                hoveredControl = 1;
                if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    ChangeEntry(-1);
            }
            else if (rightHovered)
            {
                hoveredControl = 2;
                if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    ChangeEntry(1);
            }
            else if (mouseOverPanel && rightClickPressed && Projectile.Opacity >= 0.95f)
            {
                ChangeEntry(1);
            }

            DrawEntryCard(cardArea, Projectile.Opacity);
            DrawArrowButton(leftArrowArea, false, leftHovered, Projectile.Opacity);
            DrawArrowButton(rightArrowArea, true, rightHovered, Projectile.Opacity);
            DrawFooter(panelArea, Projectile.Opacity);
            PlayHoverSound(owner, hoveredControl);

            if (!mouseOverPanel && !FadeOut && Projectile.Opacity >= 0.95f && (leftClickPressed || rightClickPressed))
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

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<LegendaryCodexPanel>(), 0, 0f, player.whoAmI);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.32f, Pitch = 0.22f }, player.Center);
            return false;
        }

        private static bool TryCloseExistingPanel(Player player)
        {
            int panelType = ModContent.ProjectileType<LegendaryCodexPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != panelType)
                    continue;

                if (projectile.ModProjectile is LegendaryCodexPanel panel)
                    panel.FadeOut = true;
                else
                    projectile.ai[0] = 1f;

                return true;
            }

            return false;
        }

        private void ChangeEntry(int direction)
        {
            previousEntryIndex = entryIndex;
            transitionDirection = direction;
            entryIndex = (entryIndex + direction + Entries.Length) % Entries.Length;
            transitionTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.66f, Pitch = direction > 0 ? 0.24f : 0.06f }, Main.LocalPlayer.Center);
        }

        private void DrawEntryCard(Rectangle cardArea, float opacity)
        {
            float transition = transitionTimer / (float)TransitionDuration;
            transition = transition * transition * (3f - 2f * transition);

            if (transitionTimer < TransitionDuration)
            {
                Rectangle previousArea = cardArea;
                previousArea.X -= (int)(transitionDirection * MathHelper.Lerp(0f, 52f, transition));
                DrawSingleEntry(Entries[previousEntryIndex], previousArea, opacity * (1f - transition));

                Rectangle nextArea = cardArea;
                nextArea.X += (int)(transitionDirection * MathHelper.Lerp(52f, 0f, transition));
                DrawSingleEntry(Entries[entryIndex], nextArea, opacity * transition);
                return;
            }

            DrawSingleEntry(Entries[entryIndex], cardArea, opacity);
        }

        private static void DrawSingleEntry(LegendaryEntry entry, Rectangle area, float opacity)
        {
            Color theme = entry.Unlocked ? new Color(72, 255, 184) : new Color(52, 92, 74);
            Color text = entry.Unlocked ? new Color(222, 255, 244) : new Color(70, 108, 92);
            DrawBorder(area, theme * (0.52f * opacity), 1);

            int itemType = entry.ItemType();
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Rectangle iconArea = new(area.X + area.Width / 2 - 92, area.Y + 42, 184, 154);
            Vector2 iconCenter = iconArea.Center.ToVector2();
            float scale = Math.Min(1.45f, 132f / Math.Max(texture.Width, texture.Height));
            Color itemColor = entry.Unlocked ? Color.White * opacity : Color.Black * (0.88f * opacity);

            Main.EntitySpriteDraw(texture, iconCenter, texture.Frame(), itemColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            if (!entry.Unlocked)
                DrawCenteredText("?", new Rectangle(iconArea.X, iconArea.Y + 18, iconArea.Width, 96), new Color(78, 255, 164), 2.1f, 0.9f, opacity);

            Rectangle nameArea = new(area.X + 26, area.Y + 214, area.Width - 52, 36);
            string name = entry.Unlocked ? Lang.GetItemNameValue(itemType) : "???";
            DrawCenteredText(name, nameArea, text, 0.9f, 0.48f, opacity);

            Rectangle tagArea = new(area.X + 42, area.Y + 260, area.Width - 84, 28);
            DrawCenteredText(entry.Unlocked ? entry.Tag : "???", tagArea, theme, 0.58f, 0.42f, opacity);

            Rectangle bodyArea = new(area.X + 48, area.Y + 304, area.Width - 96, 90);
            DrawWrappedText(entry.Unlocked ? entry.Body : "???\n???\n???", bodyArea, text, 0.58f, opacity);
        }

        private static void DrawHeader(Rectangle panelArea, float opacity)
        {
            DrawCenteredText("LEGENDARY CODEX", new Rectangle(panelArea.X + 34, panelArea.Y + 22, panelArea.Width - 68, 34), new Color(210, 255, 236), 1f, 0.62f, opacity);
            DrawCenteredText($"{Entries.Length:00} RECORDS // ACTIVE INDEX {(Main.GameUpdateCount / 6) % 100:00}", new Rectangle(panelArea.X + 34, panelArea.Y + 56, panelArea.Width - 68, 22), new Color(82, 255, 174), 0.5f, 0.36f, opacity);
        }

        private static void DrawFooter(Rectangle panelArea, float opacity)
        {
            DrawCenteredText("LEFT / RIGHT NODE SELECT // RMB NEXT", new Rectangle(panelArea.X + 34, panelArea.Bottom - 34, panelArea.Width - 68, 20), new Color(88, 180, 146), 0.48f, 0.36f, opacity);
        }

        private static void DrawArrowButton(Rectangle area, bool right, bool hovered, float opacity)
        {
            Color border = hovered ? new Color(160, 255, 216) : new Color(54, 188, 132);
            Color fill = hovered ? new Color(14, 48, 38, 230) : new Color(5, 20, 17, 210);
            DrawRectangle(area, fill * opacity);
            DrawBorder(area, border * opacity, 2);
            DrawCenteredText(right ? ">" : "<", area, hovered ? Color.White : new Color(92, 255, 178), 1.6f, 0.8f, opacity);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(2, 8, 7, 242) * opacity);
            DrawBorder(panelArea, new Color(70, 255, 188) * opacity, BorderThickness);
            Rectangle innerArea = new(panelArea.X + 9, panelArea.Y + 9, panelArea.Width - 18, panelArea.Height - 18);
            DrawBorder(innerArea, new Color(18, 98, 68, 210) * opacity, 1);

            for (int x = panelArea.X + 24; x < panelArea.Right - 24; x += 32)
                DrawRectangle(new Rectangle(x, panelArea.Y + 14, 1, panelArea.Height - 28), new Color(36, 108, 80, 70) * opacity);

            for (int y = panelArea.Y + 24; y < panelArea.Bottom - 24; y += 28)
                DrawRectangle(new Rectangle(panelArea.X + 14, y, panelArea.Width - 28, 1), new Color(36, 108, 80, 70) * opacity);
        }

        private static void DrawContentFrame(Rectangle area, float opacity)
        {
            DrawRectangle(area, new Color(4, 18, 15, 214) * opacity);
            DrawBorder(area, new Color(40, 190, 128, 180) * opacity, 1);
        }

        private static void DrawMatrixRain(Rectangle panelArea, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Color rainColor = new Color(56, 255, 168) * (opacity * 0.2f);
            float time = Main.GlobalTimeWrappedHourly * 22f;

            for (int i = 0; i < 22; i++)
            {
                int x = panelArea.X + 24 + i * 32;
                int y = panelArea.Y + 22 + (int)((time + i * 17) % (PanelHeight - 44));
                string glyph = (i % 4) switch
                {
                    0 => "01",
                    1 => "SH",
                    2 => "PC",
                    _ => "??"
                };

                ChatManager.DrawColorCodedString(Main.spriteBatch, font, glyph, new Vector2(x, y), rainColor, 0f, Vector2.Zero, Vector2.One * 0.56f);
            }
        }

        private void PlayHoverSound(Player owner, int hoveredControl)
        {
            if (hoveredControl >= 0 && hoveredControl != lastHoveredControl && Projectile.Opacity >= 0.95f)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.34f, Pitch = 0.16f }, owner.Center);

            lastHoveredControl = hoveredControl;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            float maxX = Math.Max(ScreenMargin, Main.screenWidth - PanelWidth - ScreenMargin);
            float maxY = Math.Max(ScreenMargin, Main.screenHeight - PanelHeight - ScreenMargin);
            return new Vector2(MathHelper.Clamp(desiredTopLeft.X, ScreenMargin, maxX), MathHelper.Clamp(desiredTopLeft.Y, ScreenMargin, maxY));
        }

        private static void DrawWrappedText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            int maxLines = Math.Max(1, (int)(area.Height / (font.LineSpacing * scale)));
            for (int i = 0; i < lines.Length && i < maxLines; i++)
                DrawTextWithShadow(lines[i], new Vector2(area.X, area.Y + i * font.LineSpacing * scale), color * opacity, scale, opacity);
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
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
            Vector2 position = area.Center.ToVector2() - size * scale * 0.5f;
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

        private readonly struct LegendaryEntry
        {
            public readonly string Key;
            public readonly Func<int> ItemType;
            public readonly bool Unlocked;
            public readonly string Tag;
            public readonly string Body;

            public LegendaryEntry(string key, Func<int> itemType, bool unlocked, string tag, string body)
            {
                Key = key;
                ItemType = itemType;
                Unlocked = unlocked;
                Tag = tag;
                Body = body;
            }
        }
    }
}
