using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.Vesuvius
{
    internal static class VesuviusProjectileVisuals
    {
        // There used to be VisualIntensity = 0.55f and VisualScale = 0.72f here, multiplied into
        // nearly every opacity and scale in the weapon (and an intensity clamp at 0.8 on top of
        // that). Every effect came out faint and undersized. Effects are now tuned individually
        // at their call sites — do not reintroduce a global dampener.

        internal static readonly Color LavaOrange = new(255, 88, 24);
        internal static readonly Color LavaGold = new(255, 192, 72);
        internal static readonly Color HotWhite = new(255, 242, 190);
        internal static readonly Color ScoriaSmoke = new(82, 68, 58);
        internal static readonly Color RavagerSmoke = new(74, 48, 40);
        internal static readonly Color AshGray = new(88, 84, 80);
        // ObsidianEdge was (38, 38, 42) — near-black, and it was only ever drawn additively,
        // where black adds nothing. It is now the bright violet fracture light that actually
        // makes the shard visible; ObsidianBlack stays dark for the alpha-blended body.
        internal static readonly Color ObsidianBlack = new(24, 16, 30);
        internal static readonly Color ObsidianEdge = new(126, 96, 176);

        internal static Color AdditiveColor(Color color) => new(color.R, color.G, color.B, 0);

        internal static void SpawnMoltenBloom(Vector2 center, float radius, float opacity = 0.75f)
        {
            if (Main.dedServ)
                return;

            float scale = MathHelper.Clamp(radius / 48f, 0.3f, 1.9f);
            Color color = Color.Lerp(LavaOrange, HotWhite, MathHelper.Clamp(radius / 120f, 0.08f, 0.55f));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, color * opacity, scale, 15));
        }

        internal static void SpawnMoltenMeteorTrail(Projectile projectile, float intensity, bool heavySmoke)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 basePosition = projectile.Center - direction * projectile.width * 0.28f;

            for (int side = -1; side <= 1; side += 2)
            {
                if (Main.rand.NextFloat() < 0.62f * intensity)
                {
                    Vector2 offset = normal * side * Main.rand.NextFloat(5f, 11f) * projectile.scale;
                    Dust flame = Dust.NewDustPerfect(
                        basePosition + offset,
                        DustID.CopperCoin,
                        -direction.RotatedBy(side * 0.55f) * Main.rand.NextFloat(0.8f, 2.6f) * intensity,
                        0,
                        new Color(255, Main.DiscoG, 0),
                        Main.rand.NextFloat(0.95f, 1.5f) * projectile.scale);
                    flame.noGravity = true;
                    flame.fadeIn = 0.5f;
                }
            }

            if (Main.rand.NextFloat() < 0.4f * intensity)
            {
                Particle ember = new SparkParticle(
                    basePosition + Main.rand.NextVector2Circular(7f, 7f),
                    -direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(2f, 5.2f) * intensity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.4f, 0.75f) * projectile.scale,
                    Main.rand.NextBool(5) ? HotWhite : Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (Main.rand.NextFloat() < (heavySmoke ? 0.34f : 0.15f) * intensity)
                SpawnScoriaSmoke(basePosition, -direction * Main.rand.NextFloat(0.5f, 2.2f) + Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(0.55f, 1.05f) * intensity, heavySmoke);
        }

        internal static void SpawnMoltenImpact(Vector2 center, float strength, bool ravagerWeight)
        {
            if (Main.dedServ)
                return;

            SpawnMoltenBloom(center + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(32f, 52f) * strength, 0.72f);
            GeneralParticleHandler.SpawnParticle(new ImpactParticle(center, 0.08f, 12, 0.3f * strength, HotWhite));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaOrange,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-8f, 8f),
                0.02f,
                0.13f * strength,
                16,
                true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                HotWhite * 0.72f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                0.1f * strength,
                14));

            int sparkCount = Count(11f * strength);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, ravagerWeight ? 9f : 6.5f) * strength - Vector2.UnitY * Main.rand.NextFloat(0f, 1.6f);
                Dust flame = Dust.NewDustPerfect(center, Main.rand.NextBool(4) ? DustID.InfernoFork : DustID.Obsidian, burstVelocity, 50, Color.Lerp(Color.DarkGray, LavaGold, 0.38f), Main.rand.NextFloat(0.85f, 1.45f));
                flame.noGravity = i % 2 == 0;

                if (i % 3 == 0)
                    GeneralParticleHandler.SpawnParticle(new PointParticle(center, burstVelocity * 0.62f, true, Main.rand.Next(14, 22), Main.rand.NextFloat(0.36f, 0.62f), Main.rand.NextBool(4) ? HotWhite : LavaGold, true));
            }

            int smokeCount = Count((ravagerWeight ? 6f : 4f) * strength);
            for (int i = 0; i < smokeCount; i++)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(18f, 12f) * strength, Main.rand.NextVector2Circular(2.4f, 1.4f) - Vector2.UnitY * Main.rand.NextFloat(1f, 3.4f), Main.rand.NextFloat(0.65f, 1.15f) * strength, ravagerWeight);

            int ashCount = Count(4f * strength);
            for (int i = 0; i < ashCount; i++)
                SpawnAshSquare(center + Main.rand.NextVector2Circular(16f, 12f), Main.rand.NextVector2Circular(1.8f, 1.4f) - Vector2.UnitY * Main.rand.NextFloat(0.4f, 2f), Main.rand.NextFloat(0.62f, 1.05f), Color.Lerp(AshGray, LavaOrange, 0.14f));
        }

        internal static void SpawnVolcanicAshCloud(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 center = projectile.Center + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.84f * intensity)
                SpawnAshSquare(center, projectile.velocity * Main.rand.NextFloat(-0.18f, 0.12f) + Main.rand.NextVector2Circular(0.75f, 0.75f), Main.rand.NextFloat(0.5f, 1.1f) * projectile.scale, Color.Lerp(AshGray, LavaOrange, Main.rand.NextFloat(0.08f, 0.28f)));

            if (Main.rand.NextFloat() < 0.34f * intensity)
            {
                Particle smoke = new SmallSmokeParticle(
                    center,
                    projectile.velocity * Main.rand.NextFloat(-0.08f, 0.04f) + Main.rand.NextVector2Circular(0.6f, 0.6f) - Vector2.UnitY * Main.rand.NextFloat(0.08f, 0.35f),
                    Color.Lerp(Color.DimGray, AshGray, 0.4f),
                    Color.Lerp(Color.Black, AshGray, 0.35f),
                    Main.rand.NextFloat(0.6f, 1.1f) * projectile.scale,
                    Main.rand.Next(100, 145),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextFloat() < 0.2f * intensity)
            {
                Dust coal = Dust.NewDustPerfect(center, Main.rand.NextBool(2) ? DustID.Smoke : DustID.Torch, Main.rand.NextVector2Circular(0.7f, 0.7f), 130, Color.Lerp(AshGray, LavaOrange, 0.25f), Main.rand.NextFloat(0.6f, 1.1f) * projectile.scale);
                coal.noGravity = Main.rand.NextBool();
            }
        }

        internal static void SpawnAshBurst(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < Count(16f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 5.5f) * strength;
                SpawnAshSquare(center + Main.rand.NextVector2Circular(8f, 8f), velocity, Main.rand.NextFloat(0.55f, 1.15f), Color.Lerp(AshGray, LavaOrange, Main.rand.NextFloat(0.08f, 0.22f)));
            }

            for (int i = 0; i < Count(5f * strength); i++)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(14f, 14f), Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(0.65f, 1.15f) * strength, false);
        }

        internal static void SpawnBombTrail(Projectile projectile, float intensity)
        {
            SpawnMoltenMeteorTrail(projectile, intensity * 1.15f, true);

            if (Main.dedServ || Main.rand.NextFloat() >= 0.3f)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 position = projectile.Center - direction * projectile.width * 0.45f + Main.rand.NextVector2Circular(12f, 12f);
            SpawnScoriaSmoke(position, -direction * Main.rand.NextFloat(1.5f, 3.8f) + Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(0.95f, 1.55f) * intensity, true);
        }

        internal static void SpawnBombDetonation(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            SpawnMoltenImpact(center, strength * 1.35f, true);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, LavaGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.2f, 2.9f * strength, 21));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, RavagerSmoke * 0.8f, new Vector2(1f, 0.62f), Main.rand.NextFloat(MathHelper.TwoPi), 0.35f, 3.6f * strength, 28));

            for (int i = 0; i < Count(12f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 12f) * strength;
                Dust stone = Dust.NewDustPerfect(center, Main.rand.NextBool(2) ? DustID.Stone : DustID.Iron, velocity, 110, Color.Lerp(Color.DarkGray, LavaOrange, 0.18f), Main.rand.NextFloat(1.1f, 2.3f));
                stone.noGravity = i % 2 == 0;
            }
        }

        internal static void SpawnLavaPoolBubble(Vector2 center, Vector2 spread, float intensity)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextFloat() < 0.72f * intensity)
                SpawnMoltenBloom(center + Main.rand.NextVector2Circular(spread.X, spread.Y), Main.rand.NextFloat(18f, 42f) * intensity, 0.62f);

            if (Main.rand.NextFloat() < 0.45f * intensity)
            {
                Vector2 pos = center + Main.rand.NextVector2Circular(spread.X, spread.Y * 0.65f);
                Dust flame = Dust.NewDustPerfect(pos, DustID.InfernoFork, -Vector2.UnitY.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.7f, 2.4f), 0, Main.rand.NextBool(3) ? LavaGold : LavaOrange, Main.rand.NextFloat(0.85f, 1.5f));
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.14f * intensity)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(spread.X, spread.Y), -Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 2.2f), Main.rand.NextFloat(0.7f, 1.1f) * intensity, false);
        }

        internal static void SpawnPillarTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            // A magma pillar is a stationary vertical eruption column, so everything it sheds
            // travels upward. The old code derived direction from a zeroed velocity and ended
            // up firing the core downward and the flames the opposite way.
            Vector2 up = -Vector2.UnitY;
            Vector2 center = projectile.Center + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.5f * intensity)
            {
                Particle core = new PointParticle(
                    center,
                    up.RotatedByRandom(0.08f) * Main.rand.NextFloat(4f, 9f) * intensity,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.4f, 0.72f) * intensity,
                    Main.rand.NextBool(5) ? HotWhite : LavaGold,
                    true);
                GeneralParticleHandler.SpawnParticle(core);
            }

            if (Main.rand.NextFloat() < 0.65f * intensity)
            {
                Vector2 edge = center + Vector2.UnitX * Main.rand.NextFloat(-20f, 20f) * intensity;
                Dust flame = Dust.NewDustPerfect(edge, DustID.InfernoFork, up.RotatedByRandom(0.32f) * Main.rand.NextFloat(1.5f, 4.5f), 0, Main.rand.NextBool(3) ? LavaGold : LavaOrange, Main.rand.NextFloat(1.05f, 1.7f));
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.33f * intensity)
                SpawnScoriaSmoke(center + Main.rand.NextVector2Circular(16f, 10f), up * Main.rand.NextFloat(1.2f, 2.8f) + Main.rand.NextVector2Circular(1.1f, 1.1f), Main.rand.NextFloat(0.65f, 1.1f) * intensity, false);
        }

        internal static void SpawnPillarResidual(Vector2 center, float rotation, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 axis = rotation.ToRotationVector2();
            Vector2 normal = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 offset = axis * Main.rand.NextFloat(-20f, 20f) + normal * Main.rand.NextFloat(-15f, 15f);
            SpawnLavaPoolBubble(center + offset, new Vector2(10f, 8f), intensity);

            if (Main.rand.NextFloat() < 0.22f * intensity)
                SpawnAshSquare(center + offset, Main.rand.NextVector2Circular(1.2f, 1.2f) - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.8f), Main.rand.NextFloat(0.55f, 1f), Color.Lerp(AshGray, LavaOrange, 0.16f));
        }

        internal static void SpawnCinderTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float sine = (float)System.Math.Sin((projectile.localAI[0] + projectile.identity) * 0.42f);

            for (int side = -1; side <= 1; side += 2)
            {
                if (Main.rand.NextFloat() < 0.5f * intensity)
                {
                    Vector2 offset = normal * side * sine * Main.rand.NextFloat(8f, 15f) * intensity;
                    Dust ember = Dust.NewDustPerfect(projectile.Center + offset, DustID.Flare, -direction * Main.rand.NextFloat(1.8f, 4.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat(0.15f, 0.9f)), Main.rand.NextFloat(1.2f, 2.2f));
                    ember.noGravity = true;
                }
            }

            if (Main.rand.NextFloat() < 0.72f * intensity)
            {
                Particle orb = new GlowOrbParticle(
                    projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    -direction * Main.rand.NextFloat(1.2f, 4.2f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.34f, 0.78f),
                    Main.rand.NextBool(5) ? HotWhite : Color.Lerp(LavaOrange, LavaGold, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (Main.rand.NextFloat() < 0.24f * intensity)
                SpawnScoriaSmoke(projectile.Center - direction * 12f, -direction * Main.rand.NextFloat(0.7f, 1.8f) + Main.rand.NextVector2Circular(0.7f, 0.7f), Main.rand.NextFloat(0.5f, 0.85f) * intensity, false);
        }

        internal static void SpawnCinderImpact(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            SpawnMoltenImpact(center, strength * 0.7f, false);
            for (int i = 0; i < Count(10f * strength); i++)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 6.4f) * strength, false, Main.rand.Next(14, 22), Main.rand.NextFloat(0.3f, 0.65f), Main.rand.NextBool(4) ? HotWhite : LavaGold));
        }

        // Obsidian reads as a dark solid with a violet fracture glow. Dark colours contribute
        // nothing under additive blending, so the body is drawn with alpha blending elsewhere
        // and only the fracture light is additive.
        internal static void SpawnObsidianTrail(Projectile projectile, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 center = projectile.Center - direction * projectile.width * 0.2f + Main.rand.NextVector2Circular(7f, 7f);

            if (Main.rand.NextFloat() < 0.85f * intensity)
            {
                Dust shard = Dust.NewDustPerfect(center, DustID.Obsidian, -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.4f, 2.4f) * intensity, 60, Color.Lerp(ObsidianEdge, Color.White, Main.rand.NextFloat(0.1f, 0.35f)), Main.rand.NextFloat(0.85f, 1.45f));
                shard.noGravity = Main.rand.NextBool(3);
            }

            if (Main.rand.NextFloat() < 0.5f * intensity)
            {
                Particle voidShard = new VoidSparkParticle(
                    center,
                    -direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(1.8f, 4.8f) * intensity,
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.55f, 0.95f) * intensity,
                    Color.Lerp(ObsidianEdge, Color.White, Main.rand.NextFloat(0.15f, 0.45f)),
                    0.9f);
                GeneralParticleHandler.SpawnParticle(voidShard);
            }

            if (Main.rand.NextFloat() < 0.25f * intensity)
            {
                Particle smoke = new SmallSmokeParticle(
                    center,
                    -direction * Main.rand.NextFloat(0.2f, 1.2f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    Color.Lerp(ObsidianBlack, ObsidianEdge, 0.5f),
                    Color.Black,
                    Main.rand.NextFloat(0.55f, 1f) * intensity,
                    Main.rand.Next(105, 150),
                    Main.rand.NextFloat(-0.05f, 0.05f));
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        internal static void SpawnObsidianImpact(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, ObsidianEdge, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.15f, 2.2f * strength, 18));

            for (int i = 0; i < Count(20f * strength); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8.5f) * strength;
                Dust dust = Dust.NewDustPerfect(center, DustID.Obsidian, velocity, 60, Color.Lerp(ObsidianEdge, Color.White, Main.rand.NextFloat(0.1f, 0.4f)), Main.rand.NextFloat(0.9f, 1.9f));
                dust.noGravity = Main.rand.NextBool();

                if (i % 3 == 0)
                {
                    Particle shard = new VoidSparkParticle(center, velocity * 0.55f, false, Main.rand.Next(16, 28), Main.rand.NextFloat(0.6f, 1.15f), Color.Lerp(ObsidianEdge, Color.White, Main.rand.NextFloat(0.2f, 0.5f)), 0.88f);
                    GeneralParticleHandler.SpawnParticle(shard);
                }
            }

            for (int i = 0; i < Count(7f * strength); i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(22f, 22f) * strength,
                    Main.rand.NextVector2Circular(3f, 3f) * strength,
                    Color.Lerp(ObsidianBlack, ObsidianEdge, Main.rand.NextFloat(0.25f, 0.6f)),
                    Main.rand.Next(26, 46),
                    Main.rand.NextFloat(0.75f, 1.4f) * strength,
                    0.85f,
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
                scale,
                heavy ? 0.8f : 0.6f,
                Main.rand.NextFloat(-0.07f, 0.07f),
                false,
                required: heavy);
            GeneralParticleHandler.SpawnParticle(smoke);
        }

        // SmallSmokeParticle takes opacity on a 0-255 scale (it decrements by 2-3 a frame and
        // dies at 0). Passing a 0-1 value made every ash square invisible and killed it on the
        // first update, which is why the ash layer never showed up.
        private static void SpawnAshSquare(Vector2 position, Vector2 velocity, float scale, Color color)
        {
            Particle ash = new SmallSmokeParticle(
                position,
                velocity,
                color,
                Color.Lerp(Color.Black, color, 0.25f),
                scale,
                Main.rand.Next(110, 155),
                Main.rand.NextFloat(-0.06f, 0.06f));
            GeneralParticleHandler.SpawnParticle(ash);
        }

        private static int Count(float count) => System.Math.Max(1, (int)System.Math.Ceiling(count));
    }
}
