using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_MLD_MultiLinkDistributor
{
    public class S_MLD_MultiLinkDistributorPlayer : ModPlayer
    {
        public bool S_MLD_MultiLinkDistributorEquipped;

        public override void ResetEffects()
        {
            S_MLD_MultiLinkDistributorEquipped = false;
        }
    }
}
