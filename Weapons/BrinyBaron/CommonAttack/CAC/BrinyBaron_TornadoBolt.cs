/*
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_TornadoBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityLegendsComeBack/Texture/KsTexture/light_03";

        private bool spawnedTyphoon;
        private int timer;
        private const float VisualScale = 0.2f;
        private const float HomingRange = 720f;
        private const float HomingTurnRate = 0.15f;
        private const float HomingSpeedLerp = 0.045f;
        private const int HomingDelay = 8;
        private const int WaterExplosionCount = 5;
        private const float WaterExplosionScatterRadius = 54f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            timer++;
            HomeTowardTarget();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.5f, 0.68f));
            SpawnLightOrbFlightEffects();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnTyphoon(target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnDisappearanceEffects();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Texture2D magic3 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D magic4 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D twirl = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/twirl_02").Value;
            Texture2D runeStar = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_04").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = new Color(85, 215, 255, 0);
            Color deepTheme = new Color(25, 95, 205, 0);
            float pulse = 1f + (float)Math.Sin(timer * 0.24f + Projectile.identity) * 0.1f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    texture,
                    trailPosition,
                    null,
                    Color.Lerp(theme, deepTheme, completion) * (0.62f * (1f - completion)),
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    Projectile.scale * VisualScale * MathHelper.Lerp(0.28f, 0.92f, 1f - completion),
                    SpriteEffects.None);

                if (i % 3 == 0)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    Vector2 spiralOffset = (Projectile.rotation + completion * MathHelper.TwoPi * 1.7f).ToRotationVector2().RotatedBy(MathHelper.PiOver2) * side * (4f + 10f * (1f - completion));
                    Main.EntitySpriteDraw(
                        magic4,
                        trailPosition + spiralOffset,
                        null,
                        Color.Cyan with { A = 0 } * (0.14f * (1f - completion)),
                        Projectile.rotation - completion * 2.8f,
                        magic4.Size() * 0.5f,
                        Projectile.scale * VisualScale * MathHelper.Lerp(0.08f, 0.18f, 1f - completion),
                        SpriteEffects.None);
                }
            }

            float spin = timer * 0.048f + Projectile.identity * 0.11f;
            float counterSpin = timer * -0.066f + Projectile.identity * 0.19f;
            Main.EntitySpriteDraw(circle, drawPosition, null, deepTheme * 0.34f, -spin * 0.7f, circle.Size() * 0.5f, Projectile.scale * VisualScale * 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(twirl, drawPosition, null, theme * 0.32f, counterSpin, twirl.Size() * 0.5f, Projectile.scale * VisualScale * 0.36f, SpriteEffects.None);
            Main.EntitySpriteDraw(magic3, drawPosition, null, Color.DeepSkyBlue with { A = 0 } * 0.52f, Projectile.rotation + spin, magic3.Size() * 0.5f, Projectile.scale * VisualScale * 0.46f, SpriteEffects.None);
            Main.EntitySpriteDraw(magic4, drawPosition, null, Color.Cyan with { A = 0 } * 0.42f, Projectile.rotation - spin * 1.35f, magic4.Size() * 0.5f, Projectile.scale * VisualScale * 0.34f, SpriteEffects.None);
            Main.EntitySpriteDraw(runeStar, drawPosition, null, Color.White with { A = 0 } * 0.18f, spin * 2f, runeStar.Size() * 0.5f, Projectile.scale * VisualScale * 0.2f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * 1.05f, 0f, bloom.Size() * 0.5f, Projectile.scale * VisualScale * 1.3f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(new Color(90, 205, 255), Color.White, 0.34f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * VisualScale * 0.92f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White with { A = 0 } * 0.34f, Projectile.rotation, star.Size() * 0.5f, Projectile.scale * VisualScale * 0.3f * pulse, SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void SpawnTyphoon(Vector2 center)
        {
            if (spawnedTyphoon || Main.myPlayer != Projectile.owner)
                return;

            spawnedTyphoon = true;
            Player owner = Main.player[Projectile.owner];
            if (owner.ownedProjectileCounts[ModContent.ProjectileType<BrinySpout>()] == 0)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BrinyTyphoonBubble>(),
                    Math.Max(1, (int)(Projectile.damage * 1.85f)),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            SpawnWaterExplosions(center);
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.62f, Pitch = -0.18f }, center);
        }

        private void SpawnWaterExplosions(Vector2 center)
        {
            int explosionType = ModContent.ProjectileType<BrinyBaron_TornadoWaterExplosion>();
            int explosionDamage = Math.Max(1, (int)(Projectile.damage * 0.46f));
            float explosionKnockback = Projectile.knockBack * 0.55f;

            for (int i = 0; i < WaterExplosionCount; i++)
            {
                Vector2 spawnPosition = center + Main.rand.NextVector2Circular(WaterExplosionScatterRadius, WaterExplosionScatterRadius);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    explosionType,
                    explosionDamage,
                    explosionKnockback,
                    Projectile.owner,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }
        }

        private void HomeTowardTarget()
        {
            if (timer < HomingDelay || Projectile.velocity.LengthSquared() <= 0.01f)
                return;

            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
                return;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float loosen = Utils.GetLerpValue(HomingDelay, HomingDelay + 34f, timer, true);
            float closeBoost = Utils.GetLerpValue(260f, 72f, Projectile.Distance(target.Center), true);
            float turnRate = HomingTurnRate * MathHelper.Lerp(0.5f, 2.2f, MathHelper.Max(loosen, closeBoost));
            float targetSpeed = MathHelper.Lerp(Projectile.velocity.Length(), 17.5f, HomingSpeedLerp + loosen * 0.035f);

            float steeredRotation = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), turnRate);
            Projectile.velocity = steeredRotation.ToRotationVector2() * targetSpeed;
        }

        private void SpawnLightOrbFlightEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color theme = Main.rand.NextBool() ? Color.Cyan : new Color(120, 220, 255);

            if (timer % 2 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float phase = timer * 0.34f + Projectile.identity * 0.2f + i * MathHelper.Pi;
                    Vector2 spiralOffset = right * side * (4f + (float)Math.Sin(phase) * 5f) - forward * Main.rand.NextFloat(10f, 24f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + spiralOffset,
                        -forward * Main.rand.NextFloat(0.35f, 1.2f) + right * side * Main.rand.NextFloat(0.08f, 0.34f),
                        false,
                        Main.rand.Next(9, 15),
                        Main.rand.NextFloat(0.12f, 0.24f),
                        theme,
                        true,
                        false,
                        true));
                }
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 22f) + right * Main.rand.NextFloat(-8f, 8f),
                    -forward.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.6f, 3.8f) + right * Main.rand.NextFloatDirection() * 0.5f,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.24f, 0.46f),
                    theme));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(8f, 22f) + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    -forward * Main.rand.NextFloat(0.8f, 2.1f) + right * Main.rand.NextFloatDirection() * 0.55f,
                    0,
                    theme,
                    Main.rand.NextFloat(0.72f, 1.08f));
                dust.noGravity = true;
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

        private void SpawnDisappearanceEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f),
                    0,
                    new Color(100, 220, 255),
                    Main.rand.NextFloat(0.7f, 1f));
                dust.noGravity = true;
            }
        }
    }
}
*/
