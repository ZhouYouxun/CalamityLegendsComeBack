using CalamityMod.Items.Weapons.Rogue;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    public class TideWiseHat : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/贴图/潮汐智者";

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
            player.GetModPlayer<BBAccessoryPlayer>().TideWiseHatEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<DukesDecapitator>()
                .AddIngredient(ItemID.PirateHat)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
