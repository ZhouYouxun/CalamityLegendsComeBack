using System;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir
{
    /// <summary>
    /// 「冥蚀天底 / Umbral Nadir」——小传奇武器的平衡中枢。
    /// 这里集中记载它在每个 Boss 进程阶段的：左键基础攻击力、右键基础攻击力、以及尺寸成长曲线；
    /// 以及左键三段连招的分段倍率、命中派生弹幕倍率、右键三连投掷的节奏与连锁上限。
    /// 无被动、无大招，故不含任何大招倍率表。
    /// </summary>
    internal static class UmbralNadirBalance
    {
        // =========================================================================
        // 阶段成长表（按 Boss 进程）
        // 列 1: 左键基础伤害   列 2: 右键基础伤害   列 3: 尺寸/碰撞箱缩放
        // 说明：左键各连招段还会在此基础上乘 UpSlash/DownSlash/Dash 倍率；
        //       右键投矛/连锁脉冲会在右键基础上乘 Javelin/ChainPulse 倍率。
        // 本武器为末期 Auric 档合成物，实际生效的多为月后~神后~赛后诸档，
        // 前段数值提供完整成长曲线以备测试与平衡参照。
        // =========================================================================
        private static readonly object[,] StageTable =
        {
            //                                      Left   Right  Scale
            { "Initial",                             90,     65,   0.85f },
            { "Eye of Cthulhu",                     120,     85,   0.88f },
            { "Evil Boss",                          145,    105,   0.90f },
            { "Skeletron",                          180,    130,   0.92f },
            { "Hardmode",                           260,    185,   0.95f },
            { "Any Mechanical Boss",                340,    240,   0.98f },
            { "Plantera",                           430,    300,   1.00f },
            { "Golem",                              520,    365,   1.02f },
            { "Moon Lord",                          780,    545,   1.05f },
            { "Providence",                        1150,    805,   1.10f },
            { "Polterghast",                       1450,   1015,   1.14f },
            { "Devourer of Gods",                  1750,   1225,   1.18f },
            { "Yharon",                            1950,   1365,   1.22f },
            { "Exo Mechs and Supreme Calamitas",   2150,   1505,   1.28f },
        };

        private const int LeftDamageColumn = 1;
        private const int RightDamageColumn = 2;
        private const int ScaleColumn = 3;

        // ===== 左键三段连招·本体倍率（相对左键基础伤害）=====
        public const float UpSlashDamageMult = 1.00f;    // 第一段·上挑斩
        public const float DownSlashDamageMult = 1.20f;  // 第二段·劈落斩
        public const float DashDamageMult = 1.70f;       // 第三段·冲刺贯穿（最高，且必定暴击；不施加强制位移）

        // ===== 左键命中·短促范围爆炸（相对当前 holdout 实际伤害）=====
        // 各段的爆炸伤害倍率与半径（px）
        public static readonly float[] ImpactDamageMult = { 0.28f, 0.36f, 0.55f };
        public static readonly float[] ImpactRadius = { 72f, 88f, 112f };
        public static readonly float[] ImpactScreenShake = { 0f, 0f, 3.0f }; // 仅第三段轻微震屏

        // ===== 右键·高速三连投矛 =====
        public const float JavelinDamageMult = 0.85f;       // 投矛本体（相对右键基础伤害）
        public const float JavelinSpeedMin = 4.8f;          // 每次位移（extraUpdates=3，实际飞速为其 4 倍）
        public const float JavelinSpeedMax = 5.52f;         // 原上限 4.6 × 1.20
        public const float JavelinSpreadDegrees = 4f;       // 角度散布 ±4°
        public const float JavelinLateralOffset = 14f;      // 出生横向偏移 ±14 px
        public const float JavelinForwardMin = 24f;
        public const float JavelinForwardMax = 44f;

        // 投掷节奏（帧）：每轮 3 发，发间隔 5 帧；末发→下一轮首发间隔 35 帧 → 整轮周期 45 帧。
        public const int ThrowsPerRound = 3;
        public const int ThrowInterval = 5;
        public const int RoundGap = 35;
        public const int RoundPeriod = (ThrowsPerRound - 1) * ThrowInterval + RoundGap;

        // ===== 右键·前两发沿路唤出的暗影魂针 =====
        public const float ShadowSoulDamageMult = 0.22f;     // 相对投矛伤害
        public const int ShadowSoulsPerJavelin = 3;          // 单矛最多召唤数（若要"每矛四针"改为 4）
        public const int ShadowSoulSpawnStart = 8;           // 投矛存活满 8 帧后开始
        public const int ShadowSoulSpawnInterval = 9;        // 之后每 9 帧检查一次
        public const int MaxShadowSoulsPerPlayer = 6;        // 每名玩家同时存在上限

        // ===== 右键·第三发命中终爆 =====
        public const float FinalExplosionDamageMult = 0.65f; // 相对第三发投矛伤害
        public const float FinalExplosionRadius = 132f;
        public const float FinalExplosionScreenShake = 3.8f;

        // ===== 双键·回旋斩迹 =====
        public const float SpinDamageMult = 0.46f;      // 相对左键当前基础伤害；不暴击、不额外爆炸
        public const float SpinRotationSpeed = 0.40f;   // rad/帧
        public const float SpinRadiusMin = 92f;
        public const float SpinRadiusMax = 118f;
        public const float SpinLineCollision = 160f;
        public const int SpinHitCooldown = 13;

        // ===== 旧链路保留常量（AbyssRift / VoidEssence 文件仍在工程内，本武器不再引用）=====
        public const int VoidEssenceMaxGeneration = 2;
        public const int MaxActiveVoidEssence = 45;

        /// <summary>连招段数(0/1/2) → 命中爆炸倍率。</summary>
        public static float GetImpactDamageMult(int stage) => ImpactDamageMult[Math.Clamp(stage, 0, 2)];
        /// <summary>连招段数(0/1/2) → 命中爆炸半径。</summary>
        public static float GetImpactRadius(int stage) => ImpactRadius[Math.Clamp(stage, 0, 2)];
        /// <summary>连招段数(0/1/2) → 命中震屏强度。</summary>
        public static float GetImpactScreenShake(int stage) => ImpactScreenShake[Math.Clamp(stage, 0, 2)];

        // =========================================================================
        // 阶段查询接口
        // =========================================================================

        /// <summary>物品初始（未击败任何 Boss 时）的左键基础伤害，用于 SetDefaults。</summary>
        public static int GetInitialLeftDamage() => (int)StageTable[0, LeftDamageColumn];

        /// <summary>当前进程阶段的左键基础伤害。</summary>
        public static int GetLeftBaseDamage() => (int)StageTable[CurrentStageIndex, LeftDamageColumn];

        /// <summary>当前进程阶段的右键基础伤害。</summary>
        public static int GetRightBaseDamage() => (int)StageTable[CurrentStageIndex, RightDamageColumn];

        /// <summary>当前进程阶段的左键挥砍尺寸缩放。</summary>
        public static float GetLeftScale() => (float)StageTable[CurrentStageIndex, ScaleColumn];

        private static int CurrentStageIndex =>
            Math.Clamp(GetCompletedStageIndex(), 0, StageTable.GetLength(0) - 1);

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
    }
}
