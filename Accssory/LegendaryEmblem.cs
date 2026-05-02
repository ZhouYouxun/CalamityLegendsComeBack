using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal class LegendaryEmblem : ModItem
    {
        public new string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = Item.buyPrice(0, 1);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<LegendaryEmblemPlayer>().EXAccessoryEquipped = true;
        }

        public override void AddRecipes()
        {
            RegisterRecipe(ItemID.GoldBar);
            RegisterRecipe(ItemID.PlatinumBar);
        }

        private void RegisterRecipe(int barType)
        {
            CreateRecipe()
                .AddIngredient(ItemID.ItemFrame)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(barType, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
