using CalamityLegendsComeBack.Accssory.BF.BResonatingQuiver;
using CalamityLegendsComeBack.Accssory.BF.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.CDominationQuiver
{
    public sealed class CDominationQuiver : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 20);
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.15f;
            player.GetCritChance(DamageClass.Ranged) += 5f;
            player.GetArmorPenetration(DamageClass.Ranged) += 15f;
            player.statDefense += 16;
            player.statLifeMax2 += (int)(player.statLifeMax2 * 0.15f);
            player.GetModPlayer<BFAccessoryPlayer>().EquipQuiver(3);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<global::CalamityLegendsComeBack.Accssory.BF.BResonatingQuiver.BResonatingQuiver>()
                .AddIngredient(ItemID.SharkFin, 10)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
