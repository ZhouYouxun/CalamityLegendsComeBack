using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    public sealed class FairyDance : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/FairyDance/妖精舞";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 4;
            player.statDefense += 6;
            player.GetModPlayer<BFAccessoryPlayer>().FairyDanceEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<RainbowSpiritDance>() &&
                   incomingItem.type != ModContent.ItemType<RainbowSpiritDance>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.FairyCritterPink)
                .AddIngredient(ItemID.FairyCritterGreen)
                .AddIngredient(ItemID.FairyCritterBlue)
                .AddIngredient(ItemID.PixieDust, 20)
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
