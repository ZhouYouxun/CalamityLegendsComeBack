using System;
using System.Collections.Generic;
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
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;
using CalamityMod;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.ExtraBackpack
{
    // ── Item ─────────────────────────────────────────────────────────────────
    // ── ModPlayer (storage) ───────────────────────────────────────────────────
    public class ExtraBackpackPlayer : ModPlayer
    {
        public const int SlotCount = 200;
        public Item[] backpackContents = new Item[SlotCount];

        public override void Initialize()
        {
            backpackContents = new Item[SlotCount];
            for (int i = 0; i < SlotCount; i++) backpackContents[i] = new Item();
        }

        public override void SaveData(TagCompound tag)
        {
            var list = new List<TagCompound>();
            for (int i = 0; i < SlotCount; i++)
            {
                var compound = ItemIO.Save(backpackContents[i]);
                list.Add(compound);
            }
            tag["ExtraBackpackItems"] = list;
        }

        public override void LoadData(TagCompound tag)
        {
            var list = tag.GetList<TagCompound>("ExtraBackpackItems");
            for (int i = 0; i < Math.Min(list.Count, SlotCount); i++)
                backpackContents[i] = ItemIO.Load(list[i]);
        }

        public override void PostUpdate()
        {
            if (Main.myPlayer != Player.whoAmI) return;

            // Keybind toggle
            if (global::CalamityLegendsComeBack.KeybindSystem.ExtraBackpack?.JustPressed == true)
            {
                int panelType = ModContent.ProjectileType<ExtraBackpackPanel>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active || proj.owner != Player.whoAmI || proj.type != panelType) continue;
                    ExtraBackpackPanel.RequestClose(proj);
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.6f, Pitch = 0.05f }, Player.Center);
                    return;
                }
                Projectile.NewProjectile(Player.GetSource_Misc("ExtraBackpack"), Player.Center, Vector2.Zero,
                    panelType, 0, 0f, Player.whoAmI);
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.7f, Pitch = 0.1f }, Player.Center);
            }
        }
    }

    // ── ModSystem: middle-click detection ────────────────────────────────────
    // ── Panel projectile ──────────────────────────────────────────────────────
    internal sealed class ExtraBackpackPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int Cols = 20;
        private const int Rows = 10;
        private const int SlotSize = 44;
        private const int SlotGap = 4;
        private const int PanelPad = 12;
        private const int TitleH = 30;
        private const int BorderThick = 2;
        private const float MaxIconSize = 32f;

        private static int GridW => Cols * SlotSize + (Cols - 1) * SlotGap;
        private static int GridH => Rows * SlotSize + (Rows - 1) * SlotGap;
        private static int PanelW => PanelPad * 2 + GridW;
        private static int PanelH => PanelPad * 2 + TitleH + GridH;

        private Vector2 panelTopLeft;
        private bool panelInitialized;
        private Vector2 dragOffset;
        private bool isDragging;
        private readonly int[] slotFeedback = new int[ExtraBackpackPlayer.SlotCount];

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] >= 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRect => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public static void RequestClose(Projectile proj)
        {
            if (proj.ModProjectile is ExtraBackpackPanel p) p.FadeOut = true;
            else proj.ai[0] = 1f;
        }

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

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) { Projectile.Kill(); return; }

            if (!panelInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = new Vector2(
                    (Main.screenWidth - PanelW) * 0.5f,
                    (Main.screenHeight - PanelH) * 0.5f);
                panelInitialized = true;
            }

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.15f : 0.2f), 0f, 1f);
            if (FadeOut && Projectile.Opacity <= 0f) Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner || !panelInitialized) return false;

            Player owner = Main.player[Projectile.owner];
            ExtraBackpackPlayer bp = owner.GetModPlayer<ExtraBackpackPlayer>();
            float op = Projectile.Opacity;
            bool leftClick = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClick = Main.mouseRight && Main.mouseRightRelease;

            // ── Dragging ──
            Rectangle titleBar = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelW, TitleH + PanelPad);
            if (titleBar.Intersects(MouseRect) && Main.mouseLeft && !isDragging && op >= 0.9f)
            {
                isDragging = true;
                dragOffset = Main.MouseScreen - panelTopLeft;
            }
            if (!Main.mouseLeft) isDragging = false;
            if (isDragging)
            {
                panelTopLeft = Main.MouseScreen - dragOffset;
                // Clamp to screen
                const float margin = 10f;
                panelTopLeft.X = MathHelper.Clamp(panelTopLeft.X, margin, Main.screenWidth - PanelW - margin);
                panelTopLeft.Y = MathHelper.Clamp(panelTopLeft.Y, margin, Main.screenHeight - PanelH - margin);
            }

            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelW, PanelH);
            bool mouseOverPanel = panelArea.Intersects(MouseRect);
            int clickedSlot = -1;
            bool isRightClickSlot = false;

            // ── Background ──
            DrawRect(panelArea, new Color(10, 8, 16, 242) * op);
            DrawBorder(panelArea, new Color(80, 200, 120) * op, BorderThick);
            DrawBorder(new Rectangle(panelArea.X + 6, panelArea.Y + 6, panelArea.Width - 12, panelArea.Height - 12),
                new Color(40, 100, 60, 160) * op, 1);

            // Sweep line
            int sweepX = panelArea.X + 10 + (int)(Main.GlobalTimeWrappedHourly * 60f % Math.Max(1, panelArea.Width - 20));
            DrawRect(new Rectangle(sweepX, panelArea.Y + 10, 2, panelArea.Height - 20), new Color(80, 200, 120) * (op * 0.12f));

            // ── Title ──
            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.ExtraBackpackTitle");
            Rectangle titleRect = new(panelArea.X + PanelPad, panelArea.Y + PanelPad, PanelW - PanelPad * 2, TitleH - 4);
            DrawFitText(title, titleRect, new Color(120, 240, 160), 0.88f, 0.5f, op);
            DrawRect(new Rectangle(panelArea.X + PanelPad, panelArea.Y + PanelPad + TitleH - 2, PanelW - PanelPad * 2, 1),
                new Color(80, 200, 120, 160) * op);

            // ── Slots ──
            for (int i = 0; i < ExtraBackpackPlayer.SlotCount; i++)
            {
                Rectangle slotArea = GetSlotArea(i);
                bool hovered = slotArea.Intersects(MouseRect);
                if (hovered) mouseOverPanel = true;

                Item slotItem = bp.backpackContents[i];
                DrawSlot(slotArea, slotItem, hovered, slotFeedback[i], op);

                if (hovered && op >= 0.9f)
                {
                    if (!slotItem.IsAir) { Main.HoverItem = slotItem.Clone(); Main.hoverItemName = slotItem.Name; }
                    if (leftClick && !isDragging) { clickedSlot = i; isRightClickSlot = false; }
                    if (rightClick) { clickedSlot = i; isRightClickSlot = true; }
                }

                if (slotFeedback[i] > 0) slotFeedback[i]--;
            }

            // ── Handle slot click ──
            if (clickedSlot >= 0)
            {
                HandleSlotInteraction(bp.backpackContents, clickedSlot, isRightClickSlot);
                slotFeedback[clickedSlot] = 10;
            }

            // ── Close on outside right click ──
            if (!FadeOut && rightClick && !mouseOverPanel && !isDragging && op >= 0.9f)
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.6f, Pitch = 0.05f });
            }

            if (mouseOverPanel || isDragging) { Main.blockMouse = true; owner.mouseInterface = true; }

            return false;
        }

        private static void HandleSlotInteraction(Item[] contents, int slot, bool isRightClick)
        {
            Item slotItem = contents[slot];
            if (isRightClick)
            {
                if (Main.mouseItem.IsAir && !slotItem.IsAir)
                {
                    int take = (slotItem.stack + 1) / 2;
                    Main.mouseItem = slotItem.Clone();
                    Main.mouseItem.stack = take;
                    slotItem.stack -= take;
                    if (slotItem.stack <= 0) contents[slot] = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else if (!Main.mouseItem.IsAir)
                {
                    if (slotItem.IsAir || (Main.mouseItem.type == slotItem.type && slotItem.stack < slotItem.maxStack))
                    {
                        if (slotItem.IsAir) { contents[slot] = Main.mouseItem.Clone(); contents[slot].stack = 1; }
                        else contents[slot].stack++;
                        Main.mouseItem.stack--;
                        if (Main.mouseItem.stack <= 0) Main.mouseItem = new Item();
                        SoundEngine.PlaySound(SoundID.Grab);
                    }
                }
                return;
            }

            // Left click: swap / merge
            if (Main.mouseItem.IsAir && !slotItem.IsAir)
            {
                Main.mouseItem = slotItem.Clone();
                contents[slot] = new Item();
            }
            else if (!Main.mouseItem.IsAir && slotItem.IsAir)
            {
                contents[slot] = Main.mouseItem.Clone();
                Main.mouseItem = new Item();
            }
            else if (!Main.mouseItem.IsAir && !slotItem.IsAir && Main.mouseItem.type == slotItem.type)
            {
                int canTake = Math.Min(slotItem.maxStack - slotItem.stack, Main.mouseItem.stack);
                if (canTake > 0)
                {
                    slotItem.stack += canTake;
                    Main.mouseItem.stack -= canTake;
                    if (Main.mouseItem.stack <= 0) Main.mouseItem = new Item();
                }
                else
                {
                    Item temp = slotItem.Clone();
                    contents[slot] = Main.mouseItem.Clone();
                    Main.mouseItem = temp;
                }
            }
            else if (!Main.mouseItem.IsAir && !slotItem.IsAir)
            {
                Item temp = slotItem.Clone();
                contents[slot] = Main.mouseItem.Clone();
                Main.mouseItem = temp;
            }
            SoundEngine.PlaySound(SoundID.Grab);
        }

        private Rectangle GetSlotArea(int index)
        {
            int col = index % Cols;
            int row = index / Cols;
            int x = (int)panelTopLeft.X + PanelPad + col * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPad + TitleH + row * (SlotSize + SlotGap);
            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private static void DrawSlot(Rectangle area, Item item, bool hovered, int feedback, float op)
        {
            bool hasItem = item != null && !item.IsAir;
            Color accent = new Color(80, 200, 120);
            Color back = new Color(16, 22, 16);
            if (hovered) back = Color.Lerp(back, accent, 0.18f);
            if (feedback > 0) back = Color.Lerp(back, new Color(255, 230, 80), feedback / 10f * 0.35f);

            DrawRect(area, back * (op * 0.92f));
            DrawBorder(area, (hovered && hasItem ? Color.Lerp(accent, Color.White, 0.3f) : new Color(50, 90, 60)) * op, hovered ? 2 : 1);

            if (!hasItem) return;

            try
            {
                Main.instance.LoadItem(item.type);
                Texture2D tex = TextureAssets.Item[item.type].Value;
                if (tex != null)
                {
                    float scale = Math.Min(MaxIconSize / Math.Max(1f, tex.Width), MaxIconSize / Math.Max(1f, tex.Height));
                    if (hovered) scale *= 1.07f;
                    Main.EntitySpriteDraw(tex, area.Center.ToVector2(), null,
                        Color.White * op, 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
                }
            }
            catch { }

            if (item.stack > 1)
            {
                string st = item.stack >= 10000 ? $"{item.stack / 1000}K" : item.stack.ToString();
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.ItemStack.Value,
                    st, new Vector2(area.X + 2, area.Bottom - 14), Color.White * op, 0f, Vector2.Zero, Vector2.One * 0.8f);
            }
        }

        private static void DrawRect(Rectangle r, Color c) =>
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, c);

        private static void DrawBorder(Rectangle r, Color c, int t)
        {
            DrawRect(new Rectangle(r.X, r.Y, r.Width, t), c);
            DrawRect(new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            DrawRect(new Rectangle(r.X, r.Y, t, r.Height), c);
            DrawRect(new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float op)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 sz = font.MeasureString(text);
            if (sz.X <= 0f) return;
            float scale = maxScale;
            if (sz.X * scale > area.Width) scale = area.Width / sz.X;
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 pos = new(area.X + (area.Width - sz.X * scale) * 0.5f, area.Y + (area.Height - sz.Y * scale) * 0.5f);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, pos, color * op, 0f, Vector2.Zero, Vector2.One * scale);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) { }
    }
}
