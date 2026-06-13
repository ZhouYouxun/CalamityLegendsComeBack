using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EvolutionEnergyCoreItem = CalamityLegendsComeBack.Accssory.SHPC.General.EvolutionEnergyCore.EvolutionEnergyCore;

namespace CalamityLegendsComeBack.Accssory.SHPC.General.ExoEnergyCore
{
    public class ExoEnergyCore : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SHPCEnergyCorePlayer>().SetEnergyCoreTier(4);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return SHPCEnergyCorePlayer.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<EvolutionEnergyCoreItem>()
                .AddIngredient<ExoPrism>(5)
                .AddTile<DraedonsForge>()
                .Register();
        }
    }
}
