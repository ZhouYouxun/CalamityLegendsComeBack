using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.General
{
    public sealed class ClearBreezeKunai : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            MCGeneralPlayer gen = player.GetModPlayer<MCGeneralPlayer>();
            gen.BonusStealthMax += 0.15f;
            gen.ProjectileSpeedMult += 0.12f;
            gen.KunaiArmorPen += 15;
            gen.NormalKunaiIgnoresGravity = true;
        }
    }
}
