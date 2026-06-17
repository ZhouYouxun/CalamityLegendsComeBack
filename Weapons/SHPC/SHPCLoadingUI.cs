using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
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
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    /// <summary>
    /// 装填界面：在背包中按鼠标中键（或绑定按键）开启，允许玩家手动将材料放入/取出弹夹槽。
    /// 视觉风格与 SHPCAmmoSelectionPanel 保持一致，叠加矩阵几何装饰。
    /// </summary>
    public class SHPCLoadingUI : ModSystem
    {
        #region ===== 常量 =====
        private const int SlotSize = 56;
        private const int InnerFrameSize = 44;
        private const int BorderThickness = 2;
        private const float MaxIconDrawSize = 40f;
        #endregion

        #region ===== 静态状态 =====
        public static bool IsOpen { get; private set; }
        private static int ownerWhoAmI = -1;
        private static Vector2 panelCenter;
        private static bool prevMouseMiddle;
        #endregion

        #region ===== 开关 =====
        public static void Open(int who)
        {
            IsOpen = true;
            ownerWhoAmI = who;
            Main.playerInventory = true;
        }

        public static void Close()
        {
            IsOpen = false;
            ownerWhoAmI = -1;
        }

        public static void Toggle(int who)
        {
            if (IsOpen && ownerWhoAmI == who)
                Close();
            else
                Open(who);
        }
        #endregion

        #region ===== 注册界面层 =====
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0) return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: SHPC Loading UI",
                () => { Draw(Main.spriteBatch); return true; },
                InterfaceScaleType.None));
        }
        #endregion

        #region ===== Update：检测开关触发 =====
        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) { prevMouseMiddle = Main.mouseMiddle; return; }

            Player player = Main.LocalPlayer;

            if (IsOpen && !Main.playerInventory)
            {
                Close();
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            if (!Main.playerInventory)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            bool keyBound = KeybindSystem.SHPCLoadingUI.GetAssignedKeys().Any();

            // 鼠标中键触发（仅限未绑定时）
            if (!keyBound)
            {
                bool justPressedMiddle = Main.mouseMiddle && !prevMouseMiddle;
                if (justPressedMiddle && Main.HoverItem.ModItem is NewLegendSHPC)
                {
                    bool wasOpen = IsOpen;
                    Toggle(player.whoAmI);
                    SoundEngine.PlaySound(
                        wasOpen
                            ? SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }
                            : SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f },
                        player.Center);
                }
            }

            // 绑定按键触发（背包打开时）
            if (keyBound && KeybindSystem.SHPCLoadingUI.JustPressed)
            {
                if (HasSHPCInInventory(player))
                {
                    bool wasOpen = IsOpen;
                    Toggle(player.whoAmI);
                    SoundEngine.PlaySound(
                        wasOpen
                            ? SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }
                            : SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f },
                        player.Center);
                }
            }

            prevMouseMiddle = Main.mouseMiddle;
        }

        private static bool HasSHPCInInventory(Player player)
        {
            for (int i = 0; i < 58; i++)
                if (player.inventory[i].ModItem is NewLegendSHPC) return true;
            return false;
        }

        private static NewLegendSHPC FindWeapon(Player player)
        {
            for (int i = 0; i < 58; i++)
                if (player.inventory[i].ModItem is NewLegendSHPC w) return w;
            return null;
        }
        #endregion

        #region ===== 绘制主入口 =====
        private static void Draw(SpriteBatch sb)
        {
            if (!IsOpen) return;
            if (ownerWhoAmI < 0 || ownerWhoAmI >= Main.maxPlayers) { Close(); return; }

            Player owner = Main.player[ownerWhoAmI];
            if (!owner.active || owner.dead || !Main.playerInventory) { Close(); return; }

            NewLegendSHPC weapon = FindWeapon(owner);
            if (weapon == null) { Close(); return; }

            int slotCount = weapon.GetActiveMagazineCount(owner);
            panelCenter = Main.ScreenSize.ToVector2() * 0.5f;

            DrawMatrixBackground(slotCount);

            bool hasCursorAmmo = !Main.mouseItem.IsAir && EffectRegistry.IsRegisteredAmmo(Main.mouseItem.type);

            for (int i = 0; i < slotCount; i++)
            {
                NewLegendSHPC.SHPCMagazineSlot slot = weapon.GetMagazineSlot(i, owner);
                Rectangle slotArea = GetSlotArea(i, slotCount);
                bool hovered = slotArea.Contains(Main.mouseX, Main.mouseY);

                // 绿色提示：当悬停空槽且鼠标持有有效弹药时，且该弹药未被其他槽占用
                bool canLoadHere = !slot.HasAmmo && hovered && hasCursorAmmo && !weapon.HasLoadedAmmoType(Main.mouseItem.type);
                bool slotFull = slot.HasAmmo && hovered && hasCursorAmmo;

                DrawSlot(slot, slotArea, hovered, canLoadHere, slotFull);

                if (hovered)
                    Main.hoverItemName = GetHoverText(slot, owner);
            }

            HandleInteractions(weapon, owner, slotCount);

            Rectangle blockedArea = Utils.CenteredRectangle(panelCenter, new Vector2(SlotSize * 3.2f + 72f));
            if (blockedArea.Contains(Main.mouseX, Main.mouseY))
            {
                owner.mouseInterface = true;
                Main.blockMouse = true;
            }

            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            {
                Close();
                SoundEngine.PlaySound(SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }, owner.Center);
            }
        }
        #endregion

        #region ===== 交互逻辑 =====
        private static void HandleInteractions(NewLegendSHPC weapon, Player owner, int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                Rectangle slotArea = GetSlotArea(i, slotCount);
                if (!slotArea.Contains(Main.mouseX, Main.mouseY)) continue;

                owner.mouseInterface = true;
                NewLegendSHPC.SHPCMagazineSlot slot = weapon.GetMagazineSlot(i, owner);

                // 左键：装填 or 取出
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (!slot.HasAmmo && !Main.mouseItem.IsAir
                        && EffectRegistry.IsRegisteredAmmo(Main.mouseItem.type)
                        && !weapon.HasLoadedAmmoType(Main.mouseItem.type))
                    {
                        // 从鼠标物品装填
                        int effectID = EffectRegistry.GetEffectIDByAmmo(Main.mouseItem.type);
                        int capacity = NewLegendSHPC.GetAdjustedAmmoCapacity(owner, effectID);
                        weapon.DirectLoadMagazine(i, Main.mouseItem.type, effectID, capacity);

                        Main.mouseItem.stack--;
                        if (Main.mouseItem.stack <= 0)
                            Main.mouseItem.TurnToAir();

                        SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.58f, Pitch = 0.08f }, owner.Center);
                    }
                    else if (slot.HasAmmo && Main.mouseItem.IsAir)
                    {
                        // 取出到鼠标
                        Item returned = new Item();
                        returned.SetDefaults(slot.AmmoType);
                        returned.stack = 1;
                        Main.mouseItem = returned;
                        weapon.PublicClearMagazine(i);

                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.1f }, owner.Center);
                    }
                }

                // 右键：弹回背包
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    if (slot.HasAmmo)
                    {
                        weapon.PublicClearMagazineWithReturn(owner, i);
                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = 0.1f }, owner.Center);
                    }
                }
            }
        }
        #endregion

        #region ===== 背景绘制（矩阵风格） =====
        private static void DrawMatrixBackground(int slotCount)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float pulse = 0.94f + 0.06f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);

            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(92, 176, 255, 0) * 0.22f, 0f, bloom.Size() * 0.5f, 0.54f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(255, 238, 160, 0) * 0.14f, 0f, bloom.Size() * 0.5f, 0.30f * pulse, SpriteEffects.None, 0f);

            // 外旋六边形
            DrawRegularPolygon(6, 96f, Main.GlobalTimeWrappedHourly * 0.4f, new Color(60, 180, 255, 0) * 0.28f, 1.5f);

            // 内反旋三角形
            DrawRegularPolygon(3, 46f, -Main.GlobalTimeWrappedHourly * 0.7f, new Color(200, 160, 255, 0) * 0.22f, 1.5f);

            // 六边形顶点脉冲光点
            float hexRot = Main.GlobalTimeWrappedHourly * 0.4f;
            for (int v = 0; v < 6; v++)
            {
                float angle = hexRot + MathHelper.TwoPi * v / 6f;
                Vector2 vertex = panelCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 96f;
                float brightness = 0.35f + 0.25f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + v * 1.05f);
                Main.EntitySpriteDraw(bloom, vertex, null,
                    new Color(120, 200, 255, 0) * brightness, 0f, bloom.Size() * 0.5f, 0.06f, SpriteEffects.None, 0f);
            }

            // 中心脉冲光
            float cPulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f);
            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(200, 220, 255, 0) * (0.42f * cPulse), 0f, bloom.Size() * 0.5f, 0.09f * cPulse, SpriteEffects.None, 0f);

            // 弹夹分区线
            if (slotCount <= 1) return;
            Color lineColor = new Color(132, 190, 240, 0) * 0.26f;
            float lineRadius = 86f;
            for (int i = 0; i < slotCount; i++)
            {
                Vector2 cur = GetSlotOffset(i, slotCount).SafeNormalize(-Vector2.UnitY);
                Vector2 nxt = GetSlotOffset((i + 1) % slotCount, slotCount).SafeNormalize(-Vector2.UnitY);
                Vector2 boundary = (cur + nxt).SafeNormalize(cur);
                DrawLine(panelCenter, panelCenter + boundary * lineRadius, lineColor, 1.4f);
            }
        }

        private static void DrawRegularPolygon(int sides, float radius, float rotation, Color color, float thickness)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = rotation + MathHelper.TwoPi * i / sides;
                float b = rotation + MathHelper.TwoPi * (i + 1) / sides;
                Vector2 v1 = panelCenter + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
                Vector2 v2 = panelCenter + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius;
                DrawLine(v1, v2, color, thickness);
            }
        }
        #endregion

        #region ===== 槽位绘制 =====
        private static void DrawSlot(NewLegendSHPC.SHPCMagazineSlot slot, Rectangle slotArea, bool hovered, bool canLoad, bool slotFull)
        {
            Color effectColor = slot.HasAmmo ? SHPCAmmoSelectionPanel.GetEffectColor(slot.EffectID) : new Color(86, 92, 104);
            Color slotBack = Color.Lerp(new Color(24, 28, 36), effectColor, 0.18f);
            Color slotBorder = Color.Lerp(new Color(112, 126, 150), effectColor, 0.42f);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(74, 84, 102), 0.48f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.35f);

                if (canLoad)
                    slotBorder = Color.Lerp(slotBorder, Color.LimeGreen, 0.55f);
                else if (slotFull)
                    slotBorder = Color.Lerp(slotBorder, new Color(255, 90, 90), 0.3f);
            }

            DrawRectangle(slotArea, slotBack * 0.92f);
            DrawBorder(slotArea, slotBorder, BorderThickness);

            Rectangle innerArea = Utils.CenteredRectangle(slotArea.Center.ToVector2(), new Vector2(InnerFrameSize));
            DrawRectangle(innerArea, Color.Lerp(new Color(10, 12, 18), effectColor, 0.08f) * 0.82f);
            DrawBorder(innerArea, slotBorder * 0.68f, 1);

            if (!slot.HasAmmo)
            {
                CalamityUtils.DrawBorderStringEightWay(
                    Main.spriteBatch, FontAssets.MouseText.Value,
                    (slot.Index + 1).ToString(),
                    slotArea.Center.ToVector2() - new Vector2(5f, 11f),
                    Color.Gray, Color.Black, 0.8f);
                return;
            }

            Texture2D tex = SHPCAmmoSelectionPanel.TryGetAmmoTexture(slot.EffectID, slot.AmmoType);
            if (tex == null) return;

            Rectangle src = SHPCAmmoSelectionPanel.GetCurrentFrame(tex, SHPCAmmoSelectionPanel.GetFrameCount(slot.EffectID));
            Vector2 iconCenter = slotArea.Center.ToVector2();
            Vector2 srcSize = src.Size();
            float fitScale = Math.Min(MaxIconDrawSize / Math.Max(1f, srcSize.X), MaxIconDrawSize / Math.Max(1f, srcSize.Y));
            float hoverScale = hovered ? 1.1f : 1f;
            float bob = 1f + (hovered ? 0.025f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f) : 0f);
            Color iconColor = hovered ? Color.White : Color.Lerp(Color.White, effectColor, 0.12f);

            Main.EntitySpriteDraw(tex, iconCenter, src, iconColor,
                hovered ? 0.03f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f,
                srcSize * 0.5f, fitScale * hoverScale * bob, SpriteEffects.None, 0f);

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch, FontAssets.MouseText.Value,
                slot.Power.ToString(),
                new Vector2(slotArea.Right, slotArea.Bottom) - new Vector2(18f, 18f),
                Color.White, Color.Black, 0.55f);
        }
        #endregion

        #region ===== 槽位几何（与 SHPCAmmoSelectionPanel 完全一致） =====
        private static Rectangle GetSlotArea(int index, int count)
        {
            return Utils.CenteredRectangle(panelCenter + GetSlotOffset(index, count), new Vector2(SlotSize));
        }

        private static Vector2 GetSlotOffset(int index, int count)
        {
            if (count <= 1) return new Vector2(0f, -58f);
            if (count == 2) return index == 0 ? new Vector2(-50f, -22f) : new Vector2(50f, -22f);
            if (count == 3)
            {
                return index switch
                {
                    0 => new Vector2(0f, -64f),
                    1 => new Vector2(58f, 34f),
                    _ => new Vector2(-58f, 34f)
                };
            }
            float radius = count <= 4 ? 76f : 90f;
            float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * index / count;
            return angle.ToRotationVector2() * radius;
        }
        #endregion

        #region ===== 悬停文本 =====
        private static string GetHoverText(NewLegendSHPC.SHPCMagazineSlot slot, Player owner)
        {
            bool isChinese = Language.ActiveCulture.Name.StartsWith("zh");

            if (!slot.HasAmmo)
            {
                return isChinese
                    ? $"{slot.Index + 1}号弹夹：空  [左键：将材料拿到鼠标上，再点此处装填  右键：无效]"
                    : $"Canister {slot.Index + 1}: Empty  [LClick: hold ammo on cursor then click to load]";
            }

            string itemName = Lang.GetItemNameValue(slot.AmmoType);
            int capacity = NewLegendSHPC.GetAdjustedAmmoCapacity(owner, slot.EffectID);
            string action = isChinese
                ? "  [左键：取到鼠标  右键：弹回背包]"
                : "  [LClick: pick up  RClick: return to bag]";
            return $"{slot.Index + 1}{(isChinese ? "号弹夹" : "")}: {itemName} ({slot.Power}/{capacity}){action}";
        }
        #endregion

        #region ===== 基础绘制工具 =====
        private static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f) return;
            Main.EntitySpriteDraw(pixel, start, new Rectangle(0, 0, 1, 1), color,
                edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness),
                SpriteEffects.None, 0f);
        }

        private static void DrawRectangle(Rectangle rect, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, color);
        }

        private static void DrawBorder(Rectangle rect, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            DrawRectangle(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            DrawRectangle(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            DrawRectangle(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
        #endregion
    }
}
