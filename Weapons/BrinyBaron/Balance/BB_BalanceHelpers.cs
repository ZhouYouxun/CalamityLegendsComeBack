using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static partial class BB_Balance
    {
        // 左键挥舞核心碰撞箱大小：这是调节它的核心碰撞箱大小。
        public const float LeftClickCoreHitboxSize = 146f;
        // 左键挥舞核心碰撞箱前推距离：这是调节它的核心碰撞箱位置。
        public const float LeftClickCoreHitboxOutset = LeftClickCoreHitboxSize * 0.85f;

        private const int DamageColumn = 1;
        private const int ScaleColumn = 2;

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

        public static BBLeftClickComboType[] GetLeftClickComboSequence(int growthStage)
        {
            return growthStage switch
            {
                1 => Stage1_ComboSequence,
                2 => Stage2_ComboSequence,
                3 => Stage3_ComboSequence,
                4 => Stage4_ComboSequence,
                _ => Stage5_ComboSequence
            };
        }

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(DamageSourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        public static int GetInitialLeftClickBaseDamage() => GetStageDamage(0);

        public static int GetLeftClickBaseDamage() => GetStageDamage(GetCompletedStageIndex());

        public static float GetLeftClickScale()
        {
            int stageIndex = Utils.Clamp(GetCompletedStageIndex(), 0, DefaultStageTable.GetLength(0) - 1);
            float fallback = (float)DefaultStageTable[stageIndex, ScaleColumn];
            return RuntimeBalanceData.GetSourceTableFloat(DamageSourceFile, nameof(DefaultStageTable), stageIndex, ScaleColumn, fallback);
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
            if (stageIndex < 10)
                return 4;
            return 5;
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
                if (clearedStages[i])
                    stageIndex = i + 1;
            }

            return stageIndex;
        }

        private static int GetStageDamage(int stageIndex)
        {
            stageIndex = Utils.Clamp(stageIndex, 0, DefaultStageTable.GetLength(0) - 1);
            int fallback = (int)DefaultStageTable[stageIndex, DamageColumn];
            return System.Math.Max(1, RuntimeBalanceData.GetSourceTableInt(DamageSourceFile, nameof(DefaultStageTable), stageIndex, DamageColumn, fallback));
        }
    }
}
