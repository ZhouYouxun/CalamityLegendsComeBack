using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Sea-blue variant of Hyperdeath Rift Scepter's delayed falling beam.
    internal sealed class BrinyBaron_UltimateAzureRiftBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => BB_Balance.UltimateAzureRiftBeamLifetime - Projectile.timeLeft;
        private float BeamAngle => Projectile.ai[0];
        private bool IsFinalWaterBlade => Projectile.ai[1] > 0f;
        private Vector2 BeamStart => Projectile.Center + BeamAngle.ToRotationVector2() * BB_Balance.UltimateAzureRiftBeamLength;
        private Vector2 BeamDirection => BeamStart.DirectionTo(Projectile.Center);
        private bool CanDamageBeam => Age >= BB_Balance.UltimateAzureRiftBeamWindupFrames;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BB_Balance.UltimateAzureRiftBeamLifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => CanDamageBeam ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!CanDamageBeam)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), BeamStart,
                Projectile.Center + BeamDirection * BB_Balance.UltimateAzureRiftBeamLength,
                42f, ref collisionPoint);
        }

        public override void AI()
        {
            if (Age == BB_Balance.UltimateAzureRiftBeamWindupFrames)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack")
                {
                    Volume = 0.34f,
                    Pitch = 0.18f,
                    MaxInstances = -1
                }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, 0.10f, 0.56f, 0.92f);

            if (!Main.dedServ && Age == BB_Balance.UltimateAzureRiftBeamWindupFrames)
                SpawnWaterBladeParticles();

            if (!Main.dedServ && IsFinalWaterBlade && Projectile.timeLeft == 1)
                SpawnFinalDashSlashBurst();
        }

        // The former solid beam is deliberately not drawn. Its full body is made from
        // WaterFoamParticle and water dust, so every hit reads as a passing water blade.
        public override bool PreDraw(ref Color lightColor) => false;

        private void SpawnWaterBladeParticles()
        {
            float scale = IsFinalWaterBlade ? 1.8f : 1f;
            int particleCount = IsFinalWaterBlade ? 20 : 12;
            Vector2 forward = BeamDirection.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < particleCount; i++)
            {
                float progress = (i + Main.rand.NextFloat(0.1f, 0.9f)) / particleCount;
                Vector2 position = Vector2.Lerp(BeamStart, Projectile.Center, progress) +
                    right * Main.rand.NextFloat(-18f, 18f) * scale;
                Vector2 velocity = forward * Main.rand.NextFloat(7f, 13f) +
                    right * Main.rand.NextFloat(-2.8f, 2.8f) * scale;
                Color color = Color.Lerp(new Color(78, 195, 255), Color.White, Main.rand.NextFloat(0.2f, 0.72f));

                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                    position,
                    velocity,
                    Main.rand.Next(16, 25),
                    Main.rand.NextFloat(0.32f, 0.54f) * scale,
                    color));

                Dust water = Dust.NewDustPerfect(
                    position,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
                    velocity * Main.rand.NextFloat(0.45f, 0.8f),
                    80,
                    color,
                    Main.rand.NextFloat(0.8f, 1.2f) * scale);
                water.noGravity = true;
            }

            if (!IsFinalWaterBlade)
                return;

            // Same three-point dash-slash rhythm as Supreme Catastrophe, translated
            // into the finisher's water-blue palette rather than reusing its assets.
            for (int i = -1; i <= 1; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + forward * 54f * i,
                    forward * 5f,
                    false,
                    25,
                    5f * scale,
                    Color.DeepSkyBlue * 0.35f));
            }
        }

        private void SpawnFinalDashSlashBurst()
        {
            Vector2 forward = BeamDirection.SafeNormalize(Vector2.UnitY);
            GeneralParticleHandler.SpawnParticle(new VoidSparkParticle(
                Projectile.Center,
                forward * 5f,
                false,
                9,
                1.3f,
                Color.Cyan * 0.7f));

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(14f, 14f) * Main.rand.NextFloat(0.1f, 2.5f);
                Dust water = Dust.NewDustPerfect(Projectile.Center + velocity * 2f, DustID.Water, velocity, 80, Color.DeepSkyBlue, Main.rand.NextFloat(1.2f, 1.8f));
                water.noGravity = true;
            }
        }
    }
}
