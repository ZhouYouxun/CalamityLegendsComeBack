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
            { "Initial",                              24,   0.65f },
            { "Eye of Cthulhu",                       30,   0.7f  },
            { "Evil Boss",                            35,   0.75f },
            { "Skeletron",                            48,   0.8f  },
            { "Hardmode",                             70,   0.9f  },
            { "Any Mechanical Boss",                  81,   0.95f },
            { "Plantera",                             60,   1.0f  },
            { "Golem",                                75,   1.15f },
            { "Moon Lord",                           105,   1.25f },
            { "Providence",                          325,   1.30f },
            { "Polterghast",                         695,   1.35f },
            { "Devourer of Gods",                   1000,   1.4f  },
            { "Yharon",                             1300,   1.5f  },
            { "Exo Mechs and Supreme Calamitas",    2000,   1.5f  }
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
            2.50f, // Tier 0: 肉后
            3.20f, // Tier 1: 月后
            4.00f  // Tier 2: 神后
        };
    }
}
