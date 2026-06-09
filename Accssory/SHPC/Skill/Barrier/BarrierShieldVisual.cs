using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    public class BarrierShieldVisual : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            BarrierPlayer modPlayer = owner.GetModPlayer<BarrierPlayer>();
            if (!modPlayer.ShieldActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            BarrierPlayer modPlayer = owner.GetModPlayer<BarrierPlayer>();

            float hitFlash = modPlayer.ShieldHitFlashTimer / 18f;
            float scale = 0.42f + 0.04f * (0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 1.4f + Projectile.identity * 0.2f));
            scale += hitFlash * 0.08f;
            scale *= 0.67f;

            float noiseScale = MathHelper.Lerp(0.62f, 1.12f, (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.38f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.85f + hitFlash * 1.15f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.38f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            float baseOpacity = 0.82f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseOpacity + hitFlash * 0.2f);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(5.5f + hitFlash * 2.75f);

            Color shieldColor = Color.Lerp(new Color(32, 205, 168), new Color(104, 122, 255), 0.35f + hitFlash * 0.2f);
            Color edgeColor = Color.Lerp(new Color(152, 255, 218), Color.White, 0.28f + hitFlash * 0.45f);

            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons").Value;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float texRotation = Main.GlobalTimeWrappedHourly * 0.75f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                shieldEffect,
                Main.Transform);

            Main.spriteBatch.Draw(
                tex,
                pos,
                null,
                Color.White,
                texRotation,
                tex.Size() / 2f,
                scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform);

            return false;
        }
    }
}
