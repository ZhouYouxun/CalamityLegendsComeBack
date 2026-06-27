using CalamityLegendsComeBack.Weapons.A_Tools.Toys.PlayerLocker;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.PlayerSaddle;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using System.IO;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    public class CalamityLegendsComeBack : Mod
    {
        internal const string AzureThunderGreenUltimateFilterKey = "CalamityLegendsComeBack:AzureThunderGreenUltimateFilter";

        public override void Load()
        {
            if (Main.dedServ)
                return;

            Filters.Scene[AzureThunderGreenUltimateFilterKey] = new Filter(
                new ScreenShaderData("FilterMiniTower")
                    .UseColor(0.55f, 1f, 0.65f)
                    .UseOpacity(0f),
                EffectPriority.Medium);
        }

        public override void Unload()
        {
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                return;

            GamePacketType packetType = (GamePacketType)reader.ReadByte();

            switch (packetType)
            {
                case GamePacketType.PlayerLockerRequestInventory:
                    PlayerLockerPackets.HandleInventoryRequest(reader, whoAmI);
                    break;
                case GamePacketType.PlayerLockerInventoryData:
                    PlayerLockerPackets.HandleInventoryData(reader);
                    break;
                case GamePacketType.PlayerLockerStealItem:
                    PlayerLockerPackets.HandleStealItem(reader, whoAmI);
                    break;
                case GamePacketType.PlayerLockerClearSlot:
                    PlayerLockerPackets.HandleClearSlot(reader);
                    break;
                case GamePacketType.PlayerSaddleMount:
                    PlayerSaddlePackets.HandleMount(reader, whoAmI);
                    break;
                case GamePacketType.PlayerSaddleDismount:
                    PlayerSaddlePackets.HandleDismount(reader, whoAmI);
                    break;
                default:
                    TetrisMultiplayerPackets.HandlePacket(packetType, reader, whoAmI);
                    break;
            }
        }
    }
}
