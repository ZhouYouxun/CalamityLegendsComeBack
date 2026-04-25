using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
        private static readonly int[] AbyssDustTypes = { 191, 29, 104 };

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
                Projectile.velocity.X *= 0.9945f;
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
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloomTex.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(AbyssDeep, AbyssToxic, completion) * MathHelper.Lerp(0.05f, 0.18f, completion);
                float trailScale = MathHelper.Lerp(0.16f, 0.4f, completion);

                Main.EntitySpriteDraw(
                    bloomTex,
                    oldCenter,
                    null,
                    trailColor,
                    0f,
                    bloomOrigin,
                    trailScale,
                    SpriteEffects.None);
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(
                bloomTex,
                drawPos - Projectile.velocity * 0.12f,
                null,
                Color.Lerp(AbyssBlue, AbyssToxic, 0.3f) * 0.18f,
                0f,
                bloomOrigin,
                new Vector2(0.22f, 0.48f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomTex,
                drawPos,
                null,
                AbyssToxic * 0.26f,
                0f,
                bloomOrigin,
                0.34f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomTex,
                drawPos,
                null,
                AbyssFoam * 0.16f,
                0f,
                bloomOrigin,
                0.18f,
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void SpawnLaunchEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.8f, 2.7f) + Main.rand.NextVector2Circular(0.9f, 0.9f);
                CreateAbyssDust(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    Main.rand.NextFloat(1.05f, 1.5f),
                    Main.rand.NextFloat(0.35f, 0.95f),
                    120);
            }

            for (int i = 0; i < 4; i++)
            {
                Dust foam = CreateFoamDust(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    forward * Main.rand.NextFloat(0.25f, 1.1f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    Main.rand.NextFloat(0.8f, 1.1f),
                    Main.rand.NextFloat(0.25f, 0.85f),
                    140);
                foam.velocity *= 0.7f;
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 spawnCenter = Projectile.Center - forward * Main.rand.NextFloat(3f, 8f);

            if (Main.rand.NextBool(2))
            {
                Dust mist = CreateAbyssDust(
                    spawnCenter + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.24f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    Main.rand.NextFloat(1f, 1.35f),
                    Main.rand.NextFloat(0.15f, 0.75f),
                    140);
                mist.velocity *= 0.85f;
            }

            if (Main.rand.NextBool(3))
            {
                Dust foam = CreateFoamDust(
                    spawnCenter + Main.rand.NextVector2Circular(3f, 3f),
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.12f) + Main.rand.NextVector2Circular(0.2f, 0.2f),
                    Main.rand.NextFloat(0.72f, 1f),
                    Main.rand.NextFloat(0.15f, 0.9f),
                    150);
                foam.velocity *= 0.55f;
            }

            if (Main.rand.NextBool(5))
            {
                Dust sparkle = CreateAbyssDust(
                    Projectile.Center + Main.rand.NextVector2Circular(2f, 2f),
                    Main.rand.NextVector2Circular(0.12f, 0.12f),
                    Main.rand.NextFloat(0.85f, 1.1f),
                    Main.rand.NextFloat(0.8f, 1f),
                    160);
                sparkle.velocity *= 0.25f;
            }
        }

        private void SpawnImpactEffects(Vector2 center, Vector2 forward, float intensity)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.95f) * Main.rand.NextFloat(1.1f, 4.4f) * intensity + Main.rand.NextVector2Circular(0.65f, 0.65f);
                CreateAbyssDust(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    Main.rand.NextFloat(1.05f, 1.75f) * intensity,
                    Main.rand.NextFloat(0.25f, 0.95f),
                    120);
            }

            for (int i = 0; i < 7; i++)
            {
                Dust foam = CreateFoamDust(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    forward.RotatedByRandom(1.1f) * Main.rand.NextFloat(0.7f, 2.4f) * intensity + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    Main.rand.NextFloat(0.9f, 1.25f) * intensity,
                    Main.rand.NextFloat(0.2f, 1f),
                    130);
                foam.velocity *= 0.75f;
            }
        }

        private void SpawnDeathEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            SpawnImpactEffects(Projectile.Center, forward, 1f);

            for (int i = 0; i < 6; i++)
            {
                Dust mist = CreateAbyssDust(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(1.1f, 1.1f) - forward * Main.rand.NextFloat(0.1f, 0.5f),
                    Main.rand.NextFloat(1.2f, 1.75f),
                    Main.rand.NextFloat(0.15f, 0.65f),
                    130);
                mist.velocity *= 0.9f;
            }
        }

        private static Dust CreateAbyssDust(Vector2 position, Vector2 velocity, float scale, float colorInterpolant, int alpha)
        {
            Dust dust = Dust.NewDustPerfect(
                position,
                AbyssDustTypes[Main.rand.Next(AbyssDustTypes.Length)],
                velocity,
                alpha,
                Color.Lerp(AbyssDeep, AbyssToxic, colorInterpolant),
                scale);
            dust.noGravity = true;
            dust.fadeIn = scale * 1.05f;
            return dust;
        }

        private static Dust CreateFoamDust(Vector2 position, Vector2 velocity, float scale, float colorInterpolant, int alpha)
        {
            Dust dust = Dust.NewDustPerfect(
                position,
                DustID.Water,
                velocity,
                alpha,
                Color.Lerp(AbyssCyan, AbyssFoam, colorInterpolant),
                scale);
            dust.noGravity = true;
            return dust;
        }
    }
}
