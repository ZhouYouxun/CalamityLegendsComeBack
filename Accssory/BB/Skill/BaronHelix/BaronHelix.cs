using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill.BaronHelix
{
    public class BaronHelix : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/Skill/BaronHelix/BaronHelix";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BBAccessoryPlayer>().BaronHelixEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.FragmentVortex, 9)
                .AddIngredient(ItemID.LunarBar, 6)
                .AddIngredient<Lumenyl>(6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
