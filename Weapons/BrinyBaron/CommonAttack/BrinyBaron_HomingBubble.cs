using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_HomingBubble : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "Terraria/Images/Projectile_0";

        private const float HomingRange = 620f;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 95;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, 0.02f, 0.1f, 0.16f);
            HomeTowardTarget();
            SpawnBubbleGore();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Burst();
        }

        public override void OnKill(int timeLeft)
        {
            Burst();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        private void HomeTowardTarget()
        {
            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
            {
                Projectile.velocity *= 0.985f;
                return;
            }

            Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * 9.5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.055f);
        }

        private void SpawnBubbleGore()
        {
            if (Projectile.localAI[0] % 2f != 0f)
                return;

            float offset = (float)System.Math.Sin(Projectile.localAI[0] * 0.1f) * 1.5f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 bubblePos1 = Projectile.Center + direction.RotatedBy(MathHelper.PiOver2) * offset;
            Vector2 bubblePos2 = Projectile.Center + direction.RotatedBy(-MathHelper.PiOver2) * offset;
            Gore bubble1 = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), bubblePos1, Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f), 411);
            Gore bubble2 = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), bubblePos2, Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f), 411);
            bubble1.timeLeft = 8 + Main.rand.Next(6);
            bubble2.timeLeft = 8 + Main.rand.Next(6);
            bubble1.scale = Main.rand.NextFloat(0.6f, 1f);
            bubble2.scale = Main.rand.NextFloat(0.6f, 1f);
            bubble1.type = Main.rand.NextBool(3) ? 412 : 411;
            bubble2.type = Main.rand.NextBool(3) ? 412 : 411;
        }

        private void Burst()
        {
            for (int i = 0; i < 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    Main.rand.NextVector2Circular(2.6f, 2.6f),
                    100,
                    new Color(130, 230, 255),
                    Main.rand.NextFloat(0.75f, 1.05f));
                dust.noGravity = true;
            }
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
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
    }
}
