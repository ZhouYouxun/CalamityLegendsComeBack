using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.DiffuChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.FlyChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.AIOC
{
    public class AIOC : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 40);
            Item.rare = ItemRarityID.Purple;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 扩散芯片
            player.GetModPlayer<DiffuChipPlayer>().DiffuChipEquipped = true;
            player.GetDamage(DamageClass.Magic) += 0.07f;
            player.GetCritChance(DamageClass.Magic) += 7f;

            // 快充芯片
            player.GetModPlayer<FastChipPlayer>().FastChipEquipped = true;

            // 火控芯片
            CtrlChipPlayer ctrlPlayer = player.GetModPlayer<CtrlChipPlayer>();
            ctrlPlayer.CtrlChipEquipped = true;
            ctrlPlayer.CtrlChipVisualsHidden = hideVisual;
            player.GetCritChance(DamageClass.Generic) += 7f;

            BarrierPlayer barrierPlayer = player.GetModPlayer<BarrierPlayer>();
            barrierPlayer.BarrierEquipped = true;
            barrierPlayer.BarrierVisible = !hideVisual;
            barrierPlayer.AIOCBarrierBoost = true;

            // 飞升芯片：加速度/移速/跳跃恒定生效
            player.runAcceleration *= 1.7f;
            player.moveSpeed += 0.10f;
            player.jumpSpeedBoost += 0.5f;

            // 飞升芯片：飞行时间仅在手持SHPC时生效
            bool holdingSHPC = player.HeldItem != null &&
                               !player.HeldItem.IsAir &&
                               player.HeldItem.ModItem is NewLegendSHPC;
            if (holdingSHPC)
                player.wingTimeMax += (int)(player.wingTimeMax * 0.7f);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string text = Main.keyState.PressingShift()
                ? this.GetLocalizedValue("TooltipFull")
                : this.GetLocalizedValue("TooltipCompact");
            tooltips.FindAndReplace("[GFB]", text);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<FastChip.FastChip>()
                .AddIngredient(ItemID.FragmentSolar, 10)
                .AddIngredient<CtrlChip.CtrlChip>()
                .AddIngredient(ItemID.FragmentVortex, 10)
                .AddIngredient<DiffuChip.DiffuChip>()
                .AddIngredient(ItemID.FragmentStardust, 10)
                .AddIngredient<FlyChip.FlyChip>()
                .AddIngredient(ItemID.FragmentNebula, 10)
                .AddIngredient<MatrixChargingBarrier>()
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
