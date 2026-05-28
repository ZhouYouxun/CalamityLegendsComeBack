namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14StockData
    {
        public static readonly MK14AttachmentDefinition[] Entries =
        {
            new(
                MK14AttachmentSlot.Stock,
                (int)MK14Stock.EBR,
                "Stock.EBR",
                BalanceMK14EBR.StagePlantera),

            new(
                MK14AttachmentSlot.Stock,
                (int)MK14Stock.Heavy,
                "Stock.Heavy",
                BalanceMK14EBR.StagePlaguebringer),

            new(
                MK14AttachmentSlot.Stock,
                (int)MK14Stock.Skeleton,
                "Stock.Skeleton",
                BalanceMK14EBR.StageDevourerOfGods)
        };
    }
}
