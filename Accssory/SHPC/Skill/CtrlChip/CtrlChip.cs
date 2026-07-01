using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip
{
    public class CtrlChip : ModItem
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
            CtrlChipPlayer ctrlPlayer = player.GetModPlayer<CtrlChipPlayer>();
            ctrlPlayer.CtrlChipEquipped = true;
            ctrlPlayer.CtrlChipVisualsHidden = hideVisual;
            player.GetCritChance(DamageClass.Generic) += 7f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofSight)
                .AddIngredient(ItemID.SoulofMight)
                .AddIngredient(ItemID.SoulofFright)
                .AddIngredient(ItemID.HallowedBar, 10)
                .AddIngredient(ItemID.ChlorophyteBullet, 500)
                .AddIngredient(ItemID.MechanicalEye)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
