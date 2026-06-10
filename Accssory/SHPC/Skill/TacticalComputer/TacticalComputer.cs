using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.TacticalComputer
{
    public class TacticalComputer : ModItem
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
            TacticalComputerPlayer tacticalPlayer = player.GetModPlayer<TacticalComputerPlayer>();
            tacticalPlayer.TacticalComputerEquipped = true;
            tacticalPlayer.TacticalComputerVisualsHidden = hideVisual;
            player.GetCritChance(DamageClass.Generic) += 8f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(4)
                .AddIngredient<DubiousPlating>(8)
                .AddIngredient(ItemID.SoulofNight, 6)
                .AddRecipeGroup("AnyMythrilBar", 8)
                .AddIngredient(ItemID.Wire, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
