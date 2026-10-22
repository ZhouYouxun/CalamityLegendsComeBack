using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;

// Redirect aim during weapon use without moving the player's visible cursor.
public sealed class CtrlChipWeaponAim : ModSystem
{
    public override void Load()
    {
        On_Player.ItemCheck += AimWeapon;
    }

    public override void Unload()
    {
        On_Player.ItemCheck -= AimWeapon;
    }

    private static void AimWeapon(On_Player.orig_ItemCheck orig, Player player)
    {
        CtrlChipPlayer state = player.GetModPlayer<CtrlChipPlayer>();
        if (player.whoAmI != Main.myPlayer || !state.CtrlChipEquipped || !state.ReticleHasTarget)
        {
            orig(player);
            return;
        }

        int mouseX = Main.mouseX, mouseY = Main.mouseY;
        Vector2 calamityMouse = player.Calamity().mouseWorldDeltaFromPlayer;
        try
        {
            Vector2 aimPoint = CtrlChipPlayer.GetFireAimWorld(player, state.ReticleWorld);
            Vector2 screenAim = aimPoint - Main.screenPosition;
            Main.mouseX = (int)screenAim.X;
            Main.mouseY = (int)screenAim.Y;
            player.Calamity().mouseWorldDeltaFromPlayer = aimPoint - player.MountedCenter;
            orig(player);
        }
        finally
        {
            Main.mouseX = mouseX;
            Main.mouseY = mouseY;
            player.Calamity().mouseWorldDeltaFromPlayer = calamityMouse;
        }
    }
}

public sealed class CtrlChipWeaponShots : GlobalItem
{
    public override void ModifyShootStats(Item item, Player player, ref Vector2 position,
        ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        CtrlChipPlayer state = player.GetModPlayer<CtrlChipPlayer>();
        if (player.whoAmI == Main.myPlayer && state.CtrlChipEquipped && state.ReticleHasTarget && velocity.LengthSquared() > 0f)
            velocity = (CtrlChipPlayer.GetFireAimWorld(player, state.ReticleWorld) - position).SafeNormalize(velocity.SafeNormalize(Vector2.UnitX)) * velocity.Length();
    }
}
