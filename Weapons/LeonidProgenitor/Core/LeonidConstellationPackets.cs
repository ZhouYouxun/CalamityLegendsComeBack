using System.IO;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    internal static class LeonidConstellationPackets
    {
        public static void RequestUnlock(LeonidStar star)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.LocalPlayer.GetModPlayer<LeonidConstellationPlayer>().TryUnlock(star);
                return;
            }

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LeonidConstellationRequest);
            packet.Write((byte)0);
            packet.Write((byte)star);
            packet.Send();
        }

        public static void RequestReset()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.LocalPlayer.GetModPlayer<LeonidConstellationPlayer>().ResetStars();
                return;
            }

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LeonidConstellationRequest);
            packet.Write((byte)1);
            packet.Write((byte)0);
            packet.Send();
        }

        public static void HandleRequest(BinaryReader reader, int whoAmI)
        {
            byte action = reader.ReadByte();
            LeonidStar star = (LeonidStar)reader.ReadByte();
            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;

            LeonidConstellationPlayer progress = Main.player[whoAmI].GetModPlayer<LeonidConstellationPlayer>();
            if (action == 0 && System.Enum.IsDefined(typeof(LeonidStar), star))
                progress.TryUnlock(star);
            else if (action == 1)
                progress.ResetStars();

            SendState(Main.player[whoAmI], whoAmI);
        }

        public static void SendState(Player player, int toClient = -1, int ignoreClient = -1)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LeonidConstellationStateSync);
            packet.Write((byte)player.whoAmI);
            packet.Write(player.GetModPlayer<LeonidConstellationPlayer>().GetUnlockedMask());
            packet.Send(toClient, ignoreClient);
        }

        public static void HandleState(BinaryReader reader)
        {
            int playerIndex = reader.ReadByte();
            ushort mask = reader.ReadUInt16();
            if (Main.player.IndexInRange(playerIndex))
                Main.player[playerIndex].GetModPlayer<LeonidConstellationPlayer>().ReceiveUnlockedMask(mask);
        }
    }
}
