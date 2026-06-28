using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod.Items.Weapons.Ranged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.ChangeZS
{
    [Autoload(false)]
    public sealed class BlightSpewerQuiver : ModItem
    {
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/BlightSpewer";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 14);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().BlightSpewerQuiverEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BlightSpewer>()
                .AddIngredient(ItemID.MagicQuiver)
                .AddIngredient(ItemID.ChlorophyteBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
