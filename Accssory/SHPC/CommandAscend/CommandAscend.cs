using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.CommandAscend
{
    public sealed class CommandAscend : ModItem
    {
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
            player.GetModPlayer<CommandAscendPlayer>().CommandAscendEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(4)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient<BlissfulBombardier>()
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
