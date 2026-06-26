using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static class BB_Balance
    {
        private const string SourceFile = "Weapons/BrinyBaron/BB_Balance.cs";
        private const int DamageColumn = 1;
        private const int ScaleColumn = 2;

        private static readonly object[,] DefaultStageTable =
        {
            //                                      Damage  Scale
            { "Initial",                              19,   0.6f   },
            { "Eye of Cthulhu",                       25,   0.607f },
            { "Evil Boss",                            33,   0.628f },
            { "Skeletron",                            45,   0.664f },
            { "Hardmode",                             70,   0.714f },
            { "Any Mechanical Boss",                  80,   0.778f },
            { "Plantera",                            101,   0.856f },
            { "Golem",                               124,   0.948f },
            { "Moon Lord",                           200,   1.054f },
            { "Providence",                          250,   1.174f },
            { "Polterghast",                         295,   1.3f   },
            { "Devourer of Gods",                    435,   1.3f   },
            { "Yharon",                              475,   1.3f   },
            { "Exo Mechs and Supreme Calamitas",    1000,   1.3f   }
        };

        private static readonly string[] DefaultStageNames =
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

        public static string[] StageNames => DefaultStageNames;

        public static int GetInitialLeftClickBaseDamage() => GetStageDamage(0);

        public static int GetLeftClickBaseDamage() => GetStageDamage(GetCompletedStageIndex());

        public static float GetLeftClickScale()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, DefaultStageTable.GetLength(0) - 1);
            float fallback = (float)DefaultStageTable[stageIndex, ScaleColumn];
            return RuntimeBalanceData.GetSourceTableFloat(SourceFile, nameof(DefaultStageTable), stageIndex, ScaleColumn, fallback);
        }

        public static int GetGrowthStage()
        {
            int stageIndex = GetCompletedStageIndex();
            if (stageIndex < 4)
                return 1;
            if (stageIndex < 6)
                return 2;
            if (stageIndex < 8)
                return 3;
            return 4;
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

        private static int GetStageDamage(int stageIndex)
        {
            stageIndex = Utils.Clamp(stageIndex, 0, DefaultStageTable.GetLength(0) - 1);
            int fallback = (int)DefaultStageTable[stageIndex, DamageColumn];
            return System.Math.Max(1, RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(DefaultStageTable), stageIndex, DamageColumn, fallback));
        }
    }
}
