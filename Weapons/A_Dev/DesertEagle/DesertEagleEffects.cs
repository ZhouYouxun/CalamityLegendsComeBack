using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal static class DesertEagleEffects
    {
        private const string SparkTexturePath = "CalamityMod/Particles/BloomLineSoftEdge";

        public static readonly Color SilverMain = new(214, 224, 236);
        public static readonly Color SilverAccent = new(255, 255, 255);
        public static readonly Color SilverDark = new(140, 152, 170);

        public static void SpawnSilverMuzzleFlash(Vector2 position, Vector2 direction, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Vector2 velocity = direction * 0.15f;
            Color coreColor = Color.Lerp(SilverMain, SilverAccent, 0.5f);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, coreColor, 0.7f * scale, 16));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero, coreColor * 0.7f, Vector2.One, Main.rand.NextFloat(-0.18f, 0.18f), 0f, 0.18f * scale, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, velocity, SilverAccent * 0.72f, new Vector2(0.42f, 3.9f) * scale, direction.ToRotation(), 0.12f, 0.026f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, velocity * 0.6f, SilverMain * 0.6f, new Vector2(0.34f, 2.8f) * scale, direction.ToRotation() + MathHelper.PiOver2, 0.1f, 0.024f, 16));

            for (int i = 0; i < 7; i++)
            {
                Vector2 sparkVelocity = direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.4f, 8.4f) * scale;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position + Main.rand.NextVector2Circular(3f, 3f),
                    sparkVelocity,
                    SparkTexturePath,
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.02f, 0.03f) * scale,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat(0.2f, 0.7f)),
                    new Vector2(Main.rand.NextFloat(1f, 1.45f), Main.rand.NextFloat(0.45f, 0.7f)),
                    shrinkSpeed: 0.76f));
            }

            for (int i = 0; i < 9; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.SilverFlame,
                    direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(1.2f, 5.6f) * scale,
                    110,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public static void SpawnSilverImpact(Vector2 position, Vector2 direction, float scale = 1f, bool heavy = false)
        {
            if (!Main.dedServ)
            {
                float bloomScale = heavy ? 0.95f : 0.45f;
                int sparkCount = heavy ? 14 : 7;

                GeneralParticleHandler.SpawnParticle(new StrongBloom(position, Vector2.Zero, SilverAccent, bloomScale * scale, heavy ? 24 : 14));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero, SilverMain * 0.72f, new Vector2(heavy ? 1.3f : 0.72f, heavy ? 3.8f : 2.2f) * scale, direction.ToRotation(), heavy ? 0.2f : 0.12f, 0.03f, heavy ? 22 : 14));

                for (int i = 0; i < sparkCount; i++)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(heavy ? 4f : 2f, heavy ? 12f : 6f) * scale;
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        position + Main.rand.NextVector2Circular(5f, 5f),
                        sparkVelocity,
                        SparkTexturePath,
                        false,
                        Main.rand.Next(8, heavy ? 16 : 12),
                        Main.rand.NextFloat(0.018f, heavy ? 0.04f : 0.028f) * scale,
                        Main.rand.NextBool() ? SilverMain : SilverAccent,
                        new Vector2(Main.rand.NextFloat(1f, 1.8f), Main.rand.NextFloat(0.42f, 0.8f)),
                        shrinkSpeed: 0.74f));
                }
            }

            int dustCount = heavy ? 18 : 9;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.SilverCoin,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(heavy ? 3f : 1.4f, heavy ? 9f : 4f) * scale,
                    120,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.85f, heavy ? 1.55f : 1.2f));
                dust.noGravity = true;
            }
        }

        public static void SpawnSilverSpinTrail(Vector2 center, Vector2 direction, float spinAngle, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Vector2 orbitOffset = spinAngle.ToRotationVector2() * 18f * scale;
            Vector2 sparkVelocity = orbitOffset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 1.2f + direction * 0.35f;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                center + orbitOffset,
                sparkVelocity,
                SparkTexturePath,
                false,
                Main.rand.Next(8, 12),
                Main.rand.NextFloat(0.02f, 0.028f) * scale,
                Color.Lerp(SilverDark, SilverAccent, Main.rand.NextFloat(0.18f, 0.82f)),
                new Vector2(Main.rand.NextFloat(0.85f, 1.4f), Main.rand.NextFloat(0.4f, 0.65f)),
                shrinkSpeed: 0.82f));

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    center + direction * 6f,
                    Vector2.Zero,
                    SilverMain * 0.28f,
                    new Vector2(0.18f, 1.5f) * scale,
                    spinAngle,
                    0.08f,
                    0.018f,
                    10));
            }
        }

        public static void SpawnHeavySmoke(Vector2 position, Vector2 baseVelocity, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Particle smoke = new HeavySmokeParticle(
                position,
                baseVelocity,
                Color.Lerp(Color.Gray, SilverMain, 0.45f),
                Main.rand.Next(26, 38),
                Main.rand.NextFloat(0.7f, 1.15f) * scale,
                0.95f,
                Main.rand.NextFloat(-0.03f, 0.03f),
                true);
            GeneralParticleHandler.SpawnParticle(smoke);

            Particle mist = new MediumMistParticle(
                position + Main.rand.NextVector2Circular(3f, 3f),
                baseVelocity * 0.4f + Main.rand.NextVector2Circular(0.35f, 0.35f),
                SilverMain * 0.75f,
                Color.Black,
                Main.rand.NextFloat(0.45f, 0.68f) * scale,
                Main.rand.Next(40, 60));
            GeneralParticleHandler.SpawnParticle(mist);
        }

        public static void SpawnBulletTrail(Vector2 position, Vector2 velocity, float scale = 1f, bool heavy = false)
        {
            if (Main.rand.NextBool(heavy ? 1 : 2))
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.SilverCoin,
                    -velocity * Main.rand.NextFloat(0.015f, heavy ? 0.05f : 0.035f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    100,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.72f, heavy ? 1.35f : 1.05f) * scale);
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(heavy ? 2 : 4))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position + Main.rand.NextVector2Circular(2f, 2f),
                    -velocity * Main.rand.NextFloat(0.018f, 0.04f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    SparkTexturePath,
                    false,
                    Main.rand.Next(7, heavy ? 13 : 10),
                    Main.rand.NextFloat(0.016f, heavy ? 0.03f : 0.022f) * scale,
                    Color.Lerp(SilverMain, SilverAccent, Main.rand.NextFloat()),
                    new Vector2(Main.rand.NextFloat(0.9f, 1.35f), Main.rand.NextFloat(0.36f, 0.6f)),
                    shrinkSpeed: 0.8f));
            }
        }
    }
}
