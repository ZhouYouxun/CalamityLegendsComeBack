using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_SeafoamBlade : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Texture/Calamity/RangePROJ/FlurrystormIceChunk";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 45; // Short distance
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // Sword beams usually pass through tiles
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BB_Balance.GetLeftProjectileHitCooldown(BBLeftProjectile.SeafoamBlade);
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            bool spawnedByShuriken = Projectile.ai[0] == 1f;
            bool enhanced = Projectile.ai[1] == 1f;

            if (spawnedByShuriken || enhanced)
            {
                // Homing behavior: starts immediately for enhanced sea foam, after 15 frames for normal
                if (enhanced || Projectile.timeLeft < 30)
                {
                    NPC target = FindNearestTarget(800f);
                    if (target != null)
                    {
                        float speedFactor = enhanced ? 24f : 20f;
                        float lerpFactor = enhanced ? 0.22f : 0.15f;
                        Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speedFactor;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, lerpFactor);
                    }
                }
            }
            else
            {
                // decelerates slower for Stage 2/3 to allow longer reach
                if (Projectile.ai[0] == 2f || Projectile.ai[0] == 3f)
                {
                    Projectile.velocity *= 0.995f;
                }
                else
                {
                    Projectile.velocity *= 0.99f;
                }
            }

            // Seashine style pulsation & light
            Lighting.AddLight(Projectile.Center, 0f, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.5f / 255f);
            
            if (Projectile.localAI[1] < 7f)
            {
                Projectile.localAI[1] += 1f;
            }
            else
            {
                float dustScale = 1.8f * Projectile.scale;
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y), 8, 8, DustID.Flare_Blue, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, Projectile.alpha, default, dustScale);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= -0.25f;

                dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X + 2f, Projectile.position.Y + 2f - Projectile.velocity.Y), 8, 8, DustID.Flare_Blue, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, Projectile.alpha, default, dustScale);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= -0.25f;
                Main.dust[dust].position -= Projectile.velocity * 0.5f;

                // Pulsate scale & alpha
                if (Projectile.localAI[0] == 0f)
                {
                    Projectile.scale -= 0.02f;
                    Projectile.alpha += 10;
                    if (Projectile.alpha >= 250)
                    {
                        Projectile.alpha = 255;
                        Projectile.localAI[0] = 1f;
                    }
                }
                else if (Projectile.localAI[0] == 1f)
                {
                    Projectile.scale += 0.02f;
                    Projectile.alpha -= 10;
                    if (Projectile.alpha <= 0)
                    {
                        Projectile.alpha = 0;
                        Projectile.localAI[0] = 0f;
                    }
                }
            }

            // Stage 2/3 extra visual trailing dust
            bool isStage2Or3 = Projectile.ai[0] == 2f || Projectile.ai[0] == 3f;
            if (Main.rand.NextBool(enhanced || isStage2Or3 ? 1 : 2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + (enhanced || isStage2Or3 ? Main.rand.NextVector2Circular(6f, 6f) : Vector2.Zero),
                    enhanced || isStage2Or3 ? DustID.Frost : DustID.Water,
                    Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(1f, 1f),
                    100,
                    enhanced || isStage2Or3 ? Color.Cyan : Color.DeepSkyBlue,
                    Main.rand.NextFloat(0.6f, 1.2f)
                );
                dust.noGravity = true;

                if ((enhanced || isStage2Or3) && Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center,
                        Main.rand.NextVector2Circular(2f, 2f),
                        false,
                        8,
                        Main.rand.NextFloat(0.15f, 0.35f),
                        Color.Cyan,
                        true,
                        false,
                        true));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnSplashEffects();
        }

        public override void OnKill(int timeLeft)
        {
            SpawnSplashEffects();
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BrinyBaron_SeafoamExplosion>(),
                    (int)(Projectile.damage * 0.8f),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner);
            }
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        private void SpawnSplashEffects()
        {
            bool enhanced = Projectile.ai[1] == 1f;
            int count = enhanced ? 18 : 10;
            Color particleColor = enhanced ? Color.Cyan : Color.DeepSkyBlue;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    enhanced ? DustID.Frost : DustID.Water,
                    velocity,
                    100,
                    particleColor,
                    Main.rand.NextFloat(0.7f, 1.4f)
                );
                dust.noGravity = Main.rand.NextBool();

                if (enhanced && Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center,
                        velocity * 0.5f,
                        false,
                        10,
                        Main.rand.NextFloat(0.2f, 0.45f),
                        Color.Cyan,
                        true,
                        false,
                        true));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            bool enhanced = Projectile.ai[1] == 1f;

            // 1. Afterimage Trail
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Color trailColor = enhanced ? Color.Cyan : Color.DeepSkyBlue;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float progress = i / (float)Projectile.oldPos.Length;
                Color drawColor = trailColor * (0.35f * (1f - progress)) * Projectile.Opacity;
                float scale = Projectile.scale * MathHelper.Lerp(1.1f, 0.4f, progress);

                Main.EntitySpriteDraw(texture, trailPos, null, drawColor, Projectile.oldRot[i] + MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0);
            }

            // 2. Outline / Glow (包边)
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color outlineColor = (enhanced ? Color.Cyan : Color.DeepSkyBlue) * 0.4f * Projectile.Opacity;
            float borderScale = Projectile.scale * 1.15f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(texture, drawPos + offset, null, outlineColor, Projectile.rotation, origin, borderScale, SpriteEffects.None, 0);
            }

            // 3. Main Body
            Color mainColor = Color.White * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, drawPos, null, mainColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
