using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.SurgeChainReactor
{
    public class SurgeChainReactor : ModItem
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
            player.GetModPlayer<BBAccessoryPlayer>().SurgeChainReactorEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(4)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient(ItemID.SoulofFright, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
