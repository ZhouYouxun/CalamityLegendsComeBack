using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledBoat : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.12f;
        protected override int TideCapBonus => 4;
        protected override int Rarity => ItemRarityID.LightRed;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            base.UpdateAccessory(player, hideVisual);
            player.GetModPlayer<BBAccessoryPlayer>().BottledBoatEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BottledRaft>()
                .AddIngredient(ItemID.Sail)
                .AddIngredient<EssenceofEleum>(3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
