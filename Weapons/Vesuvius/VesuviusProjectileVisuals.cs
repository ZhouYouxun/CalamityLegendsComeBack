using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    internal static class VesuviusProjectileVisuals
    {
        internal const float VisualIntensity = 0.3f;
        internal const float VisualScale = 0.72f;

        internal static readonly Color LavaOrange = new(255, 88, 24);
        internal static readonly Color LavaGold = new(255, 192, 72);
        internal static readonly Color HotWhite = new(255, 242, 190);
        internal static readonly Color ScoriaSmoke = new(82, 68, 58);
        internal static readonly Color RavagerSmoke = new(74, 48, 40);
        internal static readonly Color AshGray = new(88, 84, 80);
        internal static readonly Color ObsidianBlack = new(4, 4, 5);
        internal static readonly Color ObsidianEdge = new(38, 38, 42);

        internal static void SpawnMoltenMeteorTrail(Projectile projectile, float intensity, bool heavySmoke)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 basePosition = projectile.Center - direction * projectile.width * 0.28f;

            if (Main.rand.NextFloat() < 0.58f * intensity)
                RancorLavaMetaball.SpawnParticle(basePosition + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextFloat(12f, 24f) * projectile.scale * intensity);

            for (int side = -1; side <= 1; side += 2)
            {
                if (Main.rand.NextFloat() < 0.5f * intensity)
                {
                    Vector2 offset = normal * side * Main.rand.NextFloat(5f, 11f) * projectile.scale;
                    Dust flame = Dust.NewDustPerfect(
                        basePosition + offset,
                        DustID.CopperCoin,
                        -direction.RotatedBy(side * 0.55f) * Main.rand.NextFloat(0.8f, 2.6f) * intensity,
                        90,
                        Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat(0.15f, 0.75f)),
                        Main.rand.NextFloat(0.9f, 1.45f) * projectile.scale * VisualScale);
                    flame.noGravity = true;
                    flame.fadeIn = 0.5f;
                }
            }

            if (Main.rand.NextFloat() < 0.32f * intensity)
            {
                Particle ember = new GlowSparkParticle(
                    basePosition + Main.rand.NextVector2Circular(7f, 7f),
                    -direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(2f, 5.2f) * intensity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.2f, 0.42f) * projectile.scale * VisualScale,
                    Main.rand.NextBool(5) ? HotWhite : Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat()),
                    new Vector2(1.65f, 0.42f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (Main.rand.NextFloat() < (heavySmoke ? 0.28f : 0.11f) * intensity)
                SpawnScoriaSmoke(basePosition, -direction * Main.rand.NextFloat(0.5f, 2.2f) + Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(0.42f, 0.85f) * intensity, heavySmoke);
        }

        internal static void SpawnMoltenImpact(Vector2 center, float strength, bool ravagerWeight)
        {
            if (Main.dedServ)
                return;

            strength = ReducedIntensity(strength);
            RancorLavaMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextFloat(32f, 58f) * strength);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaOrange * 0.78f * VisualIntensity,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-8f, 8f),
                0.02f,
                0.2f * strength * VisualScale,
                17,
                true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                HotWhite * 0.5f * VisualIntensity,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                0.13f * strength * VisualScale,
                15));

            int sparkCount = ReducedCount(18f * strength);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.5f, ravagerWeight ? 12f : 8f) * strength;
                Dust flame = Dust.NewDustPerfect(center, Main.rand.NextBool(4) ? DustID.InfernoFork : DustID.CopperCoin, burstVelocity, 80, Main.rand.NextBool(3) ? LavaGold : LavaOrange, Main.rand.NextFloat(0.8f, 1.7f) * VisualScale);
                flame.noGravity = true;

                if (i % 3 == 0)
                    GeneralParticleHandler.SpawnParticle(new LineParticle(center, burstVelocity * 0.72f, false, Main.rand.Next(16, 27), Main.rand.NextFloat(0.45f, 0.95f) * VisualScale, Main.rand.NextBool(4) ? HotWhite : LavaGold));
            }

            int smokeCount = ReducedCount((ravagerWeight ? 10f : 5f) * strength);
            for (int i = 0; i < smokeCount; i++)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(22f, 18f) * strength, Main.rand.NextVector2Circular(3.2f, 2.2f) - Vector2.UnitY * Main.rand.NextFloat(1.2f, 4.6f), Main.rand.NextFloat(0.65f, 1.35f) * strength, ravagerWeight);

            int ashCount = ReducedCount(7f * strength);
            for (int i = 0; i < ashCount; i++)
                SpawnAshSquare(center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(2.6f, 2.6f) - Vector2.UnitY * Main.rand.NextFloat(0.5f, 2.8f), Main.rand.NextFloat(0.7f, 1.35f), Color.Lerp(AshGray, LavaOrange, 0.18f));
        }

        internal static void SpawnVolcanicAshCloud(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            Vector2 center = projectile.Center + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.84f * intensity)
                SpawnAshSquare(center, projectile.velocity * Main.rand.NextFloat(-0.18f, 0.12f) + Main.rand.NextVector2Circular(0.75f, 0.75f), Main.rand.NextFloat(0.45f, 1.05f) * projectile.scale, Color.Lerp(AshGray, LavaOrange, Main.rand.NextFloat(0.08f, 0.28f)));

            if (Main.rand.NextFloat() < 0.34f * intensity)
            {
                Particle smoke = new TimedSmokeParticle(
                    center,
                    projectile.velocity * Main.rand.NextFloat(-0.08f, 0.04f) + Main.rand.NextVector2Circular(0.6f, 0.6f) - Vector2.UnitY * Main.rand.NextFloat(0.08f, 0.35f),
                    Color.Lerp(Color.DimGray, AshGray, 0.4f),
                    Color.Transparent,
                    Main.rand.NextFloat(0.55f, 1.05f) * projectile.scale * VisualScale,
                    0.62f * VisualIntensity,
                    Main.rand.Next(34, 58),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextFloat() < 0.2f * intensity)
            {
                Dust coal = Dust.NewDustPerfect(center, Main.rand.NextBool(2) ? DustID.Smoke : DustID.Torch, Main.rand.NextVector2Circular(0.7f, 0.7f), 130, Color.Lerp(AshGray, LavaOrange, 0.25f), Main.rand.NextFloat(0.55f, 1.05f) * projectile.scale * VisualScale);
                coal.noGravity = Main.rand.NextBool();
            }
        }

        internal static void SpawnAshBurst(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            strength = ReducedIntensity(strength);
            for (int i = 0; i < ReducedCount(16f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 5.5f) * strength;
                SpawnAshSquare(center + Main.rand.NextVector2Circular(8f, 8f), velocity, Main.rand.NextFloat(0.5f, 1.1f), Color.Lerp(AshGray, LavaOrange, Main.rand.NextFloat(0.08f, 0.22f)));
            }

            for (int i = 0; i < ReducedCount(5f * strength); i++)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(14f, 14f), Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(0.55f, 1.05f) * strength, false);
        }

        internal static void SpawnBombTrail(Projectile projectile, float intensity)
        {
            SpawnMoltenMeteorTrail(projectile, intensity * 1.15f, true);

            if (Main.dedServ || Main.rand.NextFloat() >= 0.33f * VisualIntensity)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 position = projectile.Center - direction * projectile.width * 0.45f + Main.rand.NextVector2Circular(12f, 12f);
            SpawnScoriaSmoke(position, -direction * Main.rand.NextFloat(1.5f, 3.8f) + Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(0.85f, 1.45f) * intensity, true);
        }

        internal static void SpawnBombDetonation(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            SpawnMoltenImpact(center, strength * 1.35f, true);
            strength = ReducedIntensity(strength);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, LavaGold * 0.72f * VisualIntensity, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.2f, 2.9f * strength * VisualScale, 21));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, RavagerSmoke * 0.55f * VisualIntensity, new Vector2(1f, 0.62f), Main.rand.NextFloat(MathHelper.TwoPi), 0.35f, 3.6f * strength * VisualScale, 28));

            for (int i = 0; i < ReducedCount(12f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 12f) * strength;
                Dust stone = Dust.NewDustPerfect(center, Main.rand.NextBool(2) ? DustID.Stone : DustID.Iron, velocity, 110, Color.Lerp(Color.DarkGray, LavaOrange, 0.18f), Main.rand.NextFloat(1f, 2.2f) * VisualScale);
                stone.noGravity = i % 2 == 0;
            }
        }

        internal static void SpawnLavaPoolBubble(Vector2 center, Vector2 spread, float intensity)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            if (Main.rand.NextFloat() < 0.72f * intensity)
                RancorLavaMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(spread.X, spread.Y), Main.rand.NextFloat(16f, 38f) * intensity);

            if (Main.rand.NextFloat() < 0.45f * intensity)
            {
                Vector2 pos = center + Main.rand.NextVector2Circular(spread.X, spread.Y * 0.65f);
                Dust flame = Dust.NewDustPerfect(pos, DustID.InfernoFork, -Vector2.UnitY.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.7f, 2.4f), 100, Main.rand.NextBool(3) ? LavaGold : LavaOrange, Main.rand.NextFloat(0.7f, 1.35f) * VisualScale);
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.12f * intensity)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(spread.X, spread.Y), -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 2.2f), Main.rand.NextFloat(0.55f, 0.95f) * intensity, false);
        }

        internal static void SpawnPillarTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 center = projectile.Center + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.8f * intensity)
                RancorLavaMetaball.SpawnParticle(center, Main.rand.NextFloat(20f, 38f) * intensity);

            if (Main.rand.NextFloat() < 0.5f * intensity)
            {
                Particle core = new GlowSparkParticle(
                    center,
                    direction.RotatedByRandom(0.08f) * Main.rand.NextFloat(4f, 9f) * intensity,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.28f, 0.5f) * intensity,
                    Main.rand.NextBool(5) ? HotWhite : LavaGold,
                    new Vector2(2.2f, 0.38f),
                    true,
                    false);
                GeneralParticleHandler.SpawnParticle(core);
            }

            if (Main.rand.NextFloat() < 0.65f * intensity)
            {
                Vector2 edge = center + normal * Main.rand.NextFloat(-20f, 20f) * intensity;
                Dust flame = Dust.NewDustPerfect(edge, DustID.InfernoFork, -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.8f, 2.8f), 90, Main.rand.NextBool(3) ? LavaGold : LavaOrange, Main.rand.NextFloat(0.9f, 1.55f) * VisualScale);
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.33f * intensity)
                SpawnScoriaSmoke(center - direction * 12f + Main.rand.NextVector2Circular(16f, 10f), -direction * Main.rand.NextFloat(0.8f, 2f) + Main.rand.NextVector2Circular(1.1f, 1.1f), Main.rand.NextFloat(0.5f, 0.95f) * intensity, false);
        }

        internal static void SpawnPillarResidual(Vector2 center, float rotation, float intensity)
        {
            if (Main.dedServ)
                return;

            float reducedIntensity = ReducedIntensity(intensity);
            Vector2 axis = rotation.ToRotationVector2();
            Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = axis * Main.rand.NextFloat(-20f, 20f) + normal * Main.rand.NextFloat(-15f, 15f);
            SpawnLavaPoolBubble(center + offset, new Vector2(10f, 8f), intensity);

            if (Main.rand.NextFloat() < 0.2f * reducedIntensity)
                SpawnAshSquare(center + offset, Main.rand.NextVector2Circular(1.2f, 1.2f) - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.8f), Main.rand.NextFloat(0.45f, 0.9f), Color.Lerp(AshGray, LavaOrange, 0.16f));
        }

        internal static void SpawnCinderTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float sine = (float)System.Math.Sin((projectile.localAI[0] + projectile.identity) * 0.42f);

            for (int side = -1; side <= 1; side += 2)
            {
                if (Main.rand.NextFloat() < 0.5f * intensity)
                {
                    Vector2 offset = normal * side * sine * Main.rand.NextFloat(8f, 15f) * intensity;
                    Dust ember = Dust.NewDustPerfect(projectile.Center + offset, DustID.Flare, -direction * Main.rand.NextFloat(1.8f, 4.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 90, Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat(0.15f, 0.9f)), Main.rand.NextFloat(1.1f, 2.1f) * VisualScale);
                    ember.noGravity = true;
                }
            }

            if (Main.rand.NextFloat() < 0.72f * intensity)
            {
                Particle orb = new GlowOrbParticle(
                    projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    -direction * Main.rand.NextFloat(1.2f, 4.2f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.3f, 0.72f) * VisualScale,
                    Main.rand.NextBool(5) ? HotWhite : Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (Main.rand.NextFloat() < 0.22f * intensity)
                SpawnScoriaSmoke(projectile.Center - direction * 12f, -direction * Main.rand.NextFloat(0.7f, 1.8f) + Main.rand.NextVector2Circular(0.7f, 0.7f), Main.rand.NextFloat(0.35f, 0.7f) * intensity, false);
        }

        internal static void SpawnCinderImpact(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            SpawnMoltenImpact(center, strength * 0.7f, false);
            strength = ReducedIntensity(strength);
            for (int i = 0; i < ReducedCount(10f * strength); i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 6.4f) * strength, false, Main.rand.Next(12, 20), Main.rand.NextFloat(0.24f, 0.58f) * VisualScale, Main.rand.NextBool(4) ? HotWhite : LavaGold));
        }

        internal static void SpawnObsidianTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            intensity = ReducedIntensity(intensity);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 center = projectile.Center - direction * projectile.width * 0.2f + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.85f * intensity)
            {
                Dust shard = Dust.NewDustPerfect(center, DustID.Obsidian, -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.4f, 2.4f) * intensity, 130, Color.Lerp(ObsidianBlack, ObsidianEdge, Main.rand.NextFloat(0.15f, 0.45f)), Main.rand.NextFloat(0.75f, 1.35f) * VisualScale);
                shard.noGravity = Main.rand.NextBool(3);
            }

            if (Main.rand.NextFloat() < 0.5f * intensity)
            {
                Particle voidShard = new VoidSparkParticle(
                    center,
                    -direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(1.8f, 4.8f) * intensity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.45f, 0.85f) * intensity,
                    Main.rand.NextBool(3) ? ObsidianBlack : ObsidianEdge,
                    0.9f);
                GeneralParticleHandler.SpawnParticle(voidShard);
            }

            if (Main.rand.NextFloat() < 0.25f * intensity)
            {
                Particle smoke = new TimedSmokeParticle(
                    center,
                    -direction * Main.rand.NextFloat(0.2f, 1.2f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    Color.Lerp(ObsidianBlack, ObsidianEdge, 0.38f),
                    Color.Transparent,
                    Main.rand.NextFloat(0.48f, 0.95f) * intensity,
                    0.74f,
                    Main.rand.Next(30, 52),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        internal static void SpawnObsidianImpact(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            strength = ReducedIntensity(strength);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, ObsidianEdge * 0.78f * VisualIntensity, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.15f, 2.2f * strength * VisualScale, 18));

            for (int i = 0; i < ReducedCount(20f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8.5f) * strength;
                Dust dust = Dust.NewDustPerfect(center, DustID.Obsidian, velocity, 140, Main.rand.NextBool(3) ? ObsidianBlack : ObsidianEdge, Main.rand.NextFloat(0.8f, 1.8f) * VisualScale);
                dust.noGravity = Main.rand.NextBool();

                if (i % 3 == 0)
                {
                    Particle shard = new VoidSparkParticle(center, velocity * 0.55f, false, Main.rand.Next(14, 26), Main.rand.NextFloat(0.52f, 1f) * VisualScale, Main.rand.NextBool(2) ? ObsidianBlack : ObsidianEdge, 0.88f);
                    GeneralParticleHandler.SpawnParticle(shard);
                }
            }

            for (int i = 0; i < ReducedCount(7f * strength); i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(22f, 22f) * strength,
                    Main.rand.NextVector2Circular(3f, 3f) * strength,
                    Main.rand.NextBool(2) ? ObsidianBlack : ObsidianEdge,
                    Main.rand.Next(26, 46),
                    Main.rand.NextFloat(0.6f, 1.2f) * strength * VisualScale,
                    0.72f * VisualIntensity,
                    Main.rand.NextFloat(-0.09f, 0.09f),
                    true,
                    required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private static void SpawnScoriaSmoke(Vector2 position, Vector2 velocity, float scale, bool heavy)
        {
            Color smokeColor = Color.Lerp(ScoriaSmoke, heavy ? RavagerSmoke : AshGray, Main.rand.NextFloat(0.15f, 0.5f));
            Particle smoke = new HeavySmokeParticle(
                position,
                velocity,
                smokeColor,
                Main.rand.Next(heavy ? 30 : 22, heavy ? 58 : 42),
                scale * VisualScale,
                (heavy ? 0.9f : 0.62f) * VisualIntensity,
                Main.rand.NextFloat(-0.07f, 0.07f),
                Main.rand.NextBool(),
                required: heavy);
            GeneralParticleHandler.SpawnParticle(smoke);
        }

        private static void SpawnAshSquare(Vector2 position, Vector2 velocity, float scale, Color color)
        {
            Particle ash = new SquareAshParticle(
                position,
                velocity,
                Main.rand.Next(24, 48),
                scale * VisualScale,
                color * VisualScale);
            GeneralParticleHandler.SpawnParticle(ash);
        }

        private static float ReducedIntensity(float intensity) => MathHelper.Clamp(intensity * VisualIntensity, 0f, 0.8f);

        private static int ReducedCount(float count) => System.Math.Max(1, (int)System.Math.Ceiling(count));
    }
}
