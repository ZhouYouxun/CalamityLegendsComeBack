namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14MuzzleData
    {
        public static readonly MK14AttachmentDefinition[] Entries =
        {
            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.None,
                "Muzzle.None",
                BalanceMK14EBR.StagePlantera),

            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.UniversalSuppressor,
                "Muzzle.UniversalSuppressor",
                BalanceMK14EBR.StageGolem,
                aggroReduction: 1000,
                sustainedFireDamageRamp: true),

            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.UniversalBrake,
                "Muzzle.UniversalBrake",
                BalanceMK14EBR.StagePlaguebringer,
                sustainedFireDamageRamp: true),

            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.HeavySuppressor,
                "Muzzle.HeavySuppressor",
                BalanceMK14EBR.StageMoonLord,
                projectileSpeedMultiplier: 1.2f,
                infinitePenetrationFrames: 5,
                aggroReduction: 2000),

            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.SpiderBrake,
                "Muzzle.SpiderBrake",
                BalanceMK14EBR.StagePolterghast,
                spiderSlowOnHit: true),

            new(
                MK14AttachmentSlot.Muzzle,
                (int)MK14Muzzle.HeavyCompensator,
                "Muzzle.HeavyCompensator",
                BalanceMK14EBR.StageYharon,
                forceSingleHitAndDoubleStrike: true)
        };
    }
}
