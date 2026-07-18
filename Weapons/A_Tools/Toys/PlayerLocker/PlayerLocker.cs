using System;
using System.Collections.Generic;
using System.IO;
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
using CalamityMod;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.PlayerLocker
{
    public sealed class PlayerLocker : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.ExtendoGrip;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(230, 90, 80));
            return true;
        }

        private static int PanelType => ModContent.ProjectileType<PlayerLockerPanel>();

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2)
                return false;
            return !Main.mapFullscreen && !Main.drawingPlayerChat && !Main.gameMenu && !player.mouseInterface;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI || player.altFunctionUse != 2)
                return null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI || proj.type != PanelType)
                    continue;

                PlayerLockerPanel.RequestClose(proj);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return true;
            }

            int target = FindPlayerNearMouse(player.whoAmI);
            if (target < 0)
            {
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerLockerNoTarget"), new Color(255, 160, 90));
                return true;
            }

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                PanelType,
                0,
                0f,
                player.whoAmI,
                target,
                0f,
                0f);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.12f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.32f, Pitch = 0.22f }, player.Center);
            return true;
        }

        private static int FindPlayerNearMouse(int selfWhoAmI)
        {
            const float screenRadius = 100f;
            int nearest = -1;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || i == selfWhoAmI)
                    continue;

                float dist = Vector2.Distance(p.Center - Main.screenPosition, Main.MouseScreen);
                if (dist < screenRadius && dist < nearestDist)
                {
                    nearest = i;
                    nearestDist = dist;
                }
            }

            return nearest;
        }
    }

    internal sealed class PlayerLockerPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int Columns = 10;
        private const int Rows = 5;
        private const int SlotSize = 46;
        private const int SlotGap = 4;
        private const int TitleHeight = 28;
        private const int PanelPadding = 14;
        private const int BorderThickness = 2;
        private const int SpecialSlotSize = 40;
        private const int SpecialRowGap = 8;
        private const float MaxIconSize = 34f;

        private static int GridWidth => Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int GridHeight => Rows * SlotSize + (Rows - 1) * SlotGap;
        private static int PanelWidth => PanelPadding * 2 + GridWidth;
        private static int PanelHeight => PanelPadding * 2 + TitleHeight + GridHeight + SpecialRowGap + SpecialSlotSize;

        private static Rectangle MouseRect => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        private Item[] cachedInventory;
        private bool inventoryRequested;
        private bool inventoryReceived;
        private Vector2 panelTopLeft;
        private bool panelInitialized;
        private readonly int[] slotFeedback = new int[58];

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private int TargetWhoAmI => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            cachedInventory = new Item[58];
            for (int i = 0; i < 58; i++)
                cachedInventory[i] = new Item();
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!Main.player[TargetWhoAmI].active)
                FadeOut = true;

            if (owner.HeldItem.type != ModContent.ItemType<PlayerLocker>())
                FadeOut = true;

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            if (!panelInitialized)
            {
                panelTopLeft = new Vector2(
                    (Main.screenWidth - PanelWidth) * 0.5f,
                    (Main.screenHeight - PanelHeight) * 0.5f);
                panelInitialized = true;
            }

            if (!inventoryRequested && !FadeOut)
            {
                inventoryRequested = true;
                RequestInventory();
            }
        }

        private void RequestInventory()
        {
            if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.Server)
            {
                Player target = Main.player[TargetWhoAmI];
                for (int i = 0; i < 58; i++)
                    cachedInventory[i] = target.inventory[i].Clone();
                inventoryReceived = true;
            }
            else
            {
                PlayerLockerPackets.SendInventoryRequest(TargetWhoAmI);
            }
        }

        public void ReceiveInventoryData(Item[] items)
        {
            for (int i = 0; i < Math.Min(items.Length, 58); i++)
                cachedInventory[i] = items[i];
            inventoryReceived = true;
        }

        public static void RequestClose(Projectile proj)
        {
            if (proj.ModProjectile is PlayerLockerPanel panel)
                panel.FadeOut = true;
            else
                proj.ai[0] = 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner || !panelInitialized)
                return false;

            Player owner = Main.player[Projectile.owner];
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRect);
            bool leftClick = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClick = Main.mouseRight && Main.mouseRightRelease;
            int clickedSlot = -1;

            DrawPanel(panelArea, Projectile.Opacity);
            DrawTitle(panelArea, Main.player[TargetWhoAmI].name, Projectile.Opacity);

            if (!inventoryReceived)
            {
                DrawCenteredText(panelArea,
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerLockerLoading"),
                    new Color(255, 210, 160), Projectile.Opacity);
            }
            else
            {
                for (int i = 0; i < 50; i++)
                {
                    Rectangle slotArea = GetSlotArea(i);
                    bool hovered = slotArea.Intersects(MouseRect);
                    if (hovered) mouseOverPanel = true;

                    if (DrawSlot(slotArea, cachedInventory[i], hovered, slotFeedback[i], Projectile.Opacity,
                            i < 10 ? new Color(255, 228, 130) : Color.White)
                        && leftClick && Projectile.Opacity >= 0.94f)
                        clickedSlot = i;
                }

                for (int i = 0; i < 4; i++)
                {
                    Rectangle slotArea = GetCoinSlotArea(i);
                    bool hovered = slotArea.Intersects(MouseRect);
                    if (hovered) mouseOverPanel = true;

                    if (DrawSlot(slotArea, cachedInventory[50 + i], hovered, slotFeedback[50 + i], Projectile.Opacity,
                            new Color(220, 196, 100))
                        && leftClick && Projectile.Opacity >= 0.94f)
                        clickedSlot = 50 + i;
                }

                for (int i = 0; i < 4; i++)
                {
                    Rectangle slotArea = GetAmmoSlotArea(i);
                    bool hovered = slotArea.Intersects(MouseRect);
                    if (hovered) mouseOverPanel = true;

                    if (DrawSlot(slotArea, cachedInventory[54 + i], hovered, slotFeedback[54 + i], Projectile.Opacity,
                            new Color(140, 216, 100))
                        && leftClick && Projectile.Opacity >= 0.94f)
                        clickedSlot = 54 + i;
                }
            }

            for (int i = 0; i < 58; i++)
                if (slotFeedback[i] > 0) slotFeedback[i]--;

            if (clickedSlot >= 0)
            {
                StealItem(owner, clickedSlot);
                slotFeedback[clickedSlot] = 14;
            }

            if (!FadeOut && rightClick && !mouseOverPanel && Projectile.Opacity >= 0.94f)
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

        private void StealItem(Player thief, int slotIndex)
        {
            Item item = cachedInventory[slotIndex];
            if (item == null || item.IsAir) return;

            int targetId = TargetWhoAmI;
            string itemName = item.Name;
            string targetName = Main.player[targetId].name;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Player target = Main.player[targetId];
                Item stolen = target.inventory[slotIndex].Clone();
                if (stolen.IsAir) return;

                target.inventory[slotIndex] = new Item();
                thief.QuickSpawnItem(thief.GetSource_ItemUse(thief.HeldItem), stolen, stolen.stack);
                cachedInventory[slotIndex] = new Item();
            }
            else
            {
                PlayerLockerPackets.SendStealItem(targetId, slotIndex);
                cachedInventory[slotIndex] = new Item();
            }

            SoundEngine.PlaySound(SoundID.Grab, thief.Center);
            Main.NewText(
                Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerLockerStolen", itemName, targetName),
                new Color(220, 170, 255));
        }

        // ---- Layout ----

        private Rectangle GetSlotArea(int index)
        {
            int x = (int)panelTopLeft.X + PanelPadding + (index % Columns) * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + TitleHeight + (index / Columns) * (SlotSize + SlotGap);
            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private Rectangle GetCoinSlotArea(int index)
        {
            int x = (int)panelTopLeft.X + PanelPadding + index * (SpecialSlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + TitleHeight + GridHeight + SpecialRowGap;
            return new Rectangle(x, y, SpecialSlotSize, SpecialSlotSize);
        }

        private Rectangle GetAmmoSlotArea(int index)
        {
            int startX = (int)panelTopLeft.X + PanelWidth - PanelPadding - 4 * SpecialSlotSize - 3 * SlotGap;
            int x = startX + index * (SpecialSlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + TitleHeight + GridHeight + SpecialRowGap;
            return new Rectangle(x, y, SpecialSlotSize, SpecialSlotSize);
        }

        // ---- Drawing ----

        private static void DrawPanel(Rectangle area, float opacity)
        {
            Color accent = new(255, 120, 70);
            DrawRect(area, new Color(10, 8, 14, 238) * opacity);
            DrawBorder(area, accent * opacity, BorderThickness);
            DrawBorder(new Rectangle(area.X + 8, area.Y + 8, area.Width - 16, area.Height - 16),
                new Color(100, 50, 30, 160) * opacity, 1);

            int sweepX = area.X + 12 + (int)(Main.GlobalTimeWrappedHourly * 66f % Math.Max(1, area.Width - 24));
            DrawRect(new Rectangle(sweepX, area.Y + 12, 2, area.Height - 24), accent * (opacity * 0.14f));
        }

        private static void DrawTitle(Rectangle panelArea, string targetName, float opacity)
        {
            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerLockerTitle", targetName);
            Rectangle titleRect = new(panelArea.X + PanelPadding, panelArea.Y + PanelPadding, panelArea.Width - PanelPadding * 2, TitleHeight - 4);
            DrawFitText(title, titleRect, new Color(255, 185, 110), 0.86f, 0.5f, opacity);
            DrawRect(new Rectangle(panelArea.X + PanelPadding, panelArea.Y + PanelPadding + TitleHeight - 3,
                panelArea.Width - PanelPadding * 2, 1), new Color(255, 120, 70, 140) * opacity);
        }

        private static void DrawCenteredText(Rectangle panelArea, string text, Color color, float opacity)
        {
            Rectangle r = new(panelArea.X + 16, panelArea.Center.Y - 16, panelArea.Width - 32, 32);
            DrawFitText(text, r, color, 0.82f, 0.5f, opacity);
        }

        // Returns true if hovered and has item
        private static bool DrawSlot(Rectangle area, Item item, bool hovered, int feedback, float opacity, Color accent)
        {
            bool hasItem = item != null && !item.IsAir;
            Color back = new Color(24, 16, 32);

            if (hovered && hasItem) back = Color.Lerp(back, accent, 0.2f);
            if (feedback > 0) back = Color.Lerp(back, new Color(255, 200, 80), feedback / 14f * 0.4f);

            DrawRect(area, back * (opacity * 0.9f));
            DrawBorder(area,
                (hovered && hasItem ? Color.Lerp(accent, Color.White, 0.28f) : new Color(80, 60, 100)) * opacity,
                hovered && hasItem ? 2 : 1);

            if (!hasItem) return false;

            try
            {
                Main.instance.LoadItem(item.type);
                Texture2D tex = TextureAssets.Item[item.type].Value;
                if (tex != null)
                {
                    float scale = Math.Min(MaxIconSize / Math.Max(1f, tex.Width), MaxIconSize / Math.Max(1f, tex.Height));
                    if (hovered) scale *= 1.08f;
                    Main.EntitySpriteDraw(tex, area.Center.ToVector2(), null, Color.White * opacity, 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }
            catch { }

            if (item.stack > 1)
            {
                string stackText = item.stack >= 10000 ? $"{item.stack / 1000}K" : item.stack.ToString();
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.ItemStack.Value,
                    stackText, new Vector2(area.X + 3, area.Bottom - 14), Color.White * opacity, 0f, Vector2.Zero, Vector2.One * 0.82f);
            }

            if (hovered)
            {
                Main.HoverItem = item.Clone();
                Main.hoverItemName = item.Name;
            }

            return hovered;
        }

        private static void DrawRect(Rectangle rect, Color color)
            => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, color);

        private static void DrawBorder(Rectangle rect, Color color, int thickness)
        {
            DrawRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            DrawRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            DrawRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            DrawRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f) return;
            float scale = maxScale;
            if (size.X * scale > area.Width) scale = area.Width / size.X;
            if (size.Y * scale > area.Height) scale = Math.Min(scale, area.Height / size.Y);
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 pos = new(area.X + (area.Width - size.X * scale) * 0.5f, area.Y + (area.Height - size.Y * scale) * 0.5f);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, pos, color * opacity, 0f, Vector2.Zero, Vector2.One * scale);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) { }
    }

    internal static class PlayerLockerPackets
    {
        private static global::CalamityLegendsComeBack.CalamityLegendsComeBack Mod
            => ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>();

        public static void SendInventoryRequest(int targetWhoAmI)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GamePacketType.PlayerLockerRequestInventory);
            packet.Write((byte)targetWhoAmI);
            packet.Send();
        }

        public static void SendStealItem(int targetWhoAmI, int slotIndex)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GamePacketType.PlayerLockerStealItem);
            packet.Write((byte)targetWhoAmI);
            packet.Write((byte)slotIndex);
            packet.Send();
        }

        public static void HandleInventoryRequest(BinaryReader reader, int requesterWhoAmI)
        {
            int targetWhoAmI = reader.ReadByte();
            if (!Main.player[targetWhoAmI].active) return;
            SendInventoryToClient(targetWhoAmI, requesterWhoAmI);
        }

        public static void HandleInventoryData(BinaryReader reader)
        {
            int targetWhoAmI = reader.ReadByte();
            Item[] items = new Item[58];
            for (int i = 0; i < 58; i++)
            {
                items[i] = new Item();
                items[i].SetDefaults(reader.ReadInt32());
                items[i].stack = reader.ReadInt32();
                items[i].Prefix(reader.ReadByte());
            }

            int panelType = ModContent.ProjectileType<PlayerLockerPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != Main.myPlayer || proj.type != panelType)
                    continue;
                if ((int)proj.ai[1] == targetWhoAmI && proj.ModProjectile is PlayerLockerPanel panel)
                {
                    panel.ReceiveInventoryData(items);
                    break;
                }
            }
        }

        public static void HandleStealItem(BinaryReader reader, int requesterWhoAmI)
        {
            int targetWhoAmI = reader.ReadByte();
            int slotIndex = reader.ReadByte();

            Player target = Main.player[targetWhoAmI];
            Player thief = Main.player[requesterWhoAmI];
            if (!target.active || !thief.active) return;

            Item stolen = target.inventory[slotIndex].Clone();
            if (stolen.IsAir) return;

            target.inventory[slotIndex] = new Item();
            Item.NewItem(thief.GetSource_Misc("PlayerLocker"), thief.Center, stolen.type, stolen.stack, false, stolen.prefix);

            // Tell target client to clear this slot
            ModPacket clearPacket = Mod.GetPacket();
            clearPacket.Write((byte)GamePacketType.PlayerLockerClearSlot);
            clearPacket.Write((byte)slotIndex);
            clearPacket.Send(targetWhoAmI);

            // Send refreshed inventory to requester
            SendInventoryToClient(targetWhoAmI, requesterWhoAmI);
        }

        public static void HandleClearSlot(BinaryReader reader)
        {
            int slotIndex = reader.ReadByte();
            Item[] inv = Main.player[Main.myPlayer].inventory;
            if (slotIndex < inv.Length)
                inv[slotIndex] = new Item();
        }

        private static void SendInventoryToClient(int targetWhoAmI, int toWhoAmI)
        {
            Player target = Main.player[targetWhoAmI];
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GamePacketType.PlayerLockerInventoryData);
            packet.Write((byte)targetWhoAmI);
            for (int i = 0; i < 58; i++)
            {
                Item item = i < target.inventory.Length ? target.inventory[i] : new Item();
                packet.Write(item.type);
                packet.Write(item.stack);
                packet.Write(item.prefix);
            }
            packet.Send(toWhoAmI);
        }
    }

    internal sealed class PlayerLockerShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType != NPCID.PartyGirl)
                return;

            shop.Add(new Item(ModContent.ItemType<PlayerLocker>())
            {
                shopCustomPrice = Item.buyPrice(gold: 20)
            });
        }
    }
}
