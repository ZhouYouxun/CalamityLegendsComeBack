using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using EverfrostEnergyCoreItem = CalamityLegendsComeBack.Accssory.SHPC.General.EverfrostEnergyCore.EverfrostEnergyCore;

namespace CalamityLegendsComeBack.Accssory.SHPC.General.EvolutionEnergyCore
{
    public class EvolutionEnergyCore : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 18);
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SHPCEnergyCorePlayer>().SetEnergyCoreTier(3);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return SHPCEnergyCorePlayer.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<EverfrostEnergyCoreItem>()
                .AddIngredient<LifeAlloy>(5)
                .AddIngredient<InfectedArmorPlating>(5)
                //.AddIngredient(ModContent.Find<ModItem>("CalamityMod/PlagueCellCluster").Type, 25)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
