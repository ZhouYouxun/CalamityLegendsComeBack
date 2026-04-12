using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentEntropyEffect : DefaultEffect
    {
        public override int EffectID => 25;
        public override int AmmoType => ModContent.ItemType<MeldBlob>();

        public override Color ThemeColor => new Color(6, 6, 6);
        public override Color StartColor => new Color(20, 20, 20);
        public override Color EndColor => new Color(0, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 1.55f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.02f;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float scaleFactor = Utils.GetLerpValue(0f, 180f, projectile.timeLeft, true);

            // Ordered layer: a restrained pulse aligned with the flight axis.
            if (projectile.timeLeft % 6 == 0)
            {
                DirectionalPulseRing orderedPulse = new(
                    projectile.Center - forward * 12f,
                    projectile.velocity * 0.18f,
                    Color.Lerp(new Color(26, 28, 30), new Color(74, 84, 80), 0.35f),
                    new Vector2(0.72f, 1.55f),
                    projectile.rotation,
                    0.06f,
                    0.01f,
                    24
                );
                GeneralParticleHandler.SpawnParticle(orderedPulse);
            }

            // Chaotic layer: black shards and sickly afterimages tear backward.
            if (Main.rand.NextBool(3))
            {
                int dustType = Main.rand.NextBool(6) ? 278 : 263;

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - forward * Main.rand.NextFloat(2f, 18f) + Main.rand.NextVector2Circular(10f, 10f),
                    dustType,
                    back.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 1.25f) * projectile.velocity.Length()
                );
                dust.scale = dust.type == 278
                    ? Main.rand.NextFloat(0.3f, 0.65f)
                    : Main.rand.NextFloat(0.65f, 1.55f);
                dust.noGravity = true;
                dust.color = Color.Lerp(new Color(10, 10, 10), new Color(36, 46, 38), Main.rand.NextFloat(0.12f, 0.42f));
            }

            // Bound smoke keeps the chaos packed into a controllable volume.
            if (Main.rand.NextBool(4))
            {
                HeavySmokeParticle smoke = new(
                    projectile.Center - forward * Main.rand.NextFloat(2f, 10f) + side * Main.rand.NextFloat(-8f, 8f),
                    back * Main.rand.NextFloat(0.15f, 1.1f) + side * Main.rand.NextFloat(-0.25f, 0.25f),
                    Main.rand.NextBool(3) ? new Color(10, 10, 10) : new Color(34, 38, 34),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.4f, 0.85f),
                    0.35f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // Symmetric sparks give the projectile a readable sense of order.
            if (Main.rand.NextBool(3))
            {
                Vector2 spawnPos = projectile.Center - forward * Main.rand.NextFloat(3f, 12f) +
                                   side * Main.rand.NextFloatDirection() * Main.rand.NextFloat(5f, 12f);
                SparkParticle spark = new(
                    spawnPos,
                    back * Main.rand.NextFloat(0.2f, 0.8f) + side * Main.rand.NextFloat(-0.4f, 0.4f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Color.Lerp(new Color(90, 95, 92), new Color(28, 34, 29), Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            //// Central accretion core.
            //if (projectile.timeLeft % 10 == 0)
            //{
            //    CustomPulse corePulse = new(
            //        projectile.Center,
            //        Vector2.Zero,
            //        new Color(12, 12, 12),
            //        "CalamityMod/Particles/SmallBloom",
            //        Vector2.One,
            //        Main.rand.NextFloat(-0.15f, 0.15f),
            //        0.45f + scaleFactor * 0.3f,
            //        0f,
            //        14,
            //        false
            //    );
            //    GeneralParticleHandler.SpawnParticle(corePulse);
            //}

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    191,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    100,
                    Color.Black,
                    Main.rand.NextFloat(0.95f, 1.45f)
                );
                dust.noGravity = true;
                dust.velocity *= 0.35f;
            }

            Lighting.AddLight(projectile.Center, new Vector3(0.03f, 0.035f, 0.03f));
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<FragmentEntropyExplosion>(),
                (int)(projectile.damage * 0.1f),
                projectile.knockBack,
                projectile.owner
            );

            SpawnOrderedBurst(center);
            SpawnChaoticBurst(center);
            SpawnCollapseCore(center);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.72f, Pitch = -0.48f }, center);
        }

        private static void SpawnOrderedBurst(Vector2 center)
        {
            Color orderColor = new(68, 78, 74);
            float[] ringRadii = { 54f, 110f, 180f, 260f };
            int[] ringCounts = { 8, 14, 22, 30 };

            for (int ringIndex = 0; ringIndex < ringRadii.Length; ringIndex++)
            {
                float radius = ringRadii[ringIndex];
                int count = ringCounts[ringIndex];

                CustomPulse ringPulse = new(
                    center,
                    Vector2.Zero,
                    Color.Lerp(new Color(14, 14, 14), orderColor, ringIndex / (float)(ringRadii.Length - 1)) * 0.9f,
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    0.7f + ringIndex * 0.38f,
                    0.16f + ringIndex * 0.08f,
                    18 + ringIndex * 4,
                    false
                );
                GeneralParticleHandler.SpawnParticle(ringPulse);

                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count;
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 pos = center + dir * radius;

                    SparkParticle orderedSpark = new(
                        pos,
                        dir * Main.rand.NextFloat(0.45f, 1.35f),
                        false,
                        Main.rand.Next(28, 42),
                        Main.rand.NextFloat(0.7f, 1.1f),
                        Color.Lerp(new Color(105, 110, 108), orderColor, Main.rand.NextFloat(0.35f, 0.85f))
                    );
                    GeneralParticleHandler.SpawnParticle(orderedSpark);
                }
            }

            for (int spoke = 0; spoke < 8; spoke++)
            {
                float angle = MathHelper.TwoPi * spoke / 8f;
                Vector2 dir = angle.ToRotationVector2();

                CustomSpark spine = new(
                    center + dir * Main.rand.NextFloat(18f, 34f),
                    dir * Main.rand.NextFloat(3.8f, 7.2f),
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    Main.rand.Next(20, 28),
                    Main.rand.NextFloat(0.03f, 0.055f),
                    Color.Lerp(new Color(78, 88, 84), Color.Black, Main.rand.NextFloat(0.3f, 0.6f)),
                    new Vector2(Main.rand.NextFloat(1.4f, 2.1f), Main.rand.NextFloat(0.22f, 0.4f)),
                    false,
                    shrinkSpeed: 1.05f
                );
                GeneralParticleHandler.SpawnParticle(spine);
            }
        }

        private static void SpawnChaoticBurst(Vector2 center)
        {
            for (int i = 0; i < 28; i++)
            {
                AltSparkParticle shard = new(
                    center + Main.rand.NextVector2Circular(36f, 36f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10.5f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.9f, 1.45f),
                    Main.rand.NextBool(4) ? new Color(72, 86, 74) : Color.Black
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            for (int i = 0; i < 22; i++)
            {
                HeavySmokeParticle smoke = new(
                    center + Main.rand.NextVector2Circular(26f, 26f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.5f, 3f),
                    Main.rand.NextBool(3) ? new Color(10, 10, 10) : new Color(42, 46, 42),
                    Main.rand.Next(34, 54),
                    Main.rand.NextFloat(0.85f, 1.55f),
                    0.36f,
                    Main.rand.NextFloat(-0.1f, 0.1f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 72; i++)
            {
                int dustType = Main.rand.NextBool() ? 191 : 240;
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                float radius = Main.rand.NextFloat(12f, 300f);

                Dust dust = Dust.NewDustPerfect(
                    center + dir * radius,
                    dustType,
                    dir.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.8f, 6.5f),
                    100,
                    Main.rand.NextBool(5) ? new Color(24, 30, 24) : Color.Black,
                    Main.rand.NextFloat(1f, 1.8f)
                );
                dust.noGravity = true;
                dust.velocity *= 0.75f;
            }

            for (int i = 0; i < 30; i++)
            {
                int dustType = Main.rand.NextBool(5) ? 278 : 263;

                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(32f, 32f),
                    dustType,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f),
                    0,
                    new Color(20, 26, 20),
                    dustType == 278 ? Main.rand.NextFloat(0.55f, 0.9f) : Main.rand.NextFloat(0.9f, 1.45f)
                );
                dust.noGravity = true;
            }
        }

        private static void SpawnCollapseCore(Vector2 center)
        {
            for (int i = 0; i < 3; i++)
            {
                CustomPulse blackCore = new(
                    center,
                    Vector2.Zero,
                    i == 0 ? Color.Black : new Color(20, 22, 20),
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    1.5f + i * 0.35f,
                    0f,
                    34 + i * 8,
                    false
                );
                GeneralParticleHandler.SpawnParticle(blackCore);
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 dir = angle.ToRotationVector2();

                Dust inwardDust = Dust.NewDustPerfect(
                    center + dir * Main.rand.NextFloat(90f, 220f),
                    240,
                    -dir * Main.rand.NextFloat(3f, 7f),
                    0,
                    Color.Black,
                    Main.rand.NextFloat(1.05f, 1.5f)
                );
                inwardDust.noGravity = true;
            }

            Lighting.AddLight(center, new Vector3(0.08f, 0.085f, 0.08f));
        }
    }
}
