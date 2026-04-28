using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_OVR_ChargeLimitBreaker
{
    public class S_OVR_ChargeLimitBreaker : ModItem
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
            player.GetModPlayer<S_OVR_ChargeLimitBreakerPlayer>().S_OVR_ChargeLimitBreakerEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(4)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient(ItemID.SoulofLight, 6)
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddIngredient(ItemID.Wire, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
