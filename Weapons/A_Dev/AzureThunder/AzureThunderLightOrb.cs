using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityLegendsComeBack/Texture/KsTexture/light_03";

        private int timer;
        private const float ShaderTrailLength = 34f;
        private const float MagicDrawScale = 0.4f;
        private const float MaxHomingSpeed = 25.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AzureThunderColors.PaleYellow.ToVector3() * 0.5f);

            NPC target = AzureThunderPlayer.FindNearestTarget(Projectile.Center, 850f);
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * MaxHomingSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.055f);
            }
            else
            {
                Projectile.velocity *= 1.012f;
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f),
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.22f, 0.38f),
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    true,
                    false,
                    true));
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 sparkVelocity = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.1f, 3.6f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.12f, 0.48f) + Main.rand.NextVector2Circular(5f, 5f),
                    sparkVelocity,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.45f, 0.75f),
                    Main.rand.NextBool(3) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure));
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.2f, 0.7f) + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.FireworksRGB,
                    -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.055f),
                    0,
                    Main.rand.NextBool() ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            AzureThunderSounds.PlayOrbImpact(target.Center);

            for (int i = 0; i < 3; i++)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.18f);
                Vector2 strikePoint = target.Center + Main.rand.NextVector2Circular(18f, 18f);
                Vector2 spawnPosition = strikePoint + forward * 25f * 16f;

                AzureThunderPlayer.SpawnFlatLightning(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    strikePoint - spawnPosition,
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    0.55f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 trailDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2[] stableTrailPositions = BuildStableTrailPositions(trailDirection);

            if (GameShaders.Misc.TryGetValue("CalamityMod:SideStreakTrail", out MiscShaderData shader))
            {
                shader.UseImage1("Images/Misc/Perlin");

                float WidthFunction(float completion, Vector2 _) =>
                    Projectile.width * 0.82f * (float)Math.Sin(completion * MathHelper.Pi) * Projectile.Opacity;

                Color ColorFunction(float completion, Vector2 _)
                {
                    Color color = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.PaleYellow, completion * 0.75f);
                    color.A = 0;
                    return color * (1f - completion) * 1.15f;
                }

                PrimitiveRenderer.RenderTrail(
                    stableTrailPositions,
                    new PrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Projectile.Size * 0.5f, shader: shader),
                    38);
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            DrawGuaranteedShaderCore(texture, drawPosition, trailDirection);
            Main.EntitySpriteDraw(texture, drawPosition, null, AzureThunderColors.PaleYellow with { A = 0 } * 0.75f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.55f * MagicDrawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 0 } * 0.45f, Projectile.rotation + MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale * 0.3f * MagicDrawScale, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            DrawAlphaBlendFallback(texture, drawPosition, trailDirection);
            return false;
        }

        private Vector2[] BuildStableTrailPositions(Vector2 trailDirection)
        {
            Vector2[] stablePositions = new Vector2[Projectile.oldPos.Length];
            Vector2 fallbackTopLeft = Projectile.Center - Projectile.Size * 0.5f;

            for (int i = 0; i < stablePositions.Length; i++)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                bool invalidOldPosition = oldPosition == Vector2.Zero || Vector2.DistanceSquared(oldPosition, fallbackTopLeft) > 2200f * 2200f;
                stablePositions[i] = invalidOldPosition
                    ? fallbackTopLeft - trailDirection * ShaderTrailLength * i / Math.Max(1, stablePositions.Length - 1)
                    : oldPosition;
            }

            return stablePositions;
        }

        private void DrawGuaranteedShaderCore(Texture2D texture, Vector2 drawPosition, Vector2 trailDirection)
        {
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.86f + (float)Math.Sin((Main.GlobalTimeWrappedHourly * 12f) + Projectile.identity) * 0.08f;

            for (int i = 0; i < 5; i++)
            {
                float completion = i / 4f;
                Vector2 trailPosition = drawPosition - trailDirection * completion * ShaderTrailLength;
                float opacity = (1f - completion) * Projectile.Opacity;
                Color color = Color.Lerp(AzureThunderColors.PaleYellow, AzureThunderColors.Azure, completion * 0.7f) with { A = 120 };
                Main.EntitySpriteDraw(texture, trailPosition, null, color * opacity * 0.42f, Projectile.rotation, origin, Projectile.scale * (0.52f - completion * 0.18f) * MagicDrawScale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, AzureThunderColors.Azure with { A = 150 } * Projectile.Opacity * 0.55f, Projectile.rotation, origin, Projectile.scale * 0.86f * pulse * MagicDrawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, AzureThunderColors.PaleYellow with { A = 180 } * Projectile.Opacity * 0.72f, -Projectile.rotation, origin, Projectile.scale * 0.62f * MagicDrawScale, SpriteEffects.None);
        }

        private void DrawAlphaBlendFallback(Texture2D texture, Vector2 drawPosition, Vector2 trailDirection)
        {
            Vector2 origin = texture.Size() * 0.5f;
            for (int i = 5; i >= 0; i--)
            {
                float completion = i / 5f;
                Vector2 trailPosition = drawPosition - trailDirection * completion * ShaderTrailLength * 0.82f;
                Color color = Color.Lerp(AzureThunderColors.PaleYellow, AzureThunderColors.Azure, completion) with { A = (byte)(150 * (1f - completion)) };
                Main.EntitySpriteDraw(texture, trailPosition, null, color * Projectile.Opacity * 0.5f, Projectile.rotation, origin, Projectile.scale * (0.38f - completion * 0.11f) * MagicDrawScale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 210 } * Projectile.Opacity * 0.65f, Projectile.rotation, origin, Projectile.scale * 0.28f * MagicDrawScale, SpriteEffects.None);
        }
    }
}
