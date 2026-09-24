using System.IO;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    internal static class LegendarySupplyBoxPackets
    {
        public static void RequestRandomClaim()
        {
            ModPacket packet = ModContent.GetInstance<CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LegendarySupplyBoxClaimRequest);
            packet.Write(byte.MaxValue);
            packet.Send();
        }

        public static void RequestClaim(int selectionIndex)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                LegendarySupplyBox.TryClaimWeapon(Main.LocalPlayer, selectionIndex);
                return;
            }

            ModPacket packet = ModContent.GetInstance<CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.LegendarySupplyBoxClaimRequest);
            packet.Write((byte)selectionIndex);
            packet.Send();
        }

        public static void HandleClaimRequest(BinaryReader reader, int whoAmI)
        {
            int selectionIndex = reader.ReadByte();
            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;

            if (selectionIndex == byte.MaxValue)
                LegendarySupplyBox.TryClaimRandomWeapon(Main.player[whoAmI]);
            else
                LegendarySupplyBox.TryClaimWeapon(Main.player[whoAmI], selectionIndex);
        }
    }
}
