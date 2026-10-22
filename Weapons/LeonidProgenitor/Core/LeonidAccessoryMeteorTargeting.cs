using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;

internal static class LeonidAccessoryMeteorTargeting
{
    internal const float Range = 2000f;
    internal const float AttackSpeedMultiplier = 1.3f;
    internal const float DamageMultiplier = 1.2f;

    internal static NPC Find(Player player)
    {
        NPC best = null;
        bool bestIsBoss = false;
        float bestDistance = Range * Range;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy())
                continue;

            float distance = Vector2.DistanceSquared(player.Center, npc.Center);
            if (distance > Range * Range)
                continue;

            bool boss = npc.boss || (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss);
            if (best != null && (!boss && bestIsBoss || boss == bestIsBoss && distance >= bestDistance))
                continue;

            best = npc;
            bestIsBoss = boss;
            bestDistance = distance;
        }
        return best;
    }
}
