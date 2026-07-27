using System;
using CalamityMod;
using CalamityMod.NPCs.NormalNPCs;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    /// <summary>
    /// 反物质步枪 (Anti-Materiel Rifle) 的游戏进度阶段定义。
    /// </summary>
    internal enum AMRProgressionStage
    {
        Initial = 0,
        EyeOfCthulhu = 1,
        EvilBoss = 2,
        WallOfFlesh = 3,
        AnyMechanicalBoss = 4,
        Plantera = 5,
        MoonLord = 6,
        Providence = 7,
        DevourerOfGods = 8,
        Finale = 9
    }

    /// <summary>
    /// 反物质步枪的平衡数据、数值常量与机制解锁配置类。
    /// </summary>
    internal static class AMRBalance
    {
        private const string SourceFile = "Weapons/A_Upgrade/AntiMaterielRifle/AMRBalance.cs";

        #region 基础平衡数据表格 (2D Balance Tables)

        /// <summary>
        /// 步枪主面板伤害表 (按游戏进度阶段划分)。
        /// 列说明：
        /// 0: Stage (阶段名称)
        /// 1: BaseDamage (基础面板伤害)
        /// </summary>
        internal static readonly object[,] MainDamageTable =
        {
            // Stage (阶段)                                BaseDamage (基础伤害)
            { "Initial",                                    168 },
            { "Eye of Cthulhu",                             220 },
            { "Evil Boss",                                  275 },
            { "Wall of Flesh",                              480 },
            { "Any Mechanical Boss",                        700 },
            { "Plantera",                                  1050 },
            { "Moon Lord",                                 1818 },
            { "Providence",                                3000 },
            { "Devourer of Gods",                          4200 },
            { "Finale",                                    7000 }
        };

        /// <summary>
        /// 步枪主机制与参数表 (按游戏进度阶段划分)。
        /// 列说明：
        /// 0: Stage (阶段名称)
        /// 1: DeathMark (死亡印记解锁 - 眼后：锁定盲区标记/命中判定)
        /// 2: MetalJet (金属射流解构解锁 - 邪恶后：命中喷射金属碎屑)
        /// 3: Bullseye (战术预判准心解锁 - 肉后：自动检索周围敌人生成悬浮准心)
        /// 4: Calibration (校准射击解锁 - 花后：连续命中同一目标获得 45% 伤害增益)
        /// 5: CriticalOverflow (暴击溢出转换解锁 - 月后：>100% 暴击率转为无视防御与爆伤)
        /// 6: CoreRupture (堆芯熔毁解锁 - 亵渎后：引发强烈热辐射与爆裂效果)
        /// 7: OnyxSequence (玛瑙序列解锁 - 终章：发射玛瑙标记弹与终极子弹交替)
        /// 8: LeftBaseSpeed (左键基础弹速 - 基础为 32，随阶段递增 1.8，受 FlightSpeedScale 缩放)
        /// </summary>
        internal static readonly object[,] MainParamsTable =
        {
            // Stage                   DeathMark  MetalJet  Bullseye  Calibration  CritOverflow  CoreRupture  OnyxSequence  LeftBaseSpeed
            { "Initial",                 false,     false,    false,      false,       false,        false,        false,         32.0f },
            { "Eye of Cthulhu",          true,      false,    false,      false,       false,        false,        false,         33.8f },
            { "Evil Boss",               true,      true,     false,      false,       false,        false,        false,         35.6f },
            { "Wall of Flesh",           true,      true,     true,       false,       false,        false,        false,         37.4f },
            { "Any Mechanical Boss",     true,      true,     true,       false,       false,        false,        false,         39.2f },
            { "Plantera",                true,      true,     true,       true,        false,        false,        false,         41.0f },
            { "Moon Lord",               true,      true,     true,       true,        true,         false,        false,         42.8f },
            { "Providence",              true,      true,     true,       true,        true,         true,         false,         44.6f },
            { "Devourer of Gods",        true,      true,     true,       true,        true,         true,         false,         46.4f },
            { "Finale",                  true,      true,     true,       true,        true,         true,         true,          48.2f }
        };

        #endregion

        #region 数值常量 (Numerical Constants)

        /// <summary> 初始基础伤害 (168点) </summary>
        public const int InitialDamage = 168;

        /// <summary> 基础开火间隔帧数 (60帧 = 1秒) </summary>
        public const int BaseFireInterval = 75;

        /// <summary> 右键瞄准镜最大蓄力帧数 (90帧 = 1.5秒) </summary>
        public const int ScopeChargeFrames = 180;

        /// <summary> 右键瞄准镜允许发射的最小蓄力帧数 (12帧 = 0.2秒) </summary>
        public const int MinimumScopeChargeFrames = 30;

        /// <summary> 滑铲动作持续帧数 (15帧 = 0.25秒) </summary>
        public const int SlideFrames = 15;

        /// <summary> 连击滑铲有效输入窗口帧数 (120帧 = 2秒) </summary>
        public const int SlideChainWindowFrames = 120;

        /// <summary> 滑铲连击耗尽后的冷却时间帧数 (1800帧 = 30秒) </summary>
        public const int SlideCooldownFrames = 1800;

        /// <summary> 最大连续滑铲次数 (4次) </summary>
        public const int MaxSlideChainCount = 4;

        /// <summary> 全局弹速缩放倍率 (0.67x) </summary>
        public const float FlightSpeedScale = 0.67f;

        /// <summary> 普通敌怪最大生命值真实伤害比例 (10%) </summary>
        public const float NormalMaxLifeTrueDamageRatio = 0.10f;

        /// <summary> Boss敌怪最大生命值真实伤害比例 (0.5%) </summary>
        public const float BossMaxLifeTrueDamageRatio = 0.005f;

        /// <summary> 右键最小蓄力弹速 (未缩放前: 38) </summary>
        public const float MinRightProjectileSpeedUnscaled = 38f;

        /// <summary> 右键最大蓄力弹速 (未缩放前: 62) </summary>
        public const float MaxRightProjectileSpeedUnscaled = 62f;

        /// <summary> 右键最小蓄力伤害倍率 (1.0x) </summary>
        public const float MinRightDamageMultiplier = 0.33f;

        /// <summary> 右键最大蓄力伤害倍率 (5.0x) </summary>
        public const float MaxRightDamageMultiplier = 3.25f;

        #endregion

        #region 动态属性与获取逻辑 (Properties & Dynamic Resolvers)

        /// <summary>
        /// 获取当前世界进度所对应的反物质步枪阶段。
        /// </summary>
        public static AMRProgressionStage Stage
        {
            get
            {
                AMRProgressionStage stage = AMRProgressionStage.Initial;

                if (NPC.downedBoss1)
                    stage = AMRProgressionStage.EyeOfCthulhu;
                if (NPC.downedBoss2)
                    stage = AMRProgressionStage.EvilBoss;
                if (Main.hardMode)
                    stage = AMRProgressionStage.WallOfFlesh;
                if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                    stage = AMRProgressionStage.AnyMechanicalBoss;
                if (NPC.downedPlantBoss)
                    stage = AMRProgressionStage.Plantera;
                if (NPC.downedMoonlord)
                    stage = AMRProgressionStage.MoonLord;
                if (DownedBossSystem.downedProvidence)
                    stage = AMRProgressionStage.Providence;
                if (DownedBossSystem.downedDoG)
                    stage = AMRProgressionStage.DevourerOfGods;
                if (DownedBossSystem.downedExoMechs || DownedBossSystem.downedCalamitas)
                    stage = AMRProgressionStage.Finale;

                return stage;
            }
        }

        /// <summary> 当前阶段对应的基础伤害 </summary>
        public static int CurrentDamage => GetMainDamage((int)Stage);

        /// <summary> 基础开火间隔帧数 </summary>
        public static int FireInterval => BaseFireInterval;

        /// <summary> 死亡印记是否解锁 (眼后) </summary>
        public static bool DeathMarkUnlocked => GetMainParamBool((int)Stage, 1);

        /// <summary> 金属射流解构是否解锁 (邪恶后) </summary>
        public static bool MetalJetUnlocked => GetMainParamBool((int)Stage, 2);

        /// <summary> 战术预判准心是否解锁 (肉后) </summary>
        public static bool BullseyeUnlocked => GetMainParamBool((int)Stage, 3);

        /// <summary> 校准射击是否解锁 (花后) </summary>
        public static bool CalibrationUnlocked => GetMainParamBool((int)Stage, 4);

        /// <summary> 暴击溢出转换是否解锁 (月后) </summary>
        public static bool CriticalOverflowUnlocked => GetMainParamBool((int)Stage, 5);

        /// <summary> 堆芯熔毁是否解锁 (亵渎天神后) </summary>
        public static bool CoreRuptureUnlocked => GetMainParamBool((int)Stage, 6);

        /// <summary> 玛瑙序列是否解锁 (终章) </summary>
        public static bool OnyxSequenceUnlocked => GetMainParamBool((int)Stage, 7);

        /// <summary> 左键弹幕基础飞行速度 (已含缩放) </summary>
        public static float LeftProjectileSpeed => GetMainParamFloat((int)Stage, 8) * FlightSpeedScale;

        /// <summary> 右键弹幕根据蓄力程度计算的飞行速度 (已含缩放) </summary>
        public static float RightProjectileSpeed(float charge) =>
            MathHelper.Lerp(MinRightProjectileSpeedUnscaled, MaxRightProjectileSpeedUnscaled, charge) * FlightSpeedScale;

        /// <summary> 右键弹幕根据蓄力程度计算的伤害倍率 </summary>
        public static float RightDamageMultiplier(float charge) =>
            MathHelper.Lerp(MinRightDamageMultiplier, MaxRightDamageMultiplier, charge);

        #endregion

        #region 真实伤害与假人判定 (True Damage & Dummy Checking)

        /// <summary>
        /// 判定目标 NPC 是否为假人 (原版靶子 / 灾厄超级假人)。
        /// 假人不参与按最大生命值百分比计算的真实伤害。
        /// </summary>
        public static bool IsTrainingDummy(NPC target) =>
            target.type == NPCID.TargetDummy ||
            target.type == ModContent.NPCType<SuperDummyNPC>();

        /// <summary>
        /// 尝试计算对目标 NPC 造成的最大生命值百分比真实伤害 (受目标 DR 加成)。
        /// </summary>
        public static bool TryGetMaxLifeTrueDamage(NPC target, out int trueDamage)
        {
            trueDamage = 0;
            if (!target.active || target.lifeMax <= 5 || IsTrainingDummy(target))
                return false;

            bool isBoss = target.boss || target.realLife >= 0;
            float ratio = isBoss ? BossMaxLifeTrueDamageRatio : NormalMaxLifeTrueDamageRatio;
            float damage = target.lifeMax * ratio;

            // DR (Damage Reduction) 加成
            float dr = target.Calamity().DR;
            if (dr > 0f)
                damage *= 1f + dr;

            trueDamage = Math.Max(1, (int)damage);
            return true;
        }

        #endregion

        #region 表格读取辅助函数 (Table Helper Methods)

        private static int GetMainDamage(int stageIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, MainDamageTable.GetLength(0) - 1);
            int fallback = (int)MainDamageTable[stage, 1];
            return Math.Max(1, RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(MainDamageTable), stage, 1, fallback));
        }

        private static bool GetMainParamBool(int stageIndex, int columnIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, MainParamsTable.GetLength(0) - 1);
            bool fallback = (bool)MainParamsTable[stage, columnIndex];
            return RuntimeBalanceData.GetSourceTableBool(SourceFile, nameof(MainParamsTable), stage, columnIndex, fallback);
        }

        private static float GetMainParamFloat(int stageIndex, int columnIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, MainParamsTable.GetLength(0) - 1);
            float fallback = (float)MainParamsTable[stage, columnIndex];
            return RuntimeBalanceData.GetSourceTableFloat(SourceFile, nameof(MainParamsTable), stage, columnIndex, fallback);
        }

        #endregion
    }
}

