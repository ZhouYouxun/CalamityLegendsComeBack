using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SilvaHarp
{
    public sealed class SilvaHarp : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/SilvaHarp/SilvaHarp";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 18);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.15f;
            player.GetCritChance(DamageClass.Ranged) += 5f;
            player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped = true;
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            return equippedItem.type != ModContent.ItemType<FairyDanceSeries.BadSeed>() &&
                   incomingItem.type != ModContent.ItemType<FairyDanceSeries.BadSeed>() &&
                   base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CalamityMod.Items.Materials.EffulgentFeather>(15)
                .AddIngredient<CalamityMod.Items.Materials.AscendantSpiritEssence>(2)
                .AddIngredient<CalamityMod.Items.Placeables.Abyss.PlantyMush>(5)
                .AddIngredient(ItemID.MagicalHarp)
                .AddTile<CosmicAnvil>()
                .Register();
        }
    }
}
