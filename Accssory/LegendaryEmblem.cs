using Terraria;
using Terraria.ID;
using Terraria.Localization;
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
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.buyPrice(0, 1);
            Item.rare = ItemRarityID.LightRed;
        }

        public override bool CanUseItem(Player player)
        {
            if (!player.GetModPlayer<LegendaryEmblemPlayer>().PermanentEXUnlock)
                return true;

            if (player.whoAmI == Main.myPlayer)
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.LegendaryEmblem.AlreadyConsumed"));

            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && player.itemTime == 0)
            {
                player.itemTime = Item.useTime;
                player.GetModPlayer<LegendaryEmblemPlayer>().PermanentEXUnlock = true;
            }

            return true;
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
