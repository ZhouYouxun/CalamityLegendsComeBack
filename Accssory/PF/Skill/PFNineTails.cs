using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 无秽九尾：
    // 手持纯元时，原本的三条火狐尾巴变为九条无秽狐尾。
    // 狐尾体型大幅提高，索敌范围提高，并会自动追踪附近敌人；
    // 狐尾命中敌人时造成高额纯化伤害，并少量推进敌人的纯化进度。
    internal sealed class PFNineTails : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/PF/Skill/PFNineTails";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 6);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PFAccessoryPlayer>().NineTailsEquipped = true;
        }
    }
}
