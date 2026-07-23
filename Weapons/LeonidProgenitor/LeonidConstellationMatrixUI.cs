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
    /// Both contexts share the WeaponLoadingUI keybind with SHPC's loading UI, falling back to middle click.
    /// </summary>
    internal sealed class LeonidConstellationMatrixUI : ModSystem
    {
        private const float NodeRadius = 16f;
        private static readonly Color Dormant = new(66, 78, 128, 210);
        private static readonly Color Available = new(152, 121, 255, 245);
        private static readonly Color Lit = new(255, 223, 113, 255);

        private static bool isOpen;
        private static bool openedFromInventory;
        private static bool previousMiddleMouse;
        private static bool previousKeybind;
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

        private static void UpdateActivation()
        {
            if (Main.myPlayer < 0 || !Main.player.IndexInRange(Main.myPlayer))
                return;

            Player player = Main.LocalPlayer;
            bool keyBound = InventoryActivationInput.HasBoundKey(KeybindSystem.WeaponLoadingUI);
            bool keybindPressed = InventoryActivationInput.IsPressed(KeybindSystem.WeaponLoadingUI);
            bool justPressedKeybind = keybindPressed && !previousKeybind;
            bool justPressedMiddle = Main.mouseMiddle && !previousMiddleMouse;
            previousKeybind = keybindPressed;
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

            // In the inventory the bound key replaces middle click, matching how SHPC's loading UI opens.
            if (Main.playerInventory)
            {
                if ((keyBound ? justPressedKeybind : justPressedMiddle) && Main.HoverItem?.ModItem is LeonidProgenitor)
                    Toggle(true);
                return;
            }

            // Held in the world middle click always works, and the bound key is an extra shortcut alongside it.
            if ((justPressedMiddle || justPressedKeybind) && player.HeldItem?.ModItem is LeonidProgenitor)
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
            if (Main.netMode == NetmodeID.Server)
                return;

            // Activation lives in the draw phase so the inventory context still responds while auto-pause holds the update loop.
            UpdateActivation();

            if (!isOpen)
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
            // Keep the constellation exposed like Galaxia's holdout: the night is suggested by light,
            // rather than trapped inside a dark rectangular panel.
            DrawGalaxiaConstellation(spriteBatch, panel, constellationCenter, progress);

            LeonidConstellationNode? hoveredNode = null;
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                Vector2 position = constellationCenter + node.Position;
                bool unlocked = progress.IsUnlocked(node.Star);
                bool affordable = !unlocked && progress.AvailablePoints >= node.Cost;
                bool hovered = Vector2.DistanceSquared(new Vector2(Main.mouseX, Main.mouseY), position) <= (NodeRadius + 7f) * (NodeRadius + 7f);
                if (hovered)
                    hoveredNode = node;

                DrawNodeLabel(spriteBatch, position, node, unlocked, affordable, hovered);
            }

            string reset = Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Reset");
            string resetDisplay = $"✦ {reset} ✦";
            Vector2 resetSize = FontAssets.MouseText.Value.MeasureString(resetDisplay) * 0.72f;
            resetArea = new Rectangle(panel.Right - (int)resetSize.X - 28, panel.Bottom - 76, (int)resetSize.X + 12, 26);
            bool resetHovered = resetArea.Contains(Main.mouseX, Main.mouseY);
            float resetPulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.4f);
            DrawCenteredText(spriteBatch, resetDisplay, resetArea.Center.ToVector2(), resetHovered ? Color.White : new Color(214, 198, 255) * resetPulse, resetHovered ? 0.76f : 0.72f);

            if (hoveredNode.HasValue)
                DrawNodeDetail(spriteBatch, panel, hoveredNode.Value, progress);
            else
                DrawCenteredText(spriteBatch,
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.HoverHint"),
                    new Vector2(panel.Center.X, panel.Bottom - 82f),
                    new Color(174, 193, 255),
                    0.58f);
        }

        private static void DrawGalaxiaConstellation(SpriteBatch spriteBatch, Rectangle panel, Vector2 center, LeonidConstellationPlayer progress)
        {
            // GenericSparkle and BloomLineVFX are world particles, so they cannot be handed directly to
            // an interface layer. This is their CustomDraw code using the original textures, origins,
            // caps and additive blending rather than stretching MagicPixel as a substitute.
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            DrawCelestialBackdrop(spriteBatch, panel, center);

            int connectionIndex = 0;
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                Vector2 start = center + node.Position;
                foreach (LeonidStar connectedStar in node.Connections)
                {
                    LeonidConstellationNode connected = LeonidConstellation.GetNode(connectedStar);
                    bool bright = progress.IsUnlocked(node.Star) && progress.IsUnlocked(connectedStar);
                    Vector2 end = center + connected.Position;
                    DrawGalaxiaLine(spriteBatch, start, end, bright ? Lit : Dormant, bright ? 0.46f : 0.22f);

                    // Awakened paths carry one slow bead of starlight instead of gaining another border.
                    if (bright)
                    {
                        float travel = (Main.GlobalTimeWrappedHourly * 0.16f + connectionIndex * 0.173f) % 1f;
                        travel = travel * travel * (3f - 2f * travel);
                        DrawGalaxiaSparkle(spriteBatch, Vector2.Lerp(start, end, travel), Color.White, Lit, 0.13f, 40 + connectionIndex);
                    }

                    connectionIndex++;
                }
            }

            Texture2D ringTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            foreach (LeonidConstellationNode node in LeonidConstellation.Nodes)
            {
                bool unlocked = progress.IsUnlocked(node.Star);
                bool affordable = !unlocked && progress.AvailablePoints >= node.Cost;
                Vector2 position = center + node.Position;
                bool hovered = Vector2.DistanceSquared(new Vector2(Main.mouseX, Main.mouseY), position) <= (NodeRadius + 7f) * (NodeRadius + 7f);
                Color starColor = unlocked ? Color.White : affordable ? new Color(224, 213, 255) : new Color(125, 142, 206);
                Color bloomColor = unlocked ? Lit : affordable ? Available : Dormant;
                float scale = node.Star == LeonidStar.Regulus ? 0.82f : unlocked ? 0.58f : affordable ? 0.48f : 0.36f;
                DrawGalaxiaSparkle(spriteBatch, position, starColor, bloomColor, hovered ? scale * 1.16f : scale, (int)node.Star);

                if (hovered)
                {
                    float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.2f);
                    spriteBatch.Draw(
                        ringTexture,
                        position,
                        null,
                        bloomColor * (0.48f + pulse * 0.22f),
                        -Main.GlobalTimeWrappedHourly * 0.7f,
                        ringTexture.Size() * 0.5f,
                        0.19f + pulse * 0.025f,
                        SpriteEffects.None,
                        0f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        private static void DrawCelestialBackdrop(SpriteBatch spriteBatch, Rectangle panel, Vector2 center)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            float breath = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.72f);

            // Three broad, transparent blooms read as a drifting nebula without becoming a new container.
            spriteBatch.Draw(bloomTexture, center + new Vector2(-84f, -18f), null, new Color(48, 66, 180) * (0.09f + breath * 0.025f), 0f, bloomTexture.Size() * 0.5f, new Vector2(2.85f, 2.05f), SpriteEffects.None, 0f);
            spriteBatch.Draw(bloomTexture, center + new Vector2(154f, 52f), null, new Color(142, 81, 210) * (0.065f + (1f - breath) * 0.02f), 0f, bloomTexture.Size() * 0.5f, new Vector2(2.15f, 1.55f), SpriteEffects.None, 0f);
            spriteBatch.Draw(bloomTexture, center + new Vector2(-18f, 112f), null, Lit * (0.035f + breath * 0.012f), 0f, bloomTexture.Size() * 0.5f, new Vector2(1.5f, 0.95f), SpriteEffects.None, 0f);

            // A deterministic field of tiny points gives the constellation depth while staying calm and clean.
            for (int i = 0; i < 24; i++)
            {
                float x = panel.X + 42f + Hash01(i * 37 + 11) * (panel.Width - 84f);
                float y = panel.Y + 82f + Hash01(i * 53 + 29) * (panel.Height - 172f);
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (0.9f + Hash01(i * 71 + 5) * 1.2f) + i * 1.71f);
                float scale = 0.055f + Hash01(i * 97 + 17) * 0.07f;
                Color color = Color.Lerp(new Color(110, 145, 255), new Color(224, 207, 255), Hash01(i * 43 + 3));
                Vector2 position = new(x, y);

                spriteBatch.Draw(bloomTexture, position, null, color * (0.10f + twinkle * 0.08f), 0f, bloomTexture.Size() * 0.5f, scale * 0.72f, SpriteEffects.None, 0f);
                spriteBatch.Draw(starTexture, position, null, color * (0.18f + twinkle * 0.22f), i * 0.83f, starTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        private static void DrawNodeLabel(SpriteBatch spriteBatch, Vector2 position, LeonidConstellationNode node, bool unlocked, bool affordable, bool hovered)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + (int)node.Star * 0.8f);
            Color labelColor = unlocked ? Color.Lerp(Lit, Color.White, 0.28f + pulse * 0.24f) : affordable ? new Color(230, 218, 255) : new Color(154, 168, 218);
            string label = node.Star == LeonidStar.Regulus ? "★" : node.Cost.ToString();
            float bob = hovered ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 1.5f : 0f;

            DrawCenteredText(spriteBatch, label, position + new Vector2(0f, 22f + bob), labelColor, hovered ? 0.67f : 0.58f);
        }

        private static void DrawNodeDetail(SpriteBatch spriteBatch, Rectangle panel, LeonidConstellationNode node, LeonidConstellationPlayer progress)
        {
            string root = "Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Nodes." + LeonidConstellation.LocalizationKey(node.Star);
            string name = Language.GetTextValue(root + ".Name");
            string description = Language.GetTextValue(root + ".Description");
            string state = node.Star == LeonidStar.Regulus
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Heart")
                : progress.IsUnlocked(node.Star)
                    ? Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Unlocked")
                    : string.Format(Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.LeonidProgenitor.Constellation.Cost"), node.Cost);

            Vector2 detailOrigin = new(panel.X + 30f, panel.Bottom - 158f);
            Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.55f;
            Utils.DrawBorderString(spriteBatch, $"✦ {name}", detailOrigin, Lit, 0.74f);
            Utils.DrawBorderString(spriteBatch, state, detailOrigin + new Vector2(304f - stateSize.X, 3f), Color.White, 0.55f);
            Utils.DrawBorderString(spriteBatch, description, detailOrigin + new Vector2(0f, 35f), new Color(219, 228, 255), 0.52f, 0f, 0f, -1);
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

        private static void DrawGalaxiaSparkle(SpriteBatch spriteBatch, Vector2 position, Color color, Color bloom, float scale, int index)
        {
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/Sparkle").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float properBloomSize = (float)starTexture.Height / bloomTexture.Height;
            float opacity = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.1f + index * 0.83f);
            float rotation = Main.GlobalTimeWrappedHourly * (0.85f + index * 0.03f) + index * 0.71f;

            // Matches GenericSparkle.CustomDraw: bloom, offset star layer, then its sharp core.
            spriteBatch.Draw(bloomTexture, position, null, bloom * opacity * 0.5f, 0f, bloomTexture.Size() * 0.5f, scale * 3f * properBloomSize, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, position, null, color * opacity * 0.5f, rotation + MathHelper.PiOver4, starTexture.Size() * 0.5f, scale * 0.75f, SpriteEffects.None, 0f);
            spriteBatch.Draw(starTexture, position, null, color * opacity, rotation, starTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private static void DrawGalaxiaLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 lineVector = end - start;
            if (lineVector.LengthSquared() <= 0.01f)
                return;

            Texture2D lineTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLine").Value;
            float rotation = lineVector.ToRotation() + MathHelper.PiOver2;
            Vector2 origin = new(lineTexture.Width * 0.5f, lineTexture.Height);
            Vector2 scale = new(thickness, lineVector.Length() / lineTexture.Height);
            spriteBatch.Draw(lineTexture, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);

            // BloomLineVFX draws this matching cap at both endpoints.
            Texture2D capTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineCap").Value;
            Vector2 capOrigin = new(capTexture.Width * 0.5f, capTexture.Height);
            Vector2 capScale = new(thickness, thickness);
            spriteBatch.Draw(capTexture, start, null, color, rotation + MathHelper.Pi, capOrigin, capScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(capTexture, end, null, color, rotation, capOrigin, capScale, SpriteEffects.None, 0f);
        }

        private static float Hash01(int seed)
        {
            uint value = (uint)seed;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            return (value & 0x00FFFFFF) / 16777215f;
        }

        private static void DrawCenteredText(SpriteBatch spriteBatch, string text, Vector2 center, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Utils.DrawBorderString(spriteBatch, text, center - size * 0.5f, color, scale);
        }
    }
}
