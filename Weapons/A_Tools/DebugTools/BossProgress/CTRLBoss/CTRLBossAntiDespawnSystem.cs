using CalamityMod;
using CalamityMod.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.BossProgress
{
    internal sealed class CTRLBossAntiDespawnNPC : GlobalNPC
    {
        private const int ProtectedBossTimeLeft = 1800;
        private const float BlockedDRThreshold = 0.95f;

        public override bool PreAI(NPC npc)
        {
            ProtectBoss(npc);
            return true;
        }

        public override bool CheckActive(NPC npc)
        {
            if (CTRLBossAntiDespawnSystem.IsActive && IsProtectedBoss(npc))
                return false;

            return true;
        }

        public override void PostAI(NPC npc) => ProtectBoss(npc);

        private static void ProtectBoss(NPC npc)
        {
            if (!CTRLBossAntiDespawnSystem.IsActive || !IsProtectedBoss(npc))
                return;

            if (CTRLBossAntiDespawnSystem.TryGetAnchorPlayer(out Player player))
                npc.target = player.whoAmI;

            if (npc.timeLeft < ProtectedBossTimeLeft)
                npc.timeLeft = ProtectedBossTimeLeft;

            CalamityGlobalNPC calamityNPC = npc.Calamity();
            calamityNPC.CurrentlyEnraged = false;
            calamityNPC.CurrentlyIncreasingDefenseOrDR = false;

            if (calamityNPC.DR >= BlockedDRThreshold)
            {
                calamityNPC.DR = 0f;
                calamityNPC.unbreakableDR = false;
            }
        }

        private static bool IsProtectedBoss(NPC npc)
        {
            return npc.boss ||
                npc.realLife >= 0 ||
                NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }
    }

    internal static class CTRLBossAntiDespawnSystem
    {
        private static ulong lastScanTick = ulong.MaxValue;
        private static bool cachedActive;
        private static int cachedAnchorPlayer = -1;

        public static bool IsActive
        {
            get
            {
                RefreshCache();
                return cachedActive;
            }
        }

        public static bool TryGetAnchorPlayer(out Player player)
        {
            RefreshCache();
            if (cachedAnchorPlayer >= 0 && cachedAnchorPlayer < Main.maxPlayers)
            {
                player = Main.player[cachedAnchorPlayer];
                return player.active && !player.dead;
            }

            player = null;
            return false;
        }

        public static bool PlayerHasFavoritedTool(Player player)
        {
            if (player is null || !player.active)
                return false;

            int ctrlBossType = ModContent.ItemType<CTRLBoss>();
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item is not null && item.type == ctrlBossType && item.favorited)
                    return true;
            }

            return false;
        }

        private static void RefreshCache()
        {
            ulong currentTick = Main.GameUpdateCount;
            if (lastScanTick == currentTick)
                return;

            lastScanTick = currentTick;
            cachedActive = false;
            cachedAnchorPlayer = -1;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!PlayerHasFavoritedTool(player))
                    continue;

                cachedActive = true;
                if (!player.dead)
                {
                    cachedAnchorPlayer = i;
                    return;
                }
            }
        }
    }
}
