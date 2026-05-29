using CalamityLegendsComeBack.Accssory.TS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 天理真和持续时间条：不可见弹幕挂在玩家头顶绘制进度条。
    internal sealed class AzureThunderHarmonyBar : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            // 只承担 UI 绘制，不碰撞、不受水和地形影响。
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

            // Buff 消失、玩家死亡或离线时立刻清理条形弹幕。
            if (!owner.active || owner.dead || !owner.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
            {
                Projectile.Kill();
                return;
            }

            // 条形固定在玩家头顶，并通过 timeLeft=2 保持存活。
            Projectile.Center = owner.Center + new Vector2(0f, -62f);
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.18f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 只给本地玩家画自己的持续时间条，避免多人里重复绘制。
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            int buffIndex = owner.FindBuffIndex(ModContent.BuffType<AzureThunderHarmonyBuff>());
            if (buffIndex < 0)
                return false;

            // 优先使用本次终极记录的实际持续时间；异常时回退到饰品计算值。
            int duration = owner.GetModPlayer<AzureThunderPlayer>().ActiveHarmonyDuration;
            if (duration <= 0)
                duration = AzureThunderAccessoryPlayer.GetHarmonyDuration(owner);

            // buffTime/duration 转成剩余比例，进度越低颜色越偏青。
            float progress = MathHelper.Clamp(owner.buffTime[buffIndex] / (float)duration, 0f, 1f);
            Texture2D barBackground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barForeground = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            Vector2 drawPosition = owner.Center - Main.screenPosition + new Vector2(0f, -62f) - barBackground.Size() * 0.5f;
            Rectangle frameCrop = new(0, 0, (int)(barForeground.Width * progress), barForeground.Height);
            Color color = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.Yellow, 1f - progress) * Projectile.Opacity;

            // 先画背景，再用裁剪矩形绘制剩余长度。
            Main.spriteBatch.Draw(barBackground, drawPosition, null, color * 0.72f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(barForeground, drawPosition, frameCrop, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
