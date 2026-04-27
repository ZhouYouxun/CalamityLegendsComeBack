using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.S_MOT_MortarOverwatchTransceiver
{
    public sealed class S_MOT_MortarOverwatchTransceiver : ModItem
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
            player.GetModPlayer<S_MOT_MortarOverwatchTransceiverPlayer>().S_MOT_MortarOverwatchTransceiverEquipped = true;
        }
    }
}
