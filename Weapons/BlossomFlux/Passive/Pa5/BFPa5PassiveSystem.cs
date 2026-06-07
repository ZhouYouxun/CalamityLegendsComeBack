using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal static class BFPa5PassiveSystem
    {
        public static bool IsActive(Player player, BlossomFluxChloroplastPresetType preset)
        {
            return player?.active == true &&
                !player.dead &&
                player.HeldItem?.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                player.GetModPlayer<BFRightUIPlayer>().CurrentPreset == preset;
        }

        public static bool AnyPlayerActive(BlossomFluxChloroplastPresetType preset)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (IsActive(player, preset))
                    return true;
            }

            return false;
        }

        public static int CountHostileEnemiesOnScreen()
        {
            Rectangle screen = new(
                (int)Main.screenPosition.X - 64,
                (int)Main.screenPosition.Y - 64,
                Main.screenWidth + 128,
                Main.screenHeight + 128);

            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (IsCountedEnemy(npc) && screen.Intersects(npc.Hitbox))
                    count++;
            }

            return count;
        }

        public static bool IsCountedEnemy(NPC npc)
        {
            return npc.active &&
                !npc.friendly &&
                !npc.dontTakeDamage &&
                npc.lifeMax > 5 &&
                !npc.immortal;
        }
    }
}
