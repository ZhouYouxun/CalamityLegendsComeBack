using CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive;

internal sealed class SHPCAutoTurretPlayer : ModPlayer
{
    private int deployTimer;

    public override void UpdateDead() => deployTimer = 0;

    public override void PostUpdate()
    {
        // Server authority matches the existing drop-pod -> turret deployment path.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        if (!Main.hardMode || Player.dead || !Player.active ||
            Player.HeldItem.type != ModContent.ItemType<NewLegendSHPC>() ||
            !(Player.ZoneOverworldHeight || Player.ZoneSkyHeight) || !HasEnemyNearby())
        {
            deployTimer = 0;
            return;
        }
        if (++deployTimer < 180)
            return;
        deployTimer = 0;
        if (MilitaryTurretUtility.CountOwnedCalls(Player) >= MilitaryTurretUtility.MaxTurrets ||
            !TryFindSite(out Vector2 site))
            return;

        int damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(
            new BalanceSHPC().GetTurretRightClickBaseDamage());
        int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem),
            site - new Vector2(0f, 780f), new Vector2(0f, 17f),
            ModContent.ProjectileType<MilitaryTurretDropPod>(), 0, 0f, Player.whoAmI,
            (float)MilitaryTurretUtility.SelectBiomeTurret(Player), damage, site.Y);
        if (index >= 0 && index < Main.maxProjectiles)
        {
            Main.projectile[index].CritChance = Player.GetWeaponCrit(Player.HeldItem);
            Main.projectile[index].netUpdate = true;
        }
    }

    private bool HasEnemyNearby()
    {
        foreach (NPC enemy in Main.ActiveNPCs)
            if (enemy.CanBeChasedBy() && Vector2.DistanceSquared(enemy.Center, Player.Center) <= 1200f * 1200f)
                return true;
        return false;
    }

    private bool TryFindSite(out Vector2 site)
    {
        // Search nearby ground rather than dropping onto the player's current position.
        for (int distance = 6; distance <= 36; distance += 2)
        for (int side = -1; side <= 1; side += 2)
        {
            int x = (int)(Player.Center.X / 16f) + distance * side;
            int top = (int)(Player.Bottom.Y / 16f) - 8;
            for (int y = top; y <= top + 28; y++)
            {
                if (!WorldGen.InWorld(x - 2, y, 10) || !WorldGen.InWorld(x + 2, y, 10))
                    continue;
                bool ground = true;
                for (int dx = -1; dx <= 1; dx++)
                {
                    Tile tile = Framing.GetTileSafely(x + dx, y);
                    ground &= tile.HasTile && !tile.IsActuated &&
                        (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                }
                Vector2 point = new(x * 16f + 8f, y * 16f);
                Rectangle body = new((int)point.X - 27, (int)point.Y - 36, 54, 36);
                Rectangle playerSpace = Player.Hitbox;
                playerSpace.Inflate(48, 32);
                if (!ground || body.Intersects(playerSpace) ||
                    Collision.SolidCollision(body.TopLeft(), body.Width, body.Height) ||
                    Framing.GetTileSafely(x, y - 1).LiquidAmount > 0 ||
                    !MilitaryTurretUtility.CanDeployTurret(Player, point, out _))
                    continue;

                bool reserved = false;
                foreach (Projectile pending in Main.ActiveProjectiles)
                    if (pending.owner == Player.whoAmI && pending.type == ModContent.ProjectileType<MilitaryTurretDropPod>() &&
                        Vector2.Distance(new Vector2(pending.Center.X, pending.ai[2]), point) < MilitaryTurretUtility.MinSpacing)
                        reserved = true;
                if (reserved)
                    continue;
                site = point;
                return true;
            }
        }
        site = Vector2.Zero;
        return false;
    }
}
