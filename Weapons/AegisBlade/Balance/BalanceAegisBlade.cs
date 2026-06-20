using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.AegisBlade
{
    public class BalanceAegisBlade
    {
        private const string SourceFile = "Weapons/AegisBlade/Balance/BalanceAegisBlade.cs";

        // ── 进度阶段名（与下方数组一一对应） ──────────────────────────────
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

        // ── 左键挥舞伤害（每进度阶段） ────────────────────────────────────
        private static readonly int[] DefaultLeftClickBaseDamage =
        {
             70,  // Initial
            100,  // Eye of Cthulhu
            120,  // Evil Boss
            150,  // Skeletron
            175,  // Hardmode
            200,  // Any Mechanical Boss
            230,  // Plantera
            260,  // Golem
            290,  // Moon Lord
            320,  // Providence
            360,  // Polterghast
            400,  // Devourer of Gods
            430,  // Yharon
            450,  // Exo Mechs and Supreme Calamitas
        };

        // ── 右键盾牌幻影伤害（每进度阶段） ───────────────────────────────
        private static readonly int[] DefaultShieldPhantomDamage =
        {
             28,  // Initial
             40,  // Eye of Cthulhu
             48,  // Evil Boss
             60,  // Skeletron
             70,  // Hardmode
             80,  // Any Mechanical Boss
             92,  // Plantera
            104,  // Golem
            116,  // Moon Lord
            128,  // Providence
            144,  // Polterghast
            160,  // Devourer of Gods
            172,  // Yharon
            180,  // Exo Mechs and Supreme Calamitas
        };

        // ── 右键举盾提供的防御力 ──────────────────────────────────────────
        public const int ShieldRaiseFrames = 15;
        public const int ShieldMaxDefenseBonus = 15;

        // ── 完美格挡（在举盾的15帧内受到伤害） ───────────────────────────
        public const int ParryIFrames = 35;
        public const int PerfectParryDefenseDuration = 8 * 60;   // 8 秒最高防御
        public const float PerfectParryEnergyGain = 8f;

        // ── 埃癸斯被动（根据速度提供防御） ──────────────────────────────
        // 每 4 像素/帧速度减少 1 防御；上限随时期增加；下限 2 防御。
        public const int AegisMinDefense = 2;
        public const float AegisSpeedPerDefenseLoss = 4f;

        private static readonly int[] DefaultAegisMaxDefense =
        {
             6,  // Initial
             6,  // Eye of Cthulhu
             7,  // Evil Boss
             7,  // Skeletron
             8,  // Hardmode
             8,  // Any Mechanical Boss
             9,  // Plantera
             9,  // Golem
            10,  // Moon Lord
            11,  // Providence
            12,  // Polterghast
            13,  // Devourer of Gods
            14,  // Yharon
            15,  // Exo Mechs and Supreme Calamitas
        };

        // ── 壁垒被动（无伤害性减益时的减伤） ─────────────────────────────
        public const float BulwarkContactReduction = 0.20f;
        public const float BulwarkStationaryMultiplier = 2f;

        // ── 后方防护被动（不使用右键时） ─────────────────────────────────
        public const float RearDamageReduction = 0.30f;

        // ── 投掷生成的土墙 ────────────────────────────────────────────────
        public const int WallHeightTiles = 16;           // 16 格高
        public const int WallWidthTiles = 2;             // 2 格宽
        public const int WallDuration = 60 * 60;         // 60 秒
        public const int WallDurationBoss = 10 * 60;     // BOSS战 10 秒

        // ── 终结技能量系统 ────────────────────────────────────────────────
        public const float EnergyMax = 100f;
        public const float EnergyRegenPerSecond = 1f;
        public const float EnergyRegenMultiplierStationary = 2f;
        public const float EnergyOnHitOrParry = 8f;
        public const int UltimateDuration = 10 * 60;     // 10 秒无敌
        public const float UltimateSpeedReduction = 0.20f;

        // ── 公共接口 ──────────────────────────────────────────────────────

        public int GetLeftClickBaseDamage()
            => GetValueForStage(GetLeftClickValues(), GetStageIndex());

        public static int GetInitialLeftClickDamage()
            => DefaultLeftClickBaseDamage[0];

        public int GetShieldPhantomDamage()
            => GetValueForStage(GetShieldPhantomValues(), GetStageIndex());

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
                if (cleared[i])
                    index = i + 1;
            }
            return index;
        }

        private static int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;
            int i = System.Math.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[i]);
        }

        private static int[] GetLeftClickValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultLeftClickBaseDamage), DefaultLeftClickBaseDamage);

        private static int[] GetShieldPhantomValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultShieldPhantomDamage), DefaultShieldPhantomDamage);

        private static int[] GetAegisMaxDefenseValues()
            => RuntimeBalanceData.GetSourceIntArray(SourceFile, nameof(DefaultAegisMaxDefense), DefaultAegisMaxDefense);
    }
}
