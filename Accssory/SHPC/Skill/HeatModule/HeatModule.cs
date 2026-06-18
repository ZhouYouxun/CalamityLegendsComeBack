using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule
{
    public sealed class HeatModule : ModItem
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
            player.GetModPlayer<HeatModulePlayer>().HeatModuleEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofFright, 7)
                .AddIngredient<EssenceofHavoc>(7)
                .AddIngredient(ItemID.LavaBucket)
                .AddRecipeGroup("AnySilverBar", 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
