using System.IO;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Netcode
{
    internal static class LegendsPacketHandler
    {
        public static void SendModeSync(int toClient = -1, int ignoreClient = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.NewDiffSyncMode);
            packet.Write(LegendsWorldSystem.LegendsModeEnabled);
            packet.Send(toClient, ignoreClient);
        }

        public static void HandleModeSyncPacket(BinaryReader reader, int whoAmI)
        {
            bool enabled = reader.ReadBoolean();
            LegendsWorldSystem.SetModeEnabled(enabled, sync: false);

            if (Main.netMode == NetmodeID.Server)
                SendModeSync(ignoreClient: whoAmI);
        }

        public static void HandleYharonStatePacket(BinaryReader reader, int whoAmI)
        {
            if (reader.BaseStream.CanSeek)
                reader.BaseStream.Position = reader.BaseStream.Length;
        }
    }
}
