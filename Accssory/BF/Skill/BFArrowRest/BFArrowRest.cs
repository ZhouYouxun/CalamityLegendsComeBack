using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Skill.BFArrowRest
{
    public sealed class BFArrowRest : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BF/Skill/BFArrowRest/BFArrowRest";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.LightPurple;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<BFAccessoryPlayer>().ArrowConversionEquipped = true;
        }

        //public override void AddRecipes()
        //{
        //    CreateRecipe()
        //        .AddIngredient(ItemID.ChlorophyteBar, 18)
        //        .AddIngredient(ItemID.SoulofLight, 8)
        //        .AddIngredient(ItemID.SoulofNight, 8)
        //        .AddIngredient(ItemID.MagicQuiver)
        //        .AddTile<LunarCraftingStation>()
        //        .Register();
        //}
    }
}
