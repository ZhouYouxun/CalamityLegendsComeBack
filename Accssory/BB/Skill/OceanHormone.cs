using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    public class OceanHormone : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/贴图/海洋激素";

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
            player.GetModPlayer<BBAccessoryPlayer>().OceanHormoneEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Bottle, 1)
                .AddIngredient(ItemID.BattlePotion, 6)
                .AddIngredient(ItemID.Stinger, 6)
                .AddIngredient<DepthCells>(6)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
