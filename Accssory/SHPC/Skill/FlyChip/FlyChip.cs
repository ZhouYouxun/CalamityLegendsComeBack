using CalamityLegendsComeBack.Weapons.SHPC;
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
            // 加速度 +70%（0.7倍），恒定生效
            player.runAcceleration *= 1.7f;

            // 移动速度 +10%，恒定生效
            player.moveSpeed += 0.10f;

            // 跳跃速度 +10%（基础跳跃速度约5，10%≈0.5），恒定生效
            player.jumpSpeedBoost += 0.5f;

            // 飞行时间 +70%，仅在手持SHPC时生效
            bool holdingSHPC = player.HeldItem != null &&
                               !player.HeldItem.IsAir &&
                               player.HeldItem.ModItem is NewLegendSHPC;
            if (holdingSHPC)
                player.wingTimeMax += (int)(player.wingTimeMax * 0.7f);
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
