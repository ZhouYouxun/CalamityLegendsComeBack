namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14BarrelData
    {
        public static readonly MK14AttachmentDefinition[] Entries =
        {
            new(
                MK14AttachmentSlot.Barrel,
                (int)MK14Barrel.Long,
                "Barrel.Long",
                BalanceMK14EBR.StagePlantera,
                rpmOverride: 660,
                barrelBaseSpreadDegrees: 1f,
                barrelMaxSpreadDegrees: 5f,
                barrelSpreadRampEnd: 100),

            new(
                MK14AttachmentSlot.Barrel,
                (int)MK14Barrel.SniperHeavy,
                "Barrel.SniperHeavy",
                BalanceMK14EBR.StageProvidence,
                damageMultiplier: 1.2f,
                extraPenetration: 20,
                projectileScaleMultiplier: 1.7f,
                rpmOverride: 330,
                barrelBaseSpreadDegrees: 1f,
                barrelMaxSpreadDegrees: 5f,
                barrelSpreadRampEnd: 100),

            new(
                MK14AttachmentSlot.Barrel,
                (int)MK14Barrel.CQBShort,
                "Barrel.CQBShort",
                BalanceMK14EBR.StageDevourerOfGods,
                rpmOverride: 990,
                barrelBaseSpreadDegrees: 5f,
                barrelMaxSpreadDegrees: 15f,
                barrelSpreadRampEnd: 100)
        };
    }
}
