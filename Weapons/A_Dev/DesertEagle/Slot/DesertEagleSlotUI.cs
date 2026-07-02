using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 中键点击沙漠之鹰后弹出的单格 UI 覆盖层。
    /// 与 SHPCLoadingUI 同架构：纯界面层实现，检测、交互、绘制全部在 Draw 阶段完成，
    /// 因此自动暂停（gamePaused）期间也能正常弹出并放入/取出手枪。
    /// </summary>
    public static class DesertEagleSlotUI
    {
        // ── 面板常量 ─────────────────────────────────────────────
        private const int PanelW = 200;
        private const int PanelH = 100;
        private const int SlotSize = 52;
        private static readonly Color FrameColor = new(50, 50, 100, 200);
        private static readonly Color BorderColor = new(120, 140, 200, 240);
        private static readonly Color SlotColor = new(30, 30, 60, 220);

        // ── 状态 ────────────────────────────────────────────────
        public static bool IsOpen { get; private set; }
        private static float opacity;
        private static bool closing;

        // 面板屏幕坐标（raw 屏幕像素，与 SHPCLoadingUI 同坐标系；
        // 界面层用 InterfaceScaleType.None，此时 Main.mouseX/screenWidth 都是原始像素，
        // 绝对不要再除 UIScale——tML 只在 ScaleType.UI 的层里才做该转换，双重转换会让点击热区脱靶）
        public static Rectangle PanelRect => new(
            (Main.screenWidth - PanelW) / 2,
            (Main.screenHeight - PanelH) / 2 - 60,
            PanelW, PanelH);

        public static Rectangle SlotRect
        {
            get
            {
                Rectangle p = PanelRect;
                int slotX = p.X + (p.Width - SlotSize) / 2;
                int slotY = p.Y + (p.Height - SlotSize) / 2 + 8;
                return new Rectangle(slotX, slotY, SlotSize, SlotSize);
            }
        }

        // ── 开关 ────────────────────────────────────────────────
        public static void Toggle(Player player)
        {
            if (IsOpen && !closing)
            {
                BeginClose(player, true);
            }
            else
            {
                IsOpen = true;
                closing = false;
                SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f }, player.Center);
            }
        }

        private static void BeginClose(Player player, bool playSound)
        {
            closing = true;
            if (playSound)
                SoundEngine.PlaySound(SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }, player.Center);
        }

        // ── 主入口：每帧由界面层调用（暂停时照常执行） ─────────────
        public static void DrawAndHandle(SpriteBatch sb)
        {
            if (!IsOpen)
                return;

            Player player = Main.LocalPlayer;
            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();

            // 关闭条件：背包被收起 → 淡出
            if (!closing && !Main.playerInventory)
                closing = true;

            // 淡入/淡出
            if (!closing)
            {
                opacity = MathHelper.Clamp(opacity + 0.08f, 0f, 1f);
            }
            else
            {
                opacity -= 0.1f;
                if (opacity <= 0f)
                {
                    opacity = 0f;
                    IsOpen = false;
                    return;
                }
            }

            // ScaleType.None 层内 Main.mouseX/Y 即原始像素，直接使用。
            int mouseX = Main.mouseX;
            int mouseY = Main.mouseY;

            // 鼠标在面板区域内时拦截鼠标，防止误用武器/误丢物品
            if (!closing && PanelRect.Contains(mouseX, mouseY))
            {
                player.mouseInterface = true;
                Main.blockMouse = true;
                HandleSlotInteraction(slotPlayer, mouseX, mouseY);
            }

            DrawPanel(sb, slotPlayer, mouseX, mouseY);
        }

        // ── 格子交互 ─────────────────────────────────────────────
        private static void HandleSlotInteraction(DesertEagleSlotPlayer slotPlayer, int mouseX, int mouseY)
        {
            if (!SlotRect.Contains(mouseX, mouseY))
                return;

            if (!Main.mouseLeft || !Main.mouseLeftRelease)
                return;

            Item mouseItem = Main.mouseItem;
            Item slottedGun = slotPlayer.SlottedGun;

            bool mouseHasRegisteredGun = !mouseItem.IsAir && DEBulletRegistry.IsRegisteredGun(mouseItem.type);
            bool slotHasItem = !slottedGun.IsAir;

            if (mouseHasRegisteredGun && !slotHasItem)
            {
                // 放入格子
                slotPlayer.SlottedGun = mouseItem.Clone();
                slotPlayer.SlottedGun.stack = 1;
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.Center);
                ResetRuleCounters(slotPlayer);
            }
            else if (!mouseHasRegisteredGun && slotHasItem && mouseItem.IsAir)
            {
                // 取出格子
                Main.mouseItem = slottedGun.Clone();
                slotPlayer.SlottedGun.TurnToAir();
                slotPlayer.SlottedGun.SetDefaults(ItemID.None);
                SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.Center);
                ResetRuleCounters(slotPlayer);
            }
            else if (mouseHasRegisteredGun && slotHasItem)
            {
                // 交换
                Item temp = slottedGun.Clone();
                slotPlayer.SlottedGun = mouseItem.Clone();
                slotPlayer.SlottedGun.stack = 1;
                Main.mouseItem = temp;
                SoundEngine.PlaySound(SoundID.Grab, Main.LocalPlayer.Center);
                ResetRuleCounters(slotPlayer);
            }
        }

        private static void ResetRuleCounters(DesertEagleSlotPlayer slotPlayer)
        {
            slotPlayer.CardIndex = 0;
            slotPlayer.ToggleState = false;
            slotPlayer.BurstCounter = 0;
        }

        // ── 绘制 ─────────────────────────────────────────────────
        private static void DrawPanel(SpriteBatch sb, DesertEagleSlotPlayer slotPlayer, int mouseX, int mouseY)
        {
            Rectangle panel = PanelRect;
            Rectangle slot = SlotRect;

            // 背景面板
            DrawRect(sb, panel, FrameColor * opacity, BorderColor * opacity);

            // 标题文字
            Utils.DrawBorderStringFourWay(sb, FontAssets.MouseText.Value, "[Chamber Slot]",
                panel.X + 8, panel.Y + 8, Color.Silver * opacity, Color.Black * opacity, Vector2.Zero, 0.85f);

            // 格子（悬停时根据鼠标物品给出绿/红反馈）
            bool hovered = slot.Contains(mouseX, mouseY);
            bool mouseHasRegisteredGun = !Main.mouseItem.IsAir && DEBulletRegistry.IsRegisteredGun(Main.mouseItem.type);
            bool mouseHasWrongItem = !Main.mouseItem.IsAir && !mouseHasRegisteredGun;

            Color slotBorder = BorderColor;
            if (hovered && !closing)
            {
                if (mouseHasRegisteredGun)
                    slotBorder = Color.Lerp(slotBorder, Color.LimeGreen, 0.55f);
                else if (mouseHasWrongItem)
                    slotBorder = Color.Lerp(slotBorder, new Color(255, 90, 90), 0.5f);
                else
                    slotBorder = Color.Lerp(slotBorder, Color.White, 0.35f);
            }

            DrawRect(sb, new Rectangle(slot.X - 2, slot.Y - 2, slot.Width + 4, slot.Height + 4),
                SlotColor * opacity, slotBorder * opacity);

            // 格子内物品
            if (!slotPlayer.SlottedGun.IsAir)
            {
                Main.instance.LoadItem(slotPlayer.SlottedGun.type);
                Texture2D itemTex = TextureAssets.Item[slotPlayer.SlottedGun.type].Value;
                float scale = System.Math.Min(
                    (slot.Width - 8f) / itemTex.Width,
                    (slot.Height - 8f) / itemTex.Height);
                scale = System.Math.Min(scale, 1f);
                Vector2 center = new(slot.X + slot.Width / 2f, slot.Y + slot.Height / 2f);
                sb.Draw(itemTex, center, null, Color.White * opacity, 0f,
                    itemTex.Size() / 2f, scale, SpriteEffects.None, 0f);

                if (hovered && Main.mouseItem.IsAir)
                    Main.hoverItemName = slotPlayer.SlottedGun.Name;
            }
            else
            {
                // 空槽提示
                Utils.DrawBorderStringFourWay(sb, FontAssets.MouseText.Value, "Empty",
                    slot.X + 6, slot.Y + slot.Height / 2 - 8,
                    Color.Gray * 0.7f * opacity, Color.Black * opacity, Vector2.Zero, 0.75f);
            }

            // 提示行
            string hint = slotPlayer.SlottedGun.IsAir
                ? "Left-click to slot a pistol"
                : "Left-click to retrieve";
            Utils.DrawBorderStringFourWay(sb, FontAssets.MouseText.Value, hint,
                panel.X + 8, panel.Y + panel.Height - 22,
                Color.SkyBlue * 0.85f * opacity, Color.Black * opacity, Vector2.Zero, 0.72f);
        }

        private static void DrawRect(SpriteBatch sb, Rectangle rect, Color fill, Color border)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // 填充
            sb.Draw(pixel, rect, fill);

            // 四条边框
            int b = 2;
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, b), border);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - b, rect.Width, b), border);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, b, rect.Height), border);
            sb.Draw(pixel, new Rectangle(rect.Right - b, rect.Y, b, rect.Height), border);
        }
    }
}
