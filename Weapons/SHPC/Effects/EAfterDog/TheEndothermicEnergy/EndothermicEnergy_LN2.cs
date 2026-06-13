using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_LN2 : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private float sizeFactor = 1f;
        private int hitCount;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 15;
            Projectile.timeLeft = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            timer++;
            if (timer == 1)
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.42f, Pitch = -0.28f, PitchVariance = 0.1f, MaxInstances = 4 }, Projectile.Center);

            float progress = Utils.GetLerpValue(0f, 50f, timer, true);
            sizeFactor = MathHelper.Lerp(1f, 2f, progress);

            Vector2 center = Projectile.Center;
            Projectile.width = (int)(400 * sizeFactor);
            Projectile.height = (int)(400 * sizeFactor);
            Projectile.Center = center;

            Projectile.velocity *= 0.8f;

            for (int i = 0; i < 12; i++)
            {
                float radius = Projectile.width * 0.5f;
                Vector2 randomPos = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = new(
                    Main.rand.NextFloat(-1.2f, 1.2f),
                    Main.rand.NextFloat(-6f, -2f));

                Particle mist = new MediumMistParticle(
                    randomPos,
                    velocity,
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.6f, 1.1f),
                    Main.rand.NextFloat(200f, 300f));

                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (timer % 6 == 0)
            {
                int amount = 22;
                for (int i = 0; i < amount; i++)
                {
                    float angle = MathHelper.TwoPi / amount * i + timer * 0.03f;
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * 60f;
                    Vector2 vel = (pos - Projectile.Center).SafeNormalize(Vector2.Zero) * 2f;

                    Particle mist = new MediumMistParticle(
                        pos,
                        vel,
                        Color.White,
                        Color.Transparent,
                        0.7f,
                        180f);

                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                float radius = Projectile.width * 0.5f * 0.6f;
                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                Vector2 vel = new(
                    Main.rand.NextFloat(-1.0f, 1.0f),
                    Main.rand.NextFloat(-3.5f, -1.2f));

                Particle smoke = new HeavySmokeParticle(
                    spawnPos,
                    vel,
                    Color.Lerp(Color.White, Color.WhiteSmoke, 0.3f),
                    22,
                    Main.rand.NextFloat(1.2f, 1.8f),
                    0.55f,
                    0f,
                    true);

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (timer > 90 || hitCount >= 12)
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;
            target.AddBuff(BuffID.Frostburn, 120);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
