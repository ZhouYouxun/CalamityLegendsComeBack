using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PeacockBox
{
    public sealed class PeacockBox : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PeacockBoxPlayer>().PeacockBoxEquipped = true;
        }
    }

    public sealed class PeacockBoxPlayer : ModPlayer
    {
        public bool PeacockBoxEquipped;

        public override void ResetEffects()
        {
            PeacockBoxEquipped = false;
        }
    }
}
