using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal static class MalachiteProgression
    {
        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedPlaguebringerGoliath => DownedBossSystem.downedPlaguebringer;
        public static bool DownedMoonLord => NPC.downedMoonlord;

        public static bool NormalKunaiIgnoresGravity => DownedWallOfFlesh;

        public static float LeftClickUseSpeedMultiplier => DownedWallOfFlesh ? 1f : 1.35f;

        public static int NormalLeftClickKunaiCount
        {
            get
            {
                if (DownedMoonLord)
                    return 3;

                if (DownedPlaguebringerGoliath)
                    return 2;

                return 1;
            }
        }

        public const int DepletionBurstKunaiCount = 5;
    }
}
