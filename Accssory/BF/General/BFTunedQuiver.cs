using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.General
{
    // 调谐箭袋 — 由魔法箭袋直接升级。
    public sealed class BFTunedQuiver : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/General/BFTunedQuiver";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 6);
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.11f;
            player.GetCritChance(DamageClass.Ranged) += 6f;
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(2);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.MagicQuiver)
                .AddIngredient(ItemID.JungleSpores, 10)
                .AddIngredient(ItemID.Vine, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
