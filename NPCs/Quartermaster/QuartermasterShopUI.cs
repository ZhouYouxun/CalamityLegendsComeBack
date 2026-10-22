using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.NPCs.Quartermaster;

// Keep the shop open while navigating its pages; the dialogue button also changes pages.
public sealed class QuartermasterShopUI : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
        if (index >= 0)
            layers.Insert(index + 1, new LegacyGameInterfaceLayer("CalamityLegendsComeBack: Quartermaster pages", Draw, InterfaceScaleType.UI));
    }

    private static bool Draw()
    {
        if (Main.gameMenu || !Main.playerInventory || Main.recBigList || Main.npcShop == 0 || Main.LocalPlayer.talkNPC < 0)
            return true;
        NPC npc = Main.npc[Main.LocalPlayer.talkNPC];
        if (!npc.active || npc.ModNPC is not Quartermaster)
            return true;

        // Vanilla draws its savings at x=504 and its four shop rows from invBottom.
        // One compact button sits below savings, to the right of the item grid.
        var button = new Rectangle(504, Main.instance.invBottom + 106, 74, 32);
        bool hovered = button.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, button, hovered ? new Color(80, 105, 145) : new Color(25, 35, 55, 235));
        Utils.DrawBorderString(Main.spriteBatch, ">>>",
            new Vector2(button.Center.X, button.Y + 5), hovered ? Color.Gold : Color.White, 0.8f, 0.5f);
        if (hovered)
            Main.LocalPlayer.mouseInterface = true;
        if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
        {
            Main.mouseLeftRelease = false;
            Quartermaster.SelectNextPage();
            Main.instance.shop[Main.npcShop].SetupShop(NPCShopDatabase.GetShopName(npc.type, Quartermaster.CurrentShopName), npc);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        return true;
    }
}
