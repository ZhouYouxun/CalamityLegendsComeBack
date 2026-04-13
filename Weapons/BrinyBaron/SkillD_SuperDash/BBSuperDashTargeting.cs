using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSuperDashTargeting
    {
        private const float MaxFocusDistance = 3200f;
        private const float CurrentTargetBonus = 160f;

        public static bool IsTargetValid(int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return false;

            NPC npc = Main.npc[npcIndex];
            return npc.active && npc.CanBeChasedBy() && !npc.friendly && npc.lifeMax > 5;
        }

        public static int FindBestTargetIndex(Player owner, Vector2 focusPoint, int currentTargetIndex = -1)
        {
            int bestBoss = -1;
            float bestBossDistance = float.MaxValue;
            int bestNormal = -1;
            float bestNormalScore = float.MinValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!IsTargetValid(i))
                    continue;

                NPC npc = Main.npc[i];
                float distanceToFocus = Vector2.Distance(npc.Center, focusPoint);
                float distanceToOwner = Vector2.Distance(npc.Center, owner.Center);
                if (distanceToFocus > MaxFocusDistance && distanceToOwner > MaxFocusDistance)
                    continue;

                if (npc.boss)
                {
                    if (distanceToFocus < bestBossDistance)
                    {
                        bestBossDistance = distanceToFocus;
                        bestBoss = i;
                    }

                    continue;
                }

                float score =
                    npc.lifeMax * 0.72f -
                    distanceToFocus * 1.15f -
                    distanceToOwner * 0.24f;

                if (i == currentTargetIndex)
                    score += CurrentTargetBonus;

                if (score > bestNormalScore)
                {
                    bestNormalScore = score;
                    bestNormal = i;
                }
            }

            return bestBoss >= 0 ? bestBoss : bestNormal;
        }
    }
}
