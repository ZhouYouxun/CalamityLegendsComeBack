namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    internal static partial class BB_Balance
    {
        private const string DamageSourceFile = "Weapons/BrinyBaron/Balance/BB_DamageConfig.cs";

        // =========================================================================
        // 左键基础伤害与剑气尺寸缩放成长表 (按 Boss 进程阶段)
        // 列 1: 基础伤害 (Damage)
        // 列 2: 尺寸/碰撞箱缩放倍率 (Scale)
        // =========================================================================
        private static readonly object[,] DefaultStageTable =
        {
            //                                      Damage  Scale
            { "Initial",                              24,   0.75f },
            { "Eye of Cthulhu",                       32,   0.85f  },
            { "Evil Boss",                            37,   0.95f },
            { "Skeletron",                            50,   1.05f  },
            { "Hardmode",                             92,   1.15f  },
            { "Any Mechanical Boss",                 110,   1.25f },
            { "Plantera",                            122,   1.35f  },
            { "Golem",                               142,   1.45f },
            { "Moon Lord",                           163,   1.55f },
            { "Providence",                          223,   1.60f },
            { "Polterghast",                         252,   1.75f },
            { "Devourer of Gods",                    306,   1.8f  },
            { "Yharon",                              584,   2.0f  },
            { "Exo Mechs and Supreme Calamitas",    1300,   2.05f  }
        };

        // =========================================================================
        // 大招（超级冲刺）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
        // Tier 1: 月后（月亮领主之后）
        // Tier 2: 神后（亵渎天神 Providence 之后）
        // =========================================================================
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.77f, // Tier 0: 肉后
            3.25f, // Tier 1: 月后
            4.00f  // Tier 2: 神后
        };
    }
}
