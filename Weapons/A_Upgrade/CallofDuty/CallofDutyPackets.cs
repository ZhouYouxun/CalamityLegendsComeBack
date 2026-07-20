using System;
using System.IO;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal static class CallofDutyPackets
    {
        public static void SendUltimateRequest()
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.CallofDutyUltimateRequest);
            packet.Send();
        }

        public static void HandleUltimateRequest(int whoAmI)
        {
            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;
            ToggleUltimate(Main.player[whoAmI]);
        }

        public static void ToggleUltimate(Player player)
        {
            CallofDutyPlayer phonePlayer = player.GetModPlayer<CallofDutyPlayer>();
            if (phonePlayer.ArmyActive)
            {
                Ultimate.ResponsibilityArmyUnitBase.DismissAllFor(player.whoAmI, phonePlayer.ArmyGeneration);
                phonePlayer.BeginArmyRecall();
                SendState(player);
                return;
            }

            if (!phonePlayer.UltimateReady || player.dead || !player.active)
                return;
            if (player.HeldItem?.type != ModContent.ItemType<CallofDuty>() || !CallofDuty.HasPhoneInMainInventory(player))
                return;

            int generation = phonePlayer.ArmyGeneration + 1;
            Item phone = CallofDuty.FindPhone(player);
            int snapshotDamage = phone == null ? CallofDuty.BaseDamage : player.GetWeaponDamage(phone);
            if (!Ultimate.ResponsibilityArmyAmplifier.SpawnFor(player, generation, snapshotDamage))
                return;

            phonePlayer.StartArmy(generation);
            SendState(player);
        }

        public static void SendCommand(ResponsibilityCommandMode mode, Vector2 position, int target)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.CallofDutyCommandRequest);
            packet.Write((byte)mode);
            packet.WriteVector2(position);
            packet.Write((short)target);
            packet.Send();
        }

        public static void HandleCommand(BinaryReader reader, int whoAmI)
        {
            ResponsibilityCommandMode mode = (ResponsibilityCommandMode)reader.ReadByte();
            Vector2 position = reader.ReadVector2();
            int target = reader.ReadInt16();

            if (Main.netMode != NetmodeID.Server || !Main.player.IndexInRange(whoAmI))
                return;

            Player player = Main.player[whoAmI];
            CallofDutyPlayer phonePlayer = player.GetModPlayer<CallofDutyPlayer>();
            if (!phonePlayer.ArmyActive || mode > ResponsibilityCommandMode.Attack)
                return;
            if (Vector2.DistanceSquared(position, player.Center) > 2400f * 2400f)
                position = player.Center + player.SafeDirectionTo(position) * 2400f;
            if (mode == ResponsibilityCommandMode.Attack && (!Main.npc.IndexInRange(target) || !Main.npc[target].CanBeChasedBy()))
                mode = ResponsibilityCommandMode.Move;

            phonePlayer.SetCommand(mode, position, target);
        }

        public static void SendLanguageSelection(int index)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.CallofDutyLanguageSelection);
            packet.Write((byte)Main.myPlayer);
            packet.Write((byte)index);
            packet.Send();
        }

        public static void HandleLanguageSelection(BinaryReader reader, int whoAmI)
        {
            int claimedPlayer = reader.ReadByte();
            int index = reader.ReadByte();
            int playerIndex = Main.netMode == NetmodeID.Server ? whoAmI : claimedPlayer;
            if (!Main.player.IndexInRange(playerIndex))
                return;

            Main.player[playerIndex].GetModPlayer<CallofDutyPlayer>().ReceiveLanguageSelection(index);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket relay = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
                relay.Write((byte)GamePacketType.CallofDutyLanguageSelection);
                relay.Write((byte)playerIndex);
                relay.Write((byte)index);
                relay.Send(-1, whoAmI);
            }
        }

        public static void SendState(Player player, int toClient = -1, int ignoreClient = -1)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            CallofDutyPlayer phonePlayer = player.GetModPlayer<CallofDutyPlayer>();
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.CallofDutyStateSync);
            packet.Write((byte)player.whoAmI);
            packet.Write((short)phonePlayer.UltimateCharge);
            packet.Write(phonePlayer.ArmyActive);
            packet.Write(phonePlayer.ArmyGeneration);
            packet.Send(toClient, ignoreClient);
        }

        public static void HandleState(BinaryReader reader)
        {
            int playerIndex = reader.ReadByte();
            int charge = reader.ReadInt16();
            bool active = reader.ReadBoolean();
            int generation = reader.ReadInt32();
            if (Main.player.IndexInRange(playerIndex))
                Main.player[playerIndex].GetModPlayer<CallofDutyPlayer>().ReceiveState(charge, active, generation);
        }
    }
}
