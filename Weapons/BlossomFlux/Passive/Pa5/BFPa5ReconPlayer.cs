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
            ApplyMovementDebuffImmunities();
        }

        public override void PostUpdateMiscEffects()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return;

            Player.moveSpeed = System.Math.Max(Player.moveSpeed, 1.25f);
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                return;

            Player.maxRunSpeed *= 1.25f;
            Player.accRunSpeed *= 1.25f;
            Player.runAcceleration *= 2f;
            Player.runSlowdown *= 2f;
        }

        public override void PostUpdate()
        {
            if (BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_CDetec))
                TryReleasePlatformGrapple();
        }

        private void ApplyMovementDebuffImmunities()
        {
            Player.buffImmune[BuffID.Slow] = true;
            Player.buffImmune[BuffID.Chilled] = true;
            Player.buffImmune[BuffID.Frozen] = true;
            Player.buffImmune[BuffID.Webbed] = true;
            Player.buffImmune[BuffID.Stoned] = true;
            TrySetBuffImmune("WindPushed");
            TrySetBuffImmune("VortexDebuff");
            TrySetBuffImmune("Obstructed");
            TrySetBuffImmune("Distorted");
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

        private void TrySetBuffImmune(string buffName)
        {
            if (!ModContent.TryFind("Terraria/" + buffName, out ModBuff buff))
                return;

            if (buff.Type > 0 && buff.Type < Player.buffImmune.Length)
                Player.buffImmune[buff.Type] = true;
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
