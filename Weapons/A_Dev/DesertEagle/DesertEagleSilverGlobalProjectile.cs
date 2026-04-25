using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleSilverGlobalProjectile : GlobalProjectile
    {
        private const string SparkTexturePath = "CalamityMod/Particles/BloomLineSoftEdge";

        public static readonly Color SilverMain = new(214, 224, 236);
        public static readonly Color SilverAccent = new(255, 255, 255);
        public static readonly Color SilverDark = new(140, 152, 170);

        public override bool InstancePerEntity => true;

        public bool SilverMarked;
        private bool impactHandled;

        public override void AI(Projectile projectile)
        {
            if (!SilverMarked || !projectile.friendly || projectile.damage <= 0 || projectile.velocity.LengthSquared() <= 1f)
                return;

            SpawnBulletTrail(projectile.Center, projectile.velocity, 1f);
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!SilverMarked)
                return;

            modifiers.ArmorPenetration += 12f;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!SilverMarked || impactHandled)
                return;

            impactHandled = true;
            SpawnSilverImpact(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitX), 0.85f);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (!SilverMarked || impactHandled)
                return base.OnTileCollide(projectile, oldVelocity);

            impactHandled = true;
            SpawnSilverImpact(projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX), 0.8f);
            return base.OnTileCollide(projectile, oldVelocity);
        }

        public static void SpawnSilverMuzzleFlash(Vector2 position, Vector2 direction, float scale = 1f)
        {
        }

        private static void SpawnSilverImpact(Vector2 position, Vector2 direction, float scale = 1f)
        {
            Vector2 impactDirection = direction.SafeNormalize(Vector2.UnitY);
            const float pulseScale = 0.72f;
            const int ringLifetime = 17;
            const int sparkCount = 10;
            const int dustCount = 16;

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    position,
                    Vector2.Zero,
                    SilverAccent,
                    0.7f * scale,
                    18));

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
                    new Vector2(1f, 3.4f) * scale,
                    impactDirection.ToRotation(),
                    0.16f,
                    0.034f,
                    ringLifetime));

                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    position,
                    Vector2.Zero,
                    SilverMain * 0.55f,
                    new Vector2(1f, 3f) * scale,
                    impactDirection.ToRotation() + MathHelper.PiOver2,
                    0.14f,
                    0.03f,
                    ringLifetime - 2));

                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / sparkCount;
                    Vector2 sparkDirection = angle.ToRotationVector2();
                    Vector2 sparkVelocity = sparkDirection * 5.5f * scale;

                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        position + sparkDirection * 10f * scale,
                        sparkVelocity,
                        false,
                        12,
                        0.032f * scale,
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
                        sparkDirection * 4.5f * scale,
                        SparkTexturePath,
                        false,
                        11,
                        0.028f * scale,
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
                    dustDirection * 4.5f * scale,
                    105,
                    Color.Lerp(SilverDark, SilverAccent, i % 3 / 2f),
                    0.95f * scale);

                dust.noGravity = true;
            }
        }

        private static void SpawnBulletTrail(Vector2 position, Vector2 velocity, float scale = 1f)
        {
            Vector2 forward = velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 18f;
            float helixRadius = 5.5f * scale;

            for (int i = 0; i < 3; i++)
            {
                float helixPhase = phase + MathHelper.TwoPi * i / 3f;
                float offset = (float)System.Math.Sin(helixPhase) * helixRadius;
                float depth = 0.55f + 0.45f * (float)System.Math.Cos(helixPhase);

                Dust dust = Dust.NewDustPerfect(
                    position + side * offset,
                    i == 0 ? DustID.SilverCoin : i == 1 ? DustID.SilverFlame : DustID.TintableDustLighted,
                    -forward * 0.45f + side * (float)System.Math.Cos(helixPhase) * 0.18f,
                    105,
                    Color.Lerp(SilverDark, SilverAccent, depth),
                    MathHelper.Lerp(0.62f, 0.92f, depth) * scale);

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

            if (!Main.rand.NextBool(4))
                return;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                position,
                -forward * 1.1f,
                SparkTexturePath,
                false,
                9,
                0.023f * scale,
                SilverMain,
                new Vector2(0.46f, 2.4f),
                shrinkSpeed: 0.8f));
        }
    }
}
