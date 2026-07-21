using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDance
{
    public sealed class RainbowSpiritDance : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/FairyDance/虹灵";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 18);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 6;
            player.statDefense += 10;
            player.GetDamage(DamageClass.Ranged) += 0.10f;

            BFAccessoryPlayer accessoryPlayer = player.GetModPlayer<BFAccessoryPlayer>();
            accessoryPlayer.FairyDanceEquipped = true;
            accessoryPlayer.RainbowSpiritDanceEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<FairyDance>() &&
                   incomingItem.type != ModContent.ItemType<FairyDance>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<FairyDance>()
                .AddIngredient(ItemID.EmpressButterfly)
                .AddIngredient(ItemID.Ectoplasm, 15)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
