using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class BalanceSHPC
    {
        public string[] StageNames =
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

        public int[] LeftClickBaseDamage =
        {
            10,
            16,
            16,
            26,
            135,
            135,
            180,
            180,
            250,
            290,
            290,
            360,
            500,
            750
        };

        public int[] RightClickBaseDamage =
        {
            9,
            4,
            4,
            7,
            34,
            34,
            45,
            45,
            63,
            73,
            73,
            90,
            125,
            188
        };

        public int[] RightClickProgressStageThresholds =
        {
            5,
            8,
            11,
            13
        };

        public int GetCompletedStageIndex()
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

        public int GetLeftClickBaseDamage()
        {
            return GetValueForStage(LeftClickBaseDamage, GetCompletedStageIndex());
        }

        public int GetRightClickBaseDamage()
        {
            return GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());
        }

        public int GetRightClickProgressState()
        {
            int completedStage = GetCompletedStageIndex();
            int state = 0;

            for (int i = 0; i < RightClickProgressStageThresholds.Length; i++)
            {
                if (completedStage >= RightClickProgressStageThresholds[i])
                    state = i + 1;
            }

            return state;
        }

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }
    }
}
