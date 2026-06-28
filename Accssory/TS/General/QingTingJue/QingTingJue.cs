using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class QingTingJue : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.QingTingJue;
        protected override int FlightTime => 200;
        protected override float FlightSpeed => 10f;
        protected override float FlightAcceleration => 2.6f;
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/青霆诀";
    }
}
