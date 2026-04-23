using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.AimScope
{
    public class BFAimScope : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public ref float Charge => ref Projectile.ai[0];
        public ref float MaxChargeOrTargetRotation => ref Projectile.ai[1];
        public const float BaseMaxCharge = 60f;
        public const float MinimumCharge = 18f;
        public float ChargePercent => MathHelper.Clamp(Charge / MaxChargeOrTargetRotation, 0f, 1f);

        public Player Owner => Main.player[Projectile.owner];

        public Vector2 MousePosition => Owner.Calamity().mouseWorld - Owner.MountedCenter;
        public const float WeaponLength = 62f;
        public const float MaxSightAngle = MathHelper.Pi * (2f / 3f);

        public Color ScopeColor => Color.Lerp(scopeMainColor, scopeAccentColor, 0.24f);

        private int holdoutIdentity = -1;
        private Color scopeMainColor = Color.LightBlue;
        private Color scopeAccentColor = Color.White;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Charge = 0f;
            MaxChargeOrTargetRotation = BaseMaxCharge;
            TryFindHoldout();
        }

        public override void AI()
        {
            Owner.Calamity().mouseWorldListener = true;

            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (holdoutIdentity == -1)
                TryFindHoldout();

            Projectile trackedHoldout = FindTrackedHoldout();
            if (trackedHoldout != null)
            {
                if (Projectile.owner != Main.myPlayer)
                    return;

                Charge++;
                Vector2 scopeDirection = MousePosition.SafeNormalize(Vector2.UnitX);
                Vector2 scopeCenter = scopeDirection * WeaponLength + Owner.MountedCenter;
                Vector2 sparkOrigin = scopeCenter;

                if (trackedHoldout.ModProjectile is NewLegendBlossomFluxHoldOut holdout)
                {
                    scopeDirection = holdout.GetAimScopeDirection();
                    scopeCenter = holdout.GetAimScopeCenter(scopeDirection);
                    sparkOrigin = holdout.GetAimScopeSparkOrigin(scopeDirection);
                    scopeMainColor = holdout.GetAimScopeMainColor();
                    scopeAccentColor = holdout.GetAimScopeAccentColor();
                }
                else
                {
                    scopeMainColor = Color.LightBlue;
                    scopeAccentColor = Color.White;
                }

                Projectile.rotation = scopeDirection.ToRotation();
                Projectile.Center = scopeCenter;
                Projectile.timeLeft = 2;

                if (Charge == MaxChargeOrTargetRotation)
                    SoundEngine.PlaySound(SoundID.Item82 with { Volume = SoundID.Item82.Volume * 0.7f }, Owner.MountedCenter);

                if (ChargePercent == 1f && Charge % 2 == 0)
                {
                    Vector2 sparkVelocity = scopeDirection.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)) * 6f;
                    CritSpark spark = new CritSpark(sparkOrigin, sparkVelocity + Owner.velocity, scopeAccentColor, scopeMainColor, 1f, 16);
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.Kill();
            }
        }

        private void TryFindHoldout()
        {
            int holdoutType = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != holdoutType)
                    continue;

                holdoutIdentity = projectile.identity;
                return;
            }
        }

        private Projectile FindTrackedHoldout()
        {
            int holdoutType = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != holdoutType)
                    continue;

                if (projectile.identity == holdoutIdentity)
                    return projectile;
            }

            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Charge == -1)
                return false;

            float sightsSize = 350f;
            float sightsResolution = MathHelper.Lerp(0.04f, 0.2f, Math.Min(ChargePercent * 1.5f, 1));

            float spread = (1f - ChargePercent) * MaxSightAngle;
            float halfAngle = spread / 2f;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;

            Color sightsColor = Color.Lerp(scopeMainColor, scopeAccentColor, 0.2f + 0.45f * ChargePercent);

            Effect spreadEffect = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            spreadEffect.Parameters["centerOpacity"].SetValue(0.9f);
            spreadEffect.Parameters["mainOpacity"].SetValue(ChargePercent);
            spreadEffect.Parameters["halfSpreadAngle"].SetValue(halfAngle);
            spreadEffect.Parameters["edgeColor"].SetValue(sightsColor.ToVector3());
            spreadEffect.Parameters["centerColor"].SetValue(sightsColor.ToVector3());
            spreadEffect.Parameters["edgeBlendLength"].SetValue(0.07f);
            spreadEffect.Parameters["edgeBlendStrength"].SetValue(8f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, spreadEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), sightsSize, 0, 0);

            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);

            laserScopeEffect.Parameters["mainOpacity"].SetValue(ChargePercent);
            laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(sightsResolution * sightsSize));
            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation + halfAngle);
            laserScopeEffect.Parameters["laserWidth"].SetValue((0.0025f + (float)Math.Pow(ChargePercent, 5) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f)) * 2f);
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(7f);

            laserScopeEffect.Parameters["color"].SetValue(sightsColor.ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Black.ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(7f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, 0, new Vector2(texture.Width / 2f, texture.Height / 2f), sightsSize, 0, 0);

            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation - halfAngle);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, 0, new Vector2(texture.Width / 2f, texture.Height / 2f), sightsSize, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
