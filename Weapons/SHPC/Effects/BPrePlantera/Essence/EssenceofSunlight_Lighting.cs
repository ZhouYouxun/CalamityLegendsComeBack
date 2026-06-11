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
            Projectile.width = 25;
            Projectile.height = 25;
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

            SpawnSoftFlightParticles(forward);
            SpawnExtraAccentParticles(forward);
        }

        private void SpawnSoftFlightParticles(Vector2 forward)
        {
            // extraUpdates 较高，粒子会显得落后，所以这里稍微向前预判一点
            Vector2 futurePos = Projectile.Center + Projectile.velocity * 0.5f;

            int frontParticleCount = timer < 12 ? 1 : 2;
            float frontSpread = MathHelper.ToRadians(timer < 12 ? 3f : 7f);

            // 原本第一组 GlowSparkParticle：尖锐前端火花
            // 现在改成柔和的 SquishyLightParticle，让弹幕前端像一团太阳光斑
            for (int i = 0; i < frontParticleCount; i++)
            {
                Vector2 velocity =
                    forward.RotatedByRandom(frontSpread) * Main.rand.NextFloat(0.7f, 2.2f) +
                    Main.rand.NextVector2Circular(0.15f, 0.15f);

                Vector2 spawnPosition =
                    futurePos +
                    Main.rand.NextVector2Circular(1.2f, 1.2f);

                float scale = Main.rand.NextFloat(0.10f, 0.17f);

                Color particleColor = Color.Lerp(
                    new Color(255, 245, 165),
                    new Color(255, 160, 70),
                    Main.rand.NextFloat(0.2f, 0.55f)
                ) * 0.72f;

                int lifetime = Main.rand.Next(6, 10);

                SquishyLightParticle particle = new(
                    spawnPosition,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 原本第二组 GlowSparkParticle：尾部尖锐拖光
            // 现在改成较小的柔光粒子，形成柔和拖尾，而不是一条尖刺
            if (Main.rand.NextBool(2))
            {
                Vector2 trailPosition =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(4f, 8f) +
                    Main.rand.NextVector2Circular(1.4f, 1.4f);

                Vector2 velocity =
                    -forward * Main.rand.NextFloat(0.25f, 1.15f) +
                    Main.rand.NextVector2Circular(0.3f, 0.3f);

                float scale = Main.rand.NextFloat(0.08f, 0.14f);

                Color particleColor = Color.Lerp(
                    new Color(255, 220, 105),
                    new Color(255, 150, 55),
                    Main.rand.NextFloat(0.25f, 0.6f)
                ) * 0.58f;

                int lifetime = Main.rand.Next(8, 13);

                SquishyLightParticle particle = new(
                    trailPosition,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnExtraAccentParticles(Vector2 forward)
        {
            // 这个 CritSpark 不是 GlowSparkParticle，保留它作为少量命中感/电火花感点缀
            if (Main.rand.NextBool(3))
            {
                Vector2 sparkVel =
                    -forward * Main.rand.NextFloat(1f, 3f) +
                    Main.rand.NextVector2Circular(0.4f, 0.4f);

                CritSpark spark = new(
                    Projectile.Center - forward * 4f,
                    sparkVel,
                    Color.White,
                    new Color(255, 220, 90),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    10
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 保留 GlowOrbParticle，它本身是柔和光球，不是尖锐火花
            if (Main.rand.NextBool(4))
            {
                GlowOrbParticle orb = new(
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

            // 保留脉冲环，用来提供弹幕飞行时的节奏感
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = target.Center;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            SpawnHitCritSparks(pos, forward);
            SpawnHitSoftCore(pos);
            SpawnHitSoftJet(pos, forward);

            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.7f }, pos);
        }

        private void SpawnHitCritSparks(Vector2 pos, Vector2 forward)
        {
            // 保留少量 CritSpark，让命中瞬间仍然有清脆反馈
            for (int i = 0; i < 6; i++)
            {
                Vector2 sparkVel =
                    Main.rand.NextVector2Circular(3f, 3f) +
                    forward * Main.rand.NextFloat(2f, 5f);

                CritSpark hitSpark = new(
                    pos,
                    sparkVel,
                    Color.White,
                    new Color(255, 200, 80),
                    Main.rand.NextFloat(0.8f, 1.2f),
                    18
                );

                GeneralParticleHandler.SpawnParticle(hitSpark);
            }
        }

        private void SpawnHitSoftCore(Vector2 pos)
        {
            // 原本这里是 12 个 GlowSparkParticle 核心爆光
            // 现在改成圆润的圣光团，命中点会更柔和、更干净
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.15f, 1.15f);

                float scale = Main.rand.NextFloat(0.13f, 0.24f);

                Color particleColor = Color.Lerp(
                    new Color(255, 250, 180),
                    new Color(255, 185, 75),
                    Main.rand.NextFloat(0.15f, 0.55f)
                ) * 0.85f;

                int lifetime = Main.rand.Next(10, 17);

                SquishyLightParticle particle = new(
                    pos + Main.rand.NextVector2Circular(2f, 2f),
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnHitSoftJet(Vector2 pos, Vector2 forward)
        {
            // 原本这里是 18 个前向 GlowSparkParticle 喷射束
            // 现在改成前向柔光爆散，保留方向感，但不再像尖锐火花
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity =
                    forward.RotatedByRandom(MathHelper.ToRadians(8.5f)) * Main.rand.NextFloat(2.4f, 6.2f) +
                    Main.rand.NextVector2Circular(0.35f, 0.35f);

                float scale = Main.rand.NextFloat(0.10f, 0.19f);

                Color particleColor = Color.Lerp(
                    new Color(255, 235, 125),
                    new Color(255, 145, 55),
                    Main.rand.NextFloat(0.15f, 0.5f)
                ) * 0.76f;

                int lifetime = Main.rand.Next(11, 18);

                SquishyLightParticle particle = new(
                    pos + Main.rand.NextVector2Circular(1.4f, 1.4f),
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}