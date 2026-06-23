using Terraria.ID;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class JiDianFa : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.JiDianFa;
        public override string Texture => "Terraria/Images/Item_" + ItemID.LightningBoots;
    }
}
