using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ResonanceCore
{
    public class ResonanceCoreShieldVisual : ModProjectile
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

            ResonanceCorePlayer modPlayer = owner.GetModPlayer<ResonanceCorePlayer>();
            if (!modPlayer.ShieldReady)
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
            ResonanceCorePlayer modPlayer = owner.GetModPlayer<ResonanceCorePlayer>();

            float hitFlash = modPlayer.ShieldHitFlashTimer / 18f;
            float scale = 0.42f + 0.04f * (0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 1.4f + Projectile.identity * 0.2f));
            scale += hitFlash * 0.08f;

            float noiseScale = MathHelper.Lerp(0.42f, 0.82f, (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f) * 0.5f + 0.5f);

            Effect shieldEffect = Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f + hitFlash);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            float baseOpacity = 0.82f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseOpacity + hitFlash * 0.2f);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f + hitFlash * 2f);

            Color shieldColor = Color.Lerp(new Color(60, 120, 255), new Color(80, 225, 255), 0.45f);
            Color edgeColor = Color.Lerp(new Color(100, 210, 255), Color.White, 0.35f + hitFlash * 0.4f);

            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/FrozenCrust").Value;
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
