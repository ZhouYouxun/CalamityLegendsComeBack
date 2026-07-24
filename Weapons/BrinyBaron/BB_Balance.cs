using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    // =========================================================================
    // 左键连招挥舞产生的弹幕/效果类型枚举
    // 可以在下面的 StageX_ComboSequence 中自由挑选与组合
    // =========================================================================
    public enum BBLeftClickComboType
    {
        SeafoamBlade,             // 海蓝剑气 + 3微型海灵
        FormalWave,               // 普通光波 / 海皇斩光波
        FormalWaveFaster,         // 极速普通光波
        EnhancedWave,             // 强化光波 / 海皇狂澜击
        ThreeSeafoams,            // 3散射海蓝剑气
        SixSeaSpirits,            // 6追逐海灵
        TenSeaSpirits,            // 10狂暴海灵
        FiveShurikens,            // 5重海飞镖
        FiveParallelSeafoams,     // 5重并排海蓝剑气
        Tornado                   // 涡流龙卷
    }

    internal static class BB_Balance
    {
        private const string SourceFile = "Weapons/BrinyBaron/BB_Balance.cs";
        // 左键挥舞核心碰撞箱大小：这是调节它的核心碰撞箱大小。
        public const float LeftClickCoreHitboxSize = 146f;
        // 左键挥舞核心碰撞箱前推距离：这是调节它的核心碰撞箱位置。
        public const float LeftClickCoreHitboxOutset = LeftClickCoreHitboxSize * 0.85f;

        // =========================================================================
        // 左键连招状态机配置（按阶段划分）：
        // 可以在这里直接修改各个阶段每次挥舞释放的弹幕类型！
        // 例如：想在某个阶段让第 1 下产生普通光波，第 2 下产生强化光波，只需修改对应的数组即可。
        // 阶段 1：困难模式前 (Pre-Hardmode)
        // 阶段 2：困难模式后 (Hardmode)
        // 阶段 3：花后 (Post-Plantera)
        // 阶段 4：月后 (Post-Moon Lord)
        // 阶段 5：神后 / 终局 (Post-Dog / Endgame)
        // =========================================================================
        public static readonly BBLeftClickComboType[] Stage1_ComboSequence = new[]
        {
            BBLeftClickComboType.SeafoamBlade
        };

        public static readonly BBLeftClickComboType[] Stage2_ComboSequence = new[]
        {
            BBLeftClickComboType.ThreeSeafoams,
            BBLeftClickComboType.SixSeaSpirits,
            BBLeftClickComboType.FormalWave
        };

        public static readonly BBLeftClickComboType[] Stage3_ComboSequence = new[]
        {
            BBLeftClickComboType.FormalWave,       // 已按要求移除花后第1斩的5个并排冰锥/剑气，改为普通光波
            BBLeftClickComboType.TenSeaSpirits,
            BBLeftClickComboType.FiveShurikens,
            BBLeftClickComboType.EnhancedWave
        };

        public static readonly BBLeftClickComboType[] Stage4_ComboSequence = new[]
        {
            BBLeftClickComboType.FormalWave,       // 已按要求移除第1斩5个并排冰锥，改为普通光波
            BBLeftClickComboType.TenSeaSpirits,
            BBLeftClickComboType.FiveShurikens,
            BBLeftClickComboType.EnhancedWave
        };

        public static readonly BBLeftClickComboType[] Stage5_ComboSequence = new[]
        {
            BBLeftClickComboType.EnhancedWave,     // 终局阶段配置
            BBLeftClickComboType.TenSeaSpirits,
            BBLeftClickComboType.FiveShurikens,
            BBLeftClickComboType.EnhancedWave
        };

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

        private const int DamageColumn = 1;
        private const int ScaleColumn = 2;

        private static readonly object[,] DefaultStageTable =
        {
            //                                      Damage  Scale
            { "Initial",                              24,   0.65f   },
            { "Eye of Cthulhu",                       30,   0.7f },
            { "Evil Boss",                            35,   0.75f },
            { "Skeletron",                            48,   0.8f },
            { "Hardmode",                             70,   0.9f },
            { "Any Mechanical Boss",                  81,   0.95f },
            { "Plantera",                             55,   1.0f },
            { "Golem",                                72,   1.05f },
            { "Moon Lord",                           109,   1.15f },
            { "Providence",                          325,   1.25f },
            { "Polterghast",                         695,   1.35f   },
            { "Devourer of Gods",                   1000,   1.4f   },
            { "Yharon",                             1300,   1.5f   },
            { "Exo Mechs and Supreme Calamitas",    2000,   1.5f   }
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

        // 大招（超级冲刺）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

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
            return System.Math.Max(1, RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(DefaultStageTable), stageIndex, DamageColumn, fallback));
        }
    }
}
