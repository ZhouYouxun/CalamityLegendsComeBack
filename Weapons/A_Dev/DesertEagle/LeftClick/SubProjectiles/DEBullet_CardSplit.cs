using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    /// <summary>
    /// AcesHigh 梅花牌分裂出的追踪子弹（×3，±25°/0°散射后归巢）。
    /// </summary>
    public class DEBullet_CardSplit : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/ShockblastRound";

        private static readonly Color ClubPurple = new(160, 60, 255);
        private static readonly Color ClubLight = new(200, 120, 255);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            // 简单归巢
            NPC target = FindNearestNPC(350f);
            if (target != null && Projectile.Distance(target.Center) > 30f)
            {
                Vector2 desired = Projectile.DirectionTo(target.Center) * Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.1f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                -Projectile.velocity * 0.35f, 100, ClubPurple, 0.85f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, -Projectile.velocity * 0.12f,
                    false, 7, 0.016f, ClubLight, new Vector2(0.6f, 1.4f)));
            }
        }

        private NPC FindNearestNPC(float range)
        {
            NPC nearest = null;
            float nearestDist = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = Projectile.Distance(npc.Center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = npc;
                }
            }
            return nearest;
        }
    }
}
