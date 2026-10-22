using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle;

public sealed class AMRRangedStealthProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool rangedWeaponSource;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (projectile.owner != Main.myPlayer || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        bool shot = source is EntitySource_ItemUse use && use.Item.DamageType.CountsAsClass(DamageClass.Ranged);
        if (source is EntitySource_Parent parent && parent.Entity is Projectile controller)
        {
            rangedWeaponSource = controller.GetGlobalProjectile<AMRRangedStealthProjectile>().rangedWeaponSource;
            // Non-damaging gun controllers create the real shot later. Children of a
            // damaging projectile already inherit its damage and must not multiply again.
            shot = rangedWeaponSource && !controller.friendly;
        }
        rangedWeaponSource |= shot;

        if (!shot || !projectile.friendly || projectile.damage <= 0 ||
            !projectile.DamageType.CountsAsClass(DamageClass.Ranged))
            return;

        if (!Main.player[projectile.owner].GetModPlayer<AMRRangedStealthPlayer>().RecordShot())
            return;

        projectile.damage *= AMRRangedStealthPlayer.ShotDamageMultiplier;
        projectile.originalDamage = projectile.damage;
        projectile.netUpdate = true;
    }
}
