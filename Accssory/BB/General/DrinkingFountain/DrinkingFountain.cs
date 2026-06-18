using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class DrinkingFountain : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/General/DrinkingFountain/DrinkingFountain";

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
            player.GetModPlayer<BBAccessoryPlayer>().DrinkingFountainEquipped = true;
        }
    }
}
