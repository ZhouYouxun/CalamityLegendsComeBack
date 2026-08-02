using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 左键荧光绿能量弹（改自 SHPC 的 EndlessDevourJavOrbSmall，换成荧光绿）：
    /// 先短暂游走再追踪光标附近的敌人。命中提升同步率并累积伸冤者印记。
    /// </summary>
    public class M4A1EnergyOrb : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 150;
        private const int MaxUpdateCount = 4;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
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
            Lighting.AddLight(Projectile.Center, 0.2f, 0.6f, 0.12f);

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
            float bestScore = 2400f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceToProjectile = Projectile.Distance(npc.Center);
                if (distanceToProjectile > 2500f)
                    continue;

                float mouseBias = Vector2.Distance(Main.MouseWorld, npc.Center) * 0.35f;
                float score = distanceToProjectile + mouseBias;
                if (score >= bestScore)
                    continue;

                bestScore = score;
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
                    M4A1Visuals.NeonGreen);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(8))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f),
                    100,
                    default,
                    Main.rand.NextFloat(0.65f, 1.05f) * strength);
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.ai[0] < 18f ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[Projectile.owner];
                bool isBoss = target.boss || NPCID.Sets.ShouldBeCountedAsBoss[target.type];
                M4A1Player.Get(owner).GainSync(isBoss, hit.Crit);
                M4A1MarkGlobalNPC.RegisterHit(target, owner, damageDone);
            }

            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item157 with { Volume = 0.25f, Pitch = -0.1f, PitchVariance = 0.14f, MaxInstances = 5 }, center);
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 9f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(16, 24), Main.rand.NextFloat(0.35f, 0.58f), M4A1Visuals.NeonGreen));
            }
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(14, 22), Main.rand.NextFloat(0.3f, 0.55f), M4A1Visuals.NeonGreen));
            }
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, M4A1Visuals.NeonGreen, 0.7f, 14, true));
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
            Color c = Color.Lerp(M4A1Visuals.NeonGreenBright, M4A1Visuals.NeonGreen, completionRatio);
            return c * Utils.GetLerpValue(1f, 0.1f, completionRatio, true);
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
