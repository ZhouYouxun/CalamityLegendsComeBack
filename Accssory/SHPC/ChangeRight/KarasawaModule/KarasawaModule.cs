//using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight;
//using CalamityMod.Items.Materials;
//using CalamityMod.Items.Weapons.Ranged;
//using CalamityMod.Tiles.Furniture.CraftingStations;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.KarasawaModule
//{
//    public sealed class KarasawaModule : ModItem
//    {
//        public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/ChangeRight/MilitaryCaller/MilitaryCaller";

//        public override void SetDefaults()
//        {
//            Item.width = 32;
//            Item.height = 32;
//            Item.accessory = true;
//            Item.value = Item.sellPrice(gold: 30);
//            Item.rare = ItemRarityID.Red;
//        }

//        public override void UpdateAccessory(Player player, bool hideVisual)
//        {
//            player.GetModPlayer<KarasawaModulePlayer>().KarasawaModuleEquipped = true;
//        }

//        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
//        {
//            return SHPCChangeRightAccessoryRules.CanEquipWith(equippedItem, incomingItem) &&
//                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
//        }

//        public override void AddRecipes()
//        {
//            //CreateRecipe()
//            //    .AddIngredient<MysteriousCircuitry>(12)
//            //    .AddIngredient<DubiousPlating>(20)
//            //    .AddIngredient<Karasawa>()
//            //    .AddIngredient<ExoPrism>(5)
//            //    .AddTile<DraedonsForge>()
//            //    .Register();
//        }
//    }
//}
