using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static class BB_Balance
    {
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
