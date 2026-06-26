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
            { "Eye of Cthulhu",                             20,     9,     1,    1,      1 },
            { "Evil Boss",                                  24,     9,     1,    1,      1 },
            { "Queen Bee",                                  28,    10,     5,    1,      1 },
            { "Skeletron",                                  42,    11,    14,    1,      1 },
            { "Hardmode",                                   49,    13,    10,   14,      1 },
            { "Any Mechanical Boss",                        36,    15,    14,   19,      1 },
            { "Plantera",                                   44,    21,    18,   24,      1 },
            { "Golem",                                      52,    21,    18,   24,     21 },
            { "Plaguebringer Goliath",                      60,    27,    22,   30,     27 },
            { "Moon Lord",                                  68,    36,    30,   35,     36 },
            { "Providence",                                 77,    48,    37,   40,     48 },
            { "Polterghast",                                89,    56,    43,   46,     56 },
            { "Devourer of Gods",                          103,    70,    50,   53,     70 },
            { "Yharon",                                    120,    88,    60,   62,     88 },
            { "Exo Mechs and Supreme Calamitas",           140,   109,    69,   75,    109 }
        };

        // Edit primary non-damage knobs here. Columns are:
        // Stage,
        // Breakthrough left fire delay,
        // Breakthrough right max loaded arrows,
        // Recovery right orb count,
        // Recon left shots per trigger,
        // Recon left burst pause,
        // Recon right penetrate,
        // Bombard left min arrows per trigger,
        // Bombard left max arrows per trigger,
        // Bombard left fire delay,
        // Bombard right wave count.
        internal static readonly object[,] MainParamsTable =
        {
            // Stage                                      BrkDelay BrkMax RecOrb RecShots RecPause RecPen BombMin BombMax BombDelay BombWaves
            { "Initial",                                      15,     2,     4,       1,      90,     2,      4,      4,       10,        8 },
            { "Eye of Cthulhu",                               10,     2,     4,       1,      85,     2,      4,      4,       10,        8 },
            { "Evil Boss",                                    10,     2,     4,       1,      80,     2,      4,      4,       10,        8 },
            { "Queen Bee",                                    10,     2,     4,       1,      75,     2,      4,      4,       10,        8 },
            { "Skeletron",                                    10,     2,     4,       1,      70,     2,      4,      4,       10,        8 },
            { "Hardmode",                                      6,     3,     4,       2,      65,     2,      4,      4,       10,        8 },
            { "Any Mechanical Boss",                           6,     3,     4,       2,      60,     2,      4,      4,       10,        8 },
            { "Plantera",                                      3,     4,     4,       3,      55,     2,      4,      5,       10,        8 },
            { "Golem",                                         3,     4,     4,       3,      50,     2,      4,      5,       10,        8 },
            { "Plaguebringer Goliath",                         3,     4,     4,       3,      45,     2,      5,      5,        8,        8 },
            { "Moon Lord",                                     2,     5,     4,       3,      40,     2,      5,      6,        8,        8 },
            { "Providence",                                    2,     5,     4,       3,      36,     2,      5,      6,        8,        8 },
            { "Polterghast",                                   2,     5,     4,       3,      30,     2,      5,      6,        8,        8 },
            { "Devourer of Gods",                              2,     6,     4,       3,      24,     2,      6,      6,        7,        8 },
            { "Yharon",                                        2,     6,     4,       3,      18,     2,      6,      6,        7,        8 },
            { "Exo Mechs and Supreme Calamitas",               2,     6,     4,       3,      12,     2,      6,      6,        7,        8 },
        };

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

        internal static bool Plague_BetsysCurse => false;
        internal static bool Plague_AstralInfection => false;
        internal static bool Plague_Wither => false;
        internal static bool Plague_WhisperingDeath => false;
        internal static bool Plague_AbsorberAffliction => false;
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

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                explosionLimit = 2;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                radius = 1.25f;
                speed = 1.18f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
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

        public BFBombardRightStats(int chargeFrames, float explosionSize, float skyRainMultiplier, int waveCount)
        {
            ChargeFrames = chargeFrames;
            ExplosionSize = explosionSize;
            SkyRainMultiplier = skyRainMultiplier;
            WaveCount = waveCount;
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
                waveCount: MathMax(1, BFBalanceTable.Get(BFStat.Bombard_Right_WaveCount)));
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
                projectileSpeedMultiplier: GetProjectileSpeedMultiplier());
        }

        private static float GetProjectileSpeedMultiplier()
        {
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
                return 3f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
                return 2f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                return 1.66f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                return 1.33f;

            return 1f;
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
                framesPerArrow: 40,
                maxLoadedArrows: MathMax(1, BFBalanceTable.Get(BFStat.Breakthrough_Right_MaxArrows)),
                penetrate: 5,
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
                stackDuration: 5 * 60,
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
        public readonly int EffectTier;

        public BFReconRightStats(int chargeFrames, int markDuration, int effectTier)
        {
            ChargeFrames = chargeFrames;
            MarkDuration = markDuration;
            EffectTier = effectTier;
        }
    }

    internal static class BFReconRightBalance
    {
        public static BFReconRightStats GetStats()
        {
            return new BFReconRightStats(
                chargeFrames: 90,
                markDuration: 15 * 60,
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
            int volleyPauseFrames = 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
                volleyPauseFrames = 40;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                volleyPauseFrames = 20;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                volleyPauseFrames = 0;

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

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                heal = 7;
                maxTime = 15 * 60;
                defense = 8;
                regen = 3;
                immunePoisonAndFire = true;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
            {
                defense = 12;
                regen = 4;
                regenTime = 3;
                damageReduction = 0.10f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
            {
                heal = 10;
                maxTime = 20 * 60;
                defense = 15;
                regen = 5;
                regenTime = 4;
                damageReduction = 0.12f;
                immuneAcidVenom = true;
                debuffDamageMultiplier = 0.67f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                immunePlague = true;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                regen = 6;
                movingRegenIgnoresPenalty = true;
                healthThresholdRegenTime = true;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                heal = 15;
                maxTime = 25 * 60;
                defense = 20;
                damageReduction = 0.15f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                maxTime = 30 * 60;
                defense = 25;
                regen = 8;
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
                healAmount: 20,
                chargeDamageReduction: 0f);
        }

        private static int MathMax(int left, int right) => left > right ? left : right;
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
        Breakthrough_Right_Damage = 3,

        Recovery_Left_Damage = 4,
        Recovery_Right_OrbCount = 5,

        Recon_Left_Damage = 6,
        Recon_Left_BurstCooldown = 7,
        Recon_Left_ShotsPerBurst = 8,
        Recon_Right_Penetrate = 9,
        Recon_Right_Damage = 10,

        Bombard_Left_MinArrows = 11,
        Bombard_Left_MaxArrows = 12,
        Bombard_Left_Interval = 13,
        Bombard_Left_Damage = 14,
        Bombard_Right_Damage = 15,
        Bombard_Right_WaveCount = 16,

        Plague_Left_Damage = 17,
        Plague_Right_Damage = 18,

        StatCount = 19
    }

    internal static class BFBalanceTable
    {
        private const string SourceFile = "Weapons/BlossomFlux/BalanceBlossomFlux.cs";
        public const int StageCount = 16;

        public static int Get(BFStat stat) => Get(stat, BlossomFluxProgression.StageIndex);

        public static int Get(BFStat stat, int stageIndex)
        {
            int stage = Utils.Clamp(stageIndex, 0, StageCount - 1);

            if (TryGetMainDamageColumn(stat, out int damageColumn))
            {
                int fallback = BalanceBlossomFlux.GetMainDamageFallback(stage, damageColumn);
                int value = RuntimeBalanceData.GetSourceTableInt(SourceFile, nameof(BalanceBlossomFlux.MainDamageTable), stage, damageColumn, fallback);
                return value < 1 ? 1 : value;
            }

            if (TryGetMainParamsColumn(stat, out int paramsColumn))
            {
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

                case BFStat.Recovery_Right_OrbCount:
                    column = 3;
                    return true;

                case BFStat.Recon_Left_ShotsPerBurst:
                    column = 4;
                    return true;

                case BFStat.Recon_Left_BurstCooldown:
                    column = 5;
                    return true;

                case BFStat.Recon_Right_Penetrate:
                    column = 6;
                    return true;

                case BFStat.Bombard_Left_MinArrows:
                    column = 7;
                    return true;

                case BFStat.Bombard_Left_MaxArrows:
                    column = 8;
                    return true;

                case BFStat.Bombard_Left_Interval:
                    column = 9;
                    return true;

                case BFStat.Bombard_Right_WaveCount:
                    column = 10;
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
        public static bool ImmuneToFireAndPoison => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh);
        public static bool ImmuneToAcidVenom => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera);
        public static bool ImmuneToPlagueDebuffs => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath);
        public static bool ImmuneToMostPreDragonDebuffs => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods);
        public static bool MovementRegenIgnoresPenalty => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord);
        public static float LowHealthBonusRegenThreshold => BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord) ? 0.4f : 0f;

        public static float DebuffDamageTakenMultiplier
        {
            get
            {
                if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
                    return 0.5f;

                if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
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
