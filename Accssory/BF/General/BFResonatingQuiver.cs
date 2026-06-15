using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.General
{
    // 共鸣箭袋
    public sealed class BFResonatingQuiver : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/BResonatingQuiver/BResonatingQuiver";

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
            player.GetDamage(DamageClass.Ranged) += 0.14f;
            player.GetArmorPenetration(DamageClass.Ranged) += 12f;
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(3);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BFTunedQuiver>()
                .AddIngredient(ItemID.RangerEmblem)
                .AddIngredient(ItemID.ChlorophyteBar, 5)
                .AddIngredient(ItemID.FragmentVortex, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
