using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90CooldownBar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.P90";
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
            if (!owner.active || owner.dead || owner.GetModPlayer<NewLegendP90Player>().DashCooldownTimer <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, -76f);
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.16f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            NewLegendP90Player p90Player = owner.GetModPlayer<NewLegendP90Player>();
            float progress = p90Player.DashCooldownCompletion;
            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            const float scale = 1.35f;

            Vector2 drawPosition = owner.Center - Main.screenPosition + new Vector2(0f, -76f) - barBackground.Size() * scale * 0.5f;
            Rectangle frameCrop = new(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
            Color matrix = Color.Lerp(new Color(42, 160, 96), new Color(105, 255, 182), progress) * Projectile.Opacity;
            Color dim = new Color(8, 36, 24) * Projectile.Opacity;

            Main.spriteBatch.Draw(barBackground, drawPosition, null, dim, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            if (frameCrop.Width > 0)
                Main.spriteBatch.Draw(barForeground, drawPosition, frameCrop, matrix, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawMatrixFrame(pixel, drawPosition, barBackground.Size() * scale, matrix);

            int seconds = Math.Max(1, (int)MathF.Ceiling(p90Player.DashCooldownTimer / 60f));
            Vector2 textPos = drawPosition + new Vector2(barBackground.Width * scale + 7f, -5f);
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, seconds.ToString(), textPos, Color.White * Projectile.Opacity, Color.Black * Projectile.Opacity, 0.72f);
            return false;
        }

        private static void DrawMatrixFrame(Texture2D pixel, Vector2 topLeft, Vector2 size, Color color)
        {
            Rectangle top = new((int)topLeft.X - 3, (int)topLeft.Y - 3, (int)size.X + 6, 1);
            Rectangle bottom = new((int)topLeft.X - 3, (int)(topLeft.Y + size.Y + 2), (int)size.X + 6, 1);
            Rectangle left = new((int)topLeft.X - 3, (int)topLeft.Y - 3, 1, (int)size.Y + 6);
            Rectangle right = new((int)(topLeft.X + size.X + 2), (int)topLeft.Y - 3, 1, (int)size.Y + 6);
            Main.spriteBatch.Draw(pixel, top, color * 0.72f);
            Main.spriteBatch.Draw(pixel, bottom, color * 0.72f);
            Main.spriteBatch.Draw(pixel, left, color * 0.72f);
            Main.spriteBatch.Draw(pixel, right, color * 0.72f);

            float scan = (Main.GlobalTimeWrappedHourly * 48f) % Math.Max(1f, size.X);
            Rectangle scanLine = new((int)(topLeft.X + scan), (int)topLeft.Y - 4, 1, (int)size.Y + 8);
            Main.spriteBatch.Draw(pixel, scanLine, Color.White * 0.45f);

            for (int i = 0; i < 4; i++)
            {
                float x = topLeft.X + i * size.X / 3f;
                Rectangle tick = new((int)x, (int)topLeft.Y - 5, 1, 3);
                Main.spriteBatch.Draw(pixel, tick, color * 0.65f);
            }
        }
    }
}
