using System;
using System.IO;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // Keeps the recovery shield's game state on the server while clients receive only a display
    // snapshot. Projectile visuals remain client-side, but they no longer decide damage.
    internal static class BFRecoveryShieldPackets
    {
        public static void RequestStartBurst(Player player)
        {
            if (!player.active || player.dead)
                return;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                StartBurst(player);
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.BFRecoveryShieldStartRequest);
            packet.Send();
        }

        public static void HandleStartBurstRequest(int whoAmI)
        {
            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;

            Player player = Main.player[whoAmI];
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
                return;

            StartBurst(player);
        }

        private static void StartBurst(Player player)
        {
            BFRecoveryRightStats stats = BFRecoveryRightBalance.GetStats();
            int flashCount = Math.Max(1, stats.FlashCount + player.GetModPlayer<BFAccessoryPlayer>().RecoveryExtraFlashes);
            float capacity = Math.Max(10f, flashCount * stats.HealAmount);
            player.GetModPlayer<BFRecoveryShieldPlayer>().StartNewShieldBurst(capacity);

            if (Main.netMode == NetmodeID.Server)
                SendState(player, player.whoAmI);
        }

        public static void SendState(Player player, int toClient = -1, int ignoreClient = -1)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            BFRecoveryShieldPlayer shield = player.GetModPlayer<BFRecoveryShieldPlayer>();
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.BFRecoveryShieldStateSync);
            packet.Write((byte)player.whoAmI);
            packet.Write(shield.ShieldHitPoints);
            packet.Write(shield.ShieldMaxHitPoints);
            packet.Write((short)Math.Clamp(shield.ShieldHitFlashTimer, 0, short.MaxValue));
            packet.Send(toClient, ignoreClient);
        }

        public static void HandleState(BinaryReader reader)
        {
            int playerIndex = reader.ReadByte();
            float hitPoints = reader.ReadSingle();
            float maxHitPoints = reader.ReadSingle();
            int hitFlashTimer = reader.ReadInt16();
            if (Main.player.IndexInRange(playerIndex))
                Main.player[playerIndex].GetModPlayer<BFRecoveryShieldPlayer>().ReceiveState(hitPoints, maxHitPoints, hitFlashTimer);
        }
    }
}
