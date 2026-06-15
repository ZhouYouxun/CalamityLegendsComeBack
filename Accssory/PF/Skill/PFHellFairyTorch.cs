using CalamityLegendsComeBack.Accssory.PF;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF.Skill
{
    // 地狱妖精的火把：
    // 小怪达到当前最高纯化等级后，会直接造成最大生命值60%的真实伤害，
    // 然后周期性释放一圈友方纯化火花；
    // Boss 触发纯化爆燃时，会直接清空所有纯化等级，并立刻造成最大生命值的10%的真实伤害，
    // 然后一次性释放螺旋扩散的纯化弹幕。
    internal sealed class PFHellFairyTorch : ModItem
    {
        public new string LocalizationCategory => "Items";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/NewLegendPristineFury";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<PFAccessoryPlayer>().HellFairyTorchEquipped = true;
        }
    }
}
