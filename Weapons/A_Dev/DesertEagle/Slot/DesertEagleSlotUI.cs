using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 中键点击沙漠之鹰后弹出的单格 UI 覆盖层（以弹幕形式存在）。
    /// 渲染一个 Terraria 风格物品栏格子，玩家可将列表中的手枪拖入/取出。
    /// </summary>
    public class DesertEagleSlotUI : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "Terraria/Images/UI/Inventory_Back";

        // ── 面板常量 ─────────────────────────────────────────────
        private const int PanelW = 200;
        private const int PanelH = 100;
        private const int SlotSize = 52;
        private static readonly Color FrameColor = new(50, 50, 100, 200);
        private static readonly Color BorderColor = new(120, 140, 200, 240);
        private static readonly Color SlotColor = new(30, 30, 60, 220);

        private float Opacity
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private bool Closing
        {
            get => Projectile.ai[1] != 0;
            set => Projectile.ai[1] = value ? 1f : 0f;
        }

        // 面板屏幕坐标（固定在画面中央偏上）
        private Rectangle PanelRect => new(
            (Main.screenWidth - PanelW) / 2,
            (Main.screenHeight - PanelH) / 2 - 60,
            PanelW, PanelH);

        private Rectangle SlotRect
        {
            get
            {
                Rectangle p = PanelRect;
                int slotX = p.X + (p.Width - SlotSize) / 2;
                int slotY = p.Y + (p.Height - SlotSize) / 2 + 8;
                return new Rectangle(slotX, slotY, SlotSize, SlotSize);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = int.MaxValue;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Projectile.owner != Main.myPlayer)
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.LocalPlayer;
            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();
            slotPlayer.SlotUIOpen = true;

            // 跟随玩家位置（避免被剔除视野）
            Projectile.Center = player.Center;

            // 淡入/淡出
            if (!Closing)
            {
                Opacity = MathHelper.Clamp(Opacity + 0.08f, 0f, 1f);

                // 关闭条件：玩家打开了物品栏以外的 UI，或按下 ESC
                bool inventoryOpen = Main.playerInventory;
                if (!inventoryOpen)
                {
                    Closing = true;
                }
            }
            else
            {
                Opacity -= 0.1f;
                if (Opacity <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // 防止玩家使用武器（鼠标在面板区域内时拦截鼠标）
            if (!Closing && PanelRect.Contains(Main.MouseScreen.ToPoint()))
            {
                player.mouseInterface = true;
                HandleSlotInteraction(slotPlayer);
            }
        }

        private void HandleSlotInteraction(DesertEagleSlotPlayer slotPlayer)
        {
            Rectangle slot = SlotRect;
            if (!slot.Contains(Main.MouseScreen.ToPoint()))
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
                slotPlayer.SlottedGun.SetDefaults(0);
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
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.owner != Main.myPlayer || Main.dedServ)
                return false;

            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.UIScaleMatrix);

            float opacity = Opacity;
            Rectangle panel = PanelRect;
            Rectangle slot = SlotRect;

            // 背景面板
            DrawRect(sb, panel, FrameColor * opacity, BorderColor * opacity);

            // 标题文字
            string title = Main.LocalPlayer.HeldItem.type == ModContent.ItemType<DesertEagle>()
                ? "[Chamber Slot]"
                : "[Chamber Slot]";
            Utils.DrawBorderStringFourWay(sb, FontAssets.MouseText.Value, title,
                panel.X + 8, panel.Y + 8, Color.Silver * opacity, Color.Black * opacity, Vector2.Zero, 0.85f);

            // 格子
            DrawRect(sb, new Rectangle(slot.X - 2, slot.Y - 2, slot.Width + 4, slot.Height + 4),
                SlotColor * opacity, BorderColor * opacity);

            // 格子内物品
            DesertEagleSlotPlayer slotPlayer = Main.LocalPlayer.GetModPlayer<DesertEagleSlotPlayer>();
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

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
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

        // 不绘制武器本身贴图
        public override bool ShouldUpdatePosition() => false;
    }
}
