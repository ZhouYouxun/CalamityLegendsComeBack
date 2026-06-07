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
        // 沿用旧准心的整体缩放，让新贴图不会过大。
        private const float VisualScale = 1f / 3f;

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
            if (!owner.active || owner.dead || !tacticalPlayer.TacticalComputerEquipped || tacticalPlayer.TacticalComputerVisualsHidden || tacticalPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            // 关键逻辑不改：准心位置完全跟随 TacticalComputerPlayer 算出来的 ReticleWorld。
            // 鼠标跟随、敌人吸附、锁定判定都仍然由 TacticalComputerPlayer 负责。
            Projectile.Center = tacticalPlayer.ReticleWorld;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.whoAmI != Main.myPlayer)
                return false;

            TacticalComputerPlayer tacticalPlayer = owner.GetModPlayer<TacticalComputerPlayer>();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            bool locked = tacticalPlayer.ReticleHasTarget;
            float time = Main.GlobalTimeWrappedHourly;

            // 沿用旧准心的呼吸节奏：未锁定较慢，锁定后更急促。
            float pulse = 0.78f + 0.22f * (float)Math.Sin(time * (locked ? 10f : 7f));
            float lockInterpolant = locked ? 1f : 0f;

            Texture2D reticle = ModContent.Request<Texture2D>("CalamityMod/Particles/DestroyerReticleTelegraph").Value;
            Vector2 origin = reticle.Size() * 0.5f;

            Color techBlue = new(70, 190, 255, 0);
            Color cyan = new(150, 245, 255, 0);
            Color white = new(235, 255, 255, 0);
            Color outerColor = Color.Lerp(techBlue, cyan, locked ? 0.7f : 0.35f);
            Color innerColor = Color.Lerp(cyan, white, locked ? 0.65f : 0.35f);

            // 两层同贴图叠加：一层顺时针，一层逆时针，形成旧准心那种扫描旋转感。
            float outerScale = MathHelper.Lerp(0.46f, 0.58f, lockInterpolant) * pulse * VisualScale;
            float innerScale = MathHelper.Lerp(0.38f, 0.50f, lockInterpolant) * pulse * VisualScale;

            // 开启加法混合，使准心能够正常显示，并且呈现出科技全息的半透明发光感
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                outerColor * 0.88f,
                time * 0.95f,
                origin,
                outerScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                innerColor * 0.76f,
                -time * 0.72f,
                origin,
                innerScale,
                SpriteEffects.FlipHorizontally,
                0f);

            // 锁定时补一层很淡的中心亮光，让“吸附到敌人”的状态更容易被看出来。
            if (locked)
        {
                Main.EntitySpriteDraw(
                    reticle,
                    drawPosition,
                    null,
                    white * 0.28f,
                    time * 1.35f,
                    origin,
                    0.24f * pulse * VisualScale,
                    SpriteEffects.None,
                    0f);
        }

            // 还原为默认的 AlphaBlend 混合状态，避免污染其他图层的绘制
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
            }
        }
    }
