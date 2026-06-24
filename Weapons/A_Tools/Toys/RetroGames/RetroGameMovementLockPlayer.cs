using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Toys.RetroGames
{
    internal sealed class RetroGameMovementLockPlayer : ModPlayer
    {
        public override void SetControls()
        {
            if (!IsHoldingRetroGame(Player))
                return;

            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlHook = false;
            Player.controlMount = false;
        }

        private static bool IsHoldingRetroGame(Player player)
        {
            int heldType = player.HeldItem?.type ?? 0;
            return heldType == ModContent.ItemType<Tetris>() ||
                   heldType == ModContent.ItemType<Game2048>() ||
                   heldType == ModContent.ItemType<Snake>() ||
                   heldType == ModContent.ItemType<Minesweeper>() ||
                   heldType == ModContent.ItemType<STG>();
        }
    }
}
