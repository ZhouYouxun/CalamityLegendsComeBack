using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    internal static class YC_LeftSquadronHelper
    {
        public static bool TryGetHoldout(int owner, int holdoutIndex, out Projectile holdoutProjectile, out YC_LeftHoldOut holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (holdoutIndex >= 0 && holdoutIndex < Main.maxProjectiles)
            {
                Projectile indexedHoldout = Main.projectile[holdoutIndex];
                if (indexedHoldout.active &&
                    indexedHoldout.owner == owner &&
                    indexedHoldout.type == ModContent.ProjectileType<YC_LeftHoldOut>() &&
                    indexedHoldout.ModProjectile is YC_LeftHoldOut indexedMod)
                {
                    holdoutProjectile = indexedHoldout;
                    holdout = indexedMod;
                    return true;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner || other.type != ModContent.ProjectileType<YC_LeftHoldOut>())
                    continue;

                if (other.ModProjectile is YC_LeftHoldOut holdoutMod)
                {
                    holdoutProjectile = other;
                    holdout = holdoutMod;
                    return true;
                }
            }

            return false;
        }

        public static NPC FindPriorityTarget(Player owner, Vector2 source, float maxDistance, Vector2? coneDirection = null, float coneDegrees = 180f, bool requireLineOfSight = true)
        {
            float maxDistanceSquared = maxDistance * maxDistance;
            float maxAngle = MathHelper.ToRadians(coneDegrees);

            if (owner.HasMinionAttackTargetNPC)
            {
                NPC forcedTarget = Main.npc[owner.MinionAttackTargetNPC];
                if (IsValidTarget(forcedTarget, source, maxDistanceSquared, coneDirection, maxAngle, requireLineOfSight))
                    return forcedTarget;
            }

            NPC nearest = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!IsValidTarget(npc, source, maxDistanceSquared, coneDirection, maxAngle, requireLineOfSight))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(source, npc.Center);
                if (distanceSquared >= maxDistanceSquared)
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        public static bool IsLeftOwnedProjectileType(int projectileType)
        {
            return projectileType == ModContent.ProjectileType<YC_Left_Frigate>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_LaserCruiser>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_MissileBattleship>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_RepairShip>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_Rocket>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_FakeLazer>() ||
                   projectileType == ModContent.ProjectileType<YC_Left_RepairBolt>();
        }

        public static void EmitTechDust(Vector2 center, Vector2 motion, Color color, float scale = 1f, int dustType = DustID.RainbowTorch)
        {
            Dust dust = Dust.NewDustPerfect(
                center,
                dustType,
                motion,
                0,
                color,
                scale);
            dust.noGravity = true;
        }

        private static bool IsValidTarget(NPC npc, Vector2 source, float maxDistanceSquared, Vector2? coneDirection, float maxAngle, bool requireLineOfSight)
        {
            if (npc == null || !npc.CanBeChasedBy())
                return false;

            Vector2 toTarget = npc.Center - source;
            if (toTarget.LengthSquared() > maxDistanceSquared)
                return false;

            if (coneDirection.HasValue)
            {
                float angleDifference = System.Math.Abs(MathHelper.WrapAngle(coneDirection.Value.ToRotation() - toTarget.ToRotation()));
                if (angleDifference > maxAngle * 0.5f)
                    return false;
            }

            if (requireLineOfSight && !Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
                return false;

            return true;
        }
    }
}
