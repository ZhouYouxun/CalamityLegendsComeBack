using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleHeavyRound : ModProjectile, ILocalizedModType
    {
        private const string SparkTexturePath = "CalamityMod/Particles/BloomLineSoftEdge";

        private static readonly Color SilverMain = new(214, 224, 236);
        private static readonly Color SilverAccent = new(255, 255, 255);
        private static readonly Color SilverDark = new(140, 152, 170);

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/DesertEagle/HandheldTankShell";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 14;
            Projectile.timeLeft = 1500;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            DrawOffsetX = -8;
            DrawOriginOffsetX = -2;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.localAI[0] += 1f;

            SpawnBulletTrail(Projectile.Center, Projectile.velocity, 1.25f);

            if (Projectile.localAI[0] < 10f && !Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                    SpawnHeavySmoke(Projectile.Center - Projectile.velocity * i * 0.15f, -Projectile.velocity * 0.08f, 0.9f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnSilverImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 1.65f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.12f }, Projectile.Center);

            SpawnSilverImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 2f);

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 220;
            Projectile.Center = Projectile.position;
            Projectile.position -= new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            Projectile.Damage();

            for (int i = 0; i < 8; i++)
                SpawnHeavySmoke(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), Main.rand.NextVector2Circular(1.6f, 1.6f) + Vector2.UnitY * -Main.rand.NextFloat(0.5f, 1.8f), 1.1f);
        }

        private static void SpawnSilverImpact(Vector2 position, Vector2 direction, float scale = 1f)
        {
            Vector2 impactDirection = direction.SafeNormalize(Vector2.UnitY);
            const float pulseScale = 1.15f;
            const int ringLifetime = 24;
            const int sparkCount = 18;
            const int dustCount = 28;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    position,
                    Vector2.Zero,
                    SilverAccent,
                    1.15f * scale,
                    28));

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    position,
                    Vector2.Zero,
                    SilverAccent,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge",
                    Vector2.One,
                    0f,
                    0.01f,
                    0.08f * pulseScale * scale,
                    ringLifetime,
                    true,
                    0.95f));

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.82f,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                    Vector2.One,
                    0f,
                    0.01f,
                    0.12f * pulseScale * scale,
                    ringLifetime + 3,
                    true,
                    0.7f));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    position,
                    Vector2.Zero,
                    SilverAccent * 0.75f,
                    new Vector2(1f, 4.8f) * scale,
                    impactDirection.ToRotation(),
                    0.16f,
                    0.034f,
                    ringLifetime));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.55f,
                    new Vector2(1f, 4.2f) * scale,
                    impactDirection.ToRotation() + MathHelper.PiOver2,
                    0.14f,
                    0.03f,
                    ringLifetime - 2));

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / sparkCount;
                    Vector2 sparkDirection = angle.ToRotationVector2();
                    Vector2 sparkVelocity = sparkDirection * 9f * scale;

                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position + sparkDirection * 10f * scale,
                        sparkVelocity,
                        false,
                        16,
                        0.045f * scale,
                        Color.Lerp(SilverMain, SilverAccent, i % 2 == 0 ? 0.75f : 0.35f),
                        new Vector2(1.2f, 0.58f),
                        true));
                }

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * (i + 0.5f) / sparkCount;
                    Vector2 sparkDirection = angle.ToRotationVector2();

                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        position + sparkDirection * 7f * scale,
                        sparkDirection * 7f * scale,
                        SparkTexturePath,
                        false,
                        15,
                        0.04f * scale,
                        i % 2 == 0 ? SilverAccent : SilverMain,
                        new Vector2(0.75f, 1.8f),
                        shrinkSpeed: 0.78f));
                }
            }

            for (int i = 0; i < dustCount; i++)
            {
                float angle = MathHelper.TwoPi * i / dustCount;
                Vector2 dustDirection = angle.ToRotationVector2();

                Dust dust = Dust.NewDustPerfect(
                    position,
                    i % 2 == 0 ? DustID.SilverCoin : DustID.SilverFlame,
                    dustDirection * 7.5f * scale,
                    105,
                    Color.Lerp(SilverDark, SilverAccent, i % 3 / 2f),
                    1.25f * scale);

                dust.noGravity = true;
            }
        }

        private static void SpawnHeavySmoke(Vector2 position, Vector2 baseVelocity, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Particle smoke = new HeavySmokeParticle(
                position,
                baseVelocity * 0.35f,
                Color.Lerp(Color.Gray, SilverDark, 0.5f),
                Main.rand.Next(24, 34),
                Main.rand.NextFloat(0.58f, 0.9f) * scale,
                0.95f,
                0f,
                true);

            GeneralParticleHandler.SpawnParticle(smoke);
        }

        private static void SpawnBulletTrail(Vector2 position, Vector2 velocity, float scale = 1f)
        {
            Vector2 forward = velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 24f;
            float helixRadius = 8f * scale;

            for (int i = 0; i < 3; i++)
            {
                float helixPhase = phase + MathHelper.TwoPi * i / 3f;
                float offset = (float)System.Math.Sin(helixPhase) * helixRadius;
                float depth = 0.55f + 0.45f * (float)System.Math.Cos(helixPhase);

                Dust dust = Dust.NewDustPerfect(
                    position + side * offset,
                    i == 0 ? DustID.SilverCoin : i == 1 ? DustID.SilverFlame : DustID.TintableDustLighted,
                    -forward * 0.8f + side * (float)System.Math.Cos(helixPhase) * 0.18f,
                    105,
                    Color.Lerp(SilverDark, SilverAccent, depth),
                    MathHelper.Lerp(0.62f, 1.15f, depth) * scale);

                dust.noGravity = true;
            }

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                position + forward * 5f * scale,
                -velocity * 0.045f,
                false,
                3,
                0.014f * scale,
                SilverAccent,
                new Vector2(0.75f, 2.8f),
                false,
                true));

            if (!Main.rand.NextBool(2))
                return;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                position,
                -forward * 1.55f,
                SparkTexturePath,
                false,
                12,
                0.032f * scale,
                SilverMain,
                new Vector2(0.46f, 2.4f),
                shrinkSpeed: 0.8f));
        }
    }
}
