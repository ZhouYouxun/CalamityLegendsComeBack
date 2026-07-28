using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill
{
    public class VortexEye : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/贴图/漩涡之眼";

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
            BBAccessoryPlayer accessoryPlayer = player.GetModPlayer<BBAccessoryPlayer>();
            accessoryPlayer.SetRightClickMode(BBRightClickMode.VortexEye);
            accessoryPlayer.VortexEyeEquipped = true;
            player.GetDamage(DamageClass.Melee) += 0.10f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.10f;
            player.moveSpeed += 0.15f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Lens, 6)
                .AddIngredient(ItemID.SharkFin, 5)
                .AddIngredient<Lumenyl>(5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
