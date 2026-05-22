using System;
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
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * 17f;
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
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);

            for (int i = 0; i < 3; i++)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.18f);
                Vector2 strikePoint = target.Center + Main.rand.NextVector2Circular(18f, 18f);
                Vector2 spawnPosition = strikePoint + forward * 10f * 16f;

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

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            float WidthFunction(float completion, Vector2 _) =>
                Projectile.width * 0.62f * (float)Math.Sin(completion * MathHelper.Pi) * Projectile.Opacity;

            Color ColorFunction(float completion, Vector2 _)
            {
                Color color = Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.PaleYellow, completion * 0.75f);
                color.A = 0;
                return color * (1f - completion) * 0.9f;
            }

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]),
                38);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(texture, drawPosition, null, AzureThunderColors.PaleYellow with { A = 0 } * 0.75f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.55f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 0 } * 0.45f, Projectile.rotation + MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale * 0.3f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
