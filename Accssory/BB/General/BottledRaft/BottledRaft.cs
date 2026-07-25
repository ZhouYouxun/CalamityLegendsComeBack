using CalamityMod.Items.Placeables.SunkenSea;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledRaft : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/General/BottledRaft/BottledRaft";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.TsunamiInABottle)
                .AddIngredient(ItemID.Wood, 5)
                .AddIngredient(ItemID.Silk, 2)
                .AddIngredient<SeaPrism>(10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
