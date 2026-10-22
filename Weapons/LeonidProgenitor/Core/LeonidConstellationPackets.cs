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

            // Apply the choice locally at once; the server still validates and sends back its authoritative chart.
            if (!Main.LocalPlayer.GetModPlayer<LeonidConstellationPlayer>().TryUnlock(star))
                return;

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

        // The character owns its saved choices; the server owns the current world's budget.
        public static void SendLoadedStars(ushort savedMask)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LeonidConstellationRequest);
            packet.Write((byte)2);
            packet.Write((byte)0);
            packet.Write(savedMask);
            packet.Send();
        }

        public static void HandleRequest(BinaryReader reader, int whoAmI)
        {
            byte action = reader.ReadByte();
            LeonidStar star = (LeonidStar)reader.ReadByte();
            ushort savedMask = action == 2 ? reader.ReadUInt16() : (ushort)0;
            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;

            LeonidConstellationPlayer progress = Main.player[whoAmI].GetModPlayer<LeonidConstellationPlayer>();
            bool adjustedForWorld = false;
            if (action == 0 && System.Enum.IsDefined(typeof(LeonidStar), star))
                progress.TryUnlock(star);
            else if (action == 1)
                progress.ResetStars();
            else if (action == 2)
            {
                progress.ReceiveLoadedMask(savedMask);
                adjustedForWorld = progress.ReconcileToWorld();
            }

            SendState(Main.player[whoAmI], adjustedForWorld: adjustedForWorld);
        }

        public static void SendState(Player player, int toClient = -1, int ignoreClient = -1, bool adjustedForWorld = false)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LeonidConstellationStateSync);
            packet.Write((byte)player.whoAmI);
            packet.Write(player.GetModPlayer<LeonidConstellationPlayer>().GetUnlockedMask());
            packet.Write(adjustedForWorld);
            packet.Send(toClient, ignoreClient);
        }

        public static void HandleState(BinaryReader reader)
        {
            int playerIndex = reader.ReadByte();
            ushort mask = reader.ReadUInt16();
            bool adjustedForWorld = reader.ReadBoolean();
            if (Main.player.IndexInRange(playerIndex))
            {
                Main.player[playerIndex].GetModPlayer<LeonidConstellationPlayer>().ReceiveUnlockedMask(mask);
                if (adjustedForWorld && playerIndex == Main.myPlayer)
                    Main.NewText("狮源流星：检测到世界变动，星图已被改变", new Microsoft.Xna.Framework.Color(47, 99, 202));
            }
        }
    }
}
