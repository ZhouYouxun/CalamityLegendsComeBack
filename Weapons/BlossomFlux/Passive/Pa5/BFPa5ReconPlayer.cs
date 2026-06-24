using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal sealed class BFPa5ReconPlayer : ModPlayer
    {
        public override void PostUpdateEquips()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return;

            Player.wingTimeMax = (int)(Player.wingTimeMax * 1.5f);
        }

        public override void PostUpdateMiscEffects()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return;

            // Apply a speed floor after equipment and debuffs have modified the player.
            // This deliberately avoids a brittle per-buff allowlist: any source that would
            // reduce movement below the normal value is neutralised, while positive bonuses stay.
            Player.moveSpeed = System.Math.Max(Player.moveSpeed, 1f) + 0.25f;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return;

            Player.maxRunSpeed = System.Math.Max(Player.maxRunSpeed, 3.5f) * 1.25f;
            Player.accRunSpeed = System.Math.Max(Player.accRunSpeed, 3.5f) * 1.25f;
            Player.runAcceleration = System.Math.Max(Player.runAcceleration, 0.08f) * 2f;
            Player.runSlowdown = System.Math.Max(Player.runSlowdown, 0.08f) * 2f;
        }

        public override void PostUpdate()
        {
            if (BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                TryReleasePlatformGrapple();
        }

        private void TryReleasePlatformGrapple()
        {
            for (int i = 0; i < Player.grapCount; i++)
            {
                int projectileIndex = Player.grappling[i];
                if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
                    continue;

                Projectile hook = Main.projectile[projectileIndex];
                if (!BFPa5ReconGlobalProjectile.IsGrappleProjectile(hook) || Vector2.DistanceSquared(Player.Center, hook.Center) > 30f * 30f)
                    continue;

                if (HookedToPlatformOrSlope(hook.Center))
                {
                    Player.RemoveAllGrapplingHooks();
                    return;
                }
            }
        }

        private static bool HookedToPlatformOrSlope(Vector2 worldPosition)
        {
            Point tilePoint = worldPosition.ToTileCoordinates();
            Tile tile = Framing.GetTileSafely(tilePoint.X, tilePoint.Y);
            return tile.HasTile && (TileID.Sets.Platforms[tile.TileType] || tile.IsHalfBlock || tile.Slope != SlopeType.Solid);
        }

    }

    internal sealed class BFPa5ReconGlobalProjectile : GlobalProjectile
    {
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!IsGrappleProjectile(projectile) || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player owner = Main.player[projectile.owner];
            if (BFPa5PassiveSystem.IsActive(owner, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                projectile.velocity *= 1.3f;
        }

        public override void GrappleRetreatSpeed(Projectile projectile, Player player, ref float speed)
        {
            if (BFPa5PassiveSystem.IsActive(player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                speed *= 1.3f;
        }

        public override void GrapplePullSpeed(Projectile projectile, Player player, ref float speed)
        {
            if (BFPa5PassiveSystem.IsActive(player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                speed *= 1.3f;
        }

        public static bool IsGrappleProjectile(Projectile projectile)
        {
            return projectile.active &&
                projectile.aiStyle == ProjAIStyleID.Hook &&
                projectile.owner >= 0 &&
                projectile.owner < Main.maxPlayers;
        }
    }
}
