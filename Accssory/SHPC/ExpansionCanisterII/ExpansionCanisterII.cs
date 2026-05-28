using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ExpansionCanisterIItem = CalamityLegendsComeBack.Accssory.SHPC.ExpansionCanisterI.ExpansionCanisterI;

namespace CalamityLegendsComeBack.Accssory.SHPC.ExpansionCanisterII
{
    public class ExpansionCanisterII : ModItem
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
            player.GetModPlayer<ExpansionCanisterIIPlayer>().ExpansionCanisterIIEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ExpansionCanisterIItem>()
                .AddIngredient(ItemID.CobaltBar, 10)
                .AddIngredient<EssenceofEleum>(10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
