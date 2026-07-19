using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    /// <summary>
    /// A draw-only matrix UI. It deliberately has two opening contexts:
    /// held in the world (closing the inventory closes it) and hovered in the inventory
    /// (closing the inventory immediately closes it).
    /// </summary>
    internal sealed class LeonidConstellationMatrixUI : ModSystem
    {
        private const float NodeRadius = 16f;
        private static readonly Color Night = new(5, 10, 34, 232);
        private static readonly Color Frame = new(102, 128, 255, 210);
        private static readonly Color Dormant = new(66, 78, 128, 210);
        private static readonly Color Available = new(152, 121, 255, 245);
        private static readonly Color Lit = new(255, 223, 113, 255);

        private static bool isOpen;
        private static bool openedFromInventory;
        private static bool previousMiddleMouse;
        private static Rectangle resetArea;

        public static bool IsOpen => isOpen;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Leonid Constellation Matrix",
                () =>
                {
                    Draw(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI));
        }

        public override void PostUpdatePlayers()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0 || !Main.player.IndexInRange(Main.myPlayer))
                return;

            Player player = Main.LocalPlayer;
            bool justPressedMiddle = Main.mouseMiddle && !previousMiddleMouse;
            previousMiddleMouse = Main.mouseMiddle;

            if (isOpen)
            {
                if (!player.active || player.dead || (openedFromInventory ? !Main.playerInventory : Main.playerInventory))
                {
                    Close();
                    return;
                }

                if (!openedFromInventory && player.HeldItem?.ModItem is not LeonidProgenitor)
                {
                    Close();
                    return;
                }
            }

            if (!justPressedMiddle)
                return;

            if (Main.playerInventory && Main.HoverItem?.ModItem is LeonidProgenitor)
            {
                Toggle(true);
                return;
            }

            if (!Main.playerInventory && player.HeldItem?.ModItem is LeonidProgenitor)
                Toggle(false);
        }

        private static void Toggle(bool fromInventory)
        {
            if (isOpen)
            {
                Close();
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.65f, Pitch = 0.08f });
                return;
            }

            isOpen = true;
            openedFromInventory = fromInventory;
            SpawnConstellationFlare(Main.LocalPlayer);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.72f, Pitch = -0.08f });
        }

        private static void Close()
        {
            isOpen = false;
            openedFromInventory = false;
            resetArea = Rectangle.Empty;
        }

        private static void Draw(SpriteBatch spriteBatch)
        {
            if (!isOpen || Main.netMode == NetmodeID.Server)
                return;

            Player player = Main.LocalPlayer;
            LeonidConstellationPlayer progress = player.GetModPlayer<LeonidConstellationPlayer>();
            Vector2 screenCenter = Main.ScreenSize.ToVector2() * 0.5f;
            Rectangle panel = Utils.CenteredRectangle(screenCenter, new Vector2(790f, 590f));

            player.mouseInterface = panel.Contains(Main.mouseX, Main.mouseY);
            if (player.mouseInterface)
                Main.blockMouse = true;

            DrawPanel(spriteBatch, panel, progress);
            HandleInteractions(player, progress, panel);
        }

        private static void DrawPanel(SpriteBatch spriteBatch, Rectangle panel, LeonidConstellationPlayer progress)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, panel, Night);
            DrawOutline(spriteBatch, panel, Frame, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Title");
            string subtitle = Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Subtitle");
            string status = string.Format(
                Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Status"),
                progress.AvailablePoints,
                progress.SpentPoints,
                progress.EarnedPoints,
                LeonidConstellation.TotalCost);

            DrawCenteredText(spriteBatch, title, new Vector2(panel.Center.X, panel.Y + 22f), Lit, 1.05f);
            DrawCenteredText(spriteBatch, subtitle, new Vector2(panel.Center.X, panel.Y + 52f), new Color(174, 193, 255), 0.64f);
            DrawCenteredText(spriteBatch, status, new Vector2(panel.Center.X, panel.Bottom - 46f), Color.White, 0.72f);

            Vector2 constellationCenter = new(panel.X + 402f, panel.Y + 320f);
            DrawConstellationLines(spriteBatch, constellationCenter, progress);

            LeonidConstellationNode? hoveredNode = null;
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                Vector2 position = constellationCenter + node.Position;
                bool unlocked = progress.IsUnlocked(node.Star);
                bool affordable = !unlocked && progress.AvailablePoints >= node.Cost;
                bool hovered = Vector2.DistanceSquared(new Vector2(Main.mouseX, Main.mouseY), position) <= (NodeRadius + 7f) * (NodeRadius + 7f);
                if (hovered)
                    hoveredNode = node;

                DrawNode(spriteBatch, position, node, unlocked, affordable, hovered);
            }

            string reset = Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Reset");
            Vector2 resetSize = FontAssets.MouseText.Value.MeasureString(reset) * 0.72f;
            resetArea = new Rectangle(panel.Right - (int)resetSize.X - 44, panel.Bottom - 76, (int)resetSize.X + 22, 26);
            bool resetHovered = resetArea.Contains(Main.mouseX, Main.mouseY);
            spriteBatch.Draw(pixel, resetArea, resetHovered ? new Color(82, 48, 105, 240) : new Color(38, 29, 68, 225));
            DrawOutline(spriteBatch, resetArea, resetHovered ? Available : Dormant, 1);
            DrawCenteredText(spriteBatch, reset, resetArea.Center.ToVector2(), resetHovered ? Color.White : new Color(214, 198, 255), 0.72f);

            if (hoveredNode.HasValue)
                DrawNodeDetail(spriteBatch, panel, hoveredNode.Value, progress);
            else
                DrawCenteredText(spriteBatch,
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.HoverHint"),
                    new Vector2(panel.Center.X, panel.Bottom - 82f),
                    new Color(174, 193, 255),
                    0.58f);
        }

        private static void DrawConstellationLines(SpriteBatch spriteBatch, Vector2 center, LeonidConstellationPlayer progress)
        {
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                Vector2 start = center + node.Position;
                foreach (LeonidStar connectedStar in node.Connections)
                {
                    LeonidConstellationNode connected = LeonidConstellation.GetNode(connectedStar);
                    bool bright = progress.IsUnlocked(node.Star) && progress.IsUnlocked(connectedStar);
                    DrawLine(spriteBatch, start, center + connected.Position, bright ? Lit * 0.72f : Dormant * 0.52f, bright ? 2f : 1f);
                }
            }
        }

        private static void DrawNode(SpriteBatch spriteBatch, Vector2 position, LeonidConstellationNode node, bool unlocked, bool affordable, bool hovered)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + (int)node.Star * 0.8f);
            Color color = unlocked ? Lit : affordable ? Available : Dormant;
            float radius = NodeRadius + (unlocked ? 2f + pulse * 2f : hovered ? 2f : 0f);
            Rectangle glow = Utils.CenteredRectangle(position, new Vector2(radius * 2f + 12f));
            Rectangle core = Utils.CenteredRectangle(position, new Vector2(radius * 2f));

            spriteBatch.Draw(pixel, glow, color * (unlocked ? 0.16f + pulse * 0.1f : 0.08f));
            spriteBatch.Draw(pixel, core, color * (unlocked ? 0.95f : 0.66f));
            DrawOutline(spriteBatch, core, Color.White * (unlocked ? 0.92f : 0.4f), 1);

            string label = node.Star == LeonidStar.Regulus ? "★" : node.Cost.ToString();
            DrawCenteredText(spriteBatch, label, position, unlocked ? Color.White : new Color(230, 223, 255), node.Star == LeonidStar.Regulus ? 0.9f : 0.62f);
        }

        private static void DrawNodeDetail(SpriteBatch spriteBatch, Rectangle panel, LeonidConstellationNode node, LeonidConstellationPlayer progress)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle detail = new(panel.X + 24, panel.Bottom - 174, 310, 98);
            spriteBatch.Draw(pixel, detail, new Color(12, 18, 52, 235));
            DrawOutline(spriteBatch, detail, progress.IsUnlocked(node.Star) ? Lit : Frame, 1);

            string root = "Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Nodes." + LeonidConstellation.LocalizationKey(node.Star);
            string name = Language.GetTextValue(root + ".Name");
            string description = Language.GetTextValue(root + ".Description");
            string state = node.Star == LeonidStar.Regulus
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Heart")
                : progress.IsUnlocked(node.Star)
                    ? Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Unlocked")
                    : string.Format(Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Cost"), node.Cost);

            Utils.DrawBorderString(spriteBatch, name, detail.TopLeft() + new Vector2(12f, 9f), Lit, 0.74f);
            Utils.DrawBorderString(spriteBatch, state, detail.TopRight() + new Vector2(-112f, 10f), Color.White, 0.55f);
            Utils.DrawBorderString(spriteBatch, description, detail.TopLeft() + new Vector2(12f, 39f), new Color(219, 228, 255), 0.52f, 0f, 0f, -1);
        }

        private static void HandleInteractions(Player player, LeonidConstellationPlayer progress, Rectangle panel)
        {
            if (!Main.mouseLeft || !Main.mouseLeftRelease || !panel.Contains(Main.mouseX, Main.mouseY))
                return;

            if (resetArea.Contains(Main.mouseX, Main.mouseY))
            {
                LeonidConstellationPackets.RequestReset();
                SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = -0.16f, Volume = 0.72f });
                return;
            }

            Vector2 constellationCenter = new(panel.X + 402f, panel.Y + 320f);
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                if (Vector2.DistanceSquared(new Vector2(Main.mouseX, Main.mouseY), constellationCenter + node.Position) > (NodeRadius + 7f) * (NodeRadius + 7f))
                    continue;

                if (node.Star != LeonidStar.Regulus && !progress.IsUnlocked(node.Star) && progress.AvailablePoints >= node.Cost)
                {
                    LeonidConstellationPackets.RequestUnlock(node.Star);
                    SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = 0.14f, Volume = 0.78f });
                    SpawnConstellationFlare(player);
                }
                else
                    SoundEngine.PlaySound(SoundID.MenuClose with { Pitch = 0.34f, Volume = 0.35f });
                break;
            }
        }

        private static void SpawnConstellationFlare(Player player)
        {
            if (Main.dedServ || GeneralParticleHandler.FreeSpacesAvailable() < LeonidConstellation.Nodes.Count * 2)
                return;

            const float worldScale = 0.23f;
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                Vector2 worldPosition = player.Center + node.Position * worldScale + new Vector2(0f, -78f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(worldPosition, Vector2.Zero, Color.White, LeonidVisualUtils.StarGold, node.Star == LeonidStar.Regulus ? 1.55f : 0.8f, 26, 0f, 3f));
                foreach (LeonidStar connected in node.Connections)
                {
                    Vector2 end = player.Center + LeonidConstellation.GetNode(connected).Position * worldScale + new Vector2(0f, -78f);
                    GeneralParticleHandler.SpawnParticle(new BloomLineVFX(worldPosition, end - worldPosition, 0.35f, LeonidVisualUtils.MoonViolet, 26, true));
                }
            }
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 offset = end - start;
            float length = offset.Length();
            if (length <= 0.01f)
                return;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, null, color, offset.ToRotation(), Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private static void DrawOutline(SpriteBatch spriteBatch, Rectangle area, Color color, int thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, new Rectangle(area.X, area.Y, area.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(area.X, area.Bottom - thickness, area.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(area.X, area.Y, thickness, area.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(area.Right - thickness, area.Y, thickness, area.Height), color);
        }

        private static void DrawCenteredText(SpriteBatch spriteBatch, string text, Vector2 center, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Utils.DrawBorderString(spriteBatch, text, center - size * 0.5f, color, scale);
        }
    }
}
