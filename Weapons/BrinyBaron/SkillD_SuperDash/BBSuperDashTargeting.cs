using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSuperDashTargeting
    {
        // ===== 索敌规则 =====
        // 1. 只以玩家为圆心、半径 150 格内"选定"目标；一旦选定由调用方咬死不放，不再看距离
        // 2. Boss（含蠕虫 boss 的身体/尾部分段）永远压过普通怪，多个 boss 单位取离玩家最近的
        // 3. 蠕虫类 boss 只锁尾部，不锁头部/身体
        // 4. 全是普通怪时优先整体血量（lifeMax）最高的，一级一级往下
        private const float MaxFocusDistance = 150f * 16f;

        public static bool IsTargetValid(int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return false;

            NPC npc = Main.npc[npcIndex];
            return npc.active && npc.CanBeChasedBy() && !npc.friendly && npc.lifeMax > 5;
        }

        public static int FindBestTargetIndex(Player owner, Vector2 focusPoint)
        {
            int bestBossSegment = -1;
            float bestBossDistance = float.MaxValue;

            int bestNormal = -1;
            int bestNormalLifeMax = -1;
            float bestNormalDistance = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!IsTargetValid(i))
                    continue;

                NPC npc = Main.npc[i];
                float distanceToOwner = Vector2.Distance(npc.Center, owner.Center);

                // ❗只以玩家为圆心
                if (distanceToOwner > MaxFocusDistance)
                    continue;

                if (IsBossAffiliated(npc))
                {
                    if (distanceToOwner < bestBossDistance)
                    {
                        bestBossDistance = distanceToOwner;
                        bestBossSegment = i;
                    }
                    continue;
                }

                if (npc.lifeMax > bestNormalLifeMax ||
                    (npc.lifeMax == bestNormalLifeMax && distanceToOwner < bestNormalDistance))
                {
                    bestNormalLifeMax = npc.lifeMax;
                    bestNormalDistance = distanceToOwner;
                    bestNormal = i;
                }
            }

            if (bestBossSegment >= 0)
                return PreferWormTail(owner, bestBossSegment);

            return bestNormal;
        }

        // ===== 蠕虫类 boss 只锁尾部：在选中的 boss 单位的所有分段里找"尾"，没有尾段的普通 boss 原样返回 =====
        private static int PreferWormTail(Player owner, int segmentIndex)
        {
            long unitKey = GetBossUnitKey(Main.npc[segmentIndex]);

            int bestTail = -1;
            float bestTailDistance = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!IsTargetValid(i))
                    continue;

                NPC npc = Main.npc[i];
                if (GetBossUnitKey(npc) != unitKey || !IsTailSegment(npc))
                    continue;

                float distance = Vector2.Distance(npc.Center, owner.Center);
                if (distance < bestTailDistance)
                {
                    bestTailDistance = distance;
                    bestTail = i;
                }
            }

            return bestTail >= 0 ? bestTail : segmentIndex;
        }

        private static bool IsBossAffiliated(NPC npc)
        {
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                return true;

            // 世界吞噬怪：所有分段 boss 标记都是 false，分裂后也不共享 realLife，需要按类型特判
            if (IsEaterOfWorldsSegment(npc.type))
                return true;

            // 蠕虫 boss 的身体/尾部：boss 标记只挂在头上，通过 realLife 找回本体
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            {
                NPC root = Main.npc[npc.realLife];
                if (root.active && (root.boss || NPCID.Sets.ShouldBeCountedAsBoss[root.type]))
                    return true;
            }

            return false;
        }

        // 同一 boss 单位的分段共享一个 key：蠕虫用 realLife 的头、世吞全体算一个单位、独立 boss 用自己
        private static long GetBossUnitKey(NPC npc)
        {
            if (IsEaterOfWorldsSegment(npc.type))
                return -2;

            return npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
        }

        private static bool IsEaterOfWorldsSegment(int type) =>
            type == NPCID.EaterofWorldsHead || type == NPCID.EaterofWorldsBody || type == NPCID.EaterofWorldsTail;

        // 尾段判定：原版与灾厄的蠕虫尾段内部名都带 "Tail"（TheDestroyerTail、DevourerofGodsTail、PerforatorTailSmall 等）
        private static bool IsTailSegment(NPC npc)
        {
            string internalName = npc.ModNPC?.Name;
            if (internalName is null && !NPCID.Search.TryGetName(npc.type, out internalName))
                return false;

            return internalName.Contains("Tail");
        }
    }
}
