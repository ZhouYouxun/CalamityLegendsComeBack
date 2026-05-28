using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Plates;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ExpansionCanisterI
{
    public class ExpansionCanisterI : ModItem
    {
        //public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/f9a34c3aeaeb1d4cc5f58239e584b706";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<ExpansionCanisterIPlayer>().ExpansionCanisterIEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<DubiousPlating>(5)
                .AddIngredient<MysteriousCircuitry>(5)
                .AddIngredient<Navyplate>(15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
