using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    internal enum GamePacketType : byte
    {
        TetrisInviteRequest = 1,
        TetrisInviteIncoming = 2,
        TetrisInviteResponse = 3,
        TetrisInviteRejected = 4,
        TetrisInviteSent = 5,
        TetrisStartSession = 6,
        TetrisInput = 7,
        TetrisSnapshot = 8,
        PlayerLockerRequestInventory = 9,
        PlayerLockerInventoryData = 10,
        PlayerLockerStealItem = 11,
        PlayerLockerClearSlot = 12,
        PlayerSaddleMount = 13,
        PlayerSaddleDismount = 14,
        ArtisanTokenApplyPrefix = 15,
        ArtisanTokenPrefixApplied = 16
    }

    internal enum TetrisInputCommand : byte
    {
        MoveLeft,
        MoveRight,
        SoftDrop,
        RotateClockwise,
        RotateCounterClockwise,
        HardDrop,
        Hold,
        Reset
    }

    internal static class TetrisMultiplayerPackets
    {
        private const float InviteRange = 1600f;

        public static void HandlePacket(GamePacketType packetType, BinaryReader reader, int whoAmI)
        {
            switch (packetType)
            {
                case GamePacketType.TetrisInviteRequest:
                    HandleInviteRequest(reader, whoAmI);
                    break;
                case GamePacketType.TetrisInviteIncoming:
                    TetrisPanel.ReceiveInviteFrom(reader.ReadByte());
                    break;
                case GamePacketType.TetrisInviteResponse:
                    HandleInviteResponse(reader, whoAmI);
                    break;
                case GamePacketType.TetrisInviteRejected:
                    TetrisPanel.ShowInviteRejected();
                    break;
                case GamePacketType.TetrisInviteSent:
                    TetrisPanel.ShowInviteSent(reader.ReadByte());
                    break;
                case GamePacketType.TetrisStartSession:
                    TetrisPanel.StartMultiplayerSession(
                        reader.ReadByte(),
                        reader.ReadByte() == Main.myPlayer,
                        reader.ReadUInt16(),
                        reader.ReadInt32());
                    break;
                case GamePacketType.TetrisInput:
                    HandleInputPacket(reader, whoAmI);
                    break;
                case GamePacketType.TetrisSnapshot:
                    HandleSnapshotPacket(reader, whoAmI);
                    break;
            }
        }

        public static void TrySendInvite(Player requester)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                TetrisPanel.ShowNoInviteTarget();
                return;
            }

            int target = FindNearestEligibleTetrisPlayer(requester.whoAmI);
            if (target < 0)
            {
                TetrisPanel.ShowNoInviteTarget();
                return;
            }

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInviteRequest);
            packet.Write((byte)target);
            packet.Send();
        }

        public static void SendInviteResponse(int requester, bool accepted)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || !Main.player.IndexInRange(requester))
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInviteResponse);
            packet.Write((byte)requester);
            packet.Write(accepted);
            packet.Send();
        }

        public static void SendInput(int host, ushort sessionId, TetrisInputCommand command)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInput);
            packet.Write((byte)host);
            packet.Write(sessionId);
            packet.Write((byte)command);
            packet.Send();
        }

        public static ModPacket CreateSnapshotPacket(int peer, ushort sessionId)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisSnapshot);
            packet.Write((byte)peer);
            packet.Write(sessionId);
            return packet;
        }

        private static void HandleInviteRequest(BinaryReader reader, int whoAmI)
        {
            int target = reader.ReadByte();
            if (Main.netMode != NetmodeID.Server)
                return;

            if (!ValidateInvitePair(whoAmI, target))
            {
                SendInviteRejected(whoAmI);
                return;
            }

            SendInviteIncoming(target, whoAmI);
            SendInviteSent(whoAmI, target);
        }

        private static void HandleInviteResponse(BinaryReader reader, int whoAmI)
        {
            int requester = reader.ReadByte();
            bool accepted = reader.ReadBoolean();

            if (Main.netMode != NetmodeID.Server)
                return;

            if (!accepted || !ValidateInvitePair(requester, whoAmI))
            {
                SendInviteRejected(requester);
                return;
            }

            ushort sessionId = (ushort)((Main.GameUpdateCount + requester * 397 + whoAmI * 997) & 0xFFFF);
            int seed = Main.rand.Next();
            SendStartSession(requester, whoAmI, requester, sessionId, seed);
            SendStartSession(whoAmI, requester, requester, sessionId, seed);
        }

        private static void HandleInputPacket(BinaryReader reader, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                int host = reader.ReadByte();
                ushort sessionId = reader.ReadUInt16();
                TetrisInputCommand command = (TetrisInputCommand)reader.ReadByte();
                if (!Main.player.IndexInRange(host) || host == whoAmI)
                    return;

                ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
                packet.Write((byte)GamePacketType.TetrisInput);
                packet.Write(sessionId);
                packet.Write((byte)whoAmI);
                packet.Write((byte)command);
                packet.Send(host);
                return;
            }

            ushort clientSessionId = reader.ReadUInt16();
            int sender = reader.ReadByte();
            TetrisInputCommand clientCommand = (TetrisInputCommand)reader.ReadByte();
            TetrisPanel.ReceiveRemoteInput(clientSessionId, sender, clientCommand);
        }

        private static void HandleSnapshotPacket(BinaryReader reader, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                int peer = reader.ReadByte();
                ushort sessionId = reader.ReadUInt16();
                byte[] payload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                if (!Main.player.IndexInRange(peer) || peer == whoAmI)
                    return;

                ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
                packet.Write((byte)GamePacketType.TetrisSnapshot);
                packet.Write(sessionId);
                packet.Write((byte)whoAmI);
                packet.Write(payload);
                packet.Send(peer);
                return;
            }

            ushort clientSessionId = reader.ReadUInt16();
            int host = reader.ReadByte();
            byte[] clientPayload = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            TetrisPanel.ReceiveSnapshot(clientSessionId, host, clientPayload);
        }

        private static void SendInviteIncoming(int target, int requester)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInviteIncoming);
            packet.Write((byte)requester);
            packet.Send(target);
        }

        private static void SendInviteRejected(int requester)
        {
            if (!Main.player.IndexInRange(requester))
                return;

            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInviteRejected);
            packet.Send(requester);
        }

        private static void SendInviteSent(int requester, int target)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisInviteSent);
            packet.Write((byte)target);
            packet.Send(requester);
        }

        private static void SendStartSession(int targetClient, int peer, int host, ushort sessionId, int seed)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.TetrisStartSession);
            packet.Write((byte)peer);
            packet.Write((byte)host);
            packet.Write(sessionId);
            packet.Write(seed);
            packet.Send(targetClient);
        }

        private static bool ValidateInvitePair(int requester, int target)
        {
            if (!Main.player.IndexInRange(requester) || !Main.player.IndexInRange(target) || requester == target)
                return false;

            if (!IsEligibleTetrisPlayer(Main.player[requester]) || !IsEligibleTetrisPlayer(Main.player[target]))
                return false;

            return FindNearestEligibleTetrisPlayer(requester) == target;
        }

        private static int FindNearestEligibleTetrisPlayer(int requester)
        {
            if (!Main.player.IndexInRange(requester))
                return -1;

            Player source = Main.player[requester];
            if (!IsEligibleTetrisPlayer(source))
                return -1;

            int nearest = -1;
            float bestDistance = InviteRange * InviteRange;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (i == requester)
                    continue;

                Player candidate = Main.player[i];
                if (!IsEligibleTetrisPlayer(candidate))
                    continue;

                float distance = Vector2.DistanceSquared(source.Center, candidate.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                nearest = i;
            }

            return nearest;
        }

        private static bool IsEligibleTetrisPlayer(Player player)
        {
            return player != null &&
                   player.active &&
                   !player.dead &&
                   player.HeldItem != null &&
                   player.HeldItem.type == ModContent.ItemType<Tetris>();
        }
    }

    public class Tetris : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private static int PanelType => ModContent.ProjectileType<TetrisPanel>();

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override void HoldItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return;

            player.Calamity().rightClickListener = true;
            if (Main.mouseRight && Main.mouseRightRelease && !Main.mapFullscreen && !Main.drawingPlayerChat && !Main.gameMenu)
            {
                Main.mouseRightRelease = false;
                TetrisMultiplayerPackets.TrySendInvite(player);
            }

            if (TryKeepExistingPanel(player))
                return;

            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                PanelType,
                0,
                0f,
                player.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.58f, Pitch = 0.08f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.26f, Pitch = 0.22f }, player.Center);
        }

        private static bool TryKeepExistingPanel(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                if (projectile.ModProjectile is TetrisPanel panel)
                    panel.RequestStayOpen();
                else
                    projectile.ai[0] = 0f;

                return true;
            }

            return false;
        }
    }

    internal sealed class TetrisPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int BoardColumns = 10;
        private const int VisibleRows = 20;
        private const int HiddenRows = 2;
        private const int BoardRows = VisibleRows + HiddenRows;
        private const int CellSize = 30;
        private const int PanelPadding = 18;
        private const int HeaderHeight = 44;
        private const int SidebarGap = 16;
        private const int SidebarWidth = 138;
        private const int BorderThickness = 2;
        private const int LockDelay = 28;
        private const int MoveRepeatDelay = 11;
        private const int MoveRepeatRate = 3;
        private const int ClearFlashTime = 14;

        private static readonly Point[][][] PieceCells =
        {
            new Point[][]
            {
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) },
                new[] { new Point(2, 0), new Point(2, 1), new Point(2, 2), new Point(2, 3) },
                new[] { new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(1, 3) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(1, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new[] { new Point(1, 1), new Point(2, 1), new Point(0, 2), new Point(1, 2) },
                new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(2, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(0, 2) }
            },
            new Point[][]
            {
                new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(1, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(0, 2), new Point(1, 2) }
            },
            new Point[][]
            {
                new[] { new Point(2, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2) },
                new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(1, 2) }
            }
        };

        private static readonly Color[] PieceColors =
        {
            new Color(88, 210, 242),
            new Color(244, 214, 74),
            new Color(176, 105, 238),
            new Color(96, 210, 112),
            new Color(238, 82, 90),
            new Color(82, 128, 238),
            new Color(242, 156, 68)
        };

        private static readonly Point[] RotationKicks =
        {
            Point.Zero,
            new Point(-1, 0),
            new Point(1, 0),
            new Point(-2, 0),
            new Point(2, 0),
            new Point(0, -1),
            new Point(-1, -1),
            new Point(1, -1)
        };

        private readonly int[,] board = new int[BoardRows, BoardColumns];
        private readonly List<int> nextQueue = new();
        private int[] clearingRows = Array.Empty<int>();
        private UnifiedRandom random = new();

        private int currentPiece;
        private int currentRotation;
        private int currentX;
        private int currentY;
        private int holdPiece = -1;
        private int fallTimer;
        private int lockTimer;
        private int leftHeldTicks;
        private int rightHeldTicks;
        private int clearTimer;
        private int score;
        private int lines;
        private bool canHold = true;
        private bool initialized;
        private bool gameOver;
        private int secondPiece = -1;
        private int secondRotation;
        private int secondX;
        private int secondY;
        private int pendingInviteFrom = -1;
        private int pendingInviteTimer;
        private int multiplayerPeer = -1;
        private ushort multiplayerSessionId;
        private bool multiplayerSessionActive;
        private bool multiplayerHost;
        private int snapshotTimer;
        private int guestSoftDropTimer;
        private readonly List<TetrisInputCommand> queuedRemoteInputs = new();

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int BoardPixelWidth => BoardColumns * CellSize;
        private static int BoardPixelHeight => VisibleRows * CellSize;
        private static int PanelWidth => PanelPadding * 2 + BoardPixelWidth + SidebarGap + SidebarWidth;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + BoardPixelHeight;
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);
        private bool MultiplayerDualMode => multiplayerSessionActive;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = PanelWidth;
            Projectile.height = PanelHeight;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem.type != ModContent.ItemType<Tetris>())
                FadeOut = true;
            else
                FadeOut = false;

            Rectangle panelArea = GetPanelArea();
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelArea.Center.ToVector2() : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner && !FadeOut && Projectile.Opacity >= 0.92f)
                UpdateGame(owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Rectangle panelArea = GetPanelArea();
            Rectangle boardArea = GetBoardArea(panelArea);
            DrawPanel(panelArea, Projectile.Opacity);
            DrawBoard(boardArea, Projectile.Opacity);
            DrawSidebar(panelArea, boardArea, Projectile.Opacity);

            if (gameOver)
                DrawGameOver(boardArea, Projectile.Opacity);

            if (pendingInviteFrom >= 0)
                DrawInviteOverlay(panelArea, Projectile.Opacity);

            if (panelArea.Intersects(MouseRectangle))
            {
                Main.blockMouse = true;
                Main.player[Projectile.owner].mouseInterface = true;
            }

            return false;
        }

        public void RequestStayOpen()
        {
            FadeOut = false;
        }

        internal static void ReceiveInviteFrom(int requester)
        {
            if (!Main.player.IndexInRange(requester))
                return;

            TetrisPanel panel = EnsureLocalPanel();
            if (panel == null)
            {
                TetrisMultiplayerPackets.SendInviteResponse(requester, false);
                return;
            }

            panel.pendingInviteFrom = requester;
            panel.pendingInviteTimer = 10 * 60;
            panel.FadeOut = false;

            ShowText(
                Color.Cyan,
                "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisInviteIncoming",
                GetPlayerName(requester));

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.5f, Pitch = 0.18f }, Main.LocalPlayer.Center);
        }

        internal static void StartMultiplayerSession(int peer, bool isHost, ushort sessionId, int seed)
        {
            TetrisPanel panel = EnsureLocalPanel();
            if (panel == null)
                return;

            panel.multiplayerPeer = peer;
            panel.multiplayerHost = isHost;
            panel.multiplayerSessionActive = true;
            panel.multiplayerSessionId = sessionId;
            panel.pendingInviteFrom = -1;
            panel.pendingInviteTimer = 0;
            panel.snapshotTimer = 0;
            panel.guestSoftDropTimer = 0;
            panel.queuedRemoteInputs.Clear();
            panel.random = new UnifiedRandom(seed);
            panel.ResetGame(Main.LocalPlayer);
            panel.FadeOut = false;

            ShowText(Color.Cyan, "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisInviteAccepted");
        }

        internal static void ReceiveRemoteInput(ushort sessionId, int sender, TetrisInputCommand command)
        {
            TetrisPanel panel = FindLocalPanel();
            if (panel == null ||
                !panel.multiplayerSessionActive ||
                !panel.multiplayerHost ||
                panel.multiplayerSessionId != sessionId ||
                panel.multiplayerPeer != sender)
            {
                return;
            }

            panel.queuedRemoteInputs.Add(command);
        }

        internal static void ReceiveSnapshot(ushort sessionId, int host, byte[] payload)
        {
            TetrisPanel panel = FindLocalPanel();
            if (panel == null ||
                !panel.multiplayerSessionActive ||
                panel.multiplayerHost ||
                panel.multiplayerSessionId != sessionId ||
                host != panel.multiplayerPeer)
            {
                return;
            }

            using MemoryStream stream = new(payload);
            using BinaryReader payloadReader = new(stream);
            panel.ReadSnapshotPayload(payloadReader);
        }

        internal static void ShowNoInviteTarget()
        {
            ShowText(Color.OrangeRed, "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisNoInviteTarget");
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.46f, Pitch = -0.22f }, Main.LocalPlayer.Center);
        }

        internal static void ShowInviteRejected()
        {
            ShowText(Color.OrangeRed, "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisInviteRejected");
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f, Pitch = -0.24f }, Main.LocalPlayer.Center);
        }

        internal static void ShowInviteSent(int target)
        {
            ShowText(
                Color.Cyan,
                "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisInviteSent",
                GetPlayerName(target));
        }

        private static TetrisPanel EnsureLocalPanel()
        {
            TetrisPanel panel = FindLocalPanel();
            if (panel != null)
                return panel;

            Player localPlayer = Main.LocalPlayer;
            if (!localPlayer.active ||
                localPlayer.dead ||
                localPlayer.HeldItem == null ||
                localPlayer.HeldItem.type != ModContent.ItemType<Tetris>())
            {
                return null;
            }

            int projectileIndex = Projectile.NewProjectile(
                localPlayer.HeldItem.GetSource_FromThis(),
                localPlayer.Center,
                Vector2.Zero,
                ModContent.ProjectileType<TetrisPanel>(),
                0,
                0f,
                localPlayer.whoAmI);

            return Main.projectile.IndexInRange(projectileIndex) &&
                   Main.projectile[projectileIndex].ModProjectile is TetrisPanel createdPanel
                ? createdPanel
                : null;
        }

        private static TetrisPanel FindLocalPanel()
        {
            int panelType = ModContent.ProjectileType<TetrisPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.owner == Main.myPlayer &&
                    projectile.type == panelType &&
                    projectile.ModProjectile is TetrisPanel panel)
                {
                    return panel;
                }
            }

            return null;
        }

        private static void ShowText(Color color, string key, params object[] args)
        {
            if (Main.dedServ)
                return;

            string text = Language.GetTextValue(key, args);
            CombatText.NewText(Main.LocalPlayer.Hitbox, color, text);
        }

        private static string GetPlayerName(int playerIndex)
        {
            return Main.player.IndexInRange(playerIndex) && !string.IsNullOrWhiteSpace(Main.player[playerIndex].name)
                ? Main.player[playerIndex].name
                : "Player";
        }

        private void UpdateGame(Player owner)
        {
            if (!initialized)
                ResetGame(owner);

            HandlePendingInvite(owner);

            if (IsInputPaused())
                return;

            if (multiplayerSessionActive && !multiplayerHost)
            {
                SendGuestInputs();
                return;
            }

            if (multiplayerSessionActive && multiplayerHost)
                ProcessRemoteInputs(owner);

            if (gameOver)
            {
                if (JustPressed(Keys.R))
                {
                    ResetGame(owner);
                    SendMultiplayerSnapshot();
                }

                return;
            }

            if (clearTimer > 0)
            {
                clearTimer--;
                if (clearTimer <= 0)
                {
                    FinishLineClear(owner);
                    SendMultiplayerSnapshot();
                }

                return;
            }

            HandleHorizontalInput(owner);

            if (JustPressed(Keys.Up) || JustPressed(Keys.W) || JustPressed(Keys.X))
                TryRotate(1, owner);

            if (JustPressed(Keys.Z))
                TryRotate(-1, owner);

            if (JustPressed(Keys.Space))
            {
                HardDrop(owner);
                SendMultiplayerSnapshot();
                return;
            }

            if (JustPressed(Keys.C) || JustPressed(Keys.LeftShift) || JustPressed(Keys.RightShift))
                HoldCurrentPiece(owner);

            bool softDrop = Down(Keys.Down) || Down(Keys.S);
            fallTimer++;
            int interval = softDrop ? 2 : GetFallInterval();
            if (fallTimer >= interval)
            {
                fallTimer = 0;
                if (TryMove(0, 1, softDrop ? 1 : 0, owner))
                {
                    SendMultiplayerSnapshot();
                    return;
                }
            }

            if (!CanCurrentPieceFall())
            {
                lockTimer++;
                if (lockTimer >= LockDelay)
                {
                    LockPiece(owner);
                    SendMultiplayerSnapshot();
                }
            }
            else
                lockTimer = 0;

            if (multiplayerSessionActive && multiplayerHost && ++snapshotTimer >= 6)
            {
                snapshotTimer = 0;
                SendMultiplayerSnapshot();
            }
        }

        private void HandlePendingInvite(Player owner)
        {
            if (pendingInviteFrom < 0)
                return;

            if (!Main.player.IndexInRange(pendingInviteFrom) || pendingInviteTimer-- <= 0)
            {
                TetrisMultiplayerPackets.SendInviteResponse(pendingInviteFrom, false);
                pendingInviteFrom = -1;
                pendingInviteTimer = 0;
                return;
            }

            if (JustPressed(Keys.Y) || JustPressed(Keys.Enter))
            {
                TetrisMultiplayerPackets.SendInviteResponse(pendingInviteFrom, true);
                pendingInviteFrom = -1;
                pendingInviteTimer = 0;
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.5f, Pitch = 0.22f }, owner.Center);
                return;
            }

            if (JustPressed(Keys.N) || JustPressed(Keys.Escape))
            {
                TetrisMultiplayerPackets.SendInviteResponse(pendingInviteFrom, false);
                pendingInviteFrom = -1;
                pendingInviteTimer = 0;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.48f, Pitch = -0.25f }, owner.Center);
            }
        }

        private void SendGuestInputs()
        {
            if (!multiplayerSessionActive || multiplayerHost || multiplayerPeer < 0)
                return;

            bool left = Down(Keys.Left) || Down(Keys.A);
            bool right = Down(Keys.Right) || Down(Keys.D);

            if (left && !right)
            {
                leftHeldTicks++;
                rightHeldTicks = 0;
                if (JustPressed(Keys.Left) || JustPressed(Keys.A) || leftHeldTicks > MoveRepeatDelay && leftHeldTicks % MoveRepeatRate == 0)
                    SendInputCommand(TetrisInputCommand.MoveLeft);
            }
            else if (right && !left)
            {
                rightHeldTicks++;
                leftHeldTicks = 0;
                if (JustPressed(Keys.Right) || JustPressed(Keys.D) || rightHeldTicks > MoveRepeatDelay && rightHeldTicks % MoveRepeatRate == 0)
                    SendInputCommand(TetrisInputCommand.MoveRight);
            }
            else
            {
                leftHeldTicks = 0;
                rightHeldTicks = 0;
            }

            if (JustPressed(Keys.Up) || JustPressed(Keys.W) || JustPressed(Keys.X))
                SendInputCommand(TetrisInputCommand.RotateClockwise);

            if (JustPressed(Keys.Z))
                SendInputCommand(TetrisInputCommand.RotateCounterClockwise);

            if (JustPressed(Keys.Space))
                SendInputCommand(TetrisInputCommand.HardDrop);

            if (JustPressed(Keys.C) || JustPressed(Keys.LeftShift) || JustPressed(Keys.RightShift))
                SendInputCommand(TetrisInputCommand.Hold);

            if (JustPressed(Keys.R))
                SendInputCommand(TetrisInputCommand.Reset);

            if (Down(Keys.Down) || Down(Keys.S))
            {
                guestSoftDropTimer++;
                if (guestSoftDropTimer % 2 == 0)
                    SendInputCommand(TetrisInputCommand.SoftDrop);
            }
            else
                guestSoftDropTimer = 0;
        }

        private void SendInputCommand(TetrisInputCommand command)
        {
            TetrisMultiplayerPackets.SendInput(multiplayerPeer, multiplayerSessionId, command);
        }

        private void ProcessRemoteInputs(Player owner)
        {
            if (queuedRemoteInputs.Count <= 0)
                return;

            for (int i = 0; i < queuedRemoteInputs.Count; i++)
                ApplyInputCommand(queuedRemoteInputs[i], owner);

            queuedRemoteInputs.Clear();
            SendMultiplayerSnapshot();
        }

        private void ApplyInputCommand(TetrisInputCommand command, Player owner)
        {
            if (gameOver && command != TetrisInputCommand.Reset)
                return;

            if (clearTimer > 0 && command != TetrisInputCommand.Reset)
                return;

            switch (command)
            {
                case TetrisInputCommand.MoveLeft:
                    TryMove(-1, 0, 0, owner);
                    break;
                case TetrisInputCommand.MoveRight:
                    TryMove(1, 0, 0, owner);
                    break;
                case TetrisInputCommand.SoftDrop:
                    TryMove(0, 1, 1, owner);
                    break;
                case TetrisInputCommand.RotateClockwise:
                    TryRotate(1, owner);
                    break;
                case TetrisInputCommand.RotateCounterClockwise:
                    TryRotate(-1, owner);
                    break;
                case TetrisInputCommand.HardDrop:
                    HardDrop(owner);
                    break;
                case TetrisInputCommand.Hold:
                    HoldCurrentPiece(owner);
                    break;
                case TetrisInputCommand.Reset:
                    ResetGame(owner);
                    break;
            }
        }

        private void HandleHorizontalInput(Player owner)
        {
            bool left = Down(Keys.Left) || Down(Keys.A);
            bool right = Down(Keys.Right) || Down(Keys.D);

            if (left && !right)
            {
                leftHeldTicks++;
                rightHeldTicks = 0;
                if (JustPressed(Keys.Left) || JustPressed(Keys.A) || leftHeldTicks > MoveRepeatDelay && leftHeldTicks % MoveRepeatRate == 0)
                    TryMove(-1, 0, 0, owner);
            }
            else if (right && !left)
            {
                rightHeldTicks++;
                leftHeldTicks = 0;
                if (JustPressed(Keys.Right) || JustPressed(Keys.D) || rightHeldTicks > MoveRepeatDelay && rightHeldTicks % MoveRepeatRate == 0)
                    TryMove(1, 0, 0, owner);
            }
            else
            {
                leftHeldTicks = 0;
                rightHeldTicks = 0;
            }
        }

        private bool TryMove(int offsetX, int offsetY, int scoreBonus, Player owner)
        {
            if (MultiplayerDualMode)
            {
                if (!CanPlacePiecePair(currentX + offsetX, currentY + offsetY, currentRotation, secondX + offsetX, secondY + offsetY, secondRotation))
                    return false;

                currentX += offsetX;
                currentY += offsetY;
                secondX += offsetX;
                secondY += offsetY;
            }
            else
            {
                if (!CanPlace(currentPiece, currentX + offsetX, currentY + offsetY, currentRotation))
                    return false;

                currentX += offsetX;
                currentY += offsetY;
            }

            if (scoreBonus > 0)
                score += scoreBonus;

            if (offsetX != 0)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.18f, Pitch = 0.34f }, owner.Center);

            if (CanCurrentPieceFall())
                lockTimer = 0;

            return true;
        }

        private void TryRotate(int direction, Player owner)
        {
            int nextRotation = WrapRotation(currentRotation + direction);
            int nextSecondRotation = WrapRotation(secondRotation + direction);
            for (int i = 0; i < RotationKicks.Length; i++)
            {
                Point kick = RotationKicks[i];
                bool canRotate = MultiplayerDualMode
                    ? CanPlacePiecePair(currentX + kick.X, currentY + kick.Y, nextRotation, secondX + kick.X, secondY + kick.Y, nextSecondRotation)
                    : CanPlace(currentPiece, currentX + kick.X, currentY + kick.Y, nextRotation);

                if (!canRotate)
                    continue;

                currentX += kick.X;
                currentY += kick.Y;
                currentRotation = nextRotation;
                if (MultiplayerDualMode)
                {
                    secondX += kick.X;
                    secondY += kick.Y;
                    secondRotation = nextSecondRotation;
                }

                lockTimer = 0;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.3f, Pitch = direction > 0 ? 0.12f : -0.08f }, owner.Center);
                return;
            }
        }

        private void HardDrop(Player owner)
        {
            int distance = 0;
            while (CanCurrentPieceFall())
            {
                currentY++;
                if (MultiplayerDualMode)
                    secondY++;

                distance++;
            }

            score += distance * (MultiplayerDualMode ? 4 : 2);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = -0.18f }, owner.Center);
            LockPiece(owner);
        }

        private void HoldCurrentPiece(Player owner)
        {
            if (MultiplayerDualMode)
                return;

            if (!canHold)
                return;

            int held = holdPiece;
            holdPiece = currentPiece;
            canHold = false;

            if (held < 0)
                SpawnNextPiece(owner, false);
            else
                SpawnPiece(held, owner);

            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.28f, Pitch = 0.16f }, owner.Center);
        }

        private void LockPiece(Player owner)
        {
            PlacePieceOnBoard(currentPiece, currentX, currentY, currentRotation);
            if (MultiplayerDualMode)
                PlacePieceOnBoard(secondPiece, secondX, secondY, secondRotation);

            if (HasBlocksInHiddenRows())
            {
                SetGameOver(owner);
                return;
            }

            clearingRows = FindFullRows();
            if (clearingRows.Length > 0)
            {
                clearTimer = ClearFlashTime;
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.46f, Pitch = 0.26f + clearingRows.Length * 0.05f }, owner.Center);
                return;
            }

            SpawnNextPiece(owner);
        }

        private void FinishLineClear(Player owner)
        {
            int writeRow = BoardRows - 1;
            for (int readRow = BoardRows - 1; readRow >= 0; readRow--)
            {
                if (IsClearingRow(readRow))
                    continue;

                for (int x = 0; x < BoardColumns; x++)
                    board[writeRow, x] = board[readRow, x];

                writeRow--;
            }

            for (int y = writeRow; y >= 0; y--)
            {
                for (int x = 0; x < BoardColumns; x++)
                    board[y, x] = 0;
            }

            int cleared = clearingRows.Length;
            clearingRows = Array.Empty<int>();
            lines += cleared;
            score += GetLineScore(cleared) * GetLevel();
            SpawnNextPiece(owner);
        }

        private void ResetGame(Player owner)
        {
            if (!multiplayerSessionActive)
                random = new UnifiedRandom((int)(Main.GameUpdateCount + owner.whoAmI * 997 + Projectile.identity * 131));

            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                    board[y, x] = 0;
            }

            nextQueue.Clear();
            clearingRows = Array.Empty<int>();
            holdPiece = -1;
            secondPiece = -1;
            secondRotation = 0;
            secondX = 0;
            secondY = 0;
            fallTimer = 0;
            lockTimer = 0;
            leftHeldTicks = 0;
            rightHeldTicks = 0;
            clearTimer = 0;
            score = 0;
            lines = 0;
            canHold = true;
            gameOver = false;
            initialized = true;
            RefillQueue();
            SpawnNextPiece(owner, false);
        }

        private void SpawnNextPiece(Player owner, bool resetHold = true)
        {
            EnsureQueue(MultiplayerDualMode ? 8 : 7);
            int next = nextQueue[0];
            nextQueue.RemoveAt(0);

            if (MultiplayerDualMode)
            {
                EnsureQueue(7);
                int second = nextQueue[0];
                nextQueue.RemoveAt(0);
                EnsureQueue(8);
                SpawnPiecePair(next, second, owner);
            }
            else
            {
                EnsureQueue();
                SpawnPiece(next, owner);
            }

            if (resetHold)
                canHold = true;
        }

        private void SpawnPiece(int piece, Player owner)
        {
            currentPiece = piece;
            currentRotation = 0;
            currentX = 3;
            currentY = 0;
            fallTimer = 0;
            lockTimer = 0;

            if (!CanPlace(currentPiece, currentX, currentY, currentRotation))
                SetGameOver(owner);
        }

        private void SpawnPiecePair(int first, int second, Player owner)
        {
            currentPiece = first;
            currentRotation = 0;
            currentX = 0;
            currentY = 0;

            secondPiece = second;
            secondRotation = 0;
            secondX = 6;
            secondY = 0;

            fallTimer = 0;
            lockTimer = 0;

            if (!CanPlacePiecePair(currentX, currentY, currentRotation, secondX, secondY, secondRotation))
                SetGameOver(owner);
        }

        private void SetGameOver(Player owner)
        {
            gameOver = true;
            clearTimer = 0;
            clearingRows = Array.Empty<int>();
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = -0.25f }, owner.Center);
            SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = -0.12f }, owner.Center);
        }

        private bool CanPlace(int piece, int originX, int originY, int rotation)
        {
            foreach (Point cell in PieceCells[piece][rotation])
            {
                int boardX = originX + cell.X;
                int boardY = originY + cell.Y;

                if (boardX < 0 || boardX >= BoardColumns || boardY >= BoardRows)
                    return false;

                if (boardY >= 0 && board[boardY, boardX] != 0)
                    return false;
            }

            return true;
        }

        private bool CanPlacePiecePair(int firstX, int firstY, int firstRotation, int pairX, int pairY, int pairRotation)
        {
            if (secondPiece < 0)
                return CanPlace(currentPiece, firstX, firstY, firstRotation);

            if (!CanPlace(currentPiece, firstX, firstY, firstRotation) ||
                !CanPlace(secondPiece, pairX, pairY, pairRotation))
            {
                return false;
            }

            foreach (Point firstCell in PieceCells[currentPiece][firstRotation])
            {
                int firstBoardX = firstX + firstCell.X;
                int firstBoardY = firstY + firstCell.Y;

                foreach (Point secondCell in PieceCells[secondPiece][pairRotation])
                {
                    if (firstBoardX == pairX + secondCell.X && firstBoardY == pairY + secondCell.Y)
                        return false;
                }
            }

            return true;
        }

        private bool CanCurrentPieceFall()
        {
            return MultiplayerDualMode
                ? CanPlacePiecePair(currentX, currentY + 1, currentRotation, secondX, secondY + 1, secondRotation)
                : CanPlace(currentPiece, currentX, currentY + 1, currentRotation);
        }

        private void PlacePieceOnBoard(int piece, int originX, int originY, int rotation)
        {
            if (piece < 0)
                return;

            foreach (Point cell in PieceCells[piece][rotation])
            {
                int boardX = originX + cell.X;
                int boardY = originY + cell.Y;
                if (!InBoard(boardX, boardY))
                    continue;

                board[boardY, boardX] = piece + 1;
            }
        }

        private int[] FindFullRows()
        {
            List<int> rows = new();
            for (int y = 0; y < BoardRows; y++)
            {
                bool full = true;
                for (int x = 0; x < BoardColumns; x++)
                {
                    if (board[y, x] != 0)
                    {
                        continue;
                    }

                    full = false;
                    break;
                }

                if (full)
                    rows.Add(y);
            }

            return rows.ToArray();
        }

        private bool HasBlocksInHiddenRows()
        {
            for (int y = 0; y < HiddenRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                {
                    if (board[y, x] != 0)
                        return true;
                }
            }

            return false;
        }

        private bool IsClearingRow(int row)
        {
            for (int i = 0; i < clearingRows.Length; i++)
            {
                if (clearingRows[i] == row)
                    return true;
            }

            return false;
        }

        private void EnsureQueue(int minimumCount = 7)
        {
            while (nextQueue.Count < minimumCount)
                RefillQueue();
        }

        private void RefillQueue()
        {
            List<int> bag = new() { 0, 1, 2, 3, 4, 5, 6 };
            while (bag.Count > 0)
            {
                int index = random.Next(bag.Count);
                nextQueue.Add(bag[index]);
                bag.RemoveAt(index);
            }
        }

        private int GetLevel() => Math.Max(1, lines / 10 + 1);

        private int GetFallInterval()
        {
            int level = GetLevel();
            if (level <= 1)
                return 44;

            if (level <= 9)
                return Math.Max(8, 44 - (level - 1) * 4);

            return Math.Max(4, 10 - (level - 9) / 2);
        }

        private static int GetLineScore(int cleared)
        {
            return cleared switch
            {
                1 => 100,
                2 => 300,
                3 => 500,
                4 => 800,
                _ => 0
            };
        }

        private static bool InBoard(int x, int y)
        {
            return x >= 0 && x < BoardColumns && y >= 0 && y < BoardRows;
        }

        private static int WrapRotation(int rotation)
        {
            rotation %= 4;
            return rotation < 0 ? rotation + 4 : rotation;
        }

        private static bool Down(Keys key) => Main.keyState.IsKeyDown(key);

        private static bool JustPressed(Keys key)
        {
            return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
        }

        private static bool IsInputPaused()
        {
            return Main.mapFullscreen || Main.drawingPlayerChat || Main.gameMenu;
        }

        private static Rectangle GetPanelArea()
        {
            const int screenMargin = 16;
            int x = (Main.screenWidth - PanelWidth) / 2;
            int y = (Main.screenHeight - PanelHeight) / 2;
            int maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            int maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            x = Math.Min(Math.Max(x, screenMargin), maxX);
            y = Math.Min(Math.Max(y, screenMargin), maxY);
            return new Rectangle(x, y, PanelWidth, PanelHeight);
        }

        private static Rectangle GetBoardArea(Rectangle panelArea)
        {
            return new Rectangle(
                panelArea.X + PanelPadding,
                panelArea.Y + PanelPadding + HeaderHeight,
                BoardPixelWidth,
                BoardPixelHeight);
        }

        private static Rectangle GetSidebarArea(Rectangle panelArea, Rectangle boardArea)
        {
            return new Rectangle(
                boardArea.Right + SidebarGap,
                boardArea.Y,
                SidebarWidth,
                boardArea.Height);
        }

        private void SendMultiplayerSnapshot()
        {
            if (!multiplayerSessionActive || !multiplayerHost || multiplayerPeer < 0 || Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = TetrisMultiplayerPackets.CreateSnapshotPacket(multiplayerPeer, multiplayerSessionId);
            WriteSnapshotPayload(packet);
            packet.Send();
        }

        private void WriteSnapshotPayload(BinaryWriter writer)
        {
            writer.Write(initialized);
            writer.Write(gameOver);
            writer.Write(canHold);
            writer.Write(currentPiece);
            writer.Write(currentRotation);
            writer.Write(currentX);
            writer.Write(currentY);
            writer.Write(secondPiece);
            writer.Write(secondRotation);
            writer.Write(secondX);
            writer.Write(secondY);
            writer.Write(holdPiece);
            writer.Write(fallTimer);
            writer.Write(lockTimer);
            writer.Write(clearTimer);
            writer.Write(score);
            writer.Write(lines);

            writer.Write((byte)Math.Min(clearingRows.Length, byte.MaxValue));
            for (int i = 0; i < clearingRows.Length && i < byte.MaxValue; i++)
                writer.Write(clearingRows[i]);

            writer.Write((byte)Math.Min(nextQueue.Count, byte.MaxValue));
            for (int i = 0; i < nextQueue.Count && i < byte.MaxValue; i++)
                writer.Write(nextQueue[i]);

            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                    writer.Write((byte)board[y, x]);
            }
        }

        private void ReadSnapshotPayload(BinaryReader reader)
        {
            initialized = reader.ReadBoolean();
            gameOver = reader.ReadBoolean();
            canHold = reader.ReadBoolean();
            currentPiece = reader.ReadInt32();
            currentRotation = reader.ReadInt32();
            currentX = reader.ReadInt32();
            currentY = reader.ReadInt32();
            secondPiece = reader.ReadInt32();
            secondRotation = reader.ReadInt32();
            secondX = reader.ReadInt32();
            secondY = reader.ReadInt32();
            holdPiece = reader.ReadInt32();
            fallTimer = reader.ReadInt32();
            lockTimer = reader.ReadInt32();
            clearTimer = reader.ReadInt32();
            score = reader.ReadInt32();
            lines = reader.ReadInt32();

            int clearingCount = reader.ReadByte();
            clearingRows = new int[clearingCount];
            for (int i = 0; i < clearingCount; i++)
                clearingRows[i] = reader.ReadInt32();

            int queueCount = reader.ReadByte();
            nextQueue.Clear();
            for (int i = 0; i < queueCount; i++)
                nextQueue.Add(reader.ReadInt32());

            for (int y = 0; y < BoardRows; y++)
            {
                for (int x = 0; x < BoardColumns; x++)
                    board[y, x] = reader.ReadByte();
            }
        }

        private void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(10, 12, 18, 238) * opacity);
            DrawBorder(panelArea, new Color(116, 132, 156) * opacity, BorderThickness);
            DrawBorder(new Rectangle(panelArea.X + 3, panelArea.Y + 3, panelArea.Width - 6, panelArea.Height - 6), new Color(32, 40, 56, 220) * opacity, 1);

            DrawTextWithShadow("TETRIS", new Vector2(panelArea.X + PanelPadding, panelArea.Y + 12), new Color(232, 242, 255) * opacity, 0.92f, opacity);
            string state = gameOver
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisGameOver")
                : Language.GetTextValue(
                    multiplayerSessionActive
                        ? "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisMultiplayerLevel"
                        : "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisLevel",
                    GetLevel());

            Vector2 stateSize = FontAssets.MouseText.Value.MeasureString(state) * 0.62f;
            DrawTextWithShadow(state, new Vector2(panelArea.Right - PanelPadding - stateSize.X, panelArea.Y + 18), new Color(178, 204, 236) * opacity, 0.62f, opacity);
        }

        private void DrawBoard(Rectangle boardArea, float opacity)
        {
            DrawRectangle(boardArea, new Color(4, 6, 10, 245) * opacity);
            DrawBorder(boardArea, new Color(86, 98, 120) * opacity, 2);

            for (int visibleY = 0; visibleY < VisibleRows; visibleY++)
            {
                for (int x = 0; x < BoardColumns; x++)
                {
                    Rectangle cellArea = GetCellArea(boardArea, x, visibleY);
                    DrawRectangle(Shrink(cellArea, 1), new Color(13, 16, 23, 210) * opacity);
                }
            }

            DrawGhostPiece(boardArea, opacity);

            for (int boardY = HiddenRows; boardY < BoardRows; boardY++)
            {
                int visibleY = boardY - HiddenRows;
                for (int x = 0; x < BoardColumns; x++)
                {
                    int value = board[boardY, x];
                    if (value <= 0)
                        continue;

                    float flash = IsClearingRow(boardY) ? 0.45f + 0.35f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 40f) : 0f;
                    DrawBlock(GetCellArea(boardArea, x, visibleY), PieceColors[value - 1], opacity, flash);
                }
            }

            if (!gameOver && clearTimer <= 0)
            {
                DrawActivePiece(boardArea, currentPiece, currentX, currentY, currentRotation, opacity);
                if (MultiplayerDualMode)
                    DrawActivePiece(boardArea, secondPiece, secondX, secondY, secondRotation, opacity);
            }
        }

        private void DrawGhostPiece(Rectangle boardArea, float opacity)
        {
            if (gameOver || clearTimer > 0)
                return;

            int ghostY = currentY;
            int ghostSecondY = secondY;
            while (MultiplayerDualMode
                ? CanPlacePiecePair(currentX, ghostY + 1, currentRotation, secondX, ghostSecondY + 1, secondRotation)
                : CanPlace(currentPiece, currentX, ghostY + 1, currentRotation))
            {
                ghostY++;
                if (MultiplayerDualMode)
                    ghostSecondY++;
            }

            if (ghostY == currentY)
                return;

            DrawGhostPieceAt(boardArea, currentPiece, currentX, ghostY, currentRotation, opacity);
            if (MultiplayerDualMode)
                DrawGhostPieceAt(boardArea, secondPiece, secondX, ghostSecondY, secondRotation, opacity);
        }

        private static void DrawActivePiece(Rectangle boardArea, int piece, int originX, int originY, int rotation, float opacity)
        {
            if (piece < 0)
                return;

            foreach (Point cell in PieceCells[piece][rotation])
            {
                int boardX = originX + cell.X;
                int boardY = originY + cell.Y;
                if (boardY < HiddenRows)
                    continue;

                DrawBlock(GetCellArea(boardArea, boardX, boardY - HiddenRows), PieceColors[piece], opacity, 0f);
            }
        }

        private static void DrawGhostPieceAt(Rectangle boardArea, int piece, int originX, int originY, int rotation, float opacity)
        {
            if (piece < 0)
                return;

            Color color = PieceColors[piece];
            foreach (Point cell in PieceCells[piece][rotation])
            {
                int boardX = originX + cell.X;
                int boardY = originY + cell.Y;
                if (boardY < HiddenRows)
                    continue;

                Rectangle area = Shrink(GetCellArea(boardArea, boardX, boardY - HiddenRows), 5);
                DrawBorder(area, color * (opacity * 0.36f), 2);
            }
        }

        private void DrawSidebar(Rectangle panelArea, Rectangle boardArea, float opacity)
        {
            Rectangle sidebar = GetSidebarArea(panelArea, boardArea);
            int y = sidebar.Y;
            DrawPieceBox("HOLD", holdPiece, new Rectangle(sidebar.X, y, sidebar.Width, 100), opacity);
            y += 114;
            DrawPieceBox("NEXT", nextQueue.Count > 0 ? nextQueue[0] : -1, new Rectangle(sidebar.X, y, sidebar.Width, 100), opacity);
            y += 114;

            if (nextQueue.Count > 1)
            {
                Rectangle miniBox = new Rectangle(sidebar.X, y, sidebar.Width, 92);
                DrawInfoBox(miniBox, opacity);
                for (int i = 1; i < Math.Min(4, nextQueue.Count); i++)
                    DrawMiniPiece(nextQueue[i], new Vector2(miniBox.X + 24 + (i - 1) * 42, miniBox.Y + 42), opacity * 0.86f, 0.46f);

                y += 106;
            }

            Rectangle scoreBox = new Rectangle(sidebar.X, y, sidebar.Width, 146);
            DrawInfoBox(scoreBox, opacity);
            DrawStatLine("SCORE", score.ToString(), scoreBox.X + 12, scoreBox.Y + 12, opacity);
            DrawStatLine("LINES", lines.ToString(), scoreBox.X + 12, scoreBox.Y + 56, opacity);
            DrawStatLine("LEVEL", GetLevel().ToString(), scoreBox.X + 12, scoreBox.Y + 100, opacity);

            Rectangle rhythmBox = new Rectangle(sidebar.X, scoreBox.Bottom + 14, sidebar.Width, 82);
            DrawInfoBox(rhythmBox, opacity);
            float lockRatio = !CanCurrentPieceFall() ? lockTimer / (float)LockDelay : 0f;
            int meterWidth = rhythmBox.Width - 24;
            DrawTextWithShadow("LOCK", new Vector2(rhythmBox.X + 12, rhythmBox.Y + 10), new Color(184, 202, 228) * opacity, 0.52f, opacity);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(20, 24, 32) * opacity);
            DrawRectangle(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, (int)(meterWidth * MathHelper.Clamp(lockRatio, 0f, 1f)), 10), new Color(242, 190, 84) * opacity);
            DrawBorder(new Rectangle(rhythmBox.X + 12, rhythmBox.Y + 44, meterWidth, 10), new Color(94, 104, 124) * opacity, 1);
        }

        private void DrawPieceBox(string label, int piece, Rectangle box, float opacity)
        {
            DrawInfoBox(box, opacity);
            DrawTextWithShadow(label, new Vector2(box.X + 12, box.Y + 9), new Color(184, 202, 228) * opacity, 0.54f, opacity);
            if (piece >= 0)
                DrawMiniPiece(piece, box.Center.ToVector2() + new Vector2(-6f, 14f), opacity, 0.7f);
        }

        private static void DrawInfoBox(Rectangle box, float opacity)
        {
            DrawRectangle(box, new Color(16, 20, 28, 230) * opacity);
            DrawBorder(box, new Color(70, 82, 104) * opacity, 1);
        }

        private static void DrawStatLine(string label, string value, int x, int y, float opacity)
        {
            DrawTextWithShadow(label, new Vector2(x, y), new Color(152, 170, 198) * opacity, 0.5f, opacity);
            DrawTextWithShadow(value, new Vector2(x, y + 18), Color.White * opacity, 0.68f, opacity);
        }

        private static void DrawMiniPiece(int piece, Vector2 center, float opacity, float scale)
        {
            Color color = PieceColors[piece];
            Point[] cells = PieceCells[piece][0];
            Rectangle bounds = GetPieceBounds(cells);
            float miniCell = CellSize * scale;
            Vector2 origin = center - new Vector2(bounds.Width * miniCell, bounds.Height * miniCell) * 0.5f;

            foreach (Point cell in cells)
            {
                float drawX = origin.X + (cell.X - bounds.X) * miniCell;
                float drawY = origin.Y + (cell.Y - bounds.Y) * miniCell;
                DrawBlock(new Rectangle((int)drawX, (int)drawY, (int)miniCell, (int)miniCell), color, opacity, 0f);
            }
        }

        private static Rectangle GetPieceBounds(Point[] cells)
        {
            int minX = cells[0].X;
            int maxX = cells[0].X;
            int minY = cells[0].Y;
            int maxY = cells[0].Y;

            for (int i = 1; i < cells.Length; i++)
            {
                minX = Math.Min(minX, cells[i].X);
                maxX = Math.Max(maxX, cells[i].X);
                minY = Math.Min(minY, cells[i].Y);
                maxY = Math.Max(maxY, cells[i].Y);
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void DrawGameOver(Rectangle boardArea, float opacity)
        {
            Rectangle overlay = new Rectangle(boardArea.X + 16, boardArea.Y + boardArea.Height / 2 - 56, boardArea.Width - 32, 112);
            DrawRectangle(overlay, new Color(6, 8, 12, 224) * opacity);
            DrawBorder(overlay, new Color(220, 88, 92) * opacity, 2);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisGameOver");
            string restart = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.TetrisRestart");
            DrawCenteredText(title, new Rectangle(overlay.X, overlay.Y + 22, overlay.Width, 28), new Color(255, 212, 212), 0.78f, opacity);
            DrawCenteredText(restart, new Rectangle(overlay.X, overlay.Y + 60, overlay.Width, 26), new Color(202, 218, 238), 0.52f, opacity);
        }

        private void DrawInviteOverlay(Rectangle panelArea, float opacity)
        {
            Rectangle overlay = new Rectangle(panelArea.X + 30, panelArea.Y + panelArea.Height / 2 - 62, panelArea.Width - 60, 124);
            DrawRectangle(overlay, new Color(6, 10, 14, 232) * opacity);
            DrawBorder(overlay, new Color(82, 208, 240) * opacity, 2);

            string message = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.TheSpecialText.TetrisInviteIncoming",
                GetPlayerName(pendingInviteFrom));

            string[] lines = WrapText(message, overlay.Width - 34, 0.54f);
            int y = overlay.Y + 20;
            for (int i = 0; i < lines.Length; i++)
            {
                DrawCenteredText(lines[i], new Rectangle(overlay.X + 12, y, overlay.Width - 24, 24), new Color(222, 244, 255), 0.54f, opacity);
                y += 24;
            }
        }

        private static Rectangle GetCellArea(Rectangle boardArea, int x, int visibleY)
        {
            return new Rectangle(
                boardArea.X + x * CellSize,
                boardArea.Y + visibleY * CellSize,
                CellSize,
                CellSize);
        }

        private static Rectangle Shrink(Rectangle rectangle, int amount)
        {
            return new Rectangle(
                rectangle.X + amount,
                rectangle.Y + amount,
                rectangle.Width - amount * 2,
                rectangle.Height - amount * 2);
        }

        private static string[] WrapText(string text, int maxWidth, float scale)
        {
            string[] words = text.Split(' ');
            List<string> lines = new();
            string current = string.Empty;

            for (int i = 0; i < words.Length; i++)
            {
                string candidate = string.IsNullOrEmpty(current) ? words[i] : current + " " + words[i];
                if (FontAssets.MouseText.Value.MeasureString(candidate).X * scale <= maxWidth || string.IsNullOrEmpty(current))
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = words[i];
            }

            if (!string.IsNullOrEmpty(current))
                lines.Add(current);

            return lines.Count > 0 ? lines.ToArray() : new[] { text };
        }

        private static void DrawBlock(Rectangle area, Color color, float opacity, float flash)
        {
            Rectangle inner = Shrink(area, 2);
            Color fill = Color.Lerp(color, Color.White, flash);
            DrawRectangle(inner, fill * (opacity * 0.95f));
            DrawRectangle(new Rectangle(inner.X, inner.Y, inner.Width, 4), Color.Lerp(fill, Color.White, 0.42f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Y, 4, inner.Height), Color.Lerp(fill, Color.White, 0.25f) * opacity);
            DrawRectangle(new Rectangle(inner.X, inner.Bottom - 4, inner.Width, 4), Color.Lerp(fill, Color.Black, 0.32f) * opacity);
            DrawRectangle(new Rectangle(inner.Right - 4, inner.Y, 4, inner.Height), Color.Lerp(fill, Color.Black, 0.28f) * opacity);
            DrawBorder(inner, Color.Lerp(fill, Color.Black, 0.18f) * opacity, 1);
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float scale, float opacity)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 position = new(area.Center.X - size.X * 0.5f, area.Center.Y - size.Y * 0.5f);
            DrawTextWithShadow(text, position, color * opacity, scale, opacity);
        }

        private static void DrawTextWithShadow(string text, Vector2 position, Color color, float scale, float opacity)
        {
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                text,
                position,
                color,
                Color.Black * (0.76f * opacity),
                scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
