using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    /// <summary>
    /// 在 Draw 阶段检测背包内对沙漠之鹰的开仓操作，与 SHPCLoadingUI 共用同一个按键。
    /// 未绑定时：鼠标中键悬停在 DE 上触发；已绑定时：按键 + 背包中有 DE 触发。
    /// </summary>
    public class DesertEagleSlotInputSystem : ModSystem
    {
        private static bool prevMouseMiddle;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Desert Eagle Slot Input",
                () => { DetectInput(); return true; },
                InterfaceScaleType.None));
        }

        private static void DetectInput()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            Player player = Main.LocalPlayer;
            DesertEagleSlotPlayer slotPlayer = player.GetModPlayer<DesertEagleSlotPlayer>();

            if (Main.gamePaused)
            {
                CloseSlotUI(player, slotPlayer, false);
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            // 背包关闭时不处理
            if (!Main.playerInventory)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            int deType = ModContent.ItemType<DesertEagle>();

            bool keyBound = KeybindSystem.SHPCLoadingUI.GetAssignedKeys().Any();

            // ── 未绑定：中键悬停在 DE 上 ──────────────────────────────
            if (!keyBound)
            {
                bool justMiddle = Main.mouseMiddle && !prevMouseMiddle;
                if (justMiddle && Main.HoverItem?.type == deType)
                {
                    ToggleSlotUI(player, slotPlayer);
                }
            }

            // ── 已绑定：按下按键且悬停在 DE 上 ─────────────────────────
            if (keyBound && KeybindSystem.SHPCLoadingUI.JustPressed)
            {
                if (Main.HoverItem?.type == deType)
                {
                    ToggleSlotUI(player, slotPlayer);
                }
            }

            prevMouseMiddle = Main.mouseMiddle;
        }

        private static void ToggleSlotUI(Player player, DesertEagleSlotPlayer slotPlayer)
        {
            if (slotPlayer.SlotUIOpen)
            {
                CloseSlotUI(player, slotPlayer, true);
            }
            else
            {
                // 开启 UI
                Projectile.NewProjectile(
                    player.GetSource_Misc("DESlotUI"),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<DesertEagleSlotUI>(),
                    0, 0f, player.whoAmI, 0f, 0f);
                SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.06f, Volume = 0.62f }, player.Center);
            }
        }

        private static void CloseSlotUI(Player player, DesertEagleSlotPlayer slotPlayer, bool playSound)
        {
            bool closed = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.ModProjectile is DesertEagleSlotUI)
                {
                    p.Kill();
                    closed = true;
                }
            }

            if (closed || slotPlayer.SlotUIOpen)
            {
                slotPlayer.SlotUIOpen = false;
                if (playSound)
                    SoundEngine.PlaySound(SoundID.MenuClose with { Pitch = 0.06f, Volume = 0.62f }, player.Center);
            }
        }
    }
}
