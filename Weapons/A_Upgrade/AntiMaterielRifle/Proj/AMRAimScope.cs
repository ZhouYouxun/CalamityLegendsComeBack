using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRAimScope : ModProjectile, ILocalizedModType
    {
        private const float MaxSightAngle = MathHelper.Pi / 3f;
        private const float PreviewLaserStartCharge = 0.5f;
        private const float PreviewLaserFullCharge = 0.75f;
        private const float PreviewLaserMaxLength = 2600f;
        private int holdoutIdentity = -1;

        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
        }

        public override void OnSpawn(IEntitySource source)
        {
            holdoutIdentity = (int)Projectile.ai[0];
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Owner.Calamity().mouseWorldListener = true;

            if (!Owner.active || Owner.dead || FindHoldout() is not AMRHoldout holdout || !holdout.RightAiming)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = holdout.GunTipPosition;
            Projectile.velocity = holdout.AimDirection;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.localAI[0] = holdout.ScopeChargeCompletion;
            Projectile.timeLeft = 2;

            if (holdout.ScopeChargeCompletion >= 1f && Main.rand.NextBool(2))
            {
                Vector2 velocity = holdout.AimDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(1f, 4f);
                Dust spark = Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.GoldFlame, velocity,
                    70, new Color(255, 215, 0), Main.rand.NextFloat(0.8f, 1.2f));
                spark.noGravity = true;
            }
        }

        private AMRHoldout FindHoldout()
        {
            int holdoutType = ModContent.ProjectileType<AMRHoldout>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != holdoutType)
                    continue;

                if (holdoutIdentity < 0 || projectile.identity == holdoutIdentity)
                    return projectile.ModProjectile as AMRHoldout;
            }

            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float charge = MathHelper.Clamp(Projectile.localAI[0], 0f, 1f);
            float sightsSize = MathHelper.Lerp(260f, 430f, charge);
            float sightsResolution = MathHelper.Lerp(0.05f, 0.2f, Math.Min(charge * 1.5f, 1f));
            float halfAngle = (1f - charge) * MaxSightAngle * 0.5f;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Color sightsColor = Color.Lerp(new Color(180, 160, 100), new Color(255, 245, 213), charge);
            sightsColor = Color.Lerp(sightsColor, new Color(255, 185, 30), 0.35f + charge * 0.35f);

            Effect spreadEffect = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            spreadEffect.Parameters["centerOpacity"].SetValue(0.7f);
            spreadEffect.Parameters["mainOpacity"].SetValue(charge);
            spreadEffect.Parameters["halfSpreadAngle"].SetValue(halfAngle);
            spreadEffect.Parameters["edgeColor"].SetValue(sightsColor.ToVector3());
            spreadEffect.Parameters["centerColor"].SetValue(sightsColor.ToVector3());
            spreadEffect.Parameters["edgeBlendLength"].SetValue(0.07f);
            spreadEffect.Parameters["edgeBlendStrength"].SetValue(8f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, spreadEffect, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                Projectile.rotation, texture.Size() * 0.5f, sightsSize, SpriteEffects.None, 0f);

            DrawPreviewLaser(charge);

            Effect lineEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
            lineEffect.Parameters["sampleTexture2"].SetValue(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            lineEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
            lineEffect.Parameters["mainOpacity"].SetValue(MathHelper.Lerp(0.45f, 1f, charge));
            lineEffect.Parameters["Resolution"].SetValue(new Vector2(sightsResolution * sightsSize));
            lineEffect.Parameters["laserWidth"].SetValue((0.0035f + MathF.Pow(charge, 5f) * 0.004f) * 2f);
            lineEffect.Parameters["laserLightStrenght"].SetValue(8f);
            lineEffect.Parameters["color"].SetValue(sightsColor.ToVector3());
            lineEffect.Parameters["darkerColor"].SetValue(Color.Black.ToVector3());
            lineEffect.Parameters["bloomSize"].SetValue(0.06f);
            lineEffect.Parameters["bloomMaxOpacity"].SetValue(0.5f);
            lineEffect.Parameters["bloomFadeStrenght"].SetValue(7f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, lineEffect, Main.GameViewMatrix.TransformationMatrix);

            lineEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation + halfAngle);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                0f, texture.Size() * 0.5f, sightsSize, SpriteEffects.None, 0f);

            lineEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation - halfAngle);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                0f, texture.Size() * 0.5f, sightsSize, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        // 仅作为预瞄显示：沿用水晶 C 类连续激光的绘制，不参与任何碰撞或伤害计算。
        private void DrawPreviewLaser(float charge)
        {
            float visibility = Utils.GetLerpValue(PreviewLaserStartCharge, PreviewLaserFullCharge, charge, true);
            if (visibility <= 0f)
                return;

            float[] samples = new float[3];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, 1f, PreviewLaserMaxLength, samples);
            float beamLength = 0f;
            foreach (float sample in samples)
                beamLength += sample;
            beamLength /= samples.Length;

            // 75% 蓄力时达到完整可见度；保留较细的宽度，避免压过原有两条 Scope 辅助线。
            float beamScale = MathHelper.Lerp(0.08f, 0.34f, visibility);
            Color beamColor = Color.Lerp(new Color(255, 125, 50), new Color(255, 232, 130), visibility);
            YC_YharimBeamVisuals.DrawYharimBeam(Projectile, beamLength, beamScale, visibility, beamColor);
        }
    }
}
