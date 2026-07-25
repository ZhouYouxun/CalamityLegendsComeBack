using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.ChangeRight.VortexEye
{
    public class VortexEye : BBRightClickAccessory
    {
        protected override BBRightClickMode Mode => BBRightClickMode.VortexEye;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            base.UpdateAccessory(player, hideVisual);
            player.GetModPlayer<BBAccessoryPlayer>().VortexEyeEquipped = true;
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
