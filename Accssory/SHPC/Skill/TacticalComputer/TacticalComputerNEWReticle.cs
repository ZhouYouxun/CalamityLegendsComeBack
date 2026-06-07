using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.TacticalComputer
{
    internal sealed class TacticalComputerNEWReticle : ModProjectile
    {
        // 只改绘制尺寸：原来是 1f / 3f，现在整体平均大小扩大到 2 倍。
        private const float VisualScale = 2f / 3f;

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

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            // 准心只给本地玩家显示，服务器和其他玩家不需要维护这个视觉弹幕。
            if (Main.dedServ || Projectile.owner != Main.myPlayer)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            TacticalComputerPlayer tacticalPlayer = owner.GetModPlayer<TacticalComputerPlayer>();

            if (!owner.active ||
                owner.dead ||
                !tacticalPlayer.TacticalComputerEquipped ||
                tacticalPlayer.TacticalComputerVisualsHidden ||
                tacticalPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            // 飞行逻辑不改：准心位置仍然完全使用 Player 文件算出来的 ReticleWorld。
            Projectile.Center = tacticalPlayer.ReticleWorld;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D reticle = ModContent.Request<Texture2D>(
                "CalamityMod/Particles/DestroyerReticleTelegraph"
            ).Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = reticle.Size() * 0.5f;

            TacticalComputerPlayer tacticalPlayer =
                Main.player[Projectile.owner].GetModPlayer<TacticalComputerPlayer>();

            // 只读取已有锁定状态，不参与、不改变任何飞行逻辑。
            bool locked = tacticalPlayer.ReticleHasTarget;

            float time = Main.GlobalTimeWrappedHourly;

            // 呼吸效果：平均大小扩大 2 倍，振幅缩小约 33%。
            float pulse = 0.85f + 0.15f * (float)Math.Sin(time * (locked ? 10f : 7f));

            Color techBlue = new Color(70, 190, 255);
            Color white = Color.White;

            Color outerColor = locked
                ? Color.Lerp(techBlue, white, 0.35f)
                : techBlue;

            Color innerColor = locked
                ? Color.Lerp(white, techBlue, 0.25f)
                : white;

            float outerScale = 0.46f * pulse * VisualScale;
            float innerScale = 0.38f * pulse * VisualScale;

            // 这张灾厄贴图黑底区域依赖加法混合隐藏。
            // 如果用默认 AlphaBlend，黑色 RGB 会被真实画出来，所以这里必须临时切到 Additive。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            // 外环：转速 0.95 -> 1.26，增加约 33%。
            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                outerColor * 0.88f,
                time * 1.26f,
                origin,
                outerScale,
                SpriteEffects.None,
                0f);

            // 内环：反向旋转，转速 0.72 -> 0.96，增加约 33%。
            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                innerColor * 0.76f,
                -time * 0.96f,
                origin,
                innerScale,
                SpriteEffects.FlipHorizontally,
                0f);

            // 锁定时额外高亮层。
            if (locked)
            {
                Main.EntitySpriteDraw(
                    reticle,
                    drawPosition,
                    null,
                    white * 0.28f,
                    time * 1.80f,
                    origin,
                    0.24f * pulse * VisualScale,
                    SpriteEffects.None,
                    0f);
            }

            // 恢复 tML 默认绘制状态，避免影响后面的弹幕、NPC、UI。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
