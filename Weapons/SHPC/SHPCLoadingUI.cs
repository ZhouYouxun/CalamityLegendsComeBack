using System;
using System.Collections.Generic;
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
        private static Item targetItem;
        private static Vector2 panelCenter;
        private static bool prevMouseMiddle;
        private static bool prevKeybindPressed;
        #endregion

        #region ===== 开关 =====
        // 打开时锁定具体的那把武器实例，避免背包里多把SHPC时互相串弹药
        public static void Open(int who, Item item)
        {
            IsOpen = true;
            ownerWhoAmI = who;
            targetItem = item;
            Main.playerInventory = true;
        }

        public static void Close()
        {
            IsOpen = false;
            ownerWhoAmI = -1;
            targetItem = null;
        }

        public static void Toggle(int who, Item item)
        {
            if (IsOpen && ownerWhoAmI == who)
                Close();
            else
                Open(who, item);
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
        public override void PreUpdatePlayers()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0)
                return;

            if (IsOpen)
            {
                Vector2 center = Main.ScreenSize.ToVector2() * 0.5f;
                Rectangle blockedArea = Utils.CenteredRectangle(center, new Vector2(SlotSize * 3.2f + 72f));
                if (blockedArea.Contains(Main.mouseX, Main.mouseY))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            }
        }

        #endregion

        #region ===== 绘制主入口 =====
        private static void Draw(SpriteBatch sb)
        {
            // ---- 开关触发检测（在 Draw 内执行，自动暂停时依然有效）----
            if (Main.netMode != NetmodeID.Server && Main.myPlayer >= 0)
            {
                Player localPlayer = Main.LocalPlayer;

                // 背包关闭 → 自动收起界面
                if (IsOpen && !Main.playerInventory)
                {
                    Close();
                    prevMouseMiddle = Main.mouseMiddle;
                    prevKeybindPressed = InventoryActivationInput.IsPressed(KeybindSystem.SHPCLoadingUI);
                    return;
                }

                if (Main.playerInventory)
                {
                    bool keyBound = InventoryActivationInput.HasBoundKey(KeybindSystem.SHPCLoadingUI);
                    bool keybindPressed = InventoryActivationInput.IsPressed(KeybindSystem.SHPCLoadingUI);
                    bool keybindJustPressed = keybindPressed && !prevKeybindPressed;
                    prevKeybindPressed = keybindPressed;

                    // 鼠标中键触发（仅限未绑定时）
                    if (!keyBound)
                    {
                        bool justPressedMiddle = Main.mouseMiddle && !prevMouseMiddle;
                        if (justPressedMiddle && Main.HoverItem.ModItem is NewLegendSHPC)
                        {
                            bool wasOpen = IsOpen;
                            Toggle(localPlayer.whoAmI, Main.HoverItem);
                            SoundEngine.PlaySound(
                                wasOpen
                                    ? SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }
                                    : SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f },
                                localPlayer.Center);
                        }
                    }
                    // 绑定按键触发
                    else
                    {
                        if (keybindJustPressed && Main.HoverItem.ModItem is NewLegendSHPC)
                        {
                            bool wasOpen = IsOpen;
                            Toggle(localPlayer.whoAmI, Main.HoverItem);
                            SoundEngine.PlaySound(
                                wasOpen
                                    ? SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }
                                    : SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f },
                                localPlayer.Center);
                        }
                    }
                }
            }

            prevMouseMiddle = Main.mouseMiddle;

            if (!IsOpen) return;
            if (ownerWhoAmI < 0 || ownerWhoAmI >= Main.maxPlayers) { Close(); return; }

            Player owner = Main.player[ownerWhoAmI];
            if (!owner.active || owner.dead || !Main.playerInventory) { Close(); return; }

            if (targetItem == null || targetItem.IsAir) { Close(); return; }
            NewLegendSHPC weapon = targetItem.ModItem as NewLegendSHPC;
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

                // 绿色：可以追加（空槽装新类型，或同类型追加到外置库）
                bool canLoadHere = hovered && hasCursorAmmo && (
                    (!slot.IsConfigured && !weapon.HasLoadedAmmoType(Main.mouseItem.type)) ||
                    (slot.IsConfigured && Main.mouseItem.type == slot.AmmoType && slot.Reserve < NewLegendSHPC.MaxReservePerSlot)
                );
                // 红色：类型不符或外置库已满
                bool slotFull = hovered && hasCursorAmmo && slot.IsConfigured &&
                    (Main.mouseItem.type != slot.AmmoType || slot.Reserve >= NewLegendSHPC.MaxReservePerSlot);

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

                // 左键：装填（加入外置库）or 取出外置库
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (!Main.mouseItem.IsAir && EffectRegistry.IsRegisteredAmmo(Main.mouseItem.type))
                    {
                        if (!slot.IsConfigured && !weapon.HasLoadedAmmoType(Main.mouseItem.type))
                        {
                            // 空槽：初始化类型并把鼠标上所有材料存入外置库
                            int effectID = EffectRegistry.GetEffectIDByAmmo(Main.mouseItem.type);
                            int canAdd = Math.Min(Main.mouseItem.stack, NewLegendSHPC.MaxReservePerSlot);
                            weapon.AddToReserve(i, Main.mouseItem.type, effectID, canAdd);
                            Main.mouseItem.stack -= canAdd;
                            if (Main.mouseItem.stack <= 0)
                                Main.mouseItem.TurnToAir();
                            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.58f, Pitch = 0.08f }, owner.Center);
                        }
                        else if (slot.IsConfigured && Main.mouseItem.type == slot.AmmoType
                                 && slot.Reserve < NewLegendSHPC.MaxReservePerSlot)
                        {
                            // 相同材料：追加到外置库
                            int canAdd = Math.Min(Main.mouseItem.stack, NewLegendSHPC.MaxReservePerSlot - slot.Reserve);
                            if (canAdd > 0)
                            {
                                weapon.AddToReserve(i, slot.AmmoType, slot.EffectID, canAdd);
                                Main.mouseItem.stack -= canAdd;
                                if (Main.mouseItem.stack <= 0)
                                    Main.mouseItem.TurnToAir();
                                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.58f, Pitch = 0.08f }, owner.Center);
                            }
                        }
                    }
                    else if (Main.mouseItem.IsAir && slot.IsConfigured && slot.Reserve > 0)
                    {
                        // 取出外置库全部到鼠标（不动内弹夹）
                        Item returned = new Item();
                        returned.SetDefaults(slot.AmmoType);
                        returned.stack = slot.Reserve;
                        Main.mouseItem = returned;
                        weapon.SetReserve(i, 0);
                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.1f }, owner.Center);
                    }
                }

                // 右键：全部弹回背包（外置库直接返还 + 内弹夹概率返还）
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    if (slot.IsConfigured)
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
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            float t = Main.GlobalTimeWrappedHourly;
            float pulse = 0.92f + 0.08f * MathF.Sin(t * 5.2f);

            // 半透明深色背景板（加强与战斗轮盘的区分）
            float panelExtent = SlotSize * 1.7f + 70f;

            // 外层扩散光晕（鲜艳科技蓝）
            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(12, 170, 255, 0) * 0.38f * pulse, 0f, bloom.Size() * 0.5f, 0.68f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(0, 100, 210, 0) * 0.22f, 0f, bloom.Size() * 0.5f, 0.48f, SpriteEffects.None, 0f);

            // 水平扫描线（矩阵感，匀速向下滚动）
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                float scrollY = t * 42f % 16f;
                float scanHalfW = panelExtent * 0.88f;
                for (int i = -1; i < 18; i++)
                {
                    float lineY = panelCenter.Y - panelExtent + i * 16f + scrollY;
                    if (lineY < panelCenter.Y - panelExtent || lineY > panelCenter.Y + panelExtent) continue;
                    float distFrac = Math.Abs(lineY - panelCenter.Y) / panelExtent;
                    float scanAlpha = (0.075f - distFrac * 0.06f) * MathF.Max(0f, 1f - distFrac);
                    Main.spriteBatch.Draw(pixel,
                        new Rectangle((int)(panelCenter.X - scanHalfW), (int)lineY, (int)(scanHalfW * 2f), 1),
                        new Color(0, 195, 255, 0) * MathF.Max(0f, scanAlpha));
                }
            }

            // 三层同心六边形（不同大小，营造深度感）
            float hexRot = t * 0.30f;
            DrawRegularPolygon(6, 115f, hexRot, new Color(20, 140, 230, 0) * 0.20f, 1f);
            DrawRegularPolygon(6, 92f, hexRot + MathHelper.Pi / 6f, new Color(25, 185, 255, 0) * 0.35f, 2f);
            DrawRegularPolygon(6, 65f, hexRot, new Color(35, 200, 255, 0) * 0.22f, 1.5f);

            // 内反旋三角形（蓝紫色，与六边形形成对比）
            DrawRegularPolygon(3, 50f, -t * 0.50f, new Color(90, 155, 255, 0) * 0.40f, 2f);

            // 外六边形顶点脉冲光点（更亮）
            for (int v = 0; v < 6; v++)
            {
                float angle = hexRot + MathHelper.Pi / 6f + MathHelper.TwoPi * v / 6f;
                Vector2 vertex = panelCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 92f;
                float bright = 0.55f + 0.38f * MathF.Sin(t * 3.8f + v * 1.05f);
                Main.EntitySpriteDraw(bloom, vertex, null,
                    new Color(70, 210, 255, 0) * bright, 0f, bloom.Size() * 0.5f, 0.072f, SpriteEffects.None, 0f);
            }

            // 三角形顶点快速脉冲光点
            for (int v = 0; v < 3; v++)
            {
                float angle = -t * 0.50f + MathHelper.TwoPi * v / 3f;
                Vector2 vertex = panelCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 50f;
                float bright = 0.40f + 0.32f * MathF.Sin(t * 6.2f + v * 2.1f);
                Main.EntitySpriteDraw(bloom, vertex, null,
                    new Color(140, 180, 255, 0) * bright, 0f, bloom.Size() * 0.5f, 0.052f, SpriteEffects.None, 0f);
            }

            // 中心核心脉冲（更强烈的闪烁感）
            float cPulse = 0.5f + 0.5f * MathF.Sin(t * 9.5f);
            Main.EntitySpriteDraw(bloom, panelCenter, null,
                new Color(180, 235, 255, 0) * (0.30f + 0.45f * cPulse), 0f, bloom.Size() * 0.5f,
                0.058f + 0.028f * cPulse, SpriteEffects.None, 0f);

            // 中心十字星芒（区别于战斗轮盘，加旋转速率更快）
            float starRot = t * 1.1f;
            Main.EntitySpriteDraw(star, panelCenter, null,
                new Color(110, 230, 255, 0) * 0.50f, starRot, star.Size() * 0.5f,
                new Vector2(0.38f, 1.95f) * 0.058f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(star, panelCenter, null,
                new Color(110, 230, 255, 0) * 0.50f, starRot + MathHelper.PiOver4, star.Size() * 0.5f,
                new Vector2(0.38f, 1.95f) * 0.058f, SpriteEffects.None, 0f);

            // 弹夹分区线（更明显）
            if (slotCount <= 1) return;
            Color lineColor = new Color(50, 185, 245, 0) * 0.42f;
            float lineRadius = 108f;
            for (int i = 0; i < slotCount; i++)
            {
                Vector2 cur = GetSlotOffset(i, slotCount).SafeNormalize(-Vector2.UnitY);
                Vector2 nxt = GetSlotOffset((i + 1) % slotCount, slotCount).SafeNormalize(-Vector2.UnitY);
                Vector2 boundary = (cur + nxt).SafeNormalize(cur);
                DrawLine(panelCenter + boundary * 24f, panelCenter + boundary * lineRadius, lineColor, 1.6f);
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
            Color effectColor = slot.IsConfigured ? SHPCAmmoSelectionPanel.GetEffectColor(slot.EffectID) : new Color(86, 92, 104);
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

            if (!slot.IsConfigured)
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
            Color iconColor = hovered ? Color.White : Color.Lerp(Color.White, effectColor, slot.HasAmmo ? 0.12f : 0.55f);

            Main.EntitySpriteDraw(tex, iconCenter, src, iconColor,
                hovered ? 0.03f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f,
                srcSize * 0.5f, fitScale * hoverScale * bob, SpriteEffects.None, 0f);

            // 外置库数量（右下角，橙色）
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch, FontAssets.MouseText.Value,
                $"×{slot.Reserve}",
                new Vector2(slotArea.Right, slotArea.Bottom) - new Vector2(22f, 16f),
                slot.Reserve > 0 ? new Color(255, 185, 50) : new Color(100, 100, 100), Color.Black, 0.52f);

            // 内弹夹剩余发数（左下角，青色）
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch, FontAssets.MouseText.Value,
                slot.Power.ToString(),
                new Vector2(slotArea.Left, slotArea.Bottom) - new Vector2(-4f, 16f),
                slot.HasAmmo ? new Color(100, 220, 255) : new Color(80, 80, 80), Color.Black, 0.45f);
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

            if (!slot.IsConfigured)
            {
                return isChinese
                    ? $"{slot.Index + 1}号弹夹：空  [左键（材料在鼠标上）：存入外置库  右键：无效]"
                    : $"Canister {slot.Index + 1}: Empty  [LClick with ammo on cursor: add to reserve]";
            }

            string itemName = Lang.GetItemNameValue(slot.AmmoType);
            int capacity = NewLegendSHPC.GetAdjustedAmmoCapacity(owner, slot.EffectID);

            if (isChinese)
            {
                return $"{slot.Index + 1}号弹夹: {itemName}" +
                    $"  内弹夹 {slot.Power}/{capacity}发" +
                    $"  外置库 ×{slot.Reserve}/{NewLegendSHPC.MaxReservePerSlot}" +
                    $"  [左键（材料）：追加储备  左键（空）：取出外置库  右键：全部弹回背包]";
            }
            else
            {
                return $"Canister {slot.Index + 1}: {itemName}" +
                    $"  Internal {slot.Power}/{capacity}" +
                    $"  Reserve ×{slot.Reserve}/{NewLegendSHPC.MaxReservePerSlot}" +
                    $"  [LClick(ammo): add reserve  LClick(empty): take reserve  RClick: dump all]";
            }
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
