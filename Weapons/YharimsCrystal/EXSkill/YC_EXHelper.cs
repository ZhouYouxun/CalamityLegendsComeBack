using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YCRight = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    internal static class YC_EXHelper
    {
        public static bool TryGetActiveVip(int owner, out Projectile vipProjectile, out YC_EX_VIP vip)
        {
            vipProjectile = null;
            vip = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner || other.type != ModContent.ProjectileType<YC_EX_VIP>())
                    continue;

                if (other.ModProjectile is YC_EX_VIP vipMod)
                {
                    vipProjectile = other;
                    vip = vipMod;
                    return true;
                }
            }

            return false;
        }

        public static NPC FindNearestTarget(Projectile requester, Vector2 source, float maxDistance, bool requireLineOfSight = false)
        {
            float maxDistanceSquared = maxDistance * maxDistance;
            NPC nearest = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(requester))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(source, npc.Center);
                if (distanceSquared > maxDistanceSquared)
                    continue;

                if (requireLineOfSight && !Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        public static void EmitExDust(Vector2 center, Vector2 velocity, Color color, float scale = 1f, int dustType = DustID.GoldFlame)
        {
            Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 0, color, scale);
            dust.noGravity = true;
        }

        public static bool IsOwnedExWarshipType(int projectileType)
        {
            return projectileType == ModContent.ProjectileType<YC_EX_LaserDrone>() ||
                   projectileType == ModContent.ProjectileType<YC_EX_Drone>() ||
                   projectileType == ModContent.ProjectileType<YC_EX_LaserCruiser>() ||
                   projectileType == ModContent.ProjectileType<YC_EX_Battleship>();
        }

        public static bool IsOwnedExSupportProjectile(Projectile projectile)
        {
            return (projectile.type == ModContent.ProjectileType<YCRight.YC_Right_HeavyBolt>() && projectile.ai[1] == 1f) ||
                   (projectile.type == ModContent.ProjectileType<YC_WarshipMissile>() && projectile.ai[1] == 1f) ||
                   (projectile.type == ModContent.ProjectileType<YC_WarshipArtilleryShell>() && projectile.ai[1] == 1f) ||
                   (projectile.type == ModContent.ProjectileType<YC_WarshipPulse>() && projectile.ai[1] == 1f);
        }
    }
}
