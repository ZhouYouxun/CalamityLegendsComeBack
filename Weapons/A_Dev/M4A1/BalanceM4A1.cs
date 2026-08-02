using CalamityLegendsComeBack.Systems;
using CalamityMod;
using System;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 「传承武器箱·M4A1」的全部平衡数值集中入口。
    /// 想调枪先看这里：13 档进度表（与 SHPC 对齐）、各弹种基础伤害、战术同步率常数、大招统一档位。
    /// </summary>
    public static class BalanceM4A1
    {
        // ===================================================================
        //  一、进度档位（与 BalanceSHPC.GetCompletedStageIndex 完全一致的 13 段）
        // ===================================================================
        public static int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                NPC.downedBoss1,                                                 // 克眼
                NPC.downedBoss2,                                                 // 世界吞噬者 / 克脑
                NPC.downedBoss3,                                                 // 骷髅王
                Main.hardMode,                                                   // 进入困难模式
                NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3, // 任意机械 Boss
                NPC.downedPlantBoss,                                             // 世纪之花
                NPC.downedGolemBoss,                                             // 石巨人
                NPC.downedMoonlord,                                              // 月亮领主
                DownedBossSystem.downedProvidence,                              // 亵渎天神
                DownedBossSystem.downedPolterghast,                            // 噬魂幽花
                DownedBossSystem.downedDoG,                                     // 神明吞噬者
                DownedBossSystem.downedYharon,                                 // 犽戎
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas // 星流巨械 + 至尊灾厄
            };

            int stageIndex = 0;
            for (int i = 0; i < clearedStages.Length; i++)
            {
                if (clearedStages[i])
                    stageIndex = i + 1;
            }

            return stageIndex;
        }

        // ===================================================================
        //  二、各弹种基础伤害（14 项 = 初始 + 13 段进度）
        // ===================================================================

        // 左键：M4A1 自动步枪单发子弹。射速极快，所以单发偏低。
        private static readonly int[] BulletBaseDamage =
        {
            10, 14, 17, 22, 26, 31, 36, 40, 44, 48, 58, 66, 78, 104
        };

        // 左键：暖机时不时甩出的火箭弹。约为子弹的 4~5 倍，作为节奏点。
        private static readonly int[] RocketBaseDamage =
        {
            42, 60, 74, 96, 118, 142, 168, 190, 210, 232, 280, 320, 380, 505
        };

        public static int GetBulletBaseDamage() => GetValueForStage(BulletBaseDamage);
        public static int GetRocketBaseDamage() => GetValueForStage(RocketBaseDamage);

        // 右键便携重炮：普通炮弹伤害 = 左键子弹的 600%；终结炮弹再 ×1.6。
        public const float RightClickDamageMultiplier = 6f;
        public const float FinisherShellMultiplier = 1.6f;
        public static int GetShellBaseDamage() => Math.Max(1, (int)Math.Round(GetBulletBaseDamage() * RightClickDamageMultiplier));
        public static int GetFinisherShellBaseDamage() => Math.Max(1, (int)Math.Round(GetShellBaseDamage() * FinisherShellMultiplier));

        /// <summary>物品栏里 Item.damage 的展示基准 = 当前进度的子弹基础伤害。</summary>
        public static int GetInitialItemDamage() => BulletBaseDamage[0];

        // ===================================================================
        //  三、大招统一档位（肉后 / 月后 / 神后）——遵循传奇武器统一规则：
        //  大招每发追踪炮弹伤害 = 当前子弹基础 × 本档倍率。
        //  倍率表按规矩留在本 Balance 文件里，方便单独热调。
        // ===================================================================
        private static readonly float[] UltimateShellDamageMultipliers =
        {
            2.50f, // 肉后
            3.20f, // 月后
            4.00f  // 神后（亵渎天神之后）
        };

        public static float GetUltimateShellDamageMultiplier() =>
            UltimateDamageTier.Resolve(UltimateShellDamageMultipliers);

        public static int GetUltimateShellDamage() =>
            Math.Max(1, (int)Math.Round(GetBulletBaseDamage() * GetUltimateShellDamageMultiplier()));

        // ===================================================================
        //  四、战术同步率（Tactical Sync Rate）常数
        // ===================================================================
        public const float MaxSyncRate = 100f;

        // 阶段阈值
        public const float Stage_TacticalLock = 30f;   // 战术锁定
        public const float Stage_CommandOverride = 70f; // 指挥接管
        public const float Stage_FullSync = 100f;       // 完全同步

        // 命中获取
        public const float SyncGainPerNormalHit = 1f;
        public const float SyncGainPerBossHit = 1.75f;
        public const float SyncGainCritBonus = 0.5f;

        // 衰减：1.25 秒未命中后开始，每秒约 7 点
        public const int SyncDecayDelayTicks = 75;       // 1.25s
        public const float SyncDecayPerSecond = 7f;
        public const float SyncMinAfterDecay = 0f;       // 允许归零，但只靠自然衰减、不因一次失误清零

        // 受伤惩罚
        public const float SyncLossOnHurt = 25f;

        // 右键重炮消耗
        public const float SyncCostRightClick = 35f;

        // ===================================================================
        //  五、复仇印记（Vengeance Mark）
        // ===================================================================
        public const int MaxVengeanceMarks = 3;
        public const int HitsPerMark = 6;               // 连续命中同一目标累积一层
        public const int MarkLifetimeTicks = 300;       // 5 秒未续命中则褪去一层
        public const int Mark3DetonationInterval = 90;  // 三层印记周期性小型战术爆破间隔

        // 印记左键增益
        public const float Mark1DamageBonus = 0.12f;    // 一层：子弹对该目标 +12%
        public const int Mark2ArmorPen = 12;            // 二层：少量破甲

        // ===================================================================
        //  阶段查询工具
        // ===================================================================
        /// <summary>0 = 初始校准，1 = 战术锁定，2 = 指挥接管，3 = 完全同步。</summary>
        public static int GetSyncStage(float sync)
        {
            if (sync >= Stage_FullSync) return 3;
            if (sync >= Stage_CommandOverride) return 2;
            if (sync >= Stage_TacticalLock) return 1;
            return 0;
        }

        // 各阶段左键射速（RPM）与弹速倍率、精度（散布角度）
        // 左键射速：基准 750 RPM（略低于现实 M4A1）；指挥接管起小幅提高射速。
        public static float GetFireRateRpm(int stage) => stage switch
        {
            0 => 750f,   // 初始校准
            1 => 750f,   // 战术锁定
            2 => 850f,   // 指挥接管：提高射速
            _ => 920f    // 完全同步
        };

        // 荧光绿能量弹发射间隔（帧）：比火箭更频繁，穿插在子弹流中。
        public static int GetEnergyOrbInterval(int stage) => stage switch
        {
            0 => 60,
            1 => 54,
            2 => 46,
            _ => 38
        };

        public static float GetBulletSpeedMultiplier(int stage) => stage switch
        {
            0 => 1f,
            1 => 1.22f,  // 战术锁定：提高子弹速度
            2 => 1.35f,
            _ => 1.5f
        };

        public static float GetSpreadDegrees(int stage) => stage switch
        {
            0 => 6.5f,   // 初始校准：弹道逐渐稳定（配合暖机收束）
            1 => 3.2f,   // 战术锁定：更精准
            2 => 2.2f,
            _ => 1.2f
        };

        // 火箭弹发射间隔（帧）：阶段越高越频繁
        public static int GetRocketInterval(int stage) => stage switch
        {
            0 => 150,
            1 => 130,
            2 => 105,
            _ => 85
        };

        // 印记积累速度倍率（战术锁定起印记积累更快 -> 每次命中的印记进度）
        public static float GetMarkBuildMultiplier(int stage) => stage switch
        {
            0 => 1f,
            1 => 1.35f,  // 印记积累更快
            2 => 1.6f,
            _ => 2f
        };

        // ===================================================================
        //  内部工具
        // ===================================================================
        private static int GetValueForStage(int[] values)
        {
            int idx = Utils.Clamp(GetCompletedStageIndex(), 0, values.Length - 1);
            return values[idx];
        }
    }
}
