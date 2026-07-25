using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledBlackPearl : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.15f;
        protected override int TideCapBonus => 6;
        protected override bool BottledBlackPearlEquipped => true;
        protected override int Rarity => ItemRarityID.Yellow;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BottledBoat>()
                .AddIngredient(ItemID.Sail, 3)
                .AddIngredient<DepthCells>(9)
                .AddIngredient<ScoriaBar>(5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
