using System;
using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.ProfanedGuardianProjectileSniper
{
    // This item deliberately has no custom combat projectile. It directly spawns the selected Calamity guardian projectile.
    public sealed class ProfanedGuardianProjectileSniper : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => $"Terraria/Images/Item_{ItemID.SniperRifle}";

        public override bool AltFunctionUse(Player player) => true;

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 22;
            Item.damage = 500;
            Item.knockBack = 4f;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item40;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 14f;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
        }

        public override bool CanUseItem(Player player) =>
            Main.myPlayer == player.whoAmI &&
            !Main.mapFullscreen &&
            !ProfanedGuardianProjectileMatrixUI.IsOpen;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                ProfanedGuardianProjectileMatrixUI.Open(player);
                return false;
            }

            GuardianProjectileDefinition selected = GuardianProjectileCatalog.Get(player.GetModPlayer<ProfanedGuardianProjectileSniperPlayer>().SelectedProjectileIndex);
            Vector2 aim = Main.MouseWorld - player.MountedCenter;
            if (aim.LengthSquared() < 1f)
                aim = velocity;

            Vector2 shotVelocity = aim.SafeNormalize(Vector2.UnitX) * selected.Speed;
            int projectileIndex = Projectile.NewProjectile(source, player.MountedCenter, shotVelocity, selected.Type, damage, knockback, player.whoAmI, selected.AI0, selected.AI1, selected.AI2);
            if (projectileIndex < 0)
                return false;

            Projectile projectile = Main.projectile[projectileIndex];
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.DamageType = DamageClass.Ranged;

            // These two use ai[0..2] as their intended destination / firing mode.
            if (selected.Type == ModContent.ProjectileType<HolyBlast>() || selected.Type == ModContent.ProjectileType<MoltenBlast>())
            {
                projectile.ai[0] = Main.MouseWorld.X;
                projectile.ai[1] = Main.MouseWorld.Y;
                projectile.ai[2] = 1f;
            }

            projectile.netUpdate = true;
            return false;
        }
    }

    internal sealed class ProfanedGuardianProjectileSniperPlayer : ModPlayer
    {
        private int selectedProjectileIndex;

        public int SelectedProjectileIndex => GuardianProjectileCatalog.IsValidIndex(selectedProjectileIndex) ? selectedProjectileIndex : 0;

        public void SelectProjectile(int index)
        {
            if (GuardianProjectileCatalog.IsValidIndex(index))
                selectedProjectileIndex = index;
        }
    }

    internal readonly record struct GuardianProjectileDefinition(string Name, string Guardian, int Type, float Speed, float AI0 = 0f, float AI1 = 0f, float AI2 = 0f);

    internal static class GuardianProjectileCatalog
    {
        private static readonly GuardianProjectileDefinition[] projectiles =
        {
            new("亵渎之矛", "指挥官", ModContent.ProjectileType<ProfanedSpear>(), 18f),
            new("神圣火焰", "指挥官", ModContent.ProjectileType<HolyFire>(), 13f),
            new("追踪圣火", "指挥官", ModContent.ProjectileType<HolyFire2>(), 12f),
            new("神圣爆裂", "指挥官", ModContent.ProjectileType<HolyBlast>(), 14f),
            new("神圣长矛", "指挥官", ModContent.ProjectileType<HolySpear>(), 18f),
            new("神圣射线", "指挥官", ModContent.ProjectileType<ProvidenceHolyRay>(), 1f, AI2: 1f),
            new("神圣炸弹", "防御者", ModContent.ProjectileType<HolyBomb>(), 10f),
            new("熔火爆弹", "防御者", ModContent.ProjectileType<MoltenBlast>(), 14f),
            new("圣晶碎片", "治疗者", ModContent.ProjectileType<ProvidenceCrystalShard>(), 13f),
            new("圣焚光球", "治疗者", ModContent.ProjectileType<HolyBurnOrb>(), 12f),
            new("治疗圣光", "治疗者", ModContent.ProjectileType<HolyLight>(), 12f, AI1: 25f),
            new("神圣爆裂碎片", "二次弹幕", ModContent.ProjectileType<HolyBlastFrags>(), 12f),
            new("神圣耀焰", "二次弹幕", ModContent.ProjectileType<HolyFlare>(), 10f),
            new("熔火团", "二次弹幕", ModContent.ProjectileType<MoltenBlob>(), 12f)
        };

        public static int Count => projectiles.Length;
        public static bool IsValidIndex(int index) => index >= 0 && index < Count;
        public static GuardianProjectileDefinition Get(int index) => projectiles[Math.Clamp(index, 0, Count - 1)];
    }

    // Kept in the same source file as the item. This is a draw-only UI system, not a projectile.
    internal sealed class ProfanedGuardianProjectileMatrixUI : ModSystem
    {
        private const int Columns = 4;
        private const int Rows = 4;
        private const int SlotSize = 116;
        private const int SlotGap = 8;
        private const int PanelPadding = 14;
        private const int HeaderHeight = 38;
        private const int FooterHeight = 28;

        private static bool open;
        private static int owner = -1;
        private static Vector2 panelTopLeft;

        private static int PanelWidth => PanelPadding * 2 + Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + Rows * SlotSize + (Rows - 1) * SlotGap + FooterHeight;
        public static bool IsOpen => open;
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public static void Open(Player player)
        {
            owner = player.whoAmI;
            panelTopLeft = GetClampedPanelTopLeft(Main.MouseScreen - new Vector2(PanelWidth, PanelHeight) * 0.5f);
            open = true;
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
        }

        private static void Close(Player player, bool playSound = true)
        {
            open = false;
            owner = -1;
            if (playSound)
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (!open || Main.gameMenu || owner < 0 || owner >= Main.maxPlayers)
                return;

            Player player = Main.player[owner];
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<ProfanedGuardianProjectileSniper>())
            {
                Close(player, false);
                return;
            }

            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            ProfanedGuardianProjectileSniperPlayer sniperPlayer = player.GetModPlayer<ProfanedGuardianProjectileSniperPlayer>();

            DrawPanel(panelArea);
            DrawHeader(panelArea);

            for (int index = 0; index < GuardianProjectileCatalog.Count; index++)
            {
                GuardianProjectileDefinition definition = GuardianProjectileCatalog.Get(index);
                Rectangle slotArea = GetSlotArea(index);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool selected = sniperPlayer.SelectedProjectileIndex == index;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = $"{definition.Name}\n{definition.Guardian} · 左键选择";
                    if (leftClickPressed)
                    {
                        sniperPlayer.SelectProjectile(index);
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.48f, Pitch = 0.16f }, player.Center);
                        Close(player, false);
                        return;
                    }
                }

                DrawSlot(definition, slotArea, selected, hovered);
            }

            DrawFooter(panelArea, sniperPlayer.SelectedProjectileIndex);
            Main.blockMouse = true;
            player.mouseInterface = true;

            if (!mouseOverPanel && (leftClickPressed || rightClickPressed))
                Close(player);
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            const float screenMargin = 12f;
            float maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);
            return new Vector2(MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX), MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private static Rectangle GetSlotArea(int index)
        {
            int column = index % Columns;
            int row = index / Columns;
            int x = (int)panelTopLeft.X + PanelPadding + column * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + HeaderHeight + row * (SlotSize + SlotGap);
            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private static void DrawPanel(Rectangle panelArea)
        {
            DrawRectangle(panelArea, new Color(19, 13, 10, 236));
            DrawBorder(panelArea, new Color(255, 161, 72), 2);
            for (int y = panelArea.Top + 10; y < panelArea.Bottom; y += 18)
                DrawRectangle(new Rectangle(panelArea.Left + 2, y, panelArea.Width - 4, 1), new Color(255, 180, 100, 16));
        }

        private static void DrawHeader(Rectangle panelArea)
        {
            Rectangle headerArea = new(panelArea.X + PanelPadding, panelArea.Y + PanelPadding, panelArea.Width - PanelPadding * 2, HeaderHeight - 8);
            DrawRectangle(new Rectangle(headerArea.X, headerArea.Bottom + 4, headerArea.Width, 2), new Color(255, 161, 72, 200));
            DrawFitText("亵渎守卫弹幕矩阵", headerArea, Color.White, 0.9f, 0.46f);
        }

        private static void DrawFooter(Rectangle panelArea, int selectedIndex)
        {
            GuardianProjectileDefinition selected = GuardianProjectileCatalog.Get(selectedIndex);
            Rectangle footerArea = new(panelArea.X + PanelPadding, panelArea.Bottom - PanelPadding - FooterHeight + 4, panelArea.Width - PanelPadding * 2, FooterHeight - 4);
            DrawFitText($"当前：{selected.Guardian} - {selected.Name}    右键或点击外部关闭", footerArea, new Color(255, 224, 174), 0.62f, 0.34f);
        }

        private static void DrawSlot(GuardianProjectileDefinition definition, Rectangle slotArea, bool selected, bool hovered)
        {
            Color accent = GetGuardianColor(definition.Guardian);
            Color backColor = selected ? Color.Lerp(new Color(50, 35, 27), accent, 0.32f) : new Color(44, 31, 26);
            Color borderColor = selected ? Color.Lerp(accent, Color.White, 0.34f) : Color.Lerp(new Color(132, 96, 69), accent, 0.22f);
            if (hovered)
            {
                backColor = Color.Lerp(backColor, new Color(105, 74, 54), 0.52f);
                borderColor = Color.Lerp(borderColor, Color.White, 0.42f);
            }

            DrawRectangle(slotArea, backColor);
            DrawBorder(slotArea, borderColor, selected ? 3 : 2);

            Texture2D texture = TextureAssets.Projectile[definition.Type].Value;
            int frameCount = Math.Max(1, Main.projFrames[definition.Type]);
            int frameHeight = Math.Max(1, texture.Height / frameCount);
            Rectangle frame = new(0, frameHeight * (int)((Main.GameUpdateCount / 6UL) % (ulong)frameCount), texture.Width, frameHeight);
            float fitScale = Math.Min(54f / Math.Max(1f, frame.Width), 54f / Math.Max(1f, frame.Height));
            Vector2 iconCenter = new(slotArea.Center.X, slotArea.Top + 38f);
            Main.EntitySpriteDraw(texture, iconCenter, frame, Color.White, 0f, frame.Size() * 0.5f, fitScale * (hovered ? 1.1f : 1f), SpriteEffects.None, 0f);

            DrawFitText(definition.Name, new Rectangle(slotArea.X + 5, slotArea.Y + 68, slotArea.Width - 10, 22), Color.White, 0.54f, 0.30f);
            DrawFitText(definition.Guardian, new Rectangle(slotArea.X + 5, slotArea.Bottom - 25, slotArea.Width - 10, 18), accent, 0.48f, 0.28f);
        }

        private static Color GetGuardianColor(string guardian) => guardian switch
        {
            "指挥官" => new Color(255, 208, 82),
            "防御者" => new Color(255, 118, 55),
            "治疗者" => new Color(181, 105, 255),
            _ => new Color(255, 170, 92)
        };

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale)
        {
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            float scale = MathHelper.Clamp(Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y)), minScale, maxScale);
            Vector2 position = new(area.X + Math.Max(0f, (area.Width - size.X * scale) * 0.5f), area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, font, text, position, color, Color.Black, scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color) => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }
    }
}
