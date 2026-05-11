using CalamityLegendsComeBack.Accssory.BF.ATunedQuiver;
using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.BResonatingQuiver
{
    public sealed class BResonatingQuiver : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.Cyan;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.15f;
            player.GetCritChance(DamageClass.Ranged) += 5f;
            player.GetArmorPenetration(DamageClass.Ranged) += 15f;
            player.statDefense += 6;
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(2);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<global::CalamityLegendsComeBack.Accssory.BF.ATunedQuiver.ATunedQuiver>()
                .AddIngredient(ItemID.RangerEmblem)
                .AddIngredient(ItemID.ChlorophyteBar, 5)
                .AddIngredient(ItemID.FragmentVortex, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
