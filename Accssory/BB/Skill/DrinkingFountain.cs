using Terraria;
using CalamityMod.Items.Weapons.Rogue;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    public class DrinkingFountain : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/贴图/饮水机";

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
            player.GetModPlayer<BBAccessoryPlayer>().DrinkingFountainEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.BottledWater, 100)
                .AddIngredient(ItemID.WaterBolt)
                .AddIngredient<Whitewater>()
                .AddTile(TileID.Sinks)
                .Register();
        }
    }
}
