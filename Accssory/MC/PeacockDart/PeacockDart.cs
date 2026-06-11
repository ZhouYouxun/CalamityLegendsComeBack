using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PeacockDart
{
    public sealed class PeacockDart : ModItem
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
            player.GetModPlayer<PeacockDartPlayer>().PeacockDartEquipped = true;
        }
    }

    public sealed class PeacockDartPlayer : ModPlayer
    {
        public bool PeacockDartEquipped;

        public override void ResetEffects()
        {
            PeacockDartEquipped = false;
        }
    }
}
