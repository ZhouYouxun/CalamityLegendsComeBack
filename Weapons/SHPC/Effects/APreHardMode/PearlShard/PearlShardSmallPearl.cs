using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode.PearlShard
{
    public class PearlShardSmallPearl : ModProjectile, ILocalizedModType
    {
        private const float HomingRange = 1280f;
        private const float MaxSpeed = 19.5f;

        // 分裂弹幕追踪前飞行距离减半 (originally ~24 frames, halved is 12 frames/AI calls)
        private const int HomingDelay = 12;

        private const float HomingInertia = 16f; // 惯性越小转向越快 (Briny was 27f, so 16f is faster)
        private const float FreeFlightDamping = 0.99f; // 有一个0.99的减速
        private const float NoTargetDamping = 0.99f;
        private const float WanderingTurnStrength = 0.006f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PearlShard/PearlShardParticle";

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 23;
            Projectile.height = 23;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300; // 持续时间翻2.5倍 (originally 120)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            timer++;

            HomeTowardTarget();

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.28f, 0.18f, 0.26f));

            if (Main.rand.NextFloat() < 0.35f)
                PearlShardVisuals.SpawnPearlParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.12f), 0.18f, 14);

            PearlShardVisuals.SpawnPearlGodTrail(Projectile, 0.5f);
        }

        private void HomeTowardTarget()
        {
            if (timer <= HomingDelay)
            {
                FreeDrift();
                return;
            }

            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();

            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 4f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(Vector2.UnitX));

            // 刚过启动延迟时只轻轻拉一下，随后才逐渐变得更愿意追踪。
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 36f, timer, true);

            // 离敌人很近时稍微积极一点，避免它在敌人旁边绕半天不命中。
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));

            float targetSpeed = MathHelper.Lerp(10.5f, MaxSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            // “半追踪感”的核心：用惯性慢慢拽过去。
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            // 保留一点点游移，不让轨迹变得过于机械。
            float sideSway = (float)Math.Sin((timer + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            // 限速，避免多次惯性叠加后速度过高。
            if (Projectile.velocity.Length() > MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxSpeed;
        }

        private void FreeDrift(float damping = FreeFlightDamping)
        {
            float wander = (float)Math.Sin((timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        public override bool? CanDamage()
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            PearlShardLargePearl.PlayBreakSound(Projectile.Center, 0.72f);
            PearlShardVisuals.SpawnBurst(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 0.75f, 1.5f, 1.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            PearlShardVisuals.DrawPearl(Projectile, 0.665f);
            return false;
        }
    }
}
