using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip
{
    internal sealed class CtrlChipNEWReticle : ModProjectile
    {
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
            if (Main.dedServ || Projectile.owner != Main.myPlayer)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            CtrlChipPlayer ctrlPlayer = owner.GetModPlayer<CtrlChipPlayer>();

            if (!owner.active ||
                owner.dead ||
                !ctrlPlayer.CtrlChipEquipped ||
                ctrlPlayer.CtrlChipVisualsHidden ||
                ctrlPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ctrlPlayer.ReticleWorld;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D reticle = ModContent.Request<Texture2D>(
                "CalamityMod/Particles/DestroyerReticleTelegraph"
            ).Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = reticle.Size() * 0.5f;

            CtrlChipPlayer ctrlPlayer =
                Main.player[Projectile.owner].GetModPlayer<CtrlChipPlayer>();

            bool locked = ctrlPlayer.ReticleHasTarget;

            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(time * (locked ? 10f : 7f));

            Color techBlue = new Color(70, 190, 255);
            Color white = Color.White;

            Color outerColor = locked ? Color.Lerp(techBlue, white, 0.35f) : techBlue;
            Color innerColor = locked ? Color.Lerp(white, techBlue, 0.25f) : white;

            float outerScale = 0.46f * pulse * VisualScale;
            float innerScale = 0.38f * pulse * VisualScale;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(reticle, drawPosition, null, outerColor * 0.88f, time * 1.26f, origin, outerScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(reticle, drawPosition, null, innerColor * 0.76f, -time * 0.96f, origin, innerScale, SpriteEffects.FlipHorizontally, 0f);

            if (locked)
                Main.EntitySpriteDraw(reticle, drawPosition, null, white * 0.28f, time * 1.80f, origin, 0.24f * pulse * VisualScale, SpriteEffects.None, 0f);

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
