using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    /// <summary>
    /// 头顶速凝掩体蓄力 UI 状态条，样式与 DragoonDrizzlefishFoodStatusBar 一致。
    /// 当玩家同时按住鼠标左右键 1 秒蓄力时悬浮在玩家头顶。
    /// </summary>
    public class AegisWallStatusBar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            AegisBladePlayer bp = owner.GetModPlayer<AegisBladePlayer>();
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<AegisBlade>() || !bp.IsChargingBarrier)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, -62f);
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.16f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            AegisBladePlayer bp = owner.GetModPlayer<AegisBladePlayer>();
            if (!bp.IsChargingBarrier)
                return false;

            float progress = MathHelper.Clamp(bp.BarrierChargeTimer / 60f, 0f, 1f);
            Texture2D background = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D foreground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            const float scale = 0.92f;

            Vector2 drawPosition = owner.Center - Main.screenPosition + new Vector2(0f, -62f) - background.Size() * scale * 0.5f;
            Rectangle crop = new(0, 0, (int)(foreground.Width * progress), foreground.Height);

            Color themeColor = AegisVisuals.Gold;
            Color fill = Color.Lerp(new Color(65, 40, 20), themeColor, 0.85f) * Projectile.Opacity;

            Main.spriteBatch.Draw(background, drawPosition, null, new Color(20, 16, 20) * Projectile.Opacity, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            if (crop.Width > 0)
                Main.spriteBatch.Draw(foreground, drawPosition, crop, fill, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            Rectangle border = new((int)drawPosition.X - 2, (int)drawPosition.Y - 2, (int)(background.Width * scale) + 4, (int)(background.Height * scale) + 4);
            Main.spriteBatch.Draw(pixel, new Rectangle(border.X, border.Y, border.Width, 1), themeColor * (Projectile.Opacity * 0.72f));
            Main.spriteBatch.Draw(pixel, new Rectangle(border.X, border.Bottom - 1, border.Width, 1), themeColor * (Projectile.Opacity * 0.72f));
            Main.spriteBatch.Draw(pixel, new Rectangle(border.X, border.Y, 1, border.Height), themeColor * (Projectile.Opacity * 0.72f));
            Main.spriteBatch.Draw(pixel, new Rectangle(border.Right - 1, border.Y, 1, border.Height), themeColor * (Projectile.Opacity * 0.72f));

            int pct = (int)(progress * 100f);
            string text = $"速凝掩体蓄力 {pct}%";
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * 0.68f;
            Vector2 textPosition = new(owner.Center.X - Main.screenPosition.X - textSize.X * 0.5f, drawPosition.Y + background.Height * scale + 2f);
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, text, textPosition,
                Color.White * Projectile.Opacity, Color.Black * Projectile.Opacity, 0.68f);
            return false;
        }
    }
}
