using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin
{
    public sealed class VesuviusRightMeteorPlayer : ModPlayer
    {
        public const int MaxMeteors = 6;
        public readonly int[] MeteorSlots = new int[MaxMeteors];

        public int OrbitTimer;

        public override void Initialize()
        {
            ClearSlots();
        }

        public override void ResetEffects()
        {
            OrbitTimer++;
            ValidateSlots();
        }

        public override void UpdateDead()
        {
            KillOwnedMeteors();
            ClearSlots();
        }

        public int RegisterMeteor(int projectileIndex)
        {
            ValidateSlots();
            for (int i = 0; i < MeteorSlots.Length; i++)
            {
                if (MeteorSlots[i] == projectileIndex)
                    return i;
            }

            for (int i = 0; i < MeteorSlots.Length; i++)
            {
                if (MeteorSlots[i] == -1)
                {
                    MeteorSlots[i] = projectileIndex;
                    return i;
                }
            }

            return -1;
        }

        public int ActiveMeteorCount(bool requireReleased = false)
        {
            ValidateSlots();
            int count = 0;
            int meteorType = ModContent.ProjectileType<VesuviusRightReturningMeteor>();
            foreach (int projectileIndex in MeteorSlots)
            {
                if (!Main.projectile.IndexInRange(projectileIndex))
                    continue;

                Projectile projectile = Main.projectile[projectileIndex];
                if (!projectile.active || projectile.owner != Player.whoAmI || projectile.type != meteorType)
                    continue;

                if (requireReleased && projectile.ModProjectile is VesuviusRightReturningMeteor meteor && !meteor.ReadyForVolley)
                    continue;

                count++;
            }

            return count;
        }

        public bool HasFullReadyVolley() => ActiveMeteorCount(true) >= MaxMeteors;

        public void ClearSlot(int slot, int projectileIndex)
        {
            if ((uint)slot >= MeteorSlots.Length)
                return;

            if (MeteorSlots[slot] == projectileIndex)
                MeteorSlots[slot] = -1;
        }

        public static bool TryCreateMeteor(IEntitySource source, Player owner, Vector2 center, Vector2 inheritedVelocity, int damage, float knockBack, int stage)
        {
            VesuviusRightMeteorPlayer meteorPlayer = owner.GetModPlayer<VesuviusRightMeteorPlayer>();
            if (meteorPlayer.ActiveMeteorCount(false) >= MaxMeteors)
                return false;

            if (owner.whoAmI != Main.myPlayer)
                return false;

            int projectileIndex = Projectile.NewProjectile(
                source,
                center,
                inheritedVelocity.SafeNormalize(Vector2.UnitY) * 5f,
                ModContent.ProjectileType<VesuviusRightReturningMeteor>(),
                damage,
                knockBack,
                owner.whoAmI,
                0f,
                -1f,
                stage);

            return Main.projectile.IndexInRange(projectileIndex);
        }

        public static bool TryReleaseFullVolley(Player owner, Vector2 direction, int damage, float knockBack)
        {
            VesuviusRightMeteorPlayer meteorPlayer = owner.GetModPlayer<VesuviusRightMeteorPlayer>();
            if (!meteorPlayer.HasFullReadyVolley())
                return false;

            Vector2 safeDirection = direction.SafeNormalize(Vector2.UnitX * owner.direction);
            int launched = 0;
            for (int i = 0; i < meteorPlayer.MeteorSlots.Length; i++)
            {
                int projectileIndex = meteorPlayer.MeteorSlots[i];
                if (!Main.projectile.IndexInRange(projectileIndex))
                    continue;

                Projectile projectile = Main.projectile[projectileIndex];
                if (!projectile.active || projectile.owner != owner.whoAmI || projectile.ModProjectile is not VesuviusRightReturningMeteor meteor)
                    continue;

                Vector2 spreadDirection = safeDirection.RotatedBy(MathHelper.Lerp(-0.2f, 0.2f, i / (float)(MaxMeteors - 1)));
                meteor.LaunchFromOrbit(spreadDirection, damage, knockBack, launched);
                meteorPlayer.MeteorSlots[i] = -1;
                launched++;
            }

            return launched == MaxMeteors;
        }

        private void ValidateSlots()
        {
            int meteorType = ModContent.ProjectileType<VesuviusRightReturningMeteor>();
            for (int i = 0; i < MeteorSlots.Length; i++)
            {
                int projectileIndex = MeteorSlots[i];
                if (!Main.projectile.IndexInRange(projectileIndex))
                {
                    MeteorSlots[i] = -1;
                    continue;
                }

                Projectile projectile = Main.projectile[projectileIndex];
                if (!projectile.active || projectile.owner != Player.whoAmI || projectile.type != meteorType)
                    MeteorSlots[i] = -1;
            }
        }

        private void KillOwnedMeteors()
        {
            int meteorType = ModContent.ProjectileType<VesuviusRightReturningMeteor>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == meteorType)
                    projectile.Kill();
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < MeteorSlots.Length; i++)
                MeteorSlots[i] = -1;
        }
    }
}
