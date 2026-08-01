using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Skill.BFArrowRest
{
    public sealed class BFArrowRest : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/BFArrowRest/BFArrowRest";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.LightPurple;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += BFAccessoryPlayer.ArrowRestRangedDamageBonus;
            player.GetModPlayer<BFAccessoryPlayer>().ArrowConversionEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.RichMahogany, 30)
                .AddIngredient(ItemID.JungleSpores, 5)
                .AddIngredient(ItemID.Vine)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
