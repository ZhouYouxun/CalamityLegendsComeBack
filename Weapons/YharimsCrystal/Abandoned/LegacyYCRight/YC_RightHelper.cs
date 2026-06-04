using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using YCLeft = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    internal static class YC_RightHelper
    {
        public static bool TryGetHoldout(int owner, int holdoutIndex, out Projectile holdoutProjectile, out YC_RightHoldOut holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (holdoutIndex >= 0 && holdoutIndex < Main.maxProjectiles)
            {
                Projectile indexedHoldout = Main.projectile[holdoutIndex];
                if (indexedHoldout.active &&
                    indexedHoldout.owner == owner &&
                    indexedHoldout.type == ModContent.ProjectileType<YC_RightHoldOut>() &&
                    indexedHoldout.ModProjectile is YC_RightHoldOut indexedMod)
                {
                    holdoutProjectile = indexedHoldout;
                    holdout = indexedMod;
                    return true;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner || other.type != ModContent.ProjectileType<YC_RightHoldOut>())
                    continue;

                if (other.ModProjectile is YC_RightHoldOut holdoutMod)
                {
                    holdoutProjectile = other;
                    holdout = holdoutMod;
                    return true;
                }
            }

            return false;
        }

        public static NPC FindTargetAhead(Projectile requester, Vector2 source, Vector2 forwardDirection, float maxDistance, float coneDegrees, bool requireLineOfSight = true)
        {
            float maxDistanceSquared = maxDistance * maxDistance;
            float maxAngle = MathHelper.ToRadians(coneDegrees) * 0.5f;
            NPC nearest = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(requester))
                    continue;

                Vector2 toNpc = npc.Center - source;
                float distanceSquared = toNpc.LengthSquared();
                if (distanceSquared > maxDistanceSquared)
                    continue;

                float angleDifference = System.Math.Abs(MathHelper.WrapAngle(forwardDirection.ToRotation() - toNpc.ToRotation()));
                if (angleDifference > maxAngle)
                    continue;

                if (requireLineOfSight && !Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        public static void EmitRightDust(Vector2 center, Vector2 motion, Color color, float scale = 1f, int dustType = Terraria.ID.DustID.GoldFlame)
        {
            Dust dust = Dust.NewDustPerfect(center, dustType, motion, 0, color, scale);
            dust.noGravity = true;
        }

        public static bool IsOwnedRightProjectileType(int projectileType)
        {
            return projectileType == ModContent.ProjectileType<YC_Right_Drone>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_LaserCruiser>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_Battleship>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_RepairShip>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_RepairShield>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_TrackerLaser>() ||
                   projectileType == ModContent.ProjectileType<YC_Right_HeavyBolt>() ||
                   projectileType == ModContent.ProjectileType<YCLeft.YC_Left_RepairBolt>() ||
                   projectileType == ModContent.ProjectileType<YC_WarshipPulse>() ||
                   projectileType == ModContent.ProjectileType<YC_WarshipMissile>() ||
                   projectileType == ModContent.ProjectileType<YC_WarshipArtilleryShell>();
        }
    }
}
