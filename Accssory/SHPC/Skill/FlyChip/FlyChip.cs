using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FlyChip
{
    public class FlyChip : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 飞行时间 +300%（总量变为原来4倍）
            player.wingTimeMax += player.wingTimeMax * 3;

            // 加速度 +70%
            player.runAcceleration *= 1.7f;

            // 移动速度 +25%
            player.moveSpeed += 0.25f;

            // 跳跃速度 +25%（基础跳跃速度约5，25%≈1.25）
            player.jumpSpeedBoost += 1.25f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.EmpressFlightBooster)
                .AddIngredient(ItemID.SoulofFlight, 100)
                .AddIngredient(ItemID.Wire, 100)
                .AddIngredient(ItemID.Switch)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
