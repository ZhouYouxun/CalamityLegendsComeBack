using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSunlight_Lighting : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 25; // Reduced by 30% (originally 36)
            Projectile.height = 25; // Reduced by 30% (originally 36)
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 7;
            Projectile.timeLeft = 30;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            timer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 40f, 0.055f);

            Projectile.velocity = forward * speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(255, 220, 100).ToVector3() * 0.45f);

            Vector2 futurePos = Projectile.Center + Projectile.velocity * 0.5f;
            int sparkCount = timer < 12 ? 1 : 2;
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 vel = forward.RotatedByRandom(MathHelper.ToRadians(timer < 12 ? 1.5f : 3.5f)) * Main.rand.NextFloat(1.5f, 4.0f);
                Particle spark = new GlowSparkParticle(
                    futurePos,
                    vel,
                    false,
                    6,
                    0.09f,
                    new Color(255, 230, 120),
                    new Vector2(0.9f, 0.2f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                Particle trail = new GlowSparkParticle(
                    Projectile.Center - forward * 6f,
                    -forward * Main.rand.NextFloat(0.8f, 2.0f),
                    false,
                    8,
                    0.08f,
                    new Color(255, 200, 80),
                    new Vector2(0.75f, 0.2f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(trail);
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 sparkVel = -forward * Main.rand.NextFloat(1f, 3f) + Main.rand.NextVector2Circular(0.4f, 0.4f);
                CritSpark spark = new CritSpark(
                    Projectile.Center - forward * 4f,
                    sparkVel,
                    Color.White,
                    new Color(255, 220, 90),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    10
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(4))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    Projectile.Center,
                    -forward * 0.5f,
                    false,
                    8,
                    0.18f,
                    new Color(255, 240, 150),
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (timer % 12 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    Projectile.Center,
                    Projectile.velocity * 0.2f,
                    new Color(255, 210, 80) * 0.5f,
                    new Vector2(1f, 2f),
                    Projectile.rotation - MathHelper.PiOver2,
                    0.05f,
                    0.005f,
                    12
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        //public override bool? CanDamage() => timer > 1;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = target.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            Particle hitRing = new DirectionalPulseRing(
                pos,
                Vector2.Zero,
                new Color(255, 230, 100),
                new Vector2(1f, 1f),
                0f,
                0.1f,
                3.0f,
                16
            );
            GeneralParticleHandler.SpawnParticle(hitRing);

            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(3f, 3f) + forward * Main.rand.NextFloat(2f, 5f);
                CritSpark hitSpark = new CritSpark(
                    pos,
                    sparkVel,
                    Color.White,
                    new Color(255, 200, 80),
                    Main.rand.NextFloat(0.8f, 1.2f),
                    18
                );
                GeneralParticleHandler.SpawnParticle(hitSpark);
            }

            for (int i = 0; i < 12; i++)
            {
                Particle core = new GlowSparkParticle(
                    pos,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    false,
                    6,
                    0.2f,
                    new Color(255, 240, 150),
                    new Vector2(0.95f, 0.32f),
                    true,
                    false,
                    1);
                GeneralParticleHandler.SpawnParticle(core);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = forward.RotatedByRandom(MathHelper.ToRadians(4.8f)) * Main.rand.NextFloat(4.8f, 9.6f);
                Particle jet = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    new Color(255, 220, 100),
                    new Vector2(1.55f, 0.26f),
                    true,
                    false,
                    1);

                GeneralParticleHandler.SpawnParticle(jet);
            }

            SoundEngine.PlaySound(SoundID.Item94, pos);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
