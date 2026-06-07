using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.MilitaryCaller
{
    public sealed class MilitaryCaller : ModItem
    {
        //public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/备用贴图/定位模块";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<MilitaryCallerPlayer>().MilitaryCallerEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return SHPCChangeRightAccessoryRules.CanEquipWith(equippedItem, incomingItem) &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(18)
                .AddIngredient(ItemID.Wire, 40)
                .AddIngredient<PulseTurretRemote>()
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
