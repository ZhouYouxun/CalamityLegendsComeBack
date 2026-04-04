using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBShuriken_Light : ModProjectile
    {
        public override string Texture => "Terraria/Images/Extra_89";

        private const float HomingRange = 920f;
        private const float BaseSpeed = 12.5f;
        private const float HomingLerp = 0.085f;
        private const float EffectIntensity = 0.4f;

        private float orbitSeed;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 0;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void OnSpawn(IEntitySource source)
        {
            orbitSeed = Main.rand.NextFloat(MathHelper.TwoPi);

            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = Vector2.UnitY * -BaseSpeed;
        }

        public override void AI()
        {
            NPC target = FindNearestTarget(HomingRange);
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Math.Max(BaseSpeed, Projectile.velocity.Length());
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingLerp);
            }
            else
            {
                Projectile.velocity *= 0.9925f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, 0.08f * EffectIntensity, 0.28f * EffectIntensity, 0.42f * EffectIntensity);

            SpawnFlightEffects();
        }

        private void SpawnFlightEffects()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float wave = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + orbitSeed);
            Vector2 wakeCenter = Projectile.Center - forward * 12f;

            if (Main.rand.NextBool(5))
            {
                Vector2 wakePos = wakeCenter + right * wave * (6f * EffectIntensity) + Main.rand.NextVector2Circular(3f * EffectIntensity, 3f * EffectIntensity);
                Vector2 wakeVelocity = -forward * Main.rand.NextFloat(1.2f, 2.8f) * EffectIntensity + right * wave * 0.35f * EffectIntensity;

                Dust water = Dust.NewDustPerfect(
                    wakePos,
                    DustID.Water,
                    wakeVelocity,
                    100,
                    new Color(80, 190, 255),
                    Main.rand.NextFloat(0.95f, 1.3f) * EffectIntensity);
                water.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    Dust frost = Dust.NewDustPerfect(
                        wakePos,
                        DustID.Frost,
                        wakeVelocity * 0.72f,
                        100,
                        new Color(215, 248, 255),
                        Main.rand.NextFloat(0.85f, 1.15f) * EffectIntensity);
                    frost.noGravity = true;
                }
            }

            if (Main.rand.NextBool(8))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    wakeCenter + right * wave * (8f * EffectIntensity),
                    -forward * 0.18f * EffectIntensity,
                    false,
                    8,
                    0.48f * EffectIntensity,
                    Color.Lerp(new Color(70, 180, 255), Color.White, 0.32f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (Main.rand.NextBool(10))
            {
                GlowSparkParticle spark = new GlowSparkParticle(
                    Projectile.Center + right * Main.rand.NextFloat(-5f, 5f) * EffectIntensity,
                    forward.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f)) * Main.rand.NextFloat(1.8f, 4.2f) * EffectIntensity,
                    false,
                    Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.18f, 0.26f) * EffectIntensity,
                    Main.rand.NextBool() ? Color.Cyan : Color.LightSkyBlue,
                    new Vector2(1.6f, 0.42f) * EffectIntensity,
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6.5f) * EffectIntensity;

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVelocity, 100, new Color(80, 190, 255), Main.rand.NextFloat(1f, 1.45f) * EffectIntensity);
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVelocity * 0.72f, 100, new Color(215, 248, 255), Main.rand.NextFloat(0.95f, 1.2f) * EffectIntensity);
                frost.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                GlowSparkParticle spark = new GlowSparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 5.2f) * EffectIntensity,
                    false,
                    10,
                    0.24f * EffectIntensity,
                    Main.rand.NextBool() ? Color.Cyan : Color.LightSkyBlue,
                    new Vector2(1.7f, 0.45f) * EffectIntensity,
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ringTex = TextureAssets.Extra[89].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
            Vector2 origin = ringTex.Size() * 0.5f;
            float pulse = 1f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPos = Projectile.oldPos[i];
                if (oldPos == Vector2.Zero)
                    continue;

                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(45, 150, 255, 0), new Color(220, 250, 255, 0), factor) * factor * 0.45f * EffectIntensity;
                Main.EntitySpriteDraw(
                    ringTex,
                    oldPos + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    trailColor,
                    Projectile.rotation,
                    origin,
                    (0.34f + factor * 0.22f) * MathHelper.Lerp(0.75f, 1f, EffectIntensity),
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4f + Main.GlobalTimeWrappedHourly * (0.75f + i * 0.08f);
                Color ringColor = Color.Lerp(new Color(70, 180, 255, 0), Color.White, 0.25f + i * 0.12f) * (0.45f + i * 0.08f) * EffectIntensity;
                float scale = (0.26f + 0.04f * i) * pulse * MathHelper.Lerp(0.75f, 1f, EffectIntensity);

                Main.EntitySpriteDraw(
                    ringTex,
                    drawPosition,
                    null,
                    ringColor,
                    angle,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            return false;
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
    }
}
