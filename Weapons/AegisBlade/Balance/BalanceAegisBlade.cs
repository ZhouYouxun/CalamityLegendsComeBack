using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.AegisBlade
{
    public class BalanceAegisBlade
    {
        private const string SourceFile = "Weapons/AegisBlade/Balance/BalanceAegisBlade.cs";

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

        // ── 左键挥舞伤害 ──────────────────────────────────────────────────
        private static readonly int[] DefaultLeftClickBaseDamage =
        {
             70, 100, 120, 150, 175, 200, 230, 260, 290, 320, 360, 400, 430, 450,
        };

        // ── 盾牌幻影伤害 ──────────────────────────────────────────────────
        private static readonly int[] DefaultShieldPhantomDamage =
        {
             28,  40,  48,  60,  70,  80,  92, 104, 116, 128, 144, 160, 172, 180,
        };

        // ── 刀刃下插伤害（蓄力释放） ──────────────────────────────────────
        private static readonly int[] DefaultBladePlungeDamage =
        {
            140, 200, 240, 300, 350, 400, 460, 520, 580, 640, 720, 800, 860, 900,
        };

        // ── 举盾 ──────────────────────────────────────────────────────────
        public const int ShieldRaiseFrames      = 15;
        public const int ShieldMaxDefenseBonus  = 20;   // 举盾期间增加20点防御

        // ── 完美格挡 ──────────────────────────────────────────────────────
        public const int   ParryIFrames                = 35;
        public const int   PerfectParryDefenseDuration = 8 * 60;  // 8秒最高防御+无五毒
        public const float PerfectParryEnergyGain      = 8f;

        // ── 埃癸斯被动 ────────────────────────────────────────────────────
        public const int   AegisMinDefense         = 2;
        public const float AegisSpeedPerDefenseLoss = 4f;    // 每4mph速度 -1防御

        private static readonly int[] DefaultAegisMaxDefense =
        {
             6,  6,  7,  7,  8,  8,  9,  9, 10, 11, 12, 13, 14, 15,
        };

        // ── 壁垒被动 ──────────────────────────────────────────────────────
        public const float BulwarkContactReduction       = 0.20f;  // 接触伤害-20%
        public const float BulwarkStationaryMultiplier   = 2f;     // 静止时翻倍
        public const float BulwarkDefenseDamageReduction = 0.50f;  // 防御损伤-50%

        // ── 坚毅被动 ──────────────────────────────────────────────────────
        public const int TenacityImmunityDuration = 3 * 60;  // 3秒免死
        public const int TenacityCooldownDuration = 60 * 60; // 60秒冷却

        // ── 蓄力举盾 ──────────────────────────────────────────────────────
        public const int   ChargeHoldDelay      = 45;   // Phase1中持续多少帧后开始蓄力
        public const int   ChargeDuration       = 60;   // 蓄力完成需要的帧数
        public const float ChargePhantomKnockback = 4f; // 蓄力幻影额外击退倍率

        // ── 土墙 ──────────────────────────────────────────────────────────
        public const int   WallHeightTiles  = 16;        // 墙高（格）
        public const int   WallWidthTiles   = 2;         // 墙宽（格）
        public const int   WallDuration     = 60 * 60;   // 普通时60秒
        public const int   WallDurationBoss = 10 * 60;   // BOSS战10秒
        public const int   WallRiseTime     = 24;        // 土墙升起帧数
        public const int   WallSpreadPixels = 100;       // 墙距下插点的水平距离

        // ── 能量系统 ──────────────────────────────────────────────────────
        public const float EnergyMax                      = 100f;
        public const float EnergyRegenPerSecond           = 1f;
        public const float EnergyRegenMultiplierStationary = 2f;  // 静止时x2
        public const float EnergyOnBeingHitOrParry        = 8f;   // 挨打/完美格挡+8
        public const int   UltimateDuration               = 10 * 60;   // 10秒无敌
        public const float UltimateSpeedReduction         = 0.70f;     // 移动速度-70%

        // ── 进度门槛 ──────────────────────────────────────────────────────

        /// <summary>打败任意机械BOSS后解锁右键蓄力</summary>
        public static bool ChargeUnlocked()
            => Main.hardMode && (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3);

        /// <summary>打败世纪之花后解锁4火球</summary>
        public static bool FourFireballsUnlocked()
            => NPC.downedPlantBoss;

        // ── 公共接口 ──────────────────────────────────────────────────────

        public int GetLeftClickBaseDamage()
            => GetValueForStage(GetLeftClickValues(), GetStageIndex());

        public static int GetInitialLeftClickDamage()
            => DefaultLeftClickBaseDamage[0];

        public int GetShieldPhantomDamage()
            => GetValueForStage(GetShieldPhantomValues(), GetStageIndex());

        public int GetBladePlungeDamage()
            => GetValueForStage(GetBladePlungeValues(), GetStageIndex());

        public int GetAegisMaxDefense()
            => GetValueForStage(GetAegisMaxDefenseValues(), GetStageIndex());

        public int GetStageIndex()
        {
            bool[] cleared =
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
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas,
            };

            int index = 0;
            for (int i = 0; i < cleared.Length; i++)
            {
                if (cleared[i]) index = i + 1;
            }
            return index;
        }

        private static int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0) return 1;
            int i = System.Math.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[i]);
        }

        private static int[] GetLeftClickValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickBaseDamage), DefaultLeftClickBaseDamage);

        private static int[] GetShieldPhantomValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultShieldPhantomDamage), DefaultShieldPhantomDamage);

        private static int[] GetBladePlungeValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultBladePlungeDamage), DefaultBladePlungeDamage);

        private static int[] GetAegisMaxDefenseValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultAegisMaxDefense), DefaultAegisMaxDefense);
    }
}
