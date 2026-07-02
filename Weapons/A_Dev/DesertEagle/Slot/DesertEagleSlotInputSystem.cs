using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 在界面层（Draw 阶段）检测背包内对沙漠之鹰的开仓操作，与 SHPCLoadingUI 共用同一个按键。
    /// 未绑定时：鼠标中键悬停在 DE 上触发；已绑定时：按键 + 悬停在 DE 上触发。
    /// 全部逻辑在 Draw 阶段执行，自动暂停（gamePaused）期间依然有效。
    /// </summary>
    public class DesertEagleSlotInputSystem : ModSystem
    {
        private static bool prevMouseMiddle;
        private static bool prevKeybindPressed;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Desert Eagle Slot UI",
                () =>
                {
                    DetectInput();
                    DesertEagleSlotUI.DrawAndHandle(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.None));
        }

        public override void PreUpdatePlayers()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0)
                return;

            if (DesertEagleSlotUI.IsOpen)
            {
                // PanelRect 是 raw 屏幕像素，这里用 PlayerInput 的原始鼠标坐标，
                // 避免 Update 阶段 Main.mouseX 处于其他缩放上下文导致判定漂移。
                if (DesertEagleSlotUI.PanelRect.Contains(PlayerInput.MouseX, PlayerInput.MouseY))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            }
        }

        private static bool IsKeybindPressed(ModKeybind keybind)
        {
            if (keybind == null) return false;
            var keys = keybind.GetAssignedKeys();
            if (keys == null || keys.Count == 0) return false;

            foreach (var key in keys)
            {
                if (key == "Mouse1") { if (Main.mouseLeft) return true; }
                else if (key == "Mouse2") { if (Main.mouseRight) return true; }
                else if (key == "Mouse3") { if (Main.mouseMiddle) return true; }
                else
                {
                    if (System.Enum.TryParse<Microsoft.Xna.Framework.Input.Keys>(key, true, out var xnaKey))
                    {
                        if (Main.keyState.IsKeyDown(xnaKey))
                            return true;
                    }
                }
            }
            return false;
        }

        private static void DetectInput()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            // 背包关闭时不处理开关（已开启的 UI 会自行淡出关闭）
            if (!Main.playerInventory)
            {
                prevMouseMiddle = Main.mouseMiddle;
                prevKeybindPressed = IsKeybindPressed(KeybindSystem.SHPCLoadingUI);
                return;
            }

            Player player = Main.LocalPlayer;
            int deType = ModContent.ItemType<DesertEagle>();

            bool keyBound = KeybindSystem.SHPCLoadingUI.GetAssignedKeys().Any();
            bool keybindPressed = IsKeybindPressed(KeybindSystem.SHPCLoadingUI);
            bool keybindJustPressed = keybindPressed && !prevKeybindPressed;
            prevKeybindPressed = keybindPressed;

            // ── 未绑定：中键悬停在 DE 上 ──────────────────────────────
            if (!keyBound)
            {
                bool justMiddle = Main.mouseMiddle && !prevMouseMiddle;
                if (justMiddle && Main.HoverItem?.type == deType)
                {
                    DesertEagleSlotUI.Toggle(player);
                }
            }
            // ── 已绑定：按下按键且悬停在 DE 上 ─────────────────────────
            else
            {
                if (keybindJustPressed && Main.HoverItem?.type == deType)
                {
                    DesertEagleSlotUI.Toggle(player);
                }
            }

            prevMouseMiddle = Main.mouseMiddle;
        }
    }
}
