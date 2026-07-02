using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 存储沙漠之鹰格子槽的状态以及各规则的每发状态变量。
    /// </summary>
    public class DesertEagleSlotPlayer : ModPlayer
    {
        // ── 装填槽 ──────────────────────────────────────────────
        /// <summary>当前装填进格子槽的枪。IsAir 表示槽位为空。</summary>
        public Item SlottedGun = new Item();

        /// <summary>快捷属性：装填的枪的 ItemType；空则返回 0。</summary>
        public int SlottedGunType => SlottedGun.IsAir ? 0 : SlottedGun.type;

        // ── 各规则用状态变量 ─────────────────────────────────────
        /// <summary>AcesHigh 牌型计数器 (0=红心, 1=梅花, 2=方块, 3=黑桃)。</summary>
        public int CardIndex;

        /// <summary>交替型规则（Arietes41 / ThermoclineBlaster）的开关状态。</summary>
        public bool ToggleState;

        /// <summary>PridefulHuntersPlanarRipper 射击计数器（每4发一次强化）。</summary>
        public int BurstCounter;

        // ── 生命周期 ─────────────────────────────────────────────
        public override void Initialize()
        {
            SlottedGun = new Item();
            SlottedGun.SetDefaults(ItemID.None);   // Air
        }

        // ── 存档 / 读档 ──────────────────────────────────────────
        public override void SaveData(TagCompound tag)
        {
            if (!SlottedGun.IsAir)
                tag["DEGunItemID"] = SlottedGun.type;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("DEGunItemID", out int type) && type > 0)
            {
                SlottedGun = new Item(type);
            }
            else
            {
                SlottedGun = new Item();
                SlottedGun.SetDefaults(ItemID.None);
            }
        }
    }
}
