using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public class BalanceBrinyBaron
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

        public int[] RightClickBaseDamage =
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

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }
    }
}
