using CalamityMod.Items.Potions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    internal sealed class SHPCManaPotionGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            SHPCPassivePlayer passivePlayer = player.GetModPlayer<SHPCPassivePlayer>();
            if (!passivePlayer.HoldingSHPC)
                return;

            int targetRestore = GetSHPCManaPotionRestore(item.type);
            if (targetRestore <= 0)
                return;

            int extraRestore = System.Math.Max(0, targetRestore - item.healMana);
            if (extraRestore > 0)
            {
                int previousMana = player.statMana;
                player.statMana = Utils.Clamp(player.statMana + extraRestore, 0, player.statManaMax2);
                int restored = player.statMana - previousMana;
                if (restored > 0)
                    player.ManaEffect(restored);
            }

            passivePlayer.RegisterManaPotionUse();
        }

        private static int GetSHPCManaPotionRestore(int itemType)
        {
            if (itemType == ItemID.LesserManaPotion)
                return 100;

            if (itemType == ItemID.ManaPotion)
                return 200;

            if (itemType == ItemID.GreaterManaPotion)
                return 300;

            if (itemType == ItemID.SuperManaPotion)
                return 400;

            if (itemType == ModContent.ItemType<SupremeManaPotion>())
                return 1000;

            return 0;
        }
    }
}
