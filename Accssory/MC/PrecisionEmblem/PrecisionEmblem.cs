using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.PrecisionEmblem
{
    public sealed class PrecisionEmblem : ModItem
    {
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 7);
            Item.rare = ItemRarityID.LightRed;
        }
    }

    public sealed class PrecisionEmblemPlayer : ModPlayer
    {
        public bool PrecisionEmblemEquipped;

        public override void ResetEffects()
        {
            PrecisionEmblemEquipped = false;
        }
    }
}
