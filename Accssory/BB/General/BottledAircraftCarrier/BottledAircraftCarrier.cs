using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.General
{
    public class BottledAircraftCarrier : BBMeleeBonusAccessory
    {
        protected override float MeleeBonus => 0.15f;
        protected override int TideCapBonus => 6;
        protected override int Rarity => ItemRarityID.Cyan;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            base.UpdateAccessory(player, hideVisual);
            BBAccessoryPlayer acc = player.GetModPlayer<BBAccessoryPlayer>();
            acc.BottledAircraftCarrierEquipped = true;

            if (player.HeldItem.ModItem is NewLegendBrinyBaron)
            {
                player.GetDamage(DamageClass.Melee) += 0.15f;
                player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BottledBlackPearl>()
                .AddIngredient<Lumenyl>(10)
                .AddIngredient<LifeAlloy>(8)
                .AddIngredient<RuinousSoul>(3)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
