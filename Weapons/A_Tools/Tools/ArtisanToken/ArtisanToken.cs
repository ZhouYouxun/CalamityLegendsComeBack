using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using CalamityMod;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools;
using CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.ArtisanToken
{
    public class ArtisanToken : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.TinkerersWorkshop;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(255, 190, 60));
            return true;
        }

        private static int PanelType => ModContent.ProjectileType<ArtisanTokenPanel>();

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.sellPrice(gold: 15);
            Item.rare = ItemRarityID.Yellow;
            Item.maxStack = 9999;
        }

        public override bool CanRightClick() => true;
        public override bool ConsumeItem(Player player) => false;

        public override void RightClick(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
                TogglePanel(player, Item.GetSource_FromThis());
        }

        private static void TogglePanel(Player player, IEntitySource source)
        {
            int panelType = PanelType;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI || proj.type != panelType) continue;
                ArtisanTokenPanel.RequestClose(proj);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.6f, Pitch = 0.05f }, player.Center);
                return;
            }

            Projectile.NewProjectile(
                source, player.Center, Vector2.Zero,
                panelType, 0, 0f, player.whoAmI);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.7f, Pitch = 0.1f }, player.Center);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

    }

    internal class ArtisanTokenShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.GoblinTinkerer)
                shop.Add<ArtisanToken>();
        }
    }

    internal sealed class ArtisanTokenPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private struct PrefixEntry
        {
            public int Id;
            public string ChineseName;
            public string EnglishName;
            public string StatDesc;
            public Color AccentColor;
        }

        private static readonly PrefixEntry[] Prefixes =
        {
            new() { Id = PrefixID.Warding,  ChineseName = "护佑",  EnglishName = "Warding",  StatDesc = "+4 防御",       AccentColor = new Color(120, 200, 255) },
            new() { Id = PrefixID.Menacing, ChineseName = "险恶",  EnglishName = "Menacing", StatDesc = "+4% 伤害",      AccentColor = new Color(255, 120, 80)  },
            new() { Id = PrefixID.Lucky,    ChineseName = "幸运",  EnglishName = "Lucky",    StatDesc = "+4% 暴击",      AccentColor = new Color(255, 230, 80)  },
            new() { Id = PrefixID.Arcane,   ChineseName = "奥术",  EnglishName = "Arcane",   StatDesc = "+20 魔力",      AccentColor = new Color(160, 100, 255) },
            new() { Id = PrefixID.Violent,  ChineseName = "激烈",  EnglishName = "Violent",  StatDesc = "+2% 攻速/暴击", AccentColor = new Color(255, 160, 50)  },
        };

        private const int ButtonW = 148;
        private const int ButtonH = 56;
        private const int ButtonGap = 6;
        private const int Cols = 2;
        private const int PanelPad = 14;
        private const int TitleH = 32;
        private const int BorderThick = 2;

        private static int Rows => (Prefixes.Length + Cols - 1) / Cols;
        private static int GridW => Cols * ButtonW + (Cols - 1) * ButtonGap;
        private static int GridH => Rows * ButtonH + (Rows - 1) * ButtonGap;
        private static int PanelW => PanelPad * 2 + GridW;
        private static int PanelH => PanelPad * 2 + TitleH + GridH;

        private Vector2 panelTopLeft;
        private bool panelInitialized;
        private readonly int[] clickFeedback = new int[10];
        private readonly bool[] hoveredLast = new bool[10];

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] >= 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRect => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public static void RequestClose(Projectile proj)
        {
            if (proj.ModProjectile is ArtisanTokenPanel p) p.FadeOut = true;
            else proj.ai[0] = 1f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) { Projectile.Kill(); return; }

            if (!panelInitialized && Main.myPlayer == Projectile.owner)
            {
                panelTopLeft = new Vector2(
                    (Main.screenWidth - PanelW) * 0.5f,
                    (Main.screenHeight - PanelH) * 0.5f);
                panelInitialized = true;
            }

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.15f : 0.2f), 0f, 1f);
            if (FadeOut && Projectile.Opacity <= 0f) Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner || !panelInitialized) return false;

            Player owner = Main.player[Projectile.owner];
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelW, PanelH);
            bool mouseOverPanel = panelArea.Intersects(MouseRect);
            bool leftClick = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClick = Main.mouseRight && Main.mouseRightRelease;
            float op = Projectile.Opacity;
            int clickedIndex = -1;

            // Background
            DrawRect(panelArea, new Color(12, 10, 20, 240) * op);
            DrawBorder(panelArea, new Color(200, 160, 60) * op, BorderThick);
            DrawBorder(new Rectangle(panelArea.X + 6, panelArea.Y + 6, panelArea.Width - 12, panelArea.Height - 12),
                new Color(100, 80, 20, 160) * op, 1);

            // Title
            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.ArtisanTokenTitle");
            Rectangle titleRect = new(panelArea.X + PanelPad, panelArea.Y + PanelPad, PanelW - PanelPad * 2, TitleH - 6);
            DrawFitText(title, titleRect, new Color(255, 220, 100), 0.9f, 0.5f, op);
            DrawRect(new Rectangle(panelArea.X + PanelPad, panelArea.Y + PanelPad + TitleH - 4,
                PanelW - PanelPad * 2, 1), new Color(200, 160, 60, 140) * op);

            // Prefix buttons
            for (int i = 0; i < Prefixes.Length; i++)
            {
                Rectangle btn = GetButtonRect(i);
                bool hovered = btn.Intersects(MouseRect);
                if (hovered) mouseOverPanel = true;

                DrawPrefixButton(btn, Prefixes[i], hovered, clickFeedback[i], op);

                if (!hoveredLast[i] && hovered && op >= 0.9f)
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.35f, Pitch = 0.2f });

                if (hovered && leftClick && op >= 0.9f)
                    clickedIndex = i;

                hoveredLast[i] = hovered;
                if (clickFeedback[i] > 0) clickFeedback[i]--;
            }

            if (clickedIndex >= 0)
            {
                ApplyPrefix(owner, Prefixes[clickedIndex]);
                clickFeedback[clickedIndex] = 14;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.8f, Pitch = 0.15f }, owner.Center);
                FadeOut = true;
            }

            if (!FadeOut && rightClick && !mouseOverPanel && op >= 0.9f)
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.6f, Pitch = 0.05f });
            }

            if (mouseOverPanel) { Main.blockMouse = true; owner.mouseInterface = true; }

            return false;
        }

        private Rectangle GetButtonRect(int index)
        {
            int col = index % Cols;
            int row = index / Cols;
            int x = (int)panelTopLeft.X + PanelPad + col * (ButtonW + ButtonGap);
            int y = (int)panelTopLeft.Y + PanelPad + TitleH + row * (ButtonH + ButtonGap);
            return new Rectangle(x, y, ButtonW, ButtonH);
        }

        private static void DrawPrefixButton(Rectangle area, PrefixEntry entry, bool hovered, int feedback, float op)
        {
            Color accent = entry.AccentColor;
            Color back = Color.Lerp(new Color(20, 16, 30), accent, hovered ? 0.22f : 0.1f);
            if (feedback > 0) back = Color.Lerp(back, new Color(255, 230, 100), feedback / 14f * 0.5f);

            DrawRect(area, back * (op * 0.94f));
            DrawBorder(area, Color.Lerp(accent, Color.White, hovered ? 0.4f : 0.15f) * op, hovered ? 2 : 1);

            // Chinese name
            string cnName = entry.ChineseName;
            Vector2 cnPos = new(area.X + 10, area.Y + 8);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
                cnName, cnPos, accent * op, 0f, Vector2.Zero, Vector2.One * 0.9f);

            // English name (smaller)
            Vector2 enPos = new(area.X + 10, area.Y + 28);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
                entry.EnglishName, enPos, Color.Gray * op, 0f, Vector2.Zero, Vector2.One * 0.72f);

            // Stat desc
            Vector2 statPos = new(area.Right - 8, area.Y + 8);
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 statSize = font.MeasureString(entry.StatDesc) * 0.75f;
            statPos.X -= statSize.X;
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font,
                entry.StatDesc, statPos, Color.White * (op * 0.88f), 0f, Vector2.Zero, Vector2.One * 0.75f);
        }

        private static void ApplyPrefix(Player player, PrefixEntry entry)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ArtisanTokenPackets.RequestApplyPrefix(entry.Id);
                return;
            }

            int applied = ApplyPrefixToAccessories(player, entry.Id);
            ShowAppliedMessage(entry, applied);
        }

        internal static int ApplyPrefixToAccessories(Player player, int prefixId)
        {
            int applied = 0;
            for (int i = 0; i < player.armor.Length; i++)
            {
                Item equip = player.armor[i];
                if (equip.IsAir || !equip.accessory) continue;
                if (!equip.Prefix(prefixId))
                    continue;

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, i, equip.prefix);
                applied++;
            }
            return applied;
        }

        internal static bool IsSupportedPrefix(int prefixId)
        {
            foreach (PrefixEntry entry in Prefixes)
            {
                if (entry.Id == prefixId)
                    return true;
            }

            return false;
        }

        internal static void ShowAppliedMessage(int prefixId, int applied)
        {
            foreach (PrefixEntry entry in Prefixes)
            {
                if (entry.Id == prefixId)
                {
                    ShowAppliedMessage(entry, applied);
                    return;
                }
            }
        }

        private static void ShowAppliedMessage(PrefixEntry entry, int applied)
        {
            string msg = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.ArtisanTokenApplied",
                entry.ChineseName, applied);
            Main.NewText(msg, new Color(255, 220, 100));
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

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float op)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 sz = font.MeasureString(text);
            if (sz.X <= 0f || sz.Y <= 0f) return;
            float scale = maxScale;
            if (sz.X * scale > area.Width) scale = area.Width / sz.X;
            if (sz.Y * scale > area.Height) scale = Math.Min(scale, area.Height / sz.Y);
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 pos = new(area.X + (area.Width - sz.X * scale) * 0.5f, area.Y + (area.Height - sz.Y * scale) * 0.5f);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, pos, color * op, 0f, Vector2.Zero, Vector2.One * scale);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) { }
    }

    internal static class ArtisanTokenPackets
    {
        public static void RequestApplyPrefix(int prefixId)
        {
            ModPacket packet = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            packet.Write((byte)GamePacketType.ArtisanTokenApplyPrefix);
            packet.Write((byte)prefixId);
            packet.Send();
        }

        public static void HandleApplyPrefix(BinaryReader reader, int whoAmI)
        {
            if (Main.netMode != NetmodeID.Server || reader.BaseStream.Position >= reader.BaseStream.Length)
                return;

            int prefixId = reader.ReadByte();
            if (!Main.player.IndexInRange(whoAmI) || !ArtisanTokenPanel.IsSupportedPrefix(prefixId))
                return;

            Player player = Main.player[whoAmI];
            if (!player.active || !PlayerOwnsArtisanToken(player))
                return;

            int applied = ArtisanTokenPanel.ApplyPrefixToAccessories(player, prefixId);
            ModPacket response = ModContent.GetInstance<global::CalamityLegendsComeBack.CalamityLegendsComeBack>().GetPacket();
            response.Write((byte)GamePacketType.ArtisanTokenPrefixApplied);
            response.Write((byte)prefixId);
            response.Write((byte)applied);
            response.Send(whoAmI);
        }

        public static void HandlePrefixApplied(BinaryReader reader)
        {
            if (Main.netMode == NetmodeID.Server || reader.BaseStream.Length - reader.BaseStream.Position < 2)
                return;

            ArtisanTokenPanel.ShowAppliedMessage(reader.ReadByte(), reader.ReadByte());
        }

        private static bool PlayerOwnsArtisanToken(Player player)
        {
            int tokenType = ModContent.ItemType<ArtisanToken>();
            foreach (Item item in player.inventory)
            {
                if (!item.IsAir && item.type == tokenType)
                    return true;
            }

            return false;
        }
    }
}
