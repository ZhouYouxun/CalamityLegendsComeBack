using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.BossProgress
{
    internal sealed class CTRLBossAntiDespawnNPC : GlobalNPC
    {
        private const int ProtectedBossTimeLeft = 1800;
        private const float BlockedDRThreshold = 0.95f;
        private const float MaxEscapeDistance = 2500f;

        private bool wasDayTimeTemp;
        private bool dayTimeSpoofed;

        public override bool InstancePerEntity => true;

        public override bool PreAI(NPC npc)
        {
            if (CTRLBossAntiDespawnSystem.IsActive && IsProtectedBoss(npc))
            {
                ProtectBoss(npc);

                if (Main.dayTime && IsDaytimeRetreatingBoss(npc.type))
                {
                    wasDayTimeTemp = Main.dayTime;
                    Main.dayTime = false;
                    dayTimeSpoofed = true;
                }
            }
            return true;
        }

        public override bool CheckActive(NPC npc)
        {
            if (CTRLBossAntiDespawnSystem.IsActive && IsProtectedBoss(npc))
                return false;

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (dayTimeSpoofed)
            {
                Main.dayTime = wasDayTimeTemp;
                dayTimeSpoofed = false;
            }

            if (CTRLBossAntiDespawnSystem.IsActive && IsProtectedBoss(npc))
            {
                ProtectBoss(npc);
                PreventBossEscaping(npc);
            }
        }

        private static void ProtectBoss(NPC npc)
        {
            if (CTRLBossAntiDespawnSystem.TryGetAnchorPlayer(out Player player))
                npc.target = player.whoAmI;

            if (npc.timeLeft < ProtectedBossTimeLeft)
                npc.timeLeft = ProtectedBossTimeLeft;

            if (npc.type == NPCID.SkeletronPrime && npc.defense > npc.defDefense + 50)
                npc.defense = npc.defDefense;

            CalamityGlobalNPC calamityNPC = npc.Calamity();
            calamityNPC.DoesNotDisappearInBossRush = true;
            calamityNPC.CurrentlyEnraged = false;
            calamityNPC.CurrentlyIncreasingDefenseOrDR = false;

            if (calamityNPC.DR >= BlockedDRThreshold)
            {
                calamityNPC.DR = 0f;
                calamityNPC.unbreakableDR = false;
            }
        }

        private static void PreventBossEscaping(NPC npc)
        {
            if (!CTRLBossAntiDespawnSystem.TryGetAnchorPlayer(out Player player))
                return;

            if (Vector2.Distance(npc.Center, player.Center) <= MaxEscapeDistance)
                return;

            npc.Center = player.Center - Vector2.UnitY * 500f;
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
        }

        private static bool IsProtectedBoss(NPC npc)
        {
            if (!npc.active)
                return false;

            return npc.boss ||
                npc.realLife >= 0 ||
                NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

        private static bool IsDaytimeRetreatingBoss(int type) =>
            type == NPCID.Retinazer ||
            type == NPCID.Spazmatism ||
            type == NPCID.TheDestroyer ||
            type == NPCID.TheDestroyerBody ||
            type == NPCID.TheDestroyerTail;
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

        public static bool PlayerHasTool(Player player)
        {
            if (player is null || !player.active)
                return false;

            int ctrlBossType = ModContent.ItemType<CTRLBoss>();
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item is not null && !item.IsAir && item.type == ctrlBossType)
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
                if (!PlayerHasTool(player))
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
