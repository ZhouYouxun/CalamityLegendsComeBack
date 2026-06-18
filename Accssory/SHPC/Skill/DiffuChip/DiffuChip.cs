using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.DiffuChip
{
    public class DiffuChip : ModItem
    {
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
            player.GetModPlayer<DiffuChipPlayer>().DiffuChipEquipped = true;
            player.GetDamage(DamageClass.Magic) += 0.07f;
            player.GetCritChance(DamageClass.Magic) += 7f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofLight, 7)
                .AddIngredient(ItemID.LightShard)
                .AddIngredient(ItemID.Wire, 50)
                .AddIngredient(ItemID.Shotgun)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
