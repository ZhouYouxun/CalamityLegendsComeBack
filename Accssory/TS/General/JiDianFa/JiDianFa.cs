using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class JiDianFa : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.JiDianFa;
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/疾电法";

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<QingDianFa>()
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddIngredient(ItemID.PixieDust, 15)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
