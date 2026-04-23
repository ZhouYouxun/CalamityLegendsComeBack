using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_TAC_TacticalLockCalibrator
{
    public class S_TAC_TacticalLockCalibrator : ModItem
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
            player.GetModPlayer<S_TAC_TacticalLockCalibratorPlayer>().S_TAC_TacticalLockCalibratorEquipped = true;
        }
    }
}
