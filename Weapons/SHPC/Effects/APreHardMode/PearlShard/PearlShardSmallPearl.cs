using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode.PearlShard
{
    public class PearlShardSmallPearl : ModProjectile, ILocalizedModType
    {
        private const float HomingStartFrame = 24f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PearlShard/PearlShardParticle";

        private ref float FrameTimer => ref Projectile.ai[1];

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
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            FrameTimer += 1f / (Projectile.extraUpdates + 1f);

            HomingAI();

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.28f, 0.18f, 0.26f));

            if (Main.rand.NextFloat() < 0.35f)
                PearlShardVisuals.SpawnPearlParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.12f), 0.18f, 14);

            PearlShardVisuals.SpawnPearlGodTrail(Projectile, 0.5f);
        }

        private void HomingAI()
        {
            NPC target = FindTarget();
            if (FrameTimer < HomingStartFrame)
            {
                Projectile.velocity *= 0.99f;
                return;
            }

            float homingTimer = FrameTimer - HomingStartFrame;
            if (target == null)
            {
                Projectile.velocity *= 1.006f;
                return;
            }

            float loosen = Utils.GetLerpValue(0f, 62f, homingTimer, true);
            float closeLoosen = Utils.GetLerpValue(220f, 42f, Projectile.Distance(target.Center), true);
            float power = MathHelper.Clamp(loosen + closeLoosen * 0.55f, 0f, 1f);
            float speed = MathHelper.Lerp(6.48f, 17.55f, power);
            float turnLimit = MathHelper.ToRadians(MathHelper.Lerp(3f, 31f, power));
            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;

            float currentSpeed = MathHelper.Lerp(Projectile.velocity.Length(), speed, MathHelper.Lerp(0.05f, 0.26f, power));
            float rotation = Projectile.velocity.ToRotation().AngleTowards(desired.ToRotation(), turnLimit);
            Projectile.velocity = rotation.ToRotationVector2() * currentSpeed;
        }

        private NPC FindTarget()
        {
            NPC bestTarget = null;
            float bestDistance = 1280f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override bool? CanDamage()
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            PearlShardVisuals.SpawnBurst(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 0.75f, 1.5f, 1.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            PearlShardVisuals.DrawPearl(Projectile, 0.665f);
            return false;
        }
    }
}
