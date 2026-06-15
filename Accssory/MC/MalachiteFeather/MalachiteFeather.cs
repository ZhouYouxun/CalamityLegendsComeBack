using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.MalachiteFeather
{
    public sealed class MalachiteFeather : ModItem
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
    }

    public sealed class MalachiteFeatherPlayer : ModPlayer
    {
        public override void ResetEffects() { }
    }
}
