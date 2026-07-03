using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot
{
    public class DesertEagleSlotInputSystem : ModSystem
    {
        private static bool prevMouseMiddle;

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

            if (DesertEagleSlotUI.IsOpen &&
                DesertEagleSlotUI.PanelRect.Contains(PlayerInput.MouseX, PlayerInput.MouseY))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        private static void DetectInput()
        {
            if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            if (!Main.playerInventory)
            {
                prevMouseMiddle = Main.mouseMiddle;
                return;
            }

            bool justMiddle = Main.mouseMiddle && !prevMouseMiddle;
            if (justMiddle && Main.HoverItem?.type == ModContent.ItemType<DesertEagle>())
                DesertEagleSlotUI.Toggle(Main.LocalPlayer);

            prevMouseMiddle = Main.mouseMiddle;
        }
    }
}
