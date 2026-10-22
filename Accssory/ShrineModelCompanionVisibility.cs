using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory;

// Luxor does not read hideVisual. Hide companion bodies without suppressing their AI,
// hit effects, or attacks. Frozen Cube's own flag still controls its ancillary effects.
public sealed class ShrineModelCompanionVisibility : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
        entity.type == ModContent.ProjectileType<Luxor>() ||
        entity.type == ModContent.ProjectileType<Elumphant>();

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return true;

        var shrine = Main.player[projectile.owner].GetModPlayer<ShrineModelPlayer>();
        return !shrine.Equipped || !shrine.HideVisual;
    }
}
