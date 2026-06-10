using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class RuinousSoul_GhastlyES : ModProjectile, ILocalizedModType
    {
        private const int PreludeFrames = 30;
        private const int FullLife = 240;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = FullLife;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 3;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanHitNPC(NPC target) => Timer > PreludeFrames && target.CanBeChasedBy(Projectile);

        public override void AI()
        {
            Timer++;

            if (Timer > FullLife)
            {
                Projectile.Kill();
                return;
            }

            NPC target = AcquireTarget();
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float phase = Projectile.ai[1] * 0.77f + Projectile.identity * 0.13f;

            if (Timer <= PreludeFrames)
                PreludeAI(target, forward, phase);
            else
                HomingAI(target, forward, phase);

            Projectile.rotation = Projectile.velocity.ToRotation();
            SpawnWhiteShardTrail(target);
        }

        private void PreludeAI(NPC target, Vector2 forward, float phase)
        {
            Projectile.velocity *= 0.965f;

            if (target is null)
                return;

            Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(forward);
            float sideSign = Projectile.ai[1] % 2f == 0f ? 1f : -1f;
            Vector2 tangent = toTarget.RotatedBy(MathHelper.PiOver2 * sideSign);
            float curl = (float)System.Math.Sin(Timer * 0.26f + phase);

            Projectile.velocity += tangent * curl * 0.28f;
            Projectile.velocity += toTarget * Utils.GetLerpValue(0f, PreludeFrames, Timer, true) * 0.08f;
        }

        private void HomingAI(NPC target, Vector2 forward, float phase)
        {
            if (target is null)
            {
                Projectile.velocity *= 0.993f;
                return;
            }

            float homingPower = Utils.GetLerpValue(PreludeFrames, 92f, Timer, true);
            Vector2 predictedCenter = target.Center + target.velocity * MathHelper.Lerp(4f, 16f, homingPower);
            Vector2 desired = (predictedCenter - Projectile.Center).SafeNormalize(forward);
            float remainingCurl = MathHelper.Lerp(0.42f, 0.035f, homingPower);
            desired = desired.RotatedBy((float)System.Math.Sin(Timer * 0.15f + phase) * remainingCurl);

            float targetSpeed = MathHelper.Lerp(9f, 17.5f, homingPower);
            float inertia = MathHelper.Lerp(24f, 3.2f, homingPower);
            Projectile.velocity = (Projectile.velocity * inertia + desired * targetSpeed) / (inertia + 1f);

            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 5f, 18.5f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(desired) * speed;
        }

        private NPC AcquireTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex))
            {
                NPC target = Main.npc[targetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            NPC best = null;
            float bestScore = 1250f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                float score = npc.boss ? distance * 0.72f : distance;
                if (score >= bestScore)
                    continue;

                best = npc;
                bestScore = score;
            }

            return best;
        }

        private void SpawnWhiteShardTrail(NPC target)
        {
            float fade = Utils.GetLerpValue(FullLife, 28f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    -forward * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    40,
                    Color.White,
                    Main.rand.NextFloat(0.58f, 1.05f) * fade);
                dust.noGravity = true;
            }

            if ((int)Timer % 2 == 0)
            {
                Particle spark = new GlowSparkParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(3f, 9f),
                    -forward * Main.rand.NextFloat(0.8f, 2.6f),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.06f, 0.11f) * fade,
                    Color.White,
                    new Vector2(1.1f, 0.28f),
                    true,
                    false,
                    1f);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Timer < PreludeFrames && Main.rand.NextBool(3))
            {
                float sideSign = Projectile.ai[1] % 2f == 0f ? 1f : -1f;
                Vector2 lineVelocity = (side * sideSign + forward * 0.25f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.5f, 3.8f);
                LineParticle line = new(
                    Projectile.Center + side * sideSign * Main.rand.NextFloat(4f, 18f),
                    lineVelocity,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.28f, 0.58f),
                    Color.White);
                GeneralParticleHandler.SpawnParticle(line);
            }

            if (target is not null && Timer == PreludeFrames)
            {
                for (int i = 0; i < 5; i++)
                {
                    LineParticle snap = new(
                        Projectile.Center,
                        Projectile.DirectionTo(target.Center).RotatedByRandom(0.28f) * Main.rand.NextFloat(3f, 7f),
                        false,
                        Main.rand.Next(16, 24),
                        Main.rand.NextFloat(0.42f, 0.74f),
                        Color.White);
                    GeneralParticleHandler.SpawnParticle(snap);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    Projectile.oldVelocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-0.12f, 0.34f) + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.2f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
