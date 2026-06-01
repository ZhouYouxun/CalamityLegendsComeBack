using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    internal sealed class SHPCManaPotionGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            if (item.healMana <= 0)
                return;

            SHPCPassivePlayer passivePlayer = player.GetModPlayer<SHPCPassivePlayer>();
            if (!passivePlayer.HoldingSHPC)
                return;

            passivePlayer.RegisterManaPotionUse();
        }
    }
}
