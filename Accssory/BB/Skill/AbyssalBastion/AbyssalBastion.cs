using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill.AbyssalBastion
{
    public class AbyssalBastion : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Accssory/BB/Skill/AbyssalBastion/AbyssalBastion";

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
            player.GetModPlayer<BBAccessoryPlayer>().AbyssalBastionEquipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.CobaltShield)
                .AddIngredient<SulphuricScale>(15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
