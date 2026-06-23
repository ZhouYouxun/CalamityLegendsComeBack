using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class QingDianFa : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.QingDianFa;
        public override string Texture => "Terraria/Images/Item_" + ItemID.Aglet;
    }
}
