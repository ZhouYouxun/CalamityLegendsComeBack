using CalamityLegendsComeBack.Accssory.SHPC.General;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.General.WastelandEnergyCore
{
    public class WastelandEnergyCore : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SHPCEnergyCorePlayer>().SetEnergyCoreTier(1);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return SHPCEnergyCorePlayer.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WulfrumMetalScrap>(5)
                .AddIngredient(ModContent.Find<ModItem>("CalamityMod/ScorchedBone").Type, 10)
                .AddIngredient(ModContent.Find<ModItem>("CalamityMod/SeaPrism").Type, 5)
                .AddIngredient(ModContent.Find<ModItem>("CalamityMod/EnergyCore").Type)
                .AddIngredient(ItemID.ManaFlower)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
