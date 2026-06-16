using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 灵梦FOMO：
    // 左键使用当前印记攻击时，印记队列中未被选中的印记会周期性释放低威力残响弹幕。
    // 相当于有额外的副炮。
    internal sealed class PFLingmuFOMO : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/PF/Skill/PFLingmuFOMO";

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
            player.GetModPlayer<PFAccessoryPlayer>().LingmuFOMOEquipped = true;
        }
    }
}
