using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip
{
    public class FastChip : ModItem
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
            player.GetModPlayer<FastChipPlayer>().FastChipEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofNight, 7)
                .AddIngredient(ItemID.DarkShard)
                .AddIngredient(ItemID.Wire, 50)
                .AddIngredient<WulfrumBattery>()
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
