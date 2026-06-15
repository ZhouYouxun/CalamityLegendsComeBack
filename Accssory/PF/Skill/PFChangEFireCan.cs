using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 嫦娥怨火罐：
    // 手持纯元时，燃料背包拥有独立血量，可以阻挡敌方弹幕；
    // 若挡下的弹幕伤害超过剩余血量，燃料背包爆炸，造成 500×500 像素的纯化爆炸。
    // 爆炸不会伤害玩家与友方。
    // TODO: 实现背包血量弹幕 (PFBackpackBarrier)，当前仅激活标志位。
    internal sealed class PFChangEFireCan : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFury";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 4);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PFAccessoryPlayer>().ChangEFireCanEquipped = true;
        }
    }
}
