using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal sealed class DepthCells_Drop : ModProjectile
    {
        internal static readonly Color AbyssDeep = new(6, 16, 30);
        internal static readonly Color AbyssBlue = new(18, 74, 96);
        internal static readonly Color AbyssCyan = new(72, 208, 255);
        internal static readonly Color AbyssToxic = new(108, 255, 176);
        internal static readonly Color AbyssFoam = new(210, 255, 236);

        private const float GravityDelay = 6f;
        private const float GravityStrength = 0.165f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SpawnLaunchEffects();
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.localAI[0] > GravityDelay)
            {
                float sway = (float)System.Math.Sin((Projectile.identity * 0.6f) + Projectile.localAI[0] * 0.17f) * 0.012f;
                Projectile.velocity = Projectile.velocity.RotatedBy(sway);
                Projectile.velocity.Y += GravityStrength;
                Projectile.velocity.X *= 0.9845f;
            }

            Lighting.AddLight(Projectile.Center, Color.Lerp(AbyssToxic, AbyssCyan, 0.35f).ToVector3() * 0.55f);
            SpawnFlightEffects();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.82f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 240);
            SpawnImpactEffects(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 0.9f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpactEffects(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY), 1.05f);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnDeathEffects();
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.26f, Pitch = 0.32f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sparkTex = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smearTex = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Vector2 origin = sparkTex.Size() * 0.5f;
            float stretch = Utils.GetLerpValue(0f, 18f, Projectile.velocity.Length(), true);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(AbyssDeep, AbyssToxic, completion) * (0.08f + completion * 0.12f);
                float trailScale = MathHelper.Lerp(0.22f, 0.48f, completion);

                Main.EntitySpriteDraw(
                    bloomTex,
                    oldCenter,
                    null,
                    trailColor,
                    -Projectile.rotation * 0.2f,
                    bloomTex.Size() * 0.5f,
                    trailScale,
                    SpriteEffects.None);
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(
                smearTex,
                drawPos - Projectile.velocity * 0.18f,
                null,
                AbyssBlue * 0.35f,
                Projectile.rotation,
                smearTex.Size() * 0.5f,
                new Vector2(0.62f, 1.38f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                sparkTex,
                drawPos,
                null,
                AbyssToxic * 0.88f,
                Projectile.rotation,
                origin,
                new Vector2(0.2f, 0.72f + stretch * 0.28f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomTex,
                drawPos,
                null,
                AbyssFoam * 0.24f,
                0f,
                bloomTex.Size() * 0.5f,
                0.4f,
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void SpawnLaunchEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            BloomRing bloom = new(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(AbyssToxic, AbyssCyan, 0.3f) * 0.32f,
                0.46f,
                20);
            GeneralParticleHandler.SpawnParticle(bloom);

            for (int i = 0; i < 4; i++)
            {
                WaterGlobParticle glob = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    forward * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(1f, 1f),
                    Main.rand.NextFloat(0.72f, 0.96f));
                glob.Color = Color.Lerp(AbyssBlue, AbyssToxic, Main.rand.NextFloat(0.2f, 0.8f)) * 0.44f;
                GeneralParticleHandler.SpawnParticle(glob);
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 spawnCenter = Projectile.Center - forward * Main.rand.NextFloat(2f, 9f);

            if (Main.rand.NextBool(2))
            {
                HeavySmokeParticle smoke = new(
                    spawnCenter + Main.rand.NextVector2Circular(6f, 6f),
                    back * Main.rand.NextFloat(0.8f, 2f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    Color.Lerp(AbyssDeep, AbyssBlue, Main.rand.NextFloat(0.3f, 0.85f)),
                    Main.rand.Next(24, 36),
                    Main.rand.NextFloat(0.55f, 0.9f),
                    0.7f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(2))
            {
                WaterGlobParticle glob = new(
                    spawnCenter + Main.rand.NextVector2Circular(5f, 5f),
                    back * Main.rand.NextFloat(0.25f, 1.1f) + Main.rand.NextVector2Circular(0.7f, 0.7f),
                    Main.rand.NextFloat(0.75f, 1.08f));
                glob.Color = Color.Lerp(AbyssToxic, AbyssFoam, Main.rand.NextFloat(0.15f, 0.85f)) * 0.5f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            if (Main.rand.NextBool(3))
            {
                WaterFlavoredParticle shard = new(
                    spawnCenter + Main.rand.NextVector2Circular(6f, 6f),
                    back * Main.rand.NextFloat(0.3f, 1.45f) + Main.rand.NextVector2Circular(1.8f, 1.8f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.85f, 1.22f),
                    Color.Lerp(AbyssCyan, AbyssFoam, Main.rand.NextFloat(0.25f, 0.85f)));
                GeneralParticleHandler.SpawnParticle(shard);
            }

            if (Main.rand.NextBool(4))
            {
                AltSparkParticle spark = new(
                    Projectile.Center - forward * Main.rand.NextFloat(3f, 8f),
                    back * Main.rand.NextFloat(0.15f, 0.6f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    false,
                    12,
                    Main.rand.NextFloat(0.95f, 1.25f),
                    Color.Lerp(AbyssToxic, AbyssFoam, Main.rand.NextFloat(0.3f, 0.8f)) * 0.3f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                GenericSparkle sparkle = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Vector2.Zero,
                    AbyssFoam,
                    AbyssCyan,
                    Main.rand.NextFloat(1.6f, 2.2f),
                    12,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    1.6f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    spawnCenter + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(4) ? 191 : (Main.rand.NextBool() ? 104 : 29),
                    back * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
                    120,
                    Color.Lerp(AbyssDeep, AbyssToxic, Main.rand.NextFloat(0.4f, 0.95f)),
                    Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
        }

        private void SpawnImpactEffects(Vector2 center, Vector2 forward, float intensity)
        {
            DirectionalPulseRing ring = new(
                center,
                Vector2.Zero,
                AbyssToxic * (0.28f * intensity),
                Vector2.One,
                forward.ToRotation(),
                0.018f,
                0.12f,
                18);
            GeneralParticleHandler.SpawnParticle(ring);

            StrongBloom bloom = new(
                center,
                Vector2.Zero,
                Color.Lerp(AbyssToxic, AbyssCyan, 0.35f) * (0.32f * intensity),
                0.45f * intensity,
                18);
            GeneralParticleHandler.SpawnParticle(bloom);

            for (int i = 0; i < 7; i++)
            {
                WaterGlobParticle glob = new(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(0.5f, 3.2f) + Main.rand.NextVector2Circular(1.6f, 1.6f),
                    Main.rand.NextFloat(0.78f, 1.14f) * intensity);
                glob.Color = Color.Lerp(AbyssBlue, AbyssToxic, Main.rand.NextFloat()) * 0.52f;
                GeneralParticleHandler.SpawnParticle(glob);
            }

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? 191 : (Main.rand.NextBool() ? 29 : 104),
                    forward.RotatedByRandom(0.95f) * Main.rand.NextFloat(1.2f, 4.6f),
                    120,
                    Color.Lerp(AbyssDeep, AbyssFoam, Main.rand.NextFloat(0.3f, 0.9f)),
                    Main.rand.NextFloat(1.1f, 1.8f) * intensity);
                dust.noGravity = true;
            }
        }

        private void SpawnDeathEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            SpawnImpactEffects(Projectile.Center, forward, 1f);

            for (int i = 0; i < 5; i++)
            {
                HeavySmokeParticle smoke = new(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    Color.Lerp(AbyssDeep, AbyssBlue, Main.rand.NextFloat(0.3f, 0.8f)),
                    Main.rand.Next(26, 40),
                    Main.rand.NextFloat(0.65f, 1f),
                    0.72f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }
    }
}
