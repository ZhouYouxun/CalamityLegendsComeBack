using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    internal static class YC_YharimBeamVisuals
    {
        private const string BeamTexturePath = "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";
        private const float BeamRenderTileOffset = 10.5f;
        private const float BeamLengthReductionFactor = 14.5f;

        public static void DrawYharimBeam(Projectile projectile, float beamLength, float beamScale, float opacity, Color beamColor)
        {
            if (projectile.velocity == Vector2.Zero || beamLength <= 0f || opacity <= 0f)
                return;

            Texture2D texture = ModContent.Request<Texture2D>(BeamTexturePath).Value;
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = projectile.Center.Floor() + direction * beamScale * BeamRenderTileOffset - Main.screenPosition;
            float visibleLength = beamLength - BeamLengthReductionFactor * beamScale * beamScale;
            Vector2 end = start + direction * MathHelper.Max(8f, visibleLength);
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);

            beamColor.A = 64;
            DelegateMethods.f_1 = 1f;
            DelegateMethods.c_1 = beamColor * (0.75f * opacity);
            Utils.DrawLaser(Main.spriteBatch, texture, start, end, new Vector2(beamScale), framing);

            DelegateMethods.c_1 = Color.White * (0.1f * opacity);
            Utils.DrawLaser(Main.spriteBatch, texture, start, end, new Vector2(beamScale * 0.5f), framing);
        }

        public static void EmitYharimBeamDust(Projectile projectile, float beamLength, float beamScale, Color beamColor)
        {
            if (Main.dedServ || projectile.velocity == Vector2.Zero || beamLength <= 0f)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 laserEnd = projectile.Center + direction * (beamLength - 14.5f * beamScale);
            for (int i = 0; i < 2; i++)
            {
                float dustAngle = projectile.rotation + (Main.rand.NextBool() ? 1f : -1f) * MathHelper.PiOver2;
                Vector2 dustVelocity = dustAngle.ToRotationVector2() * Main.rand.NextFloat(1f, 1.8f);
                int dustIndex = Dust.NewDust(laserEnd, 0, 0, DustID.CopperCoin, dustVelocity.X, dustVelocity.Y, 0, beamColor, 3.3f);
                Main.dust[dustIndex].color = beamColor;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].scale = 1.2f * MathHelper.Max(1f, beamScale);
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 dustOffset = direction.RotatedBy(MathHelper.PiOver2) * (Main.rand.NextFloat() - 0.5f) * projectile.width;
                Vector2 dustPosition = laserEnd + dustOffset - Vector2.One * 4f;
                int dustIndex = Dust.NewDust(dustPosition, 8, 8, DustID.CopperCoin, 0f, 0f, 100, beamColor, 5f);
                Main.dust[dustIndex].velocity *= 0.5f;
                Main.dust[dustIndex].velocity.Y = -System.Math.Abs(Main.dust[dustIndex].velocity.Y);
            }
        }
    }
}
