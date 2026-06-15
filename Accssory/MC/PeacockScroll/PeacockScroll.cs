using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PeacockScroll
{
    public sealed class PeacockScroll : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PeacockScrollPlayer>().PeacockScrollEquipped = true;
        }
    }

    public sealed class PeacockScrollPlayer : ModPlayer
    {
        public bool PeacockScrollEquipped;

        public float RightFeatherSpeedMult => PeacockScrollEquipped ? 1.15f : 1f;

        public override void ResetEffects()
        {
            PeacockScrollEquipped = false;
        }
    }
}
