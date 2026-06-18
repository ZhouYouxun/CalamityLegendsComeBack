using CalamityLegendsComeBack.Accssory.SHPC.ChangeRight;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule
{
    public sealed class ProjectilePossessionModule : ModItem
    {
        //public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/Skill/HeatModule/HeatModule";

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
            player.GetModPlayer<ProjectilePossessionModulePlayer>().ProjectilePossessionModuleEquipped = true;
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
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.SoulofSight, 5)
                .AddIngredient<MirrorBlade>()
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
