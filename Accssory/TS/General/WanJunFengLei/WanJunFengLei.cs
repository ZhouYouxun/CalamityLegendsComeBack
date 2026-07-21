using CalamityMod.Items.Materials;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class WanJunFengLei : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.WanJunFengLei;
        protected override int FlightTime => 270;
        protected override float FlightSpeed => 11f;
        protected override float FlightAcceleration => 2.8f;
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/万钧风雷";

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<QingTingJue>()
                .AddIngredient<RuinousSoul>(5)
                .AddIngredient<UelibloomBar>(8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
