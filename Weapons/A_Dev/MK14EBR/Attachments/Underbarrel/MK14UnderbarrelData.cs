namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14UnderbarrelData
    {
        public static readonly MK14AttachmentDefinition[] Entries =
        {
            new(
                MK14AttachmentSlot.Underbarrel,
                (int)MK14Underbarrel.None,
                "Underbarrel.None",
                BalanceMK14EBR.StagePlantera),

            new(
                MK14AttachmentSlot.Underbarrel,
                (int)MK14Underbarrel.GrenadeLauncher,
                "Underbarrel.GrenadeLauncher",
                BalanceMK14EBR.StageMoonLord),

            new(
                MK14AttachmentSlot.Underbarrel,
                (int)MK14Underbarrel.DragonBreathShotgun,
                "Underbarrel.DragonBreathShotgun",
                BalanceMK14EBR.StageProvidence,
                dragonBreathMarker: true),

            new(
                MK14AttachmentSlot.Underbarrel,
                (int)MK14Underbarrel.FoldingBipod,
                "Underbarrel.FoldingBipod",
                BalanceMK14EBR.StageGolem,
                movementBipod: true),

            new(
                MK14AttachmentSlot.Underbarrel,
                (int)MK14Underbarrel.LaserPointer,
                "Underbarrel.LaserPointer",
                BalanceMK14EBR.StageDevourerOfGods,
                laserLocksSpread: true)
        };
    }
}
