using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 月饼：
    // 右键纯化火球命中或爆炸时，在范围内留下短暂的"月华烙印"。
    // 处于月华烙印中的敌人会更快积累纯化等级；
    // 已达到当前纯化上限的敌人会额外触发一次小型纯化爆燃。
    internal sealed class PFMooncake : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Accssory/PF/Skill/PFMooncake";

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
            player.GetModPlayer<PFAccessoryPlayer>().MooncakeEquipped = true;
        }
    }
}
