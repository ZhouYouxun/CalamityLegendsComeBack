using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.PlayerSaddle
{
    // ── Item ─────────────────────────────────────────────────────────────────
    public class PlayerSaddleItem : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.PaintedHorseSaddle;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(220, 180, 255));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.UseSound = null;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.LightPurple;
            Item.maxStack = 1;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return false;
            return !Main.mapFullscreen && !Main.drawingPlayerChat && !Main.gameMenu && !player.mouseInterface;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return null;

            var saddle = player.GetModPlayer<PlayerSaddlePlayer>();

            // If currently riding someone: dismount
            if (saddle.MountTargetWhoAmI >= 0)
            {
                PlayerSaddlePackets.SendDismount(player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.2f }, player.Center);
                return true;
            }

            // Right-click: throw off current rider (if someone is riding you)
            if (player.altFunctionUse == 2)
            {
                if (saddle.RiderWhoAmI >= 0)
                {
                    PlayerSaddlePackets.SendDismount(saddle.RiderWhoAmI);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.1f }, player.Center);
                }
                return true;
            }

            // Find nearest player to mouse cursor
            int target = FindNearestPlayerToMouse(player.whoAmI);
            if (target < 0)
            {
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerSaddleNoTarget"),
                    new Color(220, 180, 255));
                return true;
            }

            // Check target isn't already occupied
            if (Main.player[target].GetModPlayer<PlayerSaddlePlayer>().RiderWhoAmI >= 0)
            {
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerSaddleOccupied"),
                    new Color(255, 160, 100));
                return true;
            }

            PlayerSaddlePackets.SendMount(player.whoAmI, target);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.15f }, player.Center);
            return true;
        }

        private static int FindNearestPlayerToMouse(int selfId)
        {
            const float screenRadius = 80f;
            int nearest = -1;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || i == selfId) continue;
                float dist = Vector2.Distance(p.Center - Main.screenPosition, Main.MouseScreen);
                if (dist < screenRadius && dist < nearestDist) { nearest = i; nearestDist = dist; }
            }
            return nearest;
        }
    }

    // ── ModPlayer ────────────────────────────────────────────────────────────
    public class PlayerSaddlePlayer : ModPlayer
    {
        public int MountTargetWhoAmI = -1;  // Who am I riding (-1 = not riding)
        public int RiderWhoAmI = -1;         // Who is riding me (-1 = none)

        public override void ResetEffects() { }  // Don't reset; controlled by packets

        public override void PreUpdate()
        {
            if (MountTargetWhoAmI < 0 || Main.myPlayer != Player.whoAmI) return;

            // Block movement inputs, allow everything else
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlJump = false;
            Player.controlDown = false;
            Player.controlUp = false;
        }

        public override void PostUpdate()
        {
            if (MountTargetWhoAmI < 0) return;

            Player target = Main.player[MountTargetWhoAmI];

            // Auto-dismount if target is gone/dead
            if (!target.active || target.dead)
            {
                if (Main.myPlayer == Player.whoAmI)
                    PlayerSaddlePackets.SendDismount(Player.whoAmI);
                else
                    ApplyDismount(Player.whoAmI); // Local cleanup for other clients
                return;
            }

            // Snap rider to target's head (only on rider's own client for position authority)
            if (Main.myPlayer == Player.whoAmI || Main.netMode == NetmodeID.Server)
            {
                Player.position = new Vector2(
                    target.Center.X - Player.width * 0.5f,
                    target.position.Y - Player.height + 2f
                );
                Player.velocity = Vector2.Zero;
                Player.noFallDmg = true;
                Player.fallStart = (int)(Player.position.Y / 16f);
                Player.gravDir = target.gravDir;
            }
        }

        // ── Static state helpers ──────────────────────────────────────────────

        public static void ApplyMount(int riderId, int targetId)
        {
            if (riderId < 0 || riderId >= Main.maxPlayers) return;
            if (targetId < 0 || targetId >= Main.maxPlayers) return;

            var rider = Main.player[riderId].GetModPlayer<PlayerSaddlePlayer>();
            var tgt = Main.player[targetId].GetModPlayer<PlayerSaddlePlayer>();
            rider.MountTargetWhoAmI = targetId;
            tgt.RiderWhoAmI = riderId;

            if (Main.myPlayer == riderId || Main.myPlayer == targetId)
            {
                string riderName = Main.player[riderId].name;
                string targetName = Main.player[targetId].name;
                Main.NewText(Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.PlayerSaddleMounted",
                    riderName, targetName), new Color(220, 180, 255));
            }
        }

        public static void ApplyDismount(int riderId)
        {
            if (riderId < 0 || riderId >= Main.maxPlayers) return;
            var rider = Main.player[riderId].GetModPlayer<PlayerSaddlePlayer>();
            if (rider.MountTargetWhoAmI >= 0 && rider.MountTargetWhoAmI < Main.maxPlayers)
                Main.player[rider.MountTargetWhoAmI].GetModPlayer<PlayerSaddlePlayer>().RiderWhoAmI = -1;
            rider.MountTargetWhoAmI = -1;
        }
    }

    // ── Packet handlers ───────────────────────────────────────────────────────
    internal static class PlayerSaddlePackets
    {
        private static CalamityLegendsComeBack Mod => ModContent.GetInstance<CalamityLegendsComeBack>();

        public static void SendMount(int riderId, int targetId)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                // Validate
                Player target = Main.player[targetId];
                if (!target.active || target.dead) return;
                if (target.GetModPlayer<PlayerSaddlePlayer>().RiderWhoAmI >= 0) return;
                PlayerSaddlePlayer.ApplyMount(riderId, targetId);
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GamePacketType.PlayerSaddleMount);
            packet.Write((byte)riderId);
            packet.Write((byte)targetId);
            packet.Send();
        }

        public static void SendDismount(int riderId)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                PlayerSaddlePlayer.ApplyDismount(riderId);
                return;
            }

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GamePacketType.PlayerSaddleDismount);
            packet.Write((byte)riderId);
            packet.Send();
        }

        // Called on the server to receive and broadcast mount request
        public static void HandleMount(BinaryReader reader, int senderWhoAmI)
        {
            int riderId = reader.ReadByte();
            int targetId = reader.ReadByte();

            if (riderId != senderWhoAmI) return; // Cheat prevention
            if (riderId < 0 || targetId < 0 || riderId >= Main.maxPlayers || targetId >= Main.maxPlayers) return;

            Player rider = Main.player[riderId];
            Player target = Main.player[targetId];
            if (!rider.active || !target.active || rider.dead || target.dead) return;
            if (target.GetModPlayer<PlayerSaddlePlayer>().RiderWhoAmI >= 0) return;

            // Apply on server
            PlayerSaddlePlayer.ApplyMount(riderId, targetId);

            // Broadcast to all clients
            ModPacket broadcast = Mod.GetPacket();
            broadcast.Write((byte)GamePacketType.PlayerSaddleMount);
            broadcast.Write((byte)riderId);
            broadcast.Write((byte)targetId);
            broadcast.Send(); // Send to all clients
        }

        // Called on server or client receiving a dismount packet
        public static void HandleDismount(BinaryReader reader, int senderWhoAmI)
        {
            int riderId = reader.ReadByte();

            if (Main.netMode == NetmodeID.Server)
            {
                // Validate: only the rider themselves or the player being ridden can send a dismount
                var senderSaddle = Main.player[senderWhoAmI].GetModPlayer<PlayerSaddlePlayer>();
                bool isRider = riderId == senderWhoAmI;
                bool isTarget = senderSaddle.RiderWhoAmI == riderId;
                if (!isRider && !isTarget)
                    return;

                PlayerSaddlePlayer.ApplyDismount(riderId);

                // Broadcast
                ModPacket broadcast = Mod.GetPacket();
                broadcast.Write((byte)GamePacketType.PlayerSaddleDismount);
                broadcast.Write((byte)riderId);
                broadcast.Send();
            }
            else
            {
                // Client received dismount broadcast from server
                PlayerSaddlePlayer.ApplyDismount(riderId);
            }
        }
    }
}
