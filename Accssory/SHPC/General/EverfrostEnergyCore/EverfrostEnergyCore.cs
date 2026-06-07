using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandEnergyCoreItem = CalamityLegendsComeBack.Accssory.SHPC.General.WastelandEnergyCore.WastelandEnergyCore;

namespace CalamityLegendsComeBack.Accssory.SHPC.General.EverfrostEnergyCore
{
    public class EverfrostEnergyCore : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SHPCEnergyCorePlayer>().SetEnergyCoreTier(2);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return SHPCEnergyCorePlayer.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WastelandEnergyCoreItem>()
                .AddIngredient<CryonicBar>(10)
                .AddIngredient<PerennialBar>(25)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
