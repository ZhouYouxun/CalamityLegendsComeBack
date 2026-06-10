using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal static class MalachiteBalance
    {
        public const int DepletionBurstKunaiCount = 5;
        public const int RightFeatherMaxCount = 3;
        public const int RightFeatherGenerationFrames = 36;
        public const int RightFeatherReleaseSpacingFrames = 2;

        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedPlaguebringerGoliath => DownedBossSystem.downedPlaguebringer;
        public static bool DownedMoonLord => NPC.downedMoonlord;

        public static bool NormalKunaiIgnoresGravity => DownedWallOfFlesh;

        public static float LeftClickUseSpeedMultiplier => DownedWallOfFlesh ? 1f : 1.35f;

        public static int NormalLeftClickKunaiCount => 1;
    }
}
