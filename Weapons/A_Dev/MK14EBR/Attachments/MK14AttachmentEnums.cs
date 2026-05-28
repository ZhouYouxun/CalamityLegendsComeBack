namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal enum MK14AttachmentSlot
    {
        Barrel,
        Muzzle,
        Underbarrel,
        Stock,
        Sight
    }

    public enum MK14Barrel
    {
        Long = 0,
        SniperHeavy = 1,
        CQBShort = 2
    }

    public enum MK14Muzzle
    {
        None = 0,
        UniversalSuppressor = 1,
        UniversalBrake = 2,
        HeavySuppressor = 3,
        SpiderBrake = 4,
        HeavyCompensator = 5
    }

    public enum MK14Underbarrel
    {
        None = 0,
        GrenadeLauncher = 1,
        DragonBreathShotgun = 2,
        FoldingBipod = 3,
        LaserPointer = 4
    }

    public enum MK14Stock
    {
        EBR = 0,
        Heavy = 1,
        Skeleton = 2
    }

    public enum MK14Sight
    {
        RedDot = 0,
        FireControl = 1,
        Thermal = 2,
        HighPower = 3
    }
}
