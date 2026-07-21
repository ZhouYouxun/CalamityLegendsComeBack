using CalamityMod.Items.Materials;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class QingTingJue : AzureThunderDashAccessory
    {
        protected override AzureThunderDashTier DashTier => AzureThunderDashTier.QingTingJue;
        protected override int FlightTime => 200;
        protected override float FlightSpeed => 10f;
        protected override float FlightAcceleration => 2.6f;
        public override string Texture => "CalamityLegendsComeBack/Accssory/TS/图片放这里/青霆诀";

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<JiDianFa>()
                .AddIngredient<Lumenyl>(8)
                .AddIngredient(ItemID.SpectreBar, 10)
                .AddIngredient(ItemID.Ectoplasm, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
