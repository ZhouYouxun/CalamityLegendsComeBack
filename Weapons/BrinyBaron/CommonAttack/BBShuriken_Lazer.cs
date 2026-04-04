using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBShuriken_Lazer : ModProjectile
    {
        private const int FlightLifetime = 28;
        private const int LaserExtraUpdates = 12;

        private int timer;
        private float spiralSeed;

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = FlightLifetime;
            Projectile.extraUpdates = LaserExtraUpdates;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            timer = 0;
            spiralSeed = Main.rand.NextFloat(MathHelper.TwoPi);

            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = Vector2.UnitY;

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float speedFactor = MathHelper.Clamp(Projectile.velocity.Length() / 18f, 0.65f, 1.2f);

            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.28f, 0.42f) * speedFactor);

            if (Main.rand.NextBool(2))
            {
                float helixTime = timer * 0.65f + spiralSeed;
                float helixRadius = 8f + 4f * speedFactor;

                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float wave = (float)Math.Sin(helixTime + MathHelper.PiOver2 * i);
                    Vector2 spawnPos = Projectile.Center - forward * 10f + right * wave * helixRadius * side;
                    Vector2 driftVelocity = -forward * Main.rand.NextFloat(0.55f, 1.4f) + right * side * Main.rand.NextFloat(0.03f, 0.18f);

                    GlowOrbParticle orb = new GlowOrbParticle(
                        spawnPos,
                        driftVelocity,
                        false,
                        Main.rand.Next(8, 12),
                        Main.rand.NextFloat(0.42f, 0.62f) * speedFactor,
                        Color.Lerp(new Color(70, 188, 255), Color.White, Main.rand.NextFloat(0.25f, 0.6f)),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }

            if (timer % 2 == 0)
            {
                Vector2 sparkVelocity = (-forward * Main.rand.NextFloat(2.2f, 4.8f)).RotatedByRandom(0.2f);
                Particle line = new CustomSpark(
                    Projectile.Center + right * Main.rand.NextFloat(-4f, 4f),
                    sparkVelocity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.32f, 0.46f) * speedFactor,
                    Color.Lerp(new Color(80, 215, 255), Color.White, Main.rand.NextFloat(0.18f, 0.45f)) * 0.9f,
                    new Vector2(Main.rand.NextFloat(0.45f, 0.7f), Main.rand.NextFloat(1.7f, 2.5f)),
                    true,
                    true,
                    shrinkSpeed: Main.rand.NextFloat(0.45f, 0.68f));
                GeneralParticleHandler.SpawnParticle(line);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    -forward * Main.rand.NextFloat(0.7f, 2.1f),
                    100,
                    Main.rand.NextBool() ? new Color(95, 205, 255) : new Color(215, 248, 255),
                    Main.rand.NextFloat(0.85f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 burstVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.75f) * Main.rand.NextFloat(2.5f, 8f);

                GlowSparkParticle spark = new GlowSparkParticle(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    burstVelocity,
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.18f, 0.3f),
                    Main.rand.NextBool() ? Color.Cyan : Color.LightSkyBlue,
                    new Vector2(1.7f, 0.45f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6f),
                    100,
                    Main.rand.NextBool() ? new Color(90, 200, 255) : new Color(220, 248, 255),
                    Main.rand.NextFloat(1f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
