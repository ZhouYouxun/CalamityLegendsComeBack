using CalamityLegendsComeBack.Accssory.MC.General;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    internal static class MalachiteBalance
    {
        // 【核心数值】每行对应一个游戏时期；从左到右：共用基础伤害、左键潜伏刀扇数量、右键羽毛储存上限、每根右键羽毛的生成帧数。
        // 右键羽毛上限为 0 时，右键尚未解锁。左键、刀扇与右键都读取第一列作为基础伤害。
        // 手持或佩戴本武器时使用第四列；仅佩戴未手持时充能速度为手持的40%。
        // 仅由孔雀匣生成时仍为60帧，孔雀卷轴会使生成时间减半。
        private static readonly int[,] DefaultCoreStatsTable =
        {
            // 伤害  潜伏刀扇  右键羽毛  生成帧数
            {    12,       15,        0,       36 }, // 初始
            {    14,       15,        5,       36 }, // 击败史莱姆王
            {    18,       15,        6,       36 }, // 击败克苏鲁之眼
            {    28,       15,        7,       36 }, // 击败邪恶生物群系 Boss
            {    38,       15,        8,       36 }, // 击败骷髅王
            {    44,       15,        9,       36 }, // 击败蜂后
            {    48,       17,       10,       36 }, // 进入困难模式
            {    85,       17,       11,       36 }, // 击败任意机械 Boss
            {   130,       19,       12,       36 }, // 击败世纪之花
            {   155,       19,       13,       36 }, // 击败石巨人
            {   230,       21,       15,       36 }, // 击败瘟疫使者歌莉娅
            {   480,       23,       17,       36 }, // 击败月亮领主
            {   490,       23,       18,       36 }, // 击败亵渎天神
            {   520,       23,       19,       36 }, // 击败幽花
            {   620,       23,       20,       36 }, // 击败老公爵
            {  1280,       23,       21,       36 }, // 击败神明吞噬者
            {  1380,       23,       22,       36 }, // 击败犽戎
            {  2000,       23,       23,       36 }  // 击败星流巨械与至尊灾厄
        };

        // 【弹幕速度】从左到右：左键苦无速度、右键羽毛速度。
        private static readonly float[,] DefaultProjectileVelocities =
        {
            // 左键  右键
            {  18f,  24f }, // 肉山前
            {  22f,  30f }, // 肉山后，世纪之花前
            {  26f,  36f }, // 世纪之花后，月亮领主前
            {  30f,  42f }  // 月亮领主后
        };

        private const string SourceFile = "Weapons/Malachite/MalachiteBalance.cs";
        private const int DamageColumn = 0;

        public const int DepletionBurstKunaiCount = 7;
        public const int PeacockBoxFeatherGenerationFrames = 60;
        public const int RightFeatherReleaseSpacingFrames = 2;
        public const int LeftClickScatterInterval = 9;
        public const int LeftClickScatterWaveCount = 2;
        public const int RightClickBoostFrames = 3 * 60;
        public const int LeftClickHasteFrames = 60;
        public const int FinaleStealthAttackAdvanceFrames = 5 * 60;

        public static int StageIndex => GetCompletedStageIndex();
        public static bool DownedWallOfFlesh => StageIndex >= (int)MalachiteProgressionStage.WallOfFlesh;
        public static bool DownedPlaguebringerGoliath => StageIndex >= (int)MalachiteProgressionStage.PlaguebringerGoliath;
        public static bool DownedMoonLord => StageIndex >= (int)MalachiteProgressionStage.MoonLord;
        public static bool DownedOldDuke => StageIndex >= (int)MalachiteProgressionStage.OldDuke;

        public static bool NormalKunaiIgnoresGravity(Player player) =>
            StageSwitch(MalachiteStageSwitch.LeftNoGravity) ||
            player.GetModPlayer<MCGeneralPlayer>().NormalKunaiIgnoresGravity;

        public static bool RightClickUnlocked => RightFeatherMaxCount > 0;
        public static bool RightClickMoveBoostUnlocked => StageSwitch(MalachiteStageSwitch.RightMoveBoost);
        public static bool RightClickLeftHasteUnlocked => StageSwitch(MalachiteStageSwitch.RightLeftHaste);
        public static bool RightClickConsumesNoStealth => StageSwitch(MalachiteStageSwitch.RightNoStealthCost);
        public static bool StealthAceKunaiUnlocked => StageSwitch(MalachiteStageSwitch.StealthAceKunai);

        public static float LeftClickUseSpeedMultiplier => DownedWallOfFlesh ? 1f : 1.35f;

        public static int NormalLeftClickKunaiCount =>
            StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade) ? 3 :
            StageSwitch(MalachiteStageSwitch.LeftMultiThrow) ? 2 :
            1;

        public static int StoredPeacockKunaiPerLeftClick => NormalLeftClickKunaiCount;

        public static bool LeftClickScatterUnlocked => StageSwitch(MalachiteStageSwitch.LeftScatterBurst);

        public static int LeftClickScatterKunaiPerWave =>
            StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade) ? 7 : 5;

        public static int StealthKunaiCount => Utils.Clamp(GetCoreStat(1), 1, 100);
        public static int RightFeatherMaxCount => Utils.Clamp(GetCoreStat(2), 0, 100);
        public static int RightFeatherGenerationFrames => Utils.Clamp(GetCoreStat(3), 1, 3600);

        public static int GetCompletedStageIndex()
        {
            MalachiteProgressionStage stage = MalachiteProgressionStage.Initial;

            if (NPC.downedSlimeKing)
                stage = MalachiteProgressionStage.KingSlime;
            if (NPC.downedBoss1)
                stage = MalachiteProgressionStage.EyeOfCthulhu;
            if (NPC.downedBoss2)
                stage = MalachiteProgressionStage.EvilBoss;
            if (NPC.downedBoss3)
                stage = MalachiteProgressionStage.Skeletron;
            if (NPC.downedQueenBee)
                stage = MalachiteProgressionStage.QueenBee;
            if (Main.hardMode)
                stage = MalachiteProgressionStage.WallOfFlesh;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                stage = MalachiteProgressionStage.AnyMechanicalBoss;
            if (NPC.downedPlantBoss)
                stage = MalachiteProgressionStage.Plantera;
            if (NPC.downedGolemBoss)
                stage = MalachiteProgressionStage.Golem;
            if (DownedBossSystem.downedPlaguebringer)
                stage = MalachiteProgressionStage.PlaguebringerGoliath;
            if (NPC.downedMoonlord)
                stage = MalachiteProgressionStage.MoonLord;
            if (DownedBossSystem.downedProvidence)
                stage = MalachiteProgressionStage.Providence;
            if (DownedBossSystem.downedPolterghast)
                stage = MalachiteProgressionStage.Polterghast;
            if (DownedBossSystem.downedBoomerDuke)
                stage = MalachiteProgressionStage.OldDuke;
            if (DownedBossSystem.downedDoG)
                stage = MalachiteProgressionStage.DevourerOfGods;
            if (DownedBossSystem.downedYharon)
                stage = MalachiteProgressionStage.Yharon;
            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas)
                stage = MalachiteProgressionStage.ExoMechsAndCalamitas;

            return (int)stage;
        }

        // 大招（终章）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            2.50f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            3.20f, // Tier 1: 月后（月亮领主之后）
            4.00f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        public static float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        public static int GetBaseDamage() => System.Math.Max(1, GetCoreStat(DamageColumn));
        public static int GetLeftClickBaseDamage() => GetBaseDamage();
        public static int GetRightClickBaseDamage() => GetBaseDamage();
        public static int GetInitialLeftClickBaseDamage() => System.Math.Max(1, GetCoreStat(DamageColumn, 0));

        public static int GetLeftClickTooltipTier()
        {
            if (StageSwitch(MalachiteStageSwitch.LeftOldDukeUpgrade))
                return 4;
            if (StageSwitch(MalachiteStageSwitch.LeftScatterBurst))
                return 3;
            if (StageSwitch(MalachiteStageSwitch.LeftMultiThrow))
                return 2;
            if (StageSwitch(MalachiteStageSwitch.LeftNoGravity))
                return 1;
            return 0;
        }

        public static int GetRightClickTooltipTier()
        {
            if (StageSwitch(MalachiteStageSwitch.RightNoStealthCost))
                return 4;
            if (StageSwitch(MalachiteStageSwitch.RightLeftHaste))
                return 3;
            if (StageSwitch(MalachiteStageSwitch.RightMoveBoost))
                return 2;
            if (RightClickUnlocked)
                return 1;
            return 0;
        }

        public static int GetStealthTooltipTier()
        {
            if (StageIndex >= (int)MalachiteProgressionStage.MoonLord)
                return 4;
            if (StageIndex >= (int)MalachiteProgressionStage.PlaguebringerGoliath)
                return 3;
            if (StageIndex >= (int)MalachiteProgressionStage.Plantera)
                return 2;
            if (StageIndex >= (int)MalachiteProgressionStage.WallOfFlesh)
                return 1;
            return 0;
        }

        public static int GetVelocityStageIndex()
        {
            if (!DownedWallOfFlesh)
                return 0;
            if (StageIndex < (int)MalachiteProgressionStage.Plantera)
                return 1;
            if (!DownedMoonLord)
                return 2;
            return 3;
        }

        public static float GetLeftClickVelocity()
        {
            int stage = GetVelocityStageIndex();
            float fallback = DefaultProjectileVelocities[stage, 0];
            return RuntimeBalanceData.GetSourceFloatTableColumn(SourceFile, nameof(DefaultProjectileVelocities), stage, 0, fallback);
        }

        public static float GetRightClickVelocity()
        {
            int stage = GetVelocityStageIndex();
            float fallback = DefaultProjectileVelocities[stage, 1];
            return RuntimeBalanceData.GetSourceFloatTableColumn(SourceFile, nameof(DefaultProjectileVelocities), stage, 1, fallback);
        }

        private static int GetCoreStat(int column, int stage = -1)
        {
            int stageIndex = Utils.Clamp(stage < 0 ? StageIndex : stage, 0, DefaultCoreStatsTable.GetLength(0) - 1);
            int fallback = DefaultCoreStatsTable[stageIndex, column];
            return RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(DefaultCoreStatsTable), stageIndex, column, fallback);
        }

        // 以下为功能解锁开关，日常调整伤害、刀扇数和羽毛上限时无需修改。
        private static readonly object[,] DefaultStageSwitches =
        {
            //                                      NoGrav  Multi  Scatter OldDuke Move   Haste  FreeSS Ace
            { "Initial",                            false,  false,  false,  false, false, false, false, false },
            { "King Slime",                         false,  false,  false,  false,  false, false, false, false },
            { "Eye of Cthulhu",                     false,  false,  false,  false,  false, false, false, false },
            { "Evil Boss",                          false,  false,  false,  false,  false, false, false, false },
            { "Skeletron",                          false,  false,  false,  false,  false, false, false, false },
            { "Queen Bee",                          true,   false,  false,  false,  true,  false, false, false },
            { "Hardmode",                           true,   true,   false,  false,  true,  false, false, true },
            { "Any Mechanical Boss",                true,   true,   false,  false,  true,  false, false, true },
            { "Plantera",                           true,   true,   false,  false,  true,  false, false, true },
            { "Golem",                              true,   true,   false,  false,  true,  false, false, true },
            { "Plaguebringer Goliath",              true,   true,   true,   false,  true,  true,  false, true },
            { "Moon Lord",                          true,   true,   true,   false,  true,  true,  false, true },
            { "Providence",                         true,   true,   true,   false,  true,  true,  false, true },
            { "Polterghast",                        true,   true,   true,   false,  true,  true,  false, true },
            { "Old Duke",                           true,   true,   true,   true,  true,  true,  true,  true },
            { "Devourer of Gods",                   true,   true,   true,   true,  true,  true,  true,  true },
            { "Yharon",                             true,   true,   true,   true,  true,  true,  true,  true },
            { "Exo Mechs and Supreme Calamitas",    true,   true,   true,   true,  true,  true,  true,  true }
        };

        private static bool StageSwitch(MalachiteStageSwitch column)
        {
            int stageIndex = Utils.Clamp(StageIndex, 0, DefaultStageSwitches.GetLength(0) - 1);
            bool fallback = (bool)DefaultStageSwitches[stageIndex, (int)column];
            return RuntimeBalanceData.GetSourceTableBool(SourceFile, nameof(DefaultStageSwitches), stageIndex, (int)column, fallback);
        }
    }

    internal enum MalachiteProgressionStage
    {
        Initial                  = 0,
        KingSlime                = 1,
        EyeOfCthulhu             = 2,
        EvilBoss                 = 3,
        Skeletron                = 4,
        QueenBee                 = 5,
        WallOfFlesh              = 6,
        AnyMechanicalBoss        = 7,
        Plantera                 = 8,
        Golem                    = 9,
        PlaguebringerGoliath     = 10,
        MoonLord                 = 11,
        Providence               = 12,
        Polterghast              = 13,
        OldDuke                  = 14,
        DevourerOfGods           = 15,
        Yharon                   = 16,
        ExoMechsAndCalamitas     = 17
    }

    internal enum MalachiteStageSwitch
    {
        LeftNoGravity            = 1,
        LeftMultiThrow           = 2,
        LeftScatterBurst         = 3,
        LeftOldDukeUpgrade       = 4,
        RightMoveBoost           = 5,
        RightLeftHaste           = 6,
        RightNoStealthCost       = 7,
        StealthAceKunai          = 8
    }
}
