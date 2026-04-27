using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar
{
    internal sealed class RightClickMortar_Lazer : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        private bool fired;

        public override Color? GetAlpha(Color lightColor)
            => new Color(105, 225, 255, 0);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 500;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (!fired)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.ELRFireSound with { Volume = 0.34f, PitchVariance = 0.18f, MaxInstances = 12 }, Projectile.Center);
                fired = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, 0.2f, 0.55f, 0.65f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Projectile.DrawBeam(200f, 3f, lightColor);
            // return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = GetAlpha(lightColor) ?? lightColor;

            //Main.EntitySpriteDraw(
            //    texture,
            //    drawPosition,
            //    frame,
            //    drawColor * Projectile.Opacity,
            //    Projectile.rotation,
            //    frame.Size() * 0.5f,
            //    Projectile.scale,
            //    SpriteEffects.None,
            //    0f);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak")
            );

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                trailPoints.Length * 2
            );

            Vector2[] coreTrail = trailPoints.Take(Math.Min(9, trailPoints.Length)).ToArray();
            if (coreTrail.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak")
            );

            PrimitiveRenderer.RenderTrail(
                coreTrail,
                new PrimitiveSettings(
                    TrailCoreWidthFunction,
                    TrailCoreColorFunction,
                    TrailOffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                coreTrail.Length * 2
            );
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new Vector2[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private Vector2 TrailOffsetFunction(float completion, Vector2 _)
        {
            float lateralWave = (float)Math.Sin(completion * MathHelper.Pi * 1.15f + Main.GlobalTimeWrappedHourly * 10f) * 0.6f;
            return Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * lateralWave;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * 15f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color headColor = new Color(105, 225, 255);
            Color midColor = new Color(42, 145, 255);
            float opacity = Projectile.Opacity * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color bodyColor = Color.Lerp(headColor, midColor, completion * 0.65f) * opacity;
            Color tipColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.74f, 1f, completion, true));
            tipColor.A = 0;
            return Color.Lerp(bodyColor, tipColor, completion);
        }

        private float TrailCoreWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * 8.5f;
            float curveRatio = 0.18f;

            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color TrailCoreColorFunction(float completion, Vector2 _)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color bodyColor = Color.Lerp(Color.White, new Color(185, 245, 255), completion * 0.5f) * opacity;
            Color tipColor = Color.Lerp(bodyColor, Color.Transparent, Utils.GetLerpValue(0.78f, 1f, completion, true));
            tipColor.A = 0;
            return Color.Lerp(bodyColor, tipColor, completion);
        }
    }
}
