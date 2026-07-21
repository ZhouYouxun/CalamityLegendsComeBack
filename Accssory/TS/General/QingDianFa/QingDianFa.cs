using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class QingDianFa : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.QingDianFa;
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/青电法";

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HermesBoots)
                .AddIngredient(ItemID.FallenStar, 5)
                .AddIngredient(ItemID.GoldBar, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
