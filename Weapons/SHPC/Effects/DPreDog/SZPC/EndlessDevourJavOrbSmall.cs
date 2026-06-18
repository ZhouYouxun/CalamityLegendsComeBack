using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    internal class EndlessDevourJavOrbSmall : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 150;
        private const int MaxUpdateCount = 4;

        private static readonly Color TrailStartColor = Color.Black;
        private static readonly Color TrailMidColor = Color.Black;
        private static readonly Color TrailEndColor = Color.Black;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = MaxUpdateCount;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30 * MaxUpdateCount;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            float timer = Projectile.ai[0];
            float seed = Projectile.ai[1] == 0f ? Projectile.identity * 0.73f : Projectile.ai[1];

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.01f, 0.01f, 0.01f));

            if (timer < 34f)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(timer * 0.12f + seed) * 0.018f) * 0.992f;
                SpawnTravelEffects(0.42f);
                return;
            }

            NPC target = FindTarget();
            if (target != null)
            {
                float trackingPower = Utils.GetLerpValue(34f, 150f, timer, true);
                float speed = MathHelper.Lerp(11f, 24f, trackingPower);
                float inertia = MathHelper.Lerp(16f, 4.2f, trackingPower);

                Vector2 toTarget = target.Center - Projectile.Center;
                Vector2 baseDirection = toTarget.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                Vector2 curl = baseDirection.RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(timer * 0.17f + seed) * MathHelper.Lerp(7f, 2f, trackingPower);
                Vector2 desired = (toTarget + curl).SafeNormalize(baseDirection) * speed;

                Projectile.velocity = (Projectile.velocity * inertia + desired) / (inertia + 1f);
                if (timer % 21f == 0f)
                    Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(-0.18f, 0.18f) * (1f - trackingPower));
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(timer * 0.09f + seed) * 0.03f) * 0.985f;
            }

            if (Projectile.timeLeft < 55)
                Projectile.velocity *= 0.985f;

            SpawnTravelEffects(1f);
        }

        private NPC FindTarget()
        {
            NPC result = null;
            float bestDistance = 2400f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceToProjectile = Projectile.Distance(npc.Center);
                if (distanceToProjectile > 2500f)
                    continue;

                float mouseBias = Vector2.Distance(Main.MouseWorld, npc.Center) * 0.35f;
                float score = distanceToProjectile + mouseBias;
                if (score >= bestDistance)
                    continue;

                bestDistance = score;
                result = npc;
            }

            return result;
        }

        private void SpawnTravelEffects(float strength)
        {
            if (Main.rand.NextBool(3))
            {
                Vector2 velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.4f, 1.8f);
                Particle spark = new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.25f, 0.42f) * strength,
                    Color.Black);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(8))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Shadowflame,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f),
                    120,
                    Color.Black,
                    Main.rand.NextFloat(0.65f, 1.05f) * strength);
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.ai[0] < 18f ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ShadowboltReflect") { Volume = 0.2f, Pitch = -0.35f, PitchVariance = 0.14f, MaxInstances = 5 }, center);

            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 9f);
                Particle spark = new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.35f, 0.58f),
                    Color.Black);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 2; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextVector2Circular(1.1f, 1.1f),
                    Color.Black,
                    Main.rand.Next(20, 30),
                    Main.rand.NextFloat(0.28f, 0.46f),
                    0.35f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float width = 22f;
            if (completionRatio < 0.28f)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(0f, 0.28f, completionRatio, true));
            return width * Utils.GetLerpValue(1f, 0.74f, completionRatio, true);
        }

        private Color PrimitiveColorFunction(float completionRatio, Vector2 vertexPos)
        {
            return Color.Black * Utils.GetLerpValue(1f, 0.1f, completionRatio, true);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f);
                Particle spark = new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.32f, 0.62f),
                    Color.Black);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 8; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.4f),
                    Color.Black,
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.34f, 0.72f),
                    0.42f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            Vector2 overallOffset = Projectile.Size * 0.5f + Projectile.velocity * 1.2f;
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (_, _) => overallOffset, shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                48);
            return false;
        }
    }
}
