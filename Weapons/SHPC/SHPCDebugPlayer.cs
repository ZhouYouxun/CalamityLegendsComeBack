using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC;

public sealed class SHPCDebugPlayer : ModPlayer
{
    private HashSet<NewLegendSHPC> trackedWeapons = new();

    public override void PreUpdate()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;

        HashSet<NewLegendSHPC> currentWeapons = new();
        foreach (Item item in Player.inventory)
            if (item.ModItem is NewLegendSHPC weapon)
                currentWeapons.Add(weapon);
        foreach (Item item in Player.armor)
            if (item.ModItem is NewLegendSHPC weapon)
                currentWeapons.Add(weapon);
        if (Main.mouseItem.ModItem is NewLegendSHPC cursorWeapon)
            currentWeapons.Add(cursorWeapon);

        foreach (NewLegendSHPC weapon in trackedWeapons)
            if (!currentWeapons.Contains(weapon))
                weapon.SyncDebugMode(Player, false);
        foreach (NewLegendSHPC weapon in currentWeapons)
            weapon.SyncDebugMode(Player);
        trackedWeapons = currentWeapons;
    }
}
