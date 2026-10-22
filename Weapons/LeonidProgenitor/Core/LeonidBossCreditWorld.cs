using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    // Vanilla stores both evil bosses in NPC.downedBoss2. Track the individual kills so
    // defeating both can grant two stars, including when the second boss is summoned here.
    public sealed class LeonidBossCreditWorld : ModSystem
    {
        private static bool downedEater;
        private static bool downedBrain;
        private static bool pendingEater;
        private static bool pendingBrain;

        public static bool DownedBothEvilBosses => downedEater && downedBrain;

        public override void OnWorldLoad() => Clear();
        public override void OnWorldUnload() => Clear();

        private static void Clear()
        {
            downedEater = downedBrain = pendingEater = pendingBrain = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (downedEater)
                tag["LeonidDownedEater"] = true;
            if (downedBrain)
                tag["LeonidDownedBrain"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            downedEater = tag.GetBool("LeonidDownedEater");
            downedBrain = tag.GetBool("LeonidDownedBrain");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(downedEater);
            writer.Write(downedBrain);
        }

        public override void NetReceive(BinaryReader reader)
        {
            downedEater = reader.ReadBoolean();
            downedBrain = reader.ReadBoolean();
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // A pre-existing world only records that one evil boss was defeated. Credit its
            // native boss as the first one when no individual defeat was observed yet.
            if (NPC.downedBoss2 && !downedEater && !downedBrain && !pendingEater && !pendingBrain)
            {
                if (WorldGen.crimson)
                    downedBrain = true;
                else
                    downedEater = true;
            }

            bool changed = false;
            if (pendingBrain && NPC.downedBoss2 && !NPC.AnyNPCs(NPCID.BrainofCthulhu))
            {
                changed |= !downedBrain;
                downedBrain = true;
                pendingBrain = false;
            }

            if (pendingEater && NPC.downedBoss2 && !NPC.AnyNPCs(NPCID.EaterofWorldsHead)
                && !NPC.AnyNPCs(NPCID.EaterofWorldsBody) && !NPC.AnyNPCs(NPCID.EaterofWorldsTail))
            {
                changed |= !downedEater;
                downedEater = true;
                pendingEater = false;
            }
            else if (pendingEater && !NPC.downedBoss2 && !NPC.AnyNPCs(NPCID.EaterofWorldsHead)
                && !NPC.AnyNPCs(NPCID.EaterofWorldsBody) && !NPC.AnyNPCs(NPCID.EaterofWorldsTail))
            {
                // A partial fight that ended without the boss being defeated grants no star.
                pendingEater = false;
            }

            if (changed && Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
        }

        internal static void RecordKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.type == NPCID.BrainofCthulhu)
                pendingBrain = true;
            else if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                pendingEater = true;
        }
    }

    public sealed class LeonidBossCreditNPC : GlobalNPC
    {
        public override void OnKill(NPC npc) => LeonidBossCreditWorld.RecordKill(npc);
    }
}
