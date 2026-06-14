using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal static class MalachiteBalance
    {
        public const int DepletionBurstKunaiCount = 5;
        public const int RightFeatherMaxCount = 23;
        public const int RightFeatherGenerationFrames = 36;
        public const int PeacockBoxFeatherGenerationFrames = 60;
        public const int RightFeatherReleaseSpacingFrames = 2;

        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedPlaguebringerGoliath => DownedBossSystem.downedPlaguebringer;
        public static bool DownedMoonLord => NPC.downedMoonlord;

        public static bool NormalKunaiIgnoresGravity => DownedWallOfFlesh;

        public static float LeftClickUseSpeedMultiplier => DownedWallOfFlesh ? 1f : 1.35f;

        public static int NormalLeftClickKunaiCount => 1;

        // Damage progression arrays for left and right click
        public static readonly int[] LeftClickBaseDamage =
        {
            12,   // Initial
            18,   // Eye of Cthulhu
            28,   // Evil Boss
            38,   // Skeletron
            48,   // Hardmode
            85,   // Any Mechanical Boss
            130,  // Plantera
            155,  // Golem
            480,  // Moon Lord
            490,  // Providence
            520,  // Polterghast
            1280, // Devourer of Gods
            1380, // Yharon
            16800 // Exo Mechs and Supreme Calamitas
        };

        public static readonly int[] RightClickBaseDamage =
        {
            8,    // Initial
            12,   // Eye of Cthulhu
            18,   // Evil Boss
            24,   // Skeletron
            32,   // Hardmode
            60,   // Any Mechanical Boss
            95,   // Plantera
            110,  // Golem
            360,  // Moon Lord
            370,  // Providence
            390,  // Polterghast
            980,  // Devourer of Gods
            1060, // Yharon
            12800 // Exo Mechs and Supreme Calamitas
        };

        // 2D Array for projectile velocities: Column 0 is Left-click, Column 1 is Right-click
        public static readonly float[,] ProjectileVelocities = new float[,]
        {
            { 18f, 24f }, // Pre-hardmode
            { 22f, 30f }, // Hardmode to Plantera
            { 26f, 36f }, // Plantera to Moon Lord
            { 30f, 42f }  // Post-Moon Lord
        };

        public static int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                NPC.downedBoss3,
                Main.hardMode,
                NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas
            };

            int stageIndex = 0;
            for (int i = 0; i < clearedStages.Length; i++)
            {
                if (!clearedStages[i])
                    break;

                stageIndex = i + 1;
            }

            return stageIndex;
        }

        public static int GetLeftClickBaseDamage()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, LeftClickBaseDamage.Length - 1);
            return System.Math.Max(1, LeftClickBaseDamage[stageIndex]);
        }

        public static int GetRightClickBaseDamage()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, RightClickBaseDamage.Length - 1);
            return System.Math.Max(1, RightClickBaseDamage[stageIndex]);
        }

        public static int GetVelocityStageIndex()
        {
            if (!Main.hardMode)
                return 0;
            if (!NPC.downedPlantBoss)
                return 1;
            if (!NPC.downedMoonlord)
                return 2;
            return 3;
        }

        public static float GetLeftClickVelocity()
        {
            return ProjectileVelocities[GetVelocityStageIndex(), 0];
        }

        public static float GetRightClickVelocity()
        {
            return ProjectileVelocities[GetVelocityStageIndex(), 1];
        }
    }
}
