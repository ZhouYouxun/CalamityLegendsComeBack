using CalamityMod.Items.LoreItems;
using CalamityMod.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// Cynosure 是一次性获得的 Lore 材料。它没有配方，也不允许堆叠。
    /// 真正防止装填后丢失的逻辑位于 NewLegendSHPC：容量固定 999，取出必定返还。
    /// </summary>
    public class Cynosure : LoreItem
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/AuricCell";

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.value = 0;
            Item.ResearchUnlockCount = 0;
        }

        public override bool CanPickup(Player player)
        {
            // 正常游玩中只允许持有一个 Cynosure。装入 SHPC 的核心同样计入持有数量。
            // 世界箱子不属于单个玩家，无法在拾取钩子中可靠归属，因此这里只扫描个人物品空间。
            int cynosureType = ModContent.ItemType<Cynosure>();
            return !ContainsCynosure(player.inventory, cynosureType) &&
                   !ContainsCynosure(player.bank.item, cynosureType) &&
                   !ContainsCynosure(player.bank2.item, cynosureType) &&
                   !ContainsCynosure(player.bank3.item, cynosureType) &&
                   !ContainsCynosure(player.bank4.item, cynosureType) &&
                   !ContainsLoadedCynosure(player.inventory, cynosureType) &&
                   !ContainsLoadedCynosure(player.bank.item, cynosureType) &&
                   !ContainsLoadedCynosure(player.bank2.item, cynosureType) &&
                   !ContainsLoadedCynosure(player.bank3.item, cynosureType) &&
                   !ContainsLoadedCynosure(player.bank4.item, cynosureType);
        }

        private static bool ContainsCynosure(Item[] items, int cynosureType)
        {
            foreach (Item item in items)
            {
                if (item != null && item.stack > 0 && item.type == cynosureType)
                    return true;
            }

            return false;
        }

        private static bool ContainsLoadedCynosure(Item[] items, int cynosureType)
        {
            foreach (Item item in items)
            {
                if (item?.ModItem is NewLegendSHPC shpc && shpc.HasLoadedAmmoType(cynosureType))
                    return true;
            }

            return false;
        }
    }
}
