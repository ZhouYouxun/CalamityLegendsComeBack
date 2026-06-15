using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.General
{
    // 主宰箭袋
    public sealed class BFDominationQuiver : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/CDominationQuiver/CDominationQuiver";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 20);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.18f;
            player.GetCritChance(DamageClass.Ranged) += 8f;
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(4);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BFResonatingQuiver>()
                .AddIngredient(ItemID.SharkFin, 10)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
