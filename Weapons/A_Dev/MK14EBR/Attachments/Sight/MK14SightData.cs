namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14SightData
    {
        public static readonly MK14AttachmentDefinition[] Entries =
        {
            new(
                MK14AttachmentSlot.Sight,
                (int)MK14Sight.RedDot,
                "Sight.RedDot",
                BalanceMK14EBR.StagePlantera,
                redDotRangeProfile: true),

            new(
                MK14AttachmentSlot.Sight,
                (int)MK14Sight.FireControl,
                "Sight.FireControl",
                BalanceMK14EBR.StageMoonLord,
                homing: true),

            new(
                MK14AttachmentSlot.Sight,
                (int)MK14Sight.Thermal,
                "Sight.Thermal",
                BalanceMK14EBR.StagePolterghast,
                nightDamageBonus: true),

            new(
                MK14AttachmentSlot.Sight,
                (int)MK14Sight.HighPower,
                "Sight.HighPower",
                BalanceMK14EBR.StageYharon,
                highPowerRangeProfile: true)
        };
    }
}
