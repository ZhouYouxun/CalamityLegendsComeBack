using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod.Items.Weapons.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.ChangeZS
{
    [Autoload(false)]
    public sealed class SwordsplosionQuiver : ModItem
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Swordsplosion";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 16);
            Item.rare = ItemRarityID.Purple;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().SwordsplosionQuiverEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<Swordsplosion>()
                .AddIngredient(ItemID.MagicQuiver)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
