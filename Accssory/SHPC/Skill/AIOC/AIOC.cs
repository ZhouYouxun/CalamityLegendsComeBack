using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.DiffuChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.FlyChip;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.AllInOnePC
{
    public class AIOC : ModItem
    {
        //public override string Texture => "CalamityLegendsComeBack/Accssory/SHPC/Skill/CtrlChip/CtrlChip";

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
            player.GetCritChance(DamageClass.Generic) += 8f;

            // 飞升芯片
            player.wingTimeMax += player.wingTimeMax * 3;
            player.runAcceleration *= 1.7f;
            player.moveSpeed += 0.25f;
            player.jumpSpeedBoost += 1.25f;
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
                .AddIngredient(ItemID.LuminiteBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
