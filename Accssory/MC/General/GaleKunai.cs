using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.General
{
    public sealed class GaleKunai : ModItem
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";
        public override string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 12);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            MCGeneralPlayer gen = player.GetModPlayer<MCGeneralPlayer>();
            gen.BonusStealthMax += 0.20f;
            gen.ProjectileSpeedMult += 0.16f;
            gen.KunaiArmorPen += 25;
            gen.StealthRestoreOnStealthStrike += 0.25f;
        }
    }
}
