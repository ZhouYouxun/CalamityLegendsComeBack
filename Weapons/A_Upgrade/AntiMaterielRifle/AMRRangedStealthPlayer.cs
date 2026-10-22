using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle;

public sealed class AMRRangedStealthPlayer : ModPlayer
{
    public const int FullChargeFrames = 300;
    public const int ShotDamageMultiplier = 5;
    internal bool Equipped;
    internal int ChargeFrames;
    private ulong lastShotTick = ulong.MaxValue;
    private ulong empoweredShotTick = ulong.MaxValue;

    internal bool HoldingRanged => !Player.HeldItem.IsAir &&
        Player.HeldItem.DamageType.CountsAsClass(DamageClass.Ranged);
    internal bool ShowBar => Equipped && !Player.dead && HoldingRanged;
    internal float Completion => ChargeFrames / (float)FullChargeFrames;

    public override void ResetEffects() => Equipped = false;

    public override void UpdateDead() => ClearCharge();

    private void ClearCharge()
    {
        ChargeFrames = 0;
        lastShotTick = empoweredShotTick = ulong.MaxValue;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;
        if (!Equipped || Player.dead || !HoldingRanged)
        {
            ClearCharge();
            return;
        }

        // AMR keeps a use animation alive merely while aiming. Only actual shots
        // and left firing interrupt this passive; aiming by itself is not an attack.
        bool attackAnimation = Player.itemAnimation > 0 &&
            Player.HeldItem.type != ModContent.ItemType<NewLegendAntiMaterielRifle>();
        bool attacking = attackAnimation || Player.controlUseItem ||
            lastShotTick == Main.GameUpdateCount;
        if (attacking)
        {
            // A ready shot remains armed until an actual projectile is fired;
            // holding fire without ammunition must not silently consume it.
            if (ChargeFrames < FullChargeFrames)
                ChargeFrames = 0;
            return;
        }
        if (ChargeFrames < FullChargeFrames)
            ChargeFrames++;
    }

    internal bool RecordShot()
    {
        if (!Equipped || Player.dead || !HoldingRanged)
            return false;

        ulong tick = Main.GameUpdateCount;
        if (lastShotTick == tick)
            return empoweredShotTick == tick;

        bool empowered = ChargeFrames >= FullChargeFrames;
        ChargeFrames = 0;
        lastShotTick = tick;
        empoweredShotTick = empowered ? tick : ulong.MaxValue;
        return empowered;
    }
}
