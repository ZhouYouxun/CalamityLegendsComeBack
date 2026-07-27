using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.BossProgress
{
    internal sealed class BossSummonerPanel : ModProjectile
    {
        private const int Columns = 8;
        private const int SlotSize = 50;
        private const int SlotGap = 6;
        private const int PanelPad = 14;
        private const int SectionHeaderH = 22;
        private const int SectionGap = 10;
        private const float MaxIconDraw = 38f;
        private const int BorderThick = 2;

        private static int PreRows => (BossSummonerRegistry.PreCount + Columns - 1) / Columns;
        private static int HardRows => (BossSummonerRegistry.HardCount + Columns - 1) / Columns;
        private static int PostRows => (BossSummonerRegistry.PostCount + Columns - 1) / Columns;

        private static int SectionH(int rows) => rows * SlotSize + (rows - 1) * SlotGap;
        private static int GridWidth => Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int PanelWidth => PanelPad * 2 + GridWidth;

        private static int PanelHeight =>
            PanelPad +
            SectionHeaderH + SectionGap + SectionH(PreRows) +
            SectionGap +
            SectionHeaderH + SectionGap + SectionH(HardRows) +
            SectionGap +
            SectionHeaderH + SectionGap + SectionH(PostRows) +
            PanelPad;

        private static Rectangle MouseRect => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        private Vector2 panelTopLeft;
        private bool initialized;
        private readonly int[] feedbackTimers = new int[BossSummonerRegistry.Entries.Length];
        private readonly bool[] hoveredPrev = new bool[BossSummonerRegistry.Entries.Length];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

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

            if (owner.HeldItem.type != ModContent.ItemType<CTRLBoss>())
                FadeOut = true;

            if (!initialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 desired = Main.MouseScreen - new Vector2(PanelWidth, PanelHeight) * 0.5f;
                panelTopLeft = Clamp(desired);
                initialized = true;
            }

            Vector2 center = panelTopLeft + new Vector2(PanelWidth, PanelHeight) * 0.5f;
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + center : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            float op = Projectile.Opacity;
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOver = panelArea.Intersects(MouseRect);
            bool leftClick = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClick = Main.mouseRight && Main.mouseRightRelease;
            int clickedIndex = -1;

            DrawPanel(panelArea, op);

            int curY = panelArea.Y + PanelPad;
            curY = DrawSection(panelArea, op, curY, "前期 Boss", new Color(104, 196, 132), 0, BossSummonerRegistry.PreCount, ref clickedIndex, leftClick, op);
            curY += SectionGap;
            curY = DrawSection(panelArea, op, curY, "困难模式 Boss", new Color(104, 178, 238), BossSummonerRegistry.PreCount, BossSummonerRegistry.HardCount, ref clickedIndex, leftClick, op);
            curY += SectionGap;
            DrawSection(panelArea, op, curY, "月亮领主后 Boss", new Color(224, 140, 238), BossSummonerRegistry.PreCount + BossSummonerRegistry.HardCount, BossSummonerRegistry.PostCount, ref clickedIndex, leftClick, op);

            if (clickedIndex >= 0 && op >= 0.95f)
            {
                BossSpawnEntry entry = BossSummonerRegistry.Entries[clickedIndex];
                entry.Spawn(owner);
                feedbackTimers[clickedIndex] = 14;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.68f, Pitch = 0.1f }, owner.Center);
                Main.NewText($"召唤：{entry.DisplayName}", new Color(180, 240, 210));
            }

            if (mouseOver)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }
            else if (!FadeOut && op >= 0.95f && (leftClick || rightClick))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
            }

            return false;
        }

        private int DrawSection(Rectangle panelArea, float op, int startY, string title, Color headerColor, int startIndex, int count, ref int clickedIndex, bool leftClick, float opacity)
        {
            int x0 = panelArea.X + PanelPad;

            DrawRect(new Rectangle(x0, startY, GridWidth, SectionHeaderH), headerColor * (op * 0.18f));
            DrawBorder(new Rectangle(x0, startY, GridWidth, SectionHeaderH), headerColor * (op * 0.6f), 1);

            Vector2 textPos = new(x0 + 6, startY + (SectionHeaderH - FontAssets.MouseText.Value.LineSpacing * 0.5f) * 0.5f);
            Terraria.UI.Chat.ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value, title,
                textPos, headerColor * op, 0f, Vector2.Zero, Vector2.One * 0.5f);

            int contentY = startY + SectionHeaderH + SectionGap;

            for (int i = 0; i < count; i++)
            {
                int globalIdx = startIndex + i;
                BossSpawnEntry entry = BossSummonerRegistry.Entries[globalIdx];
                int col = i % Columns;
                int row = i / Columns;
                int sx = x0 + col * (SlotSize + SlotGap);
                int sy = contentY + row * (SlotSize + SlotGap);
                Rectangle slot = new(sx, sy, SlotSize, SlotSize);

                bool hovered = slot.Intersects(MouseRect);
                if (hovered)
                {
                    Main.hoverItemName = entry.DisplayName;
                    if (!hoveredPrev[globalIdx] && op >= 0.95f)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.38f, Pitch = 0.18f });

                    if (leftClick && op >= 0.95f)
                        clickedIndex = globalIdx;
                }

                DrawBossSlot(slot, entry, hovered, feedbackTimers[globalIdx], headerColor, op);

                hoveredPrev[globalIdx] = hovered;
                if (feedbackTimers[globalIdx] > 0)
                    feedbackTimers[globalIdx]--;
            }

            int rows = (count + Columns - 1) / Columns;
            return contentY + SectionH(rows);
        }

        private static void DrawBossSlot(Rectangle slot, BossSpawnEntry entry, bool hovered, int feedback, Color themeColor, float op)
        {
            Color back = hovered ? new Color(64, 74, 88) : new Color(32, 38, 48);
            Color border = hovered ? Color.Lerp(themeColor, Color.White, 0.36f) : new Color(80, 92, 108);

            if (feedback > 0)
            {
                back = Color.Lerp(back, new Color(120, 106, 54), 0.4f);
                border = Color.Lerp(border, new Color(255, 222, 130), 0.6f);
            }

            DrawRect(slot, back * (op * 0.92f));
            DrawBorder(slot, border * op, BorderThick);

            Texture2D icon = TryLoadIcon(entry.TexturePath);
            if (icon != null)
            {
                Vector2 sz = icon.Size();
                float scale = Math.Min(MaxIconDraw / Math.Max(1f, sz.X), MaxIconDraw / Math.Max(1f, sz.Y));
                if (hovered) scale *= 1.08f;
                if (feedback > 0) scale *= 1.06f;

                Main.EntitySpriteDraw(icon, slot.Center.ToVector2(), null,
                    Color.White * op, 0f, sz * 0.5f, scale, SpriteEffects.None, 0);
            }
            else
            {
                DrawMissingIcon(slot, themeColor, op);
            }
        }

        private static Texture2D TryLoadIcon(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            try
            {
                return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                return null;
            }
        }

        private static void DrawMissingIcon(Rectangle slot, Color color, float op)
        {
            int cx = slot.Center.X, cy = slot.Center.Y;
            DrawRect(new Rectangle(cx - 10, cy - 10, 20, 20), color * (op * 0.3f));
            DrawBorder(new Rectangle(cx - 10, cy - 10, 20, 20), color * (op * 0.7f), 1);
            DrawRect(new Rectangle(cx - 1, cy - 14, 2, 28), color * (op * 0.6f));
            DrawRect(new Rectangle(cx - 14, cy - 1, 28, 2), color * (op * 0.6f));
        }

        private static void DrawPanel(Rectangle area, float op)
        {
            DrawRect(area, new Color(14, 18, 26, 238) * op);
            DrawBorder(area, new Color(80, 96, 118) * op, BorderThick);
            DrawBorder(new Rectangle(area.X + 5, area.Y + 5, area.Width - 10, area.Height - 10),
                new Color(36, 46, 62, 200) * op, 1);
        }

        private static void DrawRect(Rectangle r, Color c) =>
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, c);

        private static void DrawBorder(Rectangle r, Color c, int t)
        {
            DrawRect(new Rectangle(r.X, r.Y, r.Width, t), c);
            DrawRect(new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            DrawRect(new Rectangle(r.X, r.Y, t, r.Height), c);
            DrawRect(new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        private static Vector2 Clamp(Vector2 tl)
        {
            const float m = 12f;
            return new Vector2(
                MathHelper.Clamp(tl.X, m, Math.Max(m, Main.screenWidth - PanelWidth - m)),
                MathHelper.Clamp(tl.Y, m, Math.Max(m, Main.screenHeight - PanelHeight - m)));
        }

        public static bool TryClose(Player player)
        {
            int type = ModContent.ProjectileType<BossSummonerPanel>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != player.whoAmI || p.type != type) continue;
                if (p.ModProjectile is BossSummonerPanel panel) panel.FadeOut = true;
                else p.ai[0] = 1f;
                return true;
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) { }
    }

    internal sealed class BossSpawnEntry
    {
        public readonly string DisplayName;
        public readonly string TexturePath;
        public readonly Action<Player> Spawn;

        public BossSpawnEntry(string displayName, string texturePath, Action<Player> spawn)
        {
            DisplayName = displayName;
            TexturePath = texturePath;
            Spawn = spawn;
        }
    }

    internal static class BossSummonerRegistry
    {
        public static readonly BossSpawnEntry[] Entries;

        public static int PreCount { get; }
        public static int HardCount { get; }
        public static int PostCount { get; }

        static BossSummonerRegistry()
        {
            var pre = new List<BossSpawnEntry>
            {
                E("史莱姆王", "PreHardmode/史莱姆王-图标", p => SpawnVanilla(p, NPCID.KingSlime)),
                E("荒漠灾虫", "PreHardmode/荒漠灾虫-图标", p => SpawnCal(p, "DesertScourgeHead")),
                E("克苏鲁之眼", "PreHardmode/克苏鲁之眼第一阶段-图标", p => SpawnVanilla(p, NPCID.EyeofCthulhu)),
                E("菌生蟹", "PreHardmode/菌生蟹-图标", p => SpawnCal(p, "Crabulon")),
                E("世界吞噬者", "PreHardmode/世界吞噬怪-图标", p => SpawnVanilla(p, NPCID.EaterofWorldsHead)),
                E("克苏鲁之脑", "PreHardmode/克苏鲁之脑-图标", p => SpawnVanilla(p, NPCID.BrainofCthulhu)),
                E("腐巢意志", "PreHardmode/腐巢意志第二阶段-图标", p => SpawnCal(p, "HiveMind")),
                E("血肉宿主", "PreHardmode/血肉宿主本体-图标", p => SpawnCal(p, "PerforatorHive")),
                E("蜂王", "PreHardmode/蜂王-图标", p => SpawnVanilla(p, NPCID.QueenBee)),
                E("骷髅王", "PreHardmode/骷髅王-图标", p => SpawnVanilla(p, NPCID.SkeletronHead)),
                E("独眼巨鹿", "PreHardmode/独眼巨鹿-图标", p => SpawnVanilla(p, NPCID.Deerclops)),
                E("史莱姆之神", "PreHardmode/史莱姆之神核心-图标", p => SpawnCal(p, "SlimeGodCore")),
                E("血肉之墙", "PreHardmode/血肉墙-图标", p => SpawnVanilla(p, NPCID.WallofFlesh)),
            };

            var hard = new List<BossSpawnEntry>
            {
                E("史莱姆皇后", "Hardmode/史莱姆皇后-图标", p => SpawnVanilla(p, NPCID.QueenSlimeBoss)),
                E("渊海灾虫", "Hardmode/渊海灾虫-图标", p => SpawnCal(p, "AquaticScourgeHead")),
                E("硫磺火元素", "Hardmode/硫磺火元素-图标", p => SpawnCal(p, "BrimstoneElemental")),
                E("极地之灵", "Hardmode/极地之灵-图标", p => SpawnCal(p, "Cryogen")),
                E("毁灭者", "Hardmode/毁灭者-图标", p => SpawnVanilla(p, NPCID.TheDestroyer)),
                E("双子魔眼", "Hardmode/双子魔眼面具", p => SpawnVanilla(p, NPCID.Retinazer)),
                E("机械骷髅王", "Hardmode/机械骷髅王-图标", p => SpawnVanilla(p, NPCID.SkeletronPrime)),
                E("灾厄之影", "Hardmode/灾厄之影-图标", p => SpawnCal(p, "CalamitasClone")),
                E("世纪之花", "Hardmode/世纪之花-图标", p => SpawnVanilla(p, NPCID.Plantera)),
                E("利维坦·阿娜希塔", "Hardmode/利维坦-图标", p => SpawnCal(p, "Anahita")),
                E("白金星舰", "Hardmode/白金星舰-图标", p => SpawnCal(p, "AstrumAureus")),
                E("石巨人", "Hardmode/石巨人-图标", p => SpawnVanilla(p, NPCID.Golem)),
                E("瘟疫使者歌莉娅", "Hardmode/瘟疫使者歌莉娅-图标", p => SpawnCal(p, "PlaguebringerGoliath")),
                E("猪龙鱼公爵", "Hardmode/猪龙鱼公爵-图标", p => SpawnVanilla(p, NPCID.DukeFishron)),
                E("光之女皇", "Hardmode/光之女皇-图标", p => SpawnVanilla(p, NPCID.HallowBoss)),
                E("毁灭魔像", "Hardmode/毁灭魔像-图标", p => SpawnCal(p, "RavagerBody")),
                E("拜月教邪教徒", "Hardmode/拜月教邪教徒-图标", p => SpawnVanilla(p, NPCID.CultistBoss)),
                E("星神游龙", "Hardmode/星神游龙-图标", p => SpawnCal(p, "AstrumDeusHead")),
                E("月亮领主", "Hardmode/月亮领主-图标", p => SpawnVanilla(p, NPCID.MoonLordCore)),
            };

            var post = new List<BossSpawnEntry>
            {
                E("亵渎守卫", "PostMoon/亵渎守卫面具", p => SpawnCal(p, "ProfanedGuardianCommander")),
                E("痴愚金龙", "PostMoon/痴愚金龙-图标", p => SpawnCal(p, "Dragonfolly")),
                E("亵渎天神·普罗维登斯", "PostMoon/亵渎天神，普罗维登斯-图标", p => SpawnCal(p, "Providence")),
                E("风暴编织者", "PostMoon/风暴编织者第一阶段地图图标", p => SpawnCal(p, "StormWeaverHead")),
                E("无尽虚空", "PostMoon/无尽虚空地图图标", p => SpawnCal(p, "CeaselessVoid")),
                E("西格纳斯", "PostMoon/西格纳斯地图图标", p => SpawnCal(p, "Signus")),
                E("噬魂幽花", "PostMoon/噬魂幽花地图图标（阶段一）", p => SpawnCal(p, "Polterghast")),
                E("硫海遗爵", "PostMoon/硫海遗爵-图标", p => SpawnCal(p, "OldDuke")),
                E("神明吞噬者", "PostMoon/神明吞噬者-图标", p => SpawnCal(p, "DevourerofGodsHead")),
                E("重生之龙·犽戎", "PostMoon/重生之龙，犽戎-图标", p => SpawnCal(p, "Yharon")),
                E("星流巨械", "PostMoon/嘉登面具", p => SpawnCal(p, "AresBody")),
                E("至尊女巫·灾厄", "PostMoon/至尊女巫，灾厄-图标", p => SpawnCal(p, "SupremeCalamitas")),
            };

            PreCount = pre.Count;
            HardCount = hard.Count;
            PostCount = post.Count;

            var all = new List<BossSpawnEntry>(pre);
            all.AddRange(hard);
            all.AddRange(post);
            Entries = all.ToArray();
        }

        private static BossSpawnEntry E(string name, string icon, Action<Player> spawn) =>
            new(name, "CalamityLegendsComeBack/Weapons/A_Tools/DebugTools/BossProgress/Assets/Icons/" + icon, spawn);

        private static void SpawnVanilla(Player player, int npcType)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            Vector2 pos = player.Center - Vector2.UnitY * 480f;
            int index = NPC.NewNPC(player.GetSource_Misc("BossSummoner"), (int)pos.X, (int)pos.Y, npcType);
            if (index >= 0 && index < Main.maxNPCs)
            {
                NPC npc = Main.npc[index];
                npc.target = player.whoAmI;
                npc.timeLeft = 1800;
                npc.netUpdate = true;
                npc.Calamity().DoesNotDisappearInBossRush = true;

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
            }

            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.8f, Pitch = 0.1f }, player.Center);
        }

        private static void SpawnCal(Player player, string npcName)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            if (!ModContent.TryFind<ModNPC>("CalamityMod/" + npcName, out ModNPC npc))
            {
                Main.NewText($"未找到：CalamityMod/{npcName}", Color.OrangeRed);
                return;
            }
            Vector2 pos = player.Center - Vector2.UnitY * 480f;
            int index = NPC.NewNPC(player.GetSource_Misc("BossSummoner"), (int)pos.X, (int)pos.Y, npc.Type);
            if (index >= 0 && index < Main.maxNPCs)
            {
                NPC spawned = Main.npc[index];
                spawned.target = player.whoAmI;
                spawned.timeLeft = 1800;
                spawned.netUpdate = true;
                spawned.Calamity().DoesNotDisappearInBossRush = true;

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
            }

            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.8f, Pitch = 0.1f }, player.Center);
        }
    }
}
