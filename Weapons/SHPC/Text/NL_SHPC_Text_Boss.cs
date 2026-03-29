using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SupremeCalamitas;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Text
{
    public class NL_SHPC_Text_Boss : ModSystem
    {
        // ===== 状态缓存（是否存在）=====
        private Dictionary<int, bool> lastState = new Dictionary<int, bool>();

        // ===== Boss定义（统一入口，方便以后扩展）=====
        private List<BossEntry> bossList;

        public override void Load()
        {
            bossList = new List<BossEntry>()
            {
                // ===== 原版 =====
                new BossEntry(NPCID.TheDestroyer, "Destroyer"),

                // ===== 灾厄 =====
                new BossEntry(ModContent.NPCType<AstrumAureus>(), "AstrumAureus"), // 白金星舰
                new BossEntry(ModContent.NPCType<AstrumDeusHead>(), "AstrumDeus"),     // 星神游龙
                new BossEntry(ModContent.NPCType<StormWeaverHead>(), "StormWeaver"),   // 风暴编织者

                // ===== 双子（任意一个触发）=====
                new BossEntry(ModContent.NPCType<Apollo>(), "ExoTwins"),
                new BossEntry(ModContent.NPCType<Artemis>(), "ExoTwins"),

                // ===== 其他 =====
                new BossEntry(ModContent.NPCType<ThanatosHead>(), "Thanatos"),
                new BossEntry(ModContent.NPCType<AresBody>(), "Ares"),
                new BossEntry(ModContent.NPCType<SupremeCalamitas>(), "Calamitas")
            };
        }

        public override void PostUpdateEverything()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            // ===== 冷却直接跳过 =====
            if (NL_SHPC_Text_Core_IsCooldown())
                return;

            // ===== 当前帧检测 =====
            HashSet<int> currentSet = new HashSet<int>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;

                currentSet.Add(npc.type);
            }

            // ===== 遍历Boss表 =====
            foreach (var boss in bossList)
            {
                bool now = currentSet.Contains(boss.Type);
                bool last = lastState.ContainsKey(boss.Type) && lastState[boss.Type];

                // ===== 出现 =====
                if (!last && now)
                {
                    NL_SHPC_Text_Core.Request(player, boss.Key + "Spawn");
                }

                // ===== 死亡 =====
                if (last && !now)
                {
                    NL_SHPC_Text_Core.Request(player, boss.Key + "Death");
                }

                lastState[boss.Type] = now;
            }
        }

        // ===== 冷却访问（避免直接依赖实现）=====
        private bool NL_SHPC_Text_Core_IsCooldown()
        {
            return typeof(NL_SHPC_Text_Core)
                .GetField("cooldownTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.GetValue(null) is int t && t > 0;
        }

        // ===== Boss结构 =====
        private class BossEntry
        {
            public int Type;
            public string Key;

            public BossEntry(int type, string key)
            {
                Type = type;
                Key = key;
            }
        }




    }
}