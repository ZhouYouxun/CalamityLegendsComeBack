using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.General
{
    // 备用箭袋 — 合成同调箭袋的前置材料。
    public sealed class BFSpareQuiver : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/General/BFSpareQuiver";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 80);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.08f;
            player.statDefense += 4;
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(1);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 30)
                .AddIngredient(ItemID.Silk, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
