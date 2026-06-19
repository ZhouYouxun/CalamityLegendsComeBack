using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    public class MatrixChargingBarrier : ModItem
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
            BarrierPlayer modPlayer = player.GetModPlayer<BarrierPlayer>();
            modPlayer.BarrierEquipped = true;
            modPlayer.BarrierVisible = !hideVisual;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(4)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient<RoverDrive>()
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
