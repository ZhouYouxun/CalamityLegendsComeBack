using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static class BB_Balance
    {
        public static readonly object[,] StageInfo =
        {
            { "Initial", 0.6f },
            { "Eye of Cthulhu", 0.72f },
            { "Evil Boss", 0.84f },
            { "Skeletron", 0.96f },
            { "Hardmode", 1.08f },
            { "Any Mechanical Boss", 1.2f },
            { "Plantera", 1.32f },
            { "Golem", 1.44f },
            { "Moon Lord", 1.56f },
            { "Providence", 1.68f },
            { "Polterghast", 1.8f },
            { "Devourer of Gods", 1.92f },
            { "Yharon", 2.04f },
            { "Exo Mechs and Supreme Calamitas", 2.15f }
        };

        public static readonly string[] StageNames =
        {
            "Initial",
            "Eye of Cthulhu",
            "Evil Boss",
            "Skeletron",
            "Hardmode",
            "Any Mechanical Boss",
            "Plantera",
            "Golem",
            "Moon Lord",
            "Providence",
            "Polterghast",
            "Devourer of Gods",
            "Yharon",
            "Exo Mechs and Supreme Calamitas"
        };

        public static readonly int[] LeftClickBaseDamage =
        {
            10,
            15,
            24,
            33,
            42,
            79,
            121,
            144,
            465,
            472,
            505,
            1248,
            1351,
            16590
        };

        public static int GetLeftClickBaseDamage()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, LeftClickBaseDamage.Length - 1);
            return System.Math.Max(1, LeftClickBaseDamage[stageIndex]);
        }

        public static float GetLeftClickScale()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, StageInfo.GetLength(0) - 1);
            return (float)StageInfo[stageIndex, 1];
        }

        public static int GetGrowthStage()
        {
            int stageIndex = GetCompletedStageIndex();
            if (stageIndex < 4) // Pre-hardmode
                return 1;
            if (stageIndex < 6) // Hardmode to pre-Plantera
                return 2;
            if (stageIndex < 8) // Plantera to Moon Lord
                return 3;
            return 4; // Moon Lord onwards
        }

        private static int GetCompletedStageIndex()
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
    }
}
