using System;
using System.Collections.Generic;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheEndothermicEnergy
{
    internal class EndothermicEnergy_ARROW : ModProjectile, ILocalizedModType
    {
        private class EndothermicCopyState
        {
            public bool PendingShadowRelease;
            public int MarkedTargetIndex = -1;
            public bool BurstSpawned;
            public int VisualTimer;
            public int SnowSeed;
        }

        private readonly Dictionary<int, EndothermicCopyState> projectileStates = new();
        private const int ExtraUpdateCount = 3;
        private const int VisibleLifetimeFrames = 108;
        private const float GoldenAngle = 2.3999631f;
        private const float PiOver6 = 0.5235988f;
        private static readonly Color FrostWhite = new(242, 252, 255);
        private static readonly Color FrostBlue = new(148, 218, 255);
        private static readonly Color FrostDeep = new(54, 122, 230);
        private static readonly Color FrostGlass = new(184, 242, 255);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private EndothermicCopyState GetState()
        {
            if (!projectileStates.TryGetValue(Projectile.whoAmI, out EndothermicCopyState state))
            {
                state = new EndothermicCopyState();
                state.SnowSeed = Projectile.identity * 37 + Projectile.whoAmI * 53;
                projectileStates[Projectile.whoAmI] = state;
            }

            return state;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = ExtraUpdateCount;
            Projectile.timeLeft = VisibleLifetimeFrames * (Projectile.extraUpdates + 1);
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EndothermicCopyState state = GetState();
            state.PendingShadowRelease = false;
            state.MarkedTargetIndex = -1;
            state.BurstSpawned = false;
            state.VisualTimer = 0;
            state.SnowSeed = Projectile.identity * 37 + Projectile.whoAmI * 53;
        }

        public override void AI()
        {
            EndothermicCopyState state = GetState();
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(170, 220, 255).ToVector3() * 0.36f);

            if (!Main.dedServ)
                UpdateFrostParticleField(state, forward);
        }

        private void UpdateFrostParticleField(EndothermicCopyState state, Vector2 forward)
        {
            state.VisualTimer++;
            EmitFlightMotes(state, forward);
        }

        private void EmitFlightMotes(EndothermicCopyState state, Vector2 forward)
        {
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float seed = state.SnowSeed * 0.011f;
            float time = state.VisualTimer * 0.25f + seed;

            for (int lane = 0; lane < 3; lane++)
            {
                float side = lane - 1f;
                float phase = time * 0.72f + lane * MathHelper.TwoPi / 3f;
                float braidOffset = (float)Math.Sin(phase) * 6.8f + side * 2.4f;
                Vector2 lanePosition = Projectile.Center - forward * (4f + lane * 4f) + right * braidOffset;
                Vector2 laneVelocity = -forward * Main.rand.NextFloat(0.08f, 0.24f) + right * ((float)Math.Cos(phase) * (side == 0f ? 0.08f : side * 0.14f));

                SpawnIceNeedleParticle(
                    lanePosition,
                    laneVelocity,
                    Color.Lerp(FrostBlue, FrostWhite, 0.28f + lane * 0.22f) * 0.92f,
                    Main.rand.NextFloat(0.48f, 0.72f),
                    Main.rand.Next(20, 32));
            }

            int step = state.VisualTimer;
            for (int i = 0; i < 2; i++)
            {
                float angle = (step * 2 + i) * GoldenAngle + seed;
                float radius = 2.5f * (float)Math.Sqrt((step + i * 7) % 23 + 1f);
                Vector2 fermatOffset = right * ((float)Math.Cos(angle) * radius) + forward * ((float)Math.Sin(angle * 0.73f) * 2.2f);
                Vector2 position = Projectile.Center - forward * Main.rand.NextFloat(10f, 34f) + fermatOffset;
                Vector2 velocity = -forward * Main.rand.NextFloat(0.03f, 0.12f) + right * ((float)Math.Sin(angle) * 0.07f);

                SpawnSnowCrystalParticle(position, velocity, Main.rand.NextFloat(0.74f, 1.08f), Main.rand.Next(30, 48));
            }

            Particle mist = new MediumMistParticle(
                Projectile.Center - forward * Main.rand.NextFloat(18f, 44f) + right * Main.rand.NextFloat(-14f, 14f),
                -forward * Main.rand.NextFloat(0.01f, 0.06f) + right * Main.rand.NextFloat(-0.04f, 0.04f),
                FrostWhite * 0.44f,
                Color.Transparent,
                Main.rand.NextFloat(0.25f, 0.42f),
                Main.rand.NextFloat(78f, 118f));
            GeneralParticleHandler.SpawnParticle(mist);

            Particle coldCore = new SquishyLightParticle(
                Projectile.Center - forward * Main.rand.NextFloat(1f, 8f),
                -forward * Main.rand.NextFloat(0.02f, 0.1f),
                Main.rand.NextFloat(0.18f, 0.28f),
                Color.Lerp(FrostBlue, FrostWhite, 0.56f) * 0.54f,
                Main.rand.Next(14, 22),
                opacity: 0.82f,
                squishStrenght: 1.25f,
                maxSquish: 2.7f);
            GeneralParticleHandler.SpawnParticle(coldCore);
        }

        private static void SpawnIceNeedleParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifetime)
        {
            //Particle needle = new CustomSpark(
            //    position,
            //    velocity,
            //    "CalamityMod/Particles/BloomLineSoftEdge",
            //    false,
            //    lifetime,
            //    scale,
            //    color,
            //    new Vector2(Main.rand.NextFloat(2.25f, 3.35f), Main.rand.NextFloat(0.32f, 0.52f)),
            //    true,
            //    true,
            //    shrinkSpeed: Main.rand.NextFloat(0.64f, 0.76f));
            //GeneralParticleHandler.SpawnParticle(needle);
        }

        private static void SpawnSnowCrystalParticle(Vector2 position, Vector2 velocity, float scale, int lifetime)
        {
            Particle snowflakeSparkle = new SnowflakeSparkle(
                position,
                velocity,
                FrostWhite,
                FrostBlue,
                scale,
                lifetime,
                Main.rand.NextFloat(0.012f, 0.026f),
                Main.rand.NextFloat(1.08f, 1.48f),
                6);
            GeneralParticleHandler.SpawnParticle(snowflakeSparkle);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicCopyState state = GetState();

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.36f, Pitch = 0.18f, PitchVariance = 0.12f, MaxInstances = 5 }, target.Center);
            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);
            if (!Main.dedServ)
            {
                SpawnIceFractalBurst(state, target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), true);
                state.BurstSpawned = true;
            }

            state.PendingShadowRelease = true;
            state.MarkedTargetIndex = target.whoAmI;

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            EndothermicCopyState state = GetState();
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (!Main.dedServ && !state.BurstSpawned)
                SpawnIceFractalBurst(state, Projectile.Center, forward, false);

            if (Projectile.owner != Main.myPlayer)
            {
                projectileStates.Remove(Projectile.whoAmI);
                return;
            }

            Vector2 explosionCenter = Projectile.Center;
            int targetIndex = -1;

            bool hasTarget = state.PendingShadowRelease && Main.npc.IndexInRange(state.MarkedTargetIndex);
            if (hasTarget)
            {
                NPC target = Main.npc[state.MarkedTargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                {
                    explosionCenter = target.Center;
                    targetIndex = target.whoAmI;
                }
                else
                {
                    hasTarget = false;
                }
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                explosionCenter,
                Vector2.Zero,
                ModContent.ProjectileType<EndothermicEnergy_LN2>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            for (int arm = 0; arm < 6; arm++)
            {
                float armAngle = MathHelper.TwoPi * arm / 6f;
                Vector2 spawnOffset = armAngle.ToRotationVector2() * 260f;

                int shadowProjIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    explosionCenter + spawnOffset,
                    Vector2.Zero,
                    ModContent.ProjectileType<EndothermicEnergy_Shadow>(),
                    (int)(Projectile.damage * 0.36f),
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    targetIndex,
                    armAngle);

                if (targetIndex == -1 && Main.projectile.IndexInRange(shadowProjIndex))
                {
                    Main.projectile[shadowProjIndex].localAI[0] = explosionCenter.X;
                    Main.projectile[shadowProjIndex].localAI[1] = explosionCenter.Y;
                }
            }

            projectileStates.Remove(Projectile.whoAmI);
        }

        private static void SpawnIceFractalBurst(EndothermicCopyState state, Vector2 center, Vector2 forward, bool strong)
        {
            float baseRotation = forward.ToRotation() + state.SnowSeed * 0.0007f;
            int armCount = 6;
            int branchCount = strong ? 3 : 2;

            Particle pulse = new CustomPulse(
                center,
                Vector2.Zero,
                FrostGlass * (strong ? 0.38f : 0.24f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                baseRotation,
                strong ? 0.025f : 0.018f,
                strong ? 0.42f : 0.26f,
                strong ? 18 : 14);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int arm = 0; arm < armCount; arm++)
            {
                float armAngle = baseRotation + MathHelper.TwoPi * arm / armCount;
                Vector2 armDirection = armAngle.ToRotationVector2();

                for (int branch = 0; branch < branchCount; branch++)
                {
                    float branchSign = branch == 1 ? -1f : 1f;
                    float branchAngle = armAngle + branchSign * PiOver6 * branch * 0.62f;
                    Vector2 direction = branchAngle.ToRotationVector2();
                    float speed = Main.rand.NextFloat(strong ? 2.4f : 1.4f, strong ? 5.2f : 3.3f);

                }

                Particle snowflake = new SnowflakeSparkle(
                    center + armDirection * Main.rand.NextFloat(6f, strong ? 22f : 15f),
                    armDirection * Main.rand.NextFloat(strong ? 1.5f : 0.8f, strong ? 3.4f : 2.2f),
                    FrostWhite,
                    FrostBlue,
                    Main.rand.NextFloat(strong ? 0.74f : 0.48f, strong ? 1.05f : 0.78f),
                    Main.rand.Next(strong ? 24 : 18, strong ? 36 : 28),
                    Main.rand.NextFloat(0.018f, 0.04f),
                    Main.rand.NextFloat(0.95f, 1.28f),
                    6);
                GeneralParticleHandler.SpawnParticle(snowflake);
            }

            int mistCount = strong ? 12 : 7;
            for (int i = 0; i < mistCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(strong ? 1.2f : 0.7f, strong ? 3.4f : 2.1f);
                Particle mist = new MediumMistParticle(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    FrostWhite * (strong ? 0.54f : 0.36f),
                    Color.Transparent,
                    Main.rand.NextFloat(strong ? 0.34f : 0.22f, strong ? 0.58f : 0.42f),
                    Main.rand.NextFloat(95f, 150f));
                GeneralParticleHandler.SpawnParticle(mist);
            }

            int shardCount = strong ? 18 : 10;
            for (int i = 0; i < shardCount; i++)
            {
                float angle = baseRotation + i * GoldenAngle;
                float radius = 1.6f * (float)Math.Sqrt(i + 1f);
                Vector2 direction = angle.ToRotationVector2();
                Particle shard = new CustomSpark(
                    center + direction * radius,
                    direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.4f, 1.2f) + direction * Main.rand.NextFloat(0.6f, strong ? 2.4f : 1.5f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(strong ? 18 : 12, strong ? 30 : 22),
                    Main.rand.NextFloat(0.32f, strong ? 0.62f : 0.48f),
                    Color.Lerp(FrostDeep, FrostWhite, Main.rand.NextFloat(0.4f, 0.9f)),
                    new Vector2(Main.rand.NextFloat(1.3f, 2.25f), Main.rand.NextFloat(0.3f, 0.5f)),
                    true,
                    true,
                    shrinkSpeed: Main.rand.NextFloat(0.66f, 0.78f));
            }
        }
    }
}
