using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class BalanceBlossomFlux
    {
        // Edit damage here. Columns are:
        // Stage, Breakthrough, Recovery, Recon, Bombard, Plague.
        //
        // Stage order intentionally puts Queen Bee before Skeletron:
        // Start -> Eye -> Evil -> Queen Bee -> Skeletron -> Hardmode ...
        internal static readonly object[,] MainDamageTable =
        {
            // Stage                                      Break  Recov  Recon  Bomb  Plague
            { "Initial",                                    14,     1,     1,    1,      1 },
            { "Eye of Cthulhu",                             14,     5,     1,    1,      1 },
            { "Evil Boss",                                  17,     8,     1,    1,      1 },
            { "Queen Bee",                                  21,    12,     5,    1,      1 },
            { "Skeletron",                                  23,    17,     8,    1,      1 },
            { "Hardmode",                                   39,    40,    12,   12,      1 },
            { "Any Mechanical Boss",                        48,    42,    16,   16,      1 },//�ĳ�2����
            { "Plantera",                                   36,    24,    14,   15,     30 },
            { "Golem",                                      42,    30,    15,   20,     51 },
            { "Plaguebringer Goliath",                      52,    44,    23,   30,     67 },
            { "Moon Lord",                                  66,    84,    52,   32,     76 },
            { "Providence",                                 92,   107,    68,   51,     98 },
            { "Polterghast",                               136,   154,   105,   93,    103 },
            { "Devourer of Gods",                          172,   176,   124,   95,    136 },
            { "Yharon",                                    277,   259,   193,  105,    179 },
            { "Exo Mechs and Supreme Calamitas",           777,   777,   777,  400,    777 }
        };

        // Edit primary non-damage knobs here. These values use the six growth
        // nodes below and are intentionally independent from MainDamageTable.
        // Columns are:
        // Stage,
        // Breakthrough left fire delay,
        // Breakthrough right max loaded arrows,
        // Breakthrough right frames per loaded arrow,
        // Recovery right flash count,
        // Recovery right healing per flash,
        // Recon left shots per trigger,
        // Recon left burst pause,
        // Recon right penetrate,
        // Recon right mark duration in seconds,
        // Bombard left min arrows per trigger,
        // Bombard left max arrows per trigger,
        // Bombard left fire delay,
        // Bombard right wave count,
        // Bombard right impact explosions per falling projectile.
        internal static readonly object[,] MainParamsTable =
        {
            // Stage                       BrkDelay BrkMax BrkFrame RecFlash RecHeal RecShots RecPause RecPen RecMark BombMin BombMax BombDelay BombWaves BombImpact
            { "Initial",                       15,     3,      30,       3,      5,       1,      90,     2,     15,      3,      3,       10,        8,          1 },
            { "Eye of Cthulhu",                10,     3,      30,       3,     15,       1,      85,     2,     15,      3,      3,       10,        8,          1 },
            { "Hardmode",                       6,     5,      20,       5,     15,       3,      60,     2,     25,      3,      3,       10,        8,          1 },
            { "Plantera",                       3,     7,      15,       7,     15,       3,      50,     2,     25,      4,      4,       10,        8,          2 },
            { "Moon Lord",                      2,     7,      15,       9,     15,       3,      36,     2,     25,      5,      5,        8,        8,          2 },
            { "Devourer of Gods",               2,     7,      15,       9,     20,       3,      24,     2,     25,      5,      5,        7,        8,          2 },
        };

        private const string SourceFile = "Weapons/BlossomFlux/BalanceBlossomFlux.cs";

        // 大招（EX 春讯弹幕雨）伤害倍率：肉后/月后/神后三档
        // 大招伤害 = 当前左键基础伤害 × 本档倍率
        private static readonly float[] UltimateDamageMultipliers =
        {
            1.75f, // Tier 0: 肉后（大招解锁起点，机械 Boss 之后）
            2.25f, // Tier 1: 月后（月亮领主之后）
            2.75f  // Tier 2: 神后（亵渎天神 Providence 之后）
        };

        internal float GetUltimateDamageMultiplier() =>
            UltimateDamageTier.Resolve(SourceFile, nameof(UltimateDamageMultipliers), UltimateDamageMultipliers);

        internal int GetLeftClickBaseDamage(BlossomFluxChloroplastPresetType preset) => BFBalanceTable.Get(GetLeftDamageStat(preset));
        internal int GetRightClickBaseDamage(BlossomFluxChloroplastPresetType preset) => BFBalanceTable.Get(GetRightDamageStat(preset));

        private static BFStat GetLeftDamageStat(BlossomFluxChloroplastPresetType preset)
        {
            return preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_BRecov => BFStat.Recovery_Left_Damage,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => BFStat.Recon_Left_Damage,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BFStat.Bombard_Left_Damage,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => BFStat.Plague_Left_Damage,
                _ => BFStat.Breakthrough_Left_Damage
            };
        }

        private static BFStat GetRightDamageStat(BlossomFluxChloroplastPresetType preset)
        {
            return preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_BRecov => BFStat.Recovery_Left_Damage,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => BFStat.Recon_Right_Damage,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BFStat.Bombard_Right_Damage,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => BFStat.Plague_Right_Damage,
                _ => BFStat.Breakthrough_Right_Damage
            };
        }

        internal static int GetMainDamageFallback(int stageIndex, int columnIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, MainDamageTable.GetLength(0) - 1);
            int column = Utils.Clamp(columnIndex, 1, MainDamageTable.GetLength(1) - 1);
            return (int)MainDamageTable[stage, column];
        }

        internal static int GetMainParamsFallback(int stageIndex, int columnIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, MainParamsTable.GetLength(0) - 1);
            int column = Utils.Clamp(columnIndex, 1, MainParamsTable.GetLength(1) - 1);
            return (int)MainParamsTable[stage, column];
        }

        internal static bool Plague_BetsysCurse => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord);
        internal static bool Plague_AstralInfection => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord);
        internal static bool Plague_Wither => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord);
        internal static bool Plague_WhisperingDeath => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods);
        internal static bool Plague_AbsorberAffliction => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods);
    }

    internal readonly struct BFBombardLeftStats
    {
        public readonly int MinArrowCount;
        public readonly int MaxArrowCount;
        public readonly int FireInterval;
        public readonly int ExplosionsPerArrow;
        public readonly float ExplosionRadiusMultiplier;
        public readonly float ProjectileSpeedMultiplier;

        public BFBombardLeftStats(int minArrowCount, int maxArrowCount, int fireInterval, int explosionsPerArrow, float explosionRadiusMultiplier, float projectileSpeedMultiplier)
        {
            MinArrowCount = minArrowCount;
            MaxArrowCount = maxArrowCount;
            FireInterval = fireInterval;
            ExplosionsPerArrow = explosionsPerArrow;
            ExplosionRadiusMultiplier = explosionRadiusMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
        }
    }

    internal static class BFBombardLeftBalance
    {
        public static BFBombardLeftStats GetStats()
        {
            int minCount = MathMax(1, BFBalanceTable.Get(BFStat.Bombard_Left_MinArrows));
            int maxCount = MathMax(minCount, BFBalanceTable.Get(BFStat.Bombard_Left_MaxArrows));
            int interval = MathMax(1, BFBalanceTable.Get(BFStat.Bombard_Left_Interval));
            int explosionLimit = 1;
            float radius = 1f;
            float speed = 1f;

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord))
                explosionLimit = 2;

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods))
            {
                radius = 1.25f;
                speed = 1.18f;
            }

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods))
                explosionLimit = 3;

            return new BFBombardLeftStats(minCount, maxCount, interval, explosionLimit, radius, speed);
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
    }

    internal readonly struct BFBombardRightStats
    {
        public readonly int ChargeFrames;
        public readonly float ExplosionSize;
        public readonly float SkyRainMultiplier;
        public readonly int WaveCount;
        public readonly int RainImpactExplosionCount;

        public BFBombardRightStats(int chargeFrames, float explosionSize, float skyRainMultiplier, int waveCount, int rainImpactExplosionCount)
        {
            ChargeFrames = chargeFrames;
            ExplosionSize = explosionSize;
            SkyRainMultiplier = skyRainMultiplier;
            WaveCount = waveCount;
            RainImpactExplosionCount = rainImpactExplosionCount;
        }
    }

    internal static class BFBombardRightBalance
    {
        public static BFBombardRightStats GetStats()
        {
            return new BFBombardRightStats(
                chargeFrames: 120,
                explosionSize: 190f,
                skyRainMultiplier: 1f,
                waveCount: MathMax(1, BFBalanceTable.Get(BFStat.Bombard_Right_WaveCount)),
                rainImpactExplosionCount: MathMax(1, BFBalanceTable.Get(BFStat.Bombard_Right_RainImpactExplosionCount)));
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
    }

    internal readonly struct BFBreakthroughLeftStats
    {
        public readonly int UseTime;
        public readonly int UseInterval;
        public readonly float ShotsPerSecond;
        public readonly float ProjectileSpeedMultiplier;

        public BFBreakthroughLeftStats(int useTime, int useInterval, float shotsPerSecond, float projectileSpeedMultiplier)
        {
            UseTime = useTime;
            UseInterval = useInterval;
            ShotsPerSecond = shotsPerSecond;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
        }
    }

    internal static class BFBreakthroughLeftBalance
    {
        public static BFBreakthroughLeftStats GetStats()
        {
            int useInterval = MathMax(1, BFBalanceTable.Get(BFStat.Breakthrough_Left_UseInterval));
            return new BFBreakthroughLeftStats(
                useTime: useInterval,
                useInterval: useInterval,
                shotsPerSecond: 60f / useInterval,
                projectileSpeedMultiplier: 1f);
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
    }

    internal readonly struct BFBreakthroughRightStats
    {
        public readonly int FramesPerArrow;
        public readonly int MaxLoadedArrows;
        public readonly int Penetrate;
        public readonly bool IgnorePenetrationDamageFalloff;
        public readonly float ProjectileSpeedMultiplier;
        public readonly float DamagePerChargeStack;

        public BFBreakthroughRightStats(
            int framesPerArrow,
            int maxLoadedArrows,
            int penetrate,
            bool ignorePenetrationDamageFalloff,
            float projectileSpeedMultiplier,
            float damagePerChargeStack)
        {
            FramesPerArrow = framesPerArrow;
            MaxLoadedArrows = maxLoadedArrows;
            Penetrate = penetrate;
            IgnorePenetrationDamageFalloff = ignorePenetrationDamageFalloff;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            DamagePerChargeStack = damagePerChargeStack;
        }
    }

    internal static class BFBreakthroughRightBalance
    {
        public static BFBreakthroughRightStats GetStats()
        {
            return new BFBreakthroughRightStats(
                framesPerArrow: MathMax(1, BFBalanceTable.Get(BFStat.Breakthrough_Right_FramesPerArrow)),
                maxLoadedArrows: MathMax(1, BFBalanceTable.Get(BFStat.Breakthrough_Right_MaxArrows)),
                penetrate: 9,
                ignorePenetrationDamageFalloff: false,
                projectileSpeedMultiplier: 1f,
                damagePerChargeStack: 0f);
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
    }

    internal readonly struct BFPlagueLeftStats
    {
        public readonly int InitialDuration;
        public readonly int StackDuration;
        public readonly int MaxDuration;
        public readonly bool InflictBetsysCurse;
        public readonly bool InflictAstralInfection;
        public readonly bool InflictWither;
        public readonly bool InflictWhisperingDeath;
        public readonly bool InflictAbsorberAffliction;

        public BFPlagueLeftStats(
            int initialDuration,
            int stackDuration,
            int maxDuration,
            bool inflictBetsysCurse,
            bool inflictAstralInfection,
            bool inflictWither,
            bool inflictWhisperingDeath,
            bool inflictAbsorberAffliction)
        {
            InitialDuration = initialDuration;
            StackDuration = stackDuration;
            MaxDuration = maxDuration;
            InflictBetsysCurse = inflictBetsysCurse;
            InflictAstralInfection = inflictAstralInfection;
            InflictWither = inflictWither;
            InflictWhisperingDeath = inflictWhisperingDeath;
            InflictAbsorberAffliction = inflictAbsorberAffliction;
        }
    }

    internal static class BFPlagueLeftBalance
    {
        public static BFPlagueLeftStats GetStats()
        {
            return new BFPlagueLeftStats(
                initialDuration: 10 * 60,
                stackDuration: 10 * 60,
                maxDuration: 30 * 60,
                inflictBetsysCurse: BalanceBlossomFlux.Plague_BetsysCurse,
                inflictAstralInfection: BalanceBlossomFlux.Plague_AstralInfection,
                inflictWither: BalanceBlossomFlux.Plague_Wither,
                inflictWhisperingDeath: BalanceBlossomFlux.Plague_WhisperingDeath,
                inflictAbsorberAffliction: BalanceBlossomFlux.Plague_AbsorberAffliction);
        }
    }

    internal readonly struct BFPlagueRightStats
    {
        public readonly int MaxPermanentStacks;
        public readonly int DefenseReductionPerStack;
        public readonly float NpcDamageReductionPerStack;
        public readonly float MarkDurationMultiplier;

        public BFPlagueRightStats(int maxPermanentStacks, int defenseReductionPerStack, float npcDamageReductionPerStack, float markDurationMultiplier)
        {
            MaxPermanentStacks = maxPermanentStacks;
            DefenseReductionPerStack = defenseReductionPerStack;
            NpcDamageReductionPerStack = npcDamageReductionPerStack;
            MarkDurationMultiplier = markDurationMultiplier;
        }
    }

    internal static class BFPlagueRightBalance
    {
        public static BFPlagueRightStats GetStats()
        {
            return new BFPlagueRightStats(1, 15, 0.05f, 1f);
        }
    }

    internal static class BFReconLeftBalance
    {
        public const int MarkDuration = 30;
        public const int HomingDelayFrames = 18;
        public const float HomingTurnResponsiveness = 0.22f;
        public const float PriorityHomingTurnResponsiveness = 0.34f;
    }

    internal readonly struct BFReconRightStats
    {
        public readonly int ChargeFrames;
        public readonly int MarkDuration;
        public readonly int DamageAmpDuration;
        public readonly int EffectTier;

        public BFReconRightStats(int chargeFrames, int markDuration, int damageAmpDuration, int effectTier)
        {
            ChargeFrames = chargeFrames;
            MarkDuration = markDuration;
            DamageAmpDuration = damageAmpDuration;
            EffectTier = effectTier;
        }
    }

    internal static class BFReconRightBalance
    {
        // 标记本体（描边/索敌）持续 15 秒，增伤窗口只有 5 秒——标记要「短暂放大」而不是常驻。
        public const int DamageAmpDuration = 5 * 60;
        public const float DamageAmpMultiplier = 1.15f;

        public static BFReconRightStats GetStats()
        {
            return new BFReconRightStats(
                chargeFrames: 90,
                markDuration: System.Math.Max(1, BFBalanceTable.Get(BFStat.Recon_Right_MarkDurationSeconds)) * 60,
                damageAmpDuration: DamageAmpDuration,
                effectTier: 0);
        }
    }

    internal readonly struct BFRecoveryLeftStats
    {
        public readonly int FlashHealAmount;
        public readonly int FlashCooldownFrames;
        public readonly int MarkedFlashCooldownFrames;
        public readonly int FlashWindowFrames;
        public readonly int FlashWindowLimit;
        public readonly int LeafTimePerFlash;
        public readonly int LeafMaxTime;
        public readonly int VolleyPauseFrames;
        public readonly int Defense;
        public readonly int LifeRegen;
        public readonly int LifeRegenPerMissingQuarter;
        public readonly int RegenTimePerTick;
        public readonly float DamageReduction;
        public readonly bool ImmunePoisonAndFire;
        public readonly bool ImmuneAcidVenom;
        public readonly bool ImmunePlague;
        public readonly bool ImmuneMostPreDragonDebuffs;
        public readonly float DebuffDamageMultiplier;
        public readonly bool MovingRegenIgnoresPenalty;
        public readonly bool HealthThresholdRegenTime;

        public BFRecoveryLeftStats(
            int flashHealAmount,
            int flashCooldownFrames,
            int markedFlashCooldownFrames,
            int flashWindowFrames,
            int flashWindowLimit,
            int leafTimePerFlash,
            int leafMaxTime,
            int volleyPauseFrames,
            int defense,
            int lifeRegen,
            int lifeRegenPerMissingQuarter,
            int regenTimePerTick,
            float damageReduction,
            bool immunePoisonAndFire,
            bool immuneAcidVenom,
            bool immunePlague,
            bool immuneMostPreDragonDebuffs,
            float debuffDamageMultiplier,
            bool movingRegenIgnoresPenalty,
            bool healthThresholdRegenTime)
        {
            FlashHealAmount = flashHealAmount;
            FlashCooldownFrames = flashCooldownFrames;
            MarkedFlashCooldownFrames = markedFlashCooldownFrames;
            FlashWindowFrames = flashWindowFrames;
            FlashWindowLimit = flashWindowLimit;
            LeafTimePerFlash = leafTimePerFlash;
            LeafMaxTime = leafMaxTime;
            VolleyPauseFrames = volleyPauseFrames;
            Defense = defense;
            LifeRegen = lifeRegen;
            LifeRegenPerMissingQuarter = lifeRegenPerMissingQuarter;
            RegenTimePerTick = regenTimePerTick;
            DamageReduction = damageReduction;
            ImmunePoisonAndFire = immunePoisonAndFire;
            ImmuneAcidVenom = immuneAcidVenom;
            ImmunePlague = immunePlague;
            ImmuneMostPreDragonDebuffs = immuneMostPreDragonDebuffs;
            DebuffDamageMultiplier = debuffDamageMultiplier;
            MovingRegenIgnoresPenalty = movingRegenIgnoresPenalty;
            HealthThresholdRegenTime = healthThresholdRegenTime;
        }
    }

    internal static class BFRecoveryLeftBalance
    {
        public static BFRecoveryLeftStats GetStats()
        {
            int volleyPauseFrames = BlossomFluxGrowthProgression.StageIndex switch
            {
                (int)BlossomFluxGrowthStage.EyeOfCthulhu => 50,
                (int)BlossomFluxGrowthStage.Hardmode => 40,
                (int)BlossomFluxGrowthStage.Plantera => 40,
                (int)BlossomFluxGrowthStage.MoonLord => 40,
                (int)BlossomFluxGrowthStage.DevourerOfGods => 40,
                _ => 60
            };

            int heal = 5;
            int maxTime = 10 * 60;
            int defense = 5;
            int regen = 2;
            int missingQuarterRegen = 0;
            int regenTime = 0;
            float damageReduction = 0f;
            bool immunePoisonAndFire = false;
            bool immuneAcidVenom = false;
            bool immunePlague = false;
            bool immuneMostPreDragonDebuffs = false;
            float debuffDamageMultiplier = 1f;
            bool movingRegenIgnoresPenalty = false;
            bool healthThresholdRegenTime = false;

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.Hardmode))
            {
                heal = 7;
                maxTime = 15 * 60;
                defense = 10;
                regen = 4;
                immunePoisonAndFire = true;
            }

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.Plantera))
            {
                heal = 10;
                maxTime = 20 * 60;
                defense = 15;
                regen = 6;
                regenTime = 4;
                damageReduction = 0.12f;
                immuneAcidVenom = true;
                debuffDamageMultiplier = 0.67f;
            }

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord))
            {
                defense = 20;
                regen = 8;
                immunePlague = true;
                movingRegenIgnoresPenalty = true;
                healthThresholdRegenTime = true;
            }

            if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods))
            {
                heal = 20;
                maxTime = 30 * 60;
                defense = 25;
                regen = 10;
                regenTime = 5;
                missingQuarterRegen = 2;
                damageReduction = 0.20f;
                immuneMostPreDragonDebuffs = true;
                debuffDamageMultiplier = 0.5f;
                movingRegenIgnoresPenalty = true;
                healthThresholdRegenTime = true;
            }

            return new BFRecoveryLeftStats(
                heal,
                120,
                75,
                5 * 60,
                2,
                5 * 60,
                maxTime,
                volleyPauseFrames,
                defense,
                regen,
                missingQuarterRegen,
                regenTime,
                damageReduction,
                immunePoisonAndFire,
                immuneAcidVenom,
                immunePlague,
                immuneMostPreDragonDebuffs,
                debuffDamageMultiplier,
                movingRegenIgnoresPenalty,
                healthThresholdRegenTime);
        }
    }

    internal readonly struct BFRecoveryRightStats
    {
        public readonly int ChargeFrames;
        public readonly int FlashCount;
        public readonly int HealAmount;
        public readonly float ChargeDamageReduction;

        public BFRecoveryRightStats(int chargeFrames, int flashCount, int healAmount, float chargeDamageReduction)
        {
            ChargeFrames = chargeFrames;
            FlashCount = flashCount;
            HealAmount = healAmount;
            ChargeDamageReduction = chargeDamageReduction;
        }
    }

    internal static class BFRecoveryRightBalance
    {
        public static BFRecoveryRightStats GetStats()
        {
            return new BFRecoveryRightStats(
                chargeFrames: 5 * 60,
                flashCount: MathMax(1, BFBalanceTable.Get(BFStat.Recovery_Right_OrbCount)),
                healAmount: MathMax(1, BFBalanceTable.Get(BFStat.Recovery_Right_HealAmount)),
                chargeDamageReduction: 0f);
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
    }

    internal enum BlossomFluxGrowthStage
    {
        Start = 0,
        EyeOfCthulhu = 1,
        Hardmode = 2,
        Plantera = 3,
        MoonLord = 4,
        DevourerOfGods = 5
    }

    internal static class BlossomFluxGrowthProgression
    {
        public static int StageIndex => (int)GetDefeatedStage();

        public static bool DownedAtLeast(BlossomFluxGrowthStage stage) => GetDefeatedStage() >= stage;

        private static BlossomFluxGrowthStage GetDefeatedStage()
        {
            BlossomFluxGrowthStage stage = BlossomFluxGrowthStage.Start;

            if (NPC.downedBoss1)
                stage = BlossomFluxGrowthStage.EyeOfCthulhu;
            if (Main.hardMode)
                stage = BlossomFluxGrowthStage.Hardmode;
            if (NPC.downedPlantBoss)
                stage = BlossomFluxGrowthStage.Plantera;
            if (NPC.downedMoonlord)
                stage = BlossomFluxGrowthStage.MoonLord;
            if (DownedBossSystem.downedDoG)
                stage = BlossomFluxGrowthStage.DevourerOfGods;

            return stage;
        }
    }

    internal enum BlossomFluxProgressionStage
    {
        Start = 0,
        EyeOfCthulhu = 1,
        EaterOrBrain = 2,
        QueenBee = 3,
        Skeletron = 4,
        WallOfFlesh = 5,
        MechBoss = 6,
        Plantera = 7,
        Golem = 8,
        PlaguebringerGoliath = 9,
        MoonLord = 10,
        Providence = 11,
        Polterghast = 12,
        DevourerOfGods = 13,
        Yharon = 14,
        ExoMechsAndCalamitas = 15
    }

    internal static class BlossomFluxProgression
    {
        public static int StageIndex => (int)GetDefeatedStage();

        public static bool DownedAtLeast(BlossomFluxProgressionStage stage) => GetDefeatedStage() >= stage;

        public static bool DownedAnyMechBoss() => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        public static bool DownedAnyBossOrMiniboss()
        {
            return GetDefeatedStage() > BlossomFluxProgressionStage.Start ||
                NPC.downedSlimeKing ||
                NPC.downedBoss2 ||
                NPC.downedBoss3 ||
                NPC.downedGoblins ||
                NPC.downedFrost ||
                NPC.downedPirates ||
                NPC.downedGolemBoss ||
                DownedBossSystem.downedDesertScourge ||
                DownedBossSystem.downedCrabulon ||
                DownedBossSystem.downedHiveMind ||
                DownedBossSystem.downedPerforator ||
                DownedBossSystem.downedSlimeGod ||
                DownedBossSystem.downedCryogen ||
                DownedBossSystem.downedAquaticScourge ||
                DownedBossSystem.downedBrimstoneElemental ||
                DownedBossSystem.downedGSS ||
                DownedBossSystem.downedCLAM ||
                DownedBossSystem.downedCragmawMire ||
                DownedBossSystem.downedMauler ||
                DownedBossSystem.downedProvidence ||
                DownedBossSystem.downedYharon ||
                DownedBossSystem.downedExoMechs ||
                DownedBossSystem.downedCalamitas;
        }

        private static BlossomFluxProgressionStage GetDefeatedStage()
        {
            BlossomFluxProgressionStage stage = BlossomFluxProgressionStage.Start;

            if (NPC.downedBoss1)
                stage = BlossomFluxProgressionStage.EyeOfCthulhu;
            if (NPC.downedBoss2)
                stage = BlossomFluxProgressionStage.EaterOrBrain;
            if (NPC.downedQueenBee)
                stage = BlossomFluxProgressionStage.QueenBee;
            if (NPC.downedBoss3)
                stage = BlossomFluxProgressionStage.Skeletron;
            if (Main.hardMode)
                stage = BlossomFluxProgressionStage.WallOfFlesh;
            if (DownedAnyMechBoss())
                stage = BlossomFluxProgressionStage.MechBoss;
            if (NPC.downedPlantBoss)
                stage = BlossomFluxProgressionStage.Plantera;
            if (NPC.downedGolemBoss)
                stage = BlossomFluxProgressionStage.Golem;
            if (DownedBossSystem.downedPlaguebringer)
                stage = BlossomFluxProgressionStage.PlaguebringerGoliath;
            if (NPC.downedMoonlord)
                stage = BlossomFluxProgressionStage.MoonLord;
            if (DownedBossSystem.downedProvidence)
                stage = BlossomFluxProgressionStage.Providence;
            if (DownedBossSystem.downedPolterghast)
                stage = BlossomFluxProgressionStage.Polterghast;
            if (DownedBossSystem.downedDoG)
                stage = BlossomFluxProgressionStage.DevourerOfGods;
            if (DownedBossSystem.downedYharon)
                stage = BlossomFluxProgressionStage.Yharon;
            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas)
                stage = BlossomFluxProgressionStage.ExoMechsAndCalamitas;

            return stage;
        }
    }

    internal enum BFStat
    {
        Breakthrough_Left_Damage = 0,
        Breakthrough_Left_UseInterval = 1,
        Breakthrough_Right_MaxArrows = 2,
        Breakthrough_Right_FramesPerArrow = 3,
        Breakthrough_Right_Damage = 4,

        Recovery_Left_Damage = 5,
        Recovery_Right_OrbCount = 6,
        Recovery_Right_HealAmount = 7,

        Recon_Left_Damage = 8,
        Recon_Left_BurstCooldown = 9,
        Recon_Left_ShotsPerBurst = 10,
        Recon_Right_Penetrate = 11,
        Recon_Right_MarkDurationSeconds = 12,
        Recon_Right_Damage = 13,

        Bombard_Left_MinArrows = 14,
        Bombard_Left_MaxArrows = 15,
        Bombard_Left_Interval = 16,
        Bombard_Left_Damage = 17,
        Bombard_Right_Damage = 18,
        Bombard_Right_WaveCount = 19,
        Bombard_Right_RainImpactExplosionCount = 20,

        Plague_Left_Damage = 21,
        Plague_Right_Damage = 22,

        StatCount = 23
    }

    internal static class BFBalanceTable
    {
        private const string SourceFile = "Weapons/BlossomFlux/BalanceBlossomFlux.cs";
        public static int Get(BFStat stat)
        {
            if (TryGetMainDamageColumn(stat, out _))
                return Get(stat, BlossomFluxProgression.StageIndex);

            if (TryGetMainParamsColumn(stat, out _))
                return Get(stat, BlossomFluxGrowthProgression.StageIndex);

            return 0;
        }

        public static int Get(BFStat stat, int stageIndex)
        {
            if (TryGetMainDamageColumn(stat, out int damageColumn))
            {
                int stage = Utils.Clamp(stageIndex, 0, BalanceBlossomFlux.MainDamageTable.GetLength(0) - 1);
                int fallback = BalanceBlossomFlux.GetMainDamageFallback(stage, damageColumn);
                int value = RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(BalanceBlossomFlux.MainDamageTable), stage, damageColumn, fallback);
                return value < 1 ? 1 : value;
            }

            if (TryGetMainParamsColumn(stat, out int paramsColumn))
            {
                int stage = Utils.Clamp(stageIndex, 0, BalanceBlossomFlux.MainParamsTable.GetLength(0) - 1);
                int fallback = BalanceBlossomFlux.GetMainParamsFallback(stage, paramsColumn);
                return RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(BalanceBlossomFlux.MainParamsTable), stage, paramsColumn, fallback);
            }

            return 0;
        }

        private static bool TryGetMainDamageColumn(BFStat stat, out int column)
        {
            switch (stat)
            {
                case BFStat.Breakthrough_Left_Damage:
                case BFStat.Breakthrough_Right_Damage:
                    column = 1;
                    return true;

                case BFStat.Recovery_Left_Damage:
                    column = 2;
                    return true;

                case BFStat.Recon_Left_Damage:
                case BFStat.Recon_Right_Damage:
                    column = 3;
                    return true;

                case BFStat.Bombard_Left_Damage:
                case BFStat.Bombard_Right_Damage:
                    column = 4;
                    return true;

                case BFStat.Plague_Left_Damage:
                case BFStat.Plague_Right_Damage:
                    column = 5;
                    return true;

                default:
                    column = 0;
                    return false;
            }
        }

        private static bool TryGetMainParamsColumn(BFStat stat, out int column)
        {
            switch (stat)
            {
                case BFStat.Breakthrough_Left_UseInterval:
                    column = 1;
                    return true;

                case BFStat.Breakthrough_Right_MaxArrows:
                    column = 2;
                    return true;

                case BFStat.Breakthrough_Right_FramesPerArrow:
                    column = 3;
                    return true;

                case BFStat.Recovery_Right_OrbCount:
                    column = 4;
                    return true;

                case BFStat.Recovery_Right_HealAmount:
                    column = 5;
                    return true;

                case BFStat.Recon_Left_ShotsPerBurst:
                    column = 6;
                    return true;

                case BFStat.Recon_Left_BurstCooldown:
                    column = 7;
                    return true;

                case BFStat.Recon_Right_Penetrate:
                    column = 8;
                    return true;

                case BFStat.Recon_Right_MarkDurationSeconds:
                    column = 9;
                    return true;

                case BFStat.Bombard_Left_MinArrows:
                    column = 10;
                    return true;

                case BFStat.Bombard_Left_MaxArrows:
                    column = 11;
                    return true;

                case BFStat.Bombard_Left_Interval:
                    column = 12;
                    return true;

                case BFStat.Bombard_Right_WaveCount:
                    column = 13;
                    return true;

                case BFStat.Bombard_Right_RainImpactExplosionCount:
                    column = 14;
                    return true;

                default:
                    column = 0;
                    return false;
            }
        }
    }

    internal static class BFBreakthroughNonNumerical
    {
        public static float RightArrowSpeedMultiplier => 1f;
        public static bool RightArrowInfinitePenetrate => false;
    }

    internal static class BFRecoveryNonNumerical
    {
        public static bool ImmuneToFireAndPoison => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.Hardmode);
        public static bool ImmuneToAcidVenom => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.Plantera);
        public static bool ImmuneToPlagueDebuffs => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord);
        public static bool ImmuneToMostPreDragonDebuffs => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods);
        public static bool MovementRegenIgnoresPenalty => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord);
        public static float LowHealthBonusRegenThreshold => BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.MoonLord) ? 0.4f : 0f;

        public static float DebuffDamageTakenMultiplier
        {
            get
            {
                if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.DevourerOfGods))
                    return 0.5f;

                if (BlossomFluxGrowthProgression.DownedAtLeast(BlossomFluxGrowthStage.Plantera))
                    return 0.67f;

                return 1f;
            }
        }
    }

    internal static class BFReconNonNumerical
    {
        public const int MarkDurationFrames = 30;
        public const int HomingDelayFrames = 18;
        public const float HomingTurnResponsiveness = 0.22f;
        public const float PriorityHomingTurnResponsiveness = 0.34f;
        public static int ReconEffectTier => 0;
        public static int PriorityMarkDurationFrames => 15 * 60;
        public static int ChargeFrames => 90;
    }

    internal static class BFPlagueNonNumerical
    {
        public static bool InflictBetsysCurse => BalanceBlossomFlux.Plague_BetsysCurse;
        public static bool InflictAstralInfection => BalanceBlossomFlux.Plague_AstralInfection;
        public static bool InflictWither => BalanceBlossomFlux.Plague_Wither;
        public static bool InflictWhisperingDeath => BalanceBlossomFlux.Plague_WhisperingDeath;
        public static bool InflictAbsorberAffliction => BalanceBlossomFlux.Plague_AbsorberAffliction;
        public static int MaxPermanentMarkStacks => 1;
        public static float MarkDurationMultiplier => 1f;
    }
}
