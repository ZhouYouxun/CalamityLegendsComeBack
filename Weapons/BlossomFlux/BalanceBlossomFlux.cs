using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class BalanceBlossomFlux
    {
        public readonly struct BreakthroughFireStats
        {
            public readonly int UseTime;
            public readonly int UseInterval;
            public readonly float ShotsPerSecond;
            public readonly float ProjectileSpeedMultiplier;

            public BreakthroughFireStats(int useTime, int useInterval, float shotsPerSecond, float projectileSpeedMultiplier)
            {
                UseTime = useTime;
                UseInterval = useInterval;
                ShotsPerSecond = shotsPerSecond;
                ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            }
        }

        public readonly struct BreakthroughChargeStats
        {
            public readonly int FramesPerArrow;
            public readonly int MaxLoadedArrows;
            public readonly int Penetrate;
            public readonly bool IgnorePenetrationDamageFalloff;
            public readonly float ProjectileSpeedMultiplier;

            public BreakthroughChargeStats(int framesPerArrow, int maxLoadedArrows, int penetrate, bool ignorePenetrationDamageFalloff, float projectileSpeedMultiplier)
            {
                FramesPerArrow = framesPerArrow;
                MaxLoadedArrows = maxLoadedArrows;
                Penetrate = penetrate;
                IgnorePenetrationDamageFalloff = ignorePenetrationDamageFalloff;
                ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            }
        }

        public readonly struct RecoveryLeafStats
        {
            public readonly int FlashHealAmount;
            public readonly int FlashCooldownFrames;
            public readonly int FlashWindowFrames;
            public readonly int FlashWindowLimit;
            public readonly int LeafMaxTime;
            public readonly int Defense;
            public readonly int LifeRegen;
            public readonly int RegenTimePerTick;
            public readonly float DamageReduction;
            public readonly float DebuffDamageReduction;
            public readonly bool ImmunePoisonAndFire;
            public readonly bool ImmuneVenom;
            public readonly bool ImmunePlague;
            public readonly bool BroadDebuffImmunity;
            public readonly bool MoonLordHealthRegenFloor;
            public readonly bool YharonHealthRegenFloor;
            public readonly bool MovingRegenBoost;

            public RecoveryLeafStats(
                int flashHealAmount,
                int flashCooldownFrames,
                int flashWindowFrames,
                int flashWindowLimit,
                int leafMaxTime,
                int defense,
                int lifeRegen,
                int regenTimePerTick,
                float damageReduction,
                float debuffDamageReduction,
                bool immunePoisonAndFire,
                bool immuneVenom,
                bool immunePlague,
                bool broadDebuffImmunity,
                bool moonLordHealthRegenFloor,
                bool yharonHealthRegenFloor,
                bool movingRegenBoost)
            {
                FlashHealAmount = flashHealAmount;
                FlashCooldownFrames = flashCooldownFrames;
                FlashWindowFrames = flashWindowFrames;
                FlashWindowLimit = flashWindowLimit;
                LeafMaxTime = leafMaxTime;
                Defense = defense;
                LifeRegen = lifeRegen;
                RegenTimePerTick = regenTimePerTick;
                DamageReduction = damageReduction;
                DebuffDamageReduction = debuffDamageReduction;
                ImmunePoisonAndFire = immunePoisonAndFire;
                ImmuneVenom = immuneVenom;
                ImmunePlague = immunePlague;
                BroadDebuffImmunity = broadDebuffImmunity;
                MoonLordHealthRegenFloor = moonLordHealthRegenFloor;
                YharonHealthRegenFloor = yharonHealthRegenFloor;
                MovingRegenBoost = movingRegenBoost;
            }
        }

        public readonly struct RecoveryChargeStats
        {
            public readonly int ChargeFrames;
            public readonly int FlashCount;
            public readonly int HealAmount;
            public readonly float ChargeDamageReduction;

            public RecoveryChargeStats(int chargeFrames, int flashCount, int healAmount, float chargeDamageReduction)
            {
                ChargeFrames = chargeFrames;
                FlashCount = flashCount;
                HealAmount = healAmount;
                ChargeDamageReduction = chargeDamageReduction;
            }
        }

        public readonly struct ReconChargeStats
        {
            public readonly int ChargeFrames;
            public readonly int MarkDuration;
            public readonly int EffectTier;

            public ReconChargeStats(int chargeFrames, int markDuration, int effectTier)
            {
                ChargeFrames = chargeFrames;
                MarkDuration = markDuration;
                EffectTier = effectTier;
            }
        }

        public readonly struct BombardLeftStats
        {
            public readonly int MinArrowCount;
            public readonly int MaxArrowCount;
            public readonly int FireInterval;
            public readonly int ExplosionsPerArrow;
            public readonly float ExplosionRadiusMultiplier;
            public readonly float ProjectileSpeedMultiplier;

            public BombardLeftStats(int minArrowCount, int maxArrowCount, int fireInterval, int explosionsPerArrow, float explosionRadiusMultiplier, float projectileSpeedMultiplier)
            {
                MinArrowCount = minArrowCount;
                MaxArrowCount = maxArrowCount;
                FireInterval = fireInterval;
                ExplosionsPerArrow = explosionsPerArrow;
                ExplosionRadiusMultiplier = explosionRadiusMultiplier;
                ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            }
        }

        public readonly struct BombardChargeStats
        {
            public readonly int FirstChargeFrames;
            public readonly int SecondChargeFrames;
            public readonly bool CanSecondCharge;
            public readonly float ExplosionSize;

            public BombardChargeStats(int firstChargeFrames, int secondChargeFrames, bool canSecondCharge, float explosionSize)
            {
                FirstChargeFrames = firstChargeFrames;
                SecondChargeFrames = secondChargeFrames;
                CanSecondCharge = canSecondCharge;
                ExplosionSize = explosionSize;
            }
        }

        public readonly struct PlagueDebuffStats
        {
            public readonly int InitialDuration;
            public readonly int StackDuration;
            public readonly int MaxDuration;
            public readonly bool InflictDragonfire;
            public readonly bool InflictAstralInfection;
            public readonly bool InflictWither;
            public readonly bool InflictWhisperingDeath;
            public readonly bool InflictAbsorberAffliction;

            public PlagueDebuffStats(int initialDuration, int stackDuration, int maxDuration, bool inflictDragonfire, bool inflictAstralInfection, bool inflictWither, bool inflictWhisperingDeath, bool inflictAbsorberAffliction)
            {
                InitialDuration = initialDuration;
                StackDuration = stackDuration;
                MaxDuration = maxDuration;
                InflictDragonfire = inflictDragonfire;
                InflictAstralInfection = inflictAstralInfection;
                InflictWither = inflictWither;
                InflictWhisperingDeath = inflictWhisperingDeath;
                InflictAbsorberAffliction = inflictAbsorberAffliction;
            }
        }

        public readonly struct PlagueChargeStats
        {
            public readonly int MaxPermanentStacks;
            public readonly int DefenseReductionPerStack;
            public readonly float NpcDamageReductionPerStack;
            public readonly float VulnerabilityPerStack;

            public PlagueChargeStats(int maxPermanentStacks, int defenseReductionPerStack, float npcDamageReductionPerStack, float vulnerabilityPerStack)
            {
                MaxPermanentStacks = maxPermanentStacks;
                DefenseReductionPerStack = defenseReductionPerStack;
                NpcDamageReductionPerStack = npcDamageReductionPerStack;
                VulnerabilityPerStack = vulnerabilityPerStack;
            }
        }

        public int[] LeftClickBaseDamage =
        {
            10, 12, 17, 27, 40, 54, 82, 96, 155, 185, 255, 320, 395, 560
        };

        public int[] RightClickBaseDamage =
        {
            10, 12, 17, 27, 40, 54, 82, 96, 155, 185, 255, 320, 395, 560
        };

        public int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                NPC.downedBoss3,
                Main.hardMode,
                DownedAnyMechBoss(),
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
                if (!clearedStages[i])
                    break;

                stageIndex = i + 1;
            }

            return stageIndex;
        }

        public int GetLeftClickBaseDamage() => GetValueForStage(LeftClickBaseDamage, GetCompletedStageIndex());

        public int GetRightClickBaseDamage() => GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());

        public BreakthroughFireStats GetBreakthroughFireStats()
        {
            int useTime = 15;
            int useInterval = 15;
            float shotsPerSecond = 4f;

            if (AnyEarlyBossOrMinibossDowned())
            {
                useTime = 12;
                useInterval = 12;
                shotsPerSecond = 5f;
            }

            if (NPC.downedBoss1)
            {
                useTime = 10;
                useInterval = 10;
                shotsPerSecond = 6f;
            }

            if (NPC.downedQueenBee)
            {
                useTime = 12;
                useInterval = 6;
                shotsPerSecond = 10f;
            }

            if (Main.hardMode)
            {
                useTime = 12;
                useInterval = 4;
                shotsPerSecond = 15f;
            }

            if (DownedAnyMechBoss())
            {
                useTime = 15;
                useInterval = 3;
                shotsPerSecond = 20f;
            }

            if (NPC.downedPlantBoss)
            {
                useTime = 10;
                useInterval = 2;
                shotsPerSecond = 30f;
            }

            return new BreakthroughFireStats(useTime, useInterval, shotsPerSecond, GetBreakthroughLeftProjectileSpeedMultiplier());
        }

        public BreakthroughChargeStats GetBreakthroughChargeStats()
        {
            int framesPerArrow = 45;
            int maxArrows = 3;
            int penetrate = 4;
            bool noFalloff = false;
            float speedMult = 1f;

            if (NPC.downedSlimeKing)
                penetrate = 5;

            if (NPC.downedBoss1)
                framesPerArrow = 40;

            if (NPC.downedQueenBee)
                maxArrows = 4;

            if (Main.hardMode)
            {
                framesPerArrow = 35;
                maxArrows = 5;
            }

            if (NPC.downedMechBoss3)
                maxArrows = 6;

            if (NPC.downedPlantBoss)
                penetrate = 7;

            if (DownedBossSystem.downedPlaguebringer)
                maxArrows = 7;

            if (NPC.downedGolemBoss)
                speedMult = 1.25f;

            if (NPC.downedMoonlord)
            {
                penetrate = 15;
                noFalloff = true;
                framesPerArrow = 30;
            }

            if (DownedBossSystem.downedPolterghast)
            {
                penetrate = -1;
                speedMult = 1.65f;
            }

            if (DownedBossSystem.downedDoG)
                speedMult = 2.15f;

            framesPerArrow = System.Math.Max(25, framesPerArrow);
            return new BreakthroughChargeStats(framesPerArrow, maxArrows, penetrate, noFalloff, speedMult);
        }

        public RecoveryLeafStats GetRecoveryLeafStats()
        {
            int heal = 5;
            int maxTime = 10 * 60;
            int defense = 5;
            int regen = 2;
            int regenTime = 0;
            float dr = 0f;
            float debuffDr = 0f;
            bool poisonFire = false;
            bool venom = false;
            bool plague = false;
            bool broad = false;
            bool moonLifeFloor = false;
            bool yharonLifeFloor = false;
            bool movingRegen = false;

            if (Main.hardMode)
            {
                heal = 7;
                maxTime = 15 * 60;
                defense = 8;
                regen = 3;
                poisonFire = true;
            }

            if (DownedAnyMechBoss())
            {
                defense = 12;
                regen = 4;
                regenTime = 3;
                dr = 0.10f;
            }

            if (NPC.downedPlantBoss)
            {
                heal = 10;
                maxTime = 20 * 60;
                defense = 15;
                regen = 5;
                regenTime = 4;
                dr = 0.12f;
                venom = true;
                debuffDr = 0.33f;
            }

            if (DownedBossSystem.downedPlaguebringer)
                plague = true;

            if (NPC.downedMoonlord)
            {
                regen = 6;
                moonLifeFloor = true;
                movingRegen = true;
            }

            if (DownedBossSystem.downedPolterghast)
            {
                heal = 15;
                maxTime = 25 * 60;
                defense = 20;
                dr = 0.15f;
            }

            if (DownedBossSystem.downedYharon)
            {
                maxTime = 30 * 60;
                defense = 25;
                regen = 8;
                regenTime = 5;
                dr = 0.20f;
                debuffDr = 0.50f;
                broad = true;
                yharonLifeFloor = true;
                movingRegen = true;
            }

            return new RecoveryLeafStats(heal, 120, 5 * 60, 2, maxTime, defense, regen, regenTime, dr, debuffDr, poisonFire, venom, plague, broad, moonLifeFloor, yharonLifeFloor, movingRegen);
        }

        public RecoveryChargeStats GetRecoveryChargeStats()
        {
            int chargeFrames = 5 * 60;
            int flashCount = 4;
            int heal = 10;
            float chargeDr = 0f;

            if (Main.hardMode)
            {
                flashCount = 5;
                heal = 12;
            }

            if (DownedAnyMechBoss())
                flashCount = 6;

            if (NPC.downedPlantBoss)
            {
                flashCount = 7;
                chargeFrames = 4 * 60;
            }

            if (NPC.downedMoonlord)
                heal = 15;

            if (DownedBossSystem.downedPolterghast)
                flashCount = 9;

            if (DownedBossSystem.downedYharon)
            {
                heal = 20;
                chargeFrames = 3 * 60;
                chargeDr = 0.30f;
            }

            return new RecoveryChargeStats(chargeFrames, flashCount, heal, chargeDr);
        }

        public ReconChargeStats GetReconChargeStats()
        {
            int chargeFrames = 90;
            int markDuration = 15 * 60;
            int effectTier = 0;

            if (DownedAnyMechBoss())
                markDuration = 20 * 60;

            if (NPC.downedPlantBoss)
                chargeFrames = 75;

            if (DownedBossSystem.downedPlaguebringer)
                markDuration = 25 * 60;

            if (NPC.downedMoonlord)
            {
                chargeFrames = 60;
                effectTier = 1;
            }

            if (DownedBossSystem.downedPolterghast)
                markDuration = 30 * 60;

            if (DownedBossSystem.downedDoG)
            {
                chargeFrames = 45;
                effectTier = 2;
            }

            return new ReconChargeStats(chargeFrames, markDuration, effectTier);
        }

        public BombardLeftStats GetBombardLeftStats()
        {
            int minCount = 3;
            int maxCount = 3;
            int interval = 20;
            int explosionLimit = 1;
            float radius = 1f;
            float speed = 1f;

            if (NPC.downedPlantBoss)
                maxCount = 4;

            if (DownedBossSystem.downedPlaguebringer)
            {
                minCount = 4;
                maxCount = 4;
                interval = 17;
            }

            if (NPC.downedMoonlord)
            {
                minCount = 4;
                maxCount = 5;
                interval = 16;
                explosionLimit = 2;
            }

            if (DownedBossSystem.downedNuclearTerror)
            {
                radius = 1.25f;
                speed = 1.18f;
            }

            if (DownedBossSystem.downedDoG)
            {
                minCount = 5;
                maxCount = 5;
                interval = 14;
                explosionLimit = 3;
            }

            return new BombardLeftStats(minCount, maxCount, interval, explosionLimit, radius, speed);
        }

        public BombardChargeStats GetBombardChargeStats()
        {
            float size = 190f;
            if (NPC.downedMoonlord)
                size += 55f;

            if (DownedBossSystem.downedNuclearTerror)
                size += 65f;

            bool exo = DownedBossSystem.downedExoMechs;
            if (exo)
                size = 420f;

            return new BombardChargeStats(60, 120, exo, size);
        }

        public PlagueDebuffStats GetPlagueDebuffStats()
        {
            int initial = 10 * 60;
            int stack = 5 * 60;
            int max = 30 * 60;
            bool dragonfire = false;
            bool astral = false;
            bool wither = false;
            bool whisper = false;
            bool absorber = false;

            if (DownedBossSystem.downedDragonfolly)
                dragonfire = true;

            if (DownedBossSystem.downedAstrumDeus)
            {
                astral = true;
                initial = 15 * 60;
                stack = 450;
                max = 45 * 60;
            }

            if (NPC.downedMoonlord)
            {
                wither = true;
                stack = 10 * 60;
                max = 60 * 60;
            }

            if (DownedBossSystem.downedProvidence)
            {
                whisper = true;
                initial = 20 * 60;
                stack = 15 * 60;
                max = 75 * 60;
            }

            if (DownedBossSystem.downedNuclearTerror)
            {
                absorber = true;
                initial = 30 * 60;
                max = 90 * 60;
            }

            return new PlagueDebuffStats(initial, stack, max, dragonfire, astral, wither, whisper, absorber);
        }

        public PlagueChargeStats GetPlagueChargeStats()
        {
            int stacks = 1;
            if (DownedBossSystem.downedProvidence)
                stacks = 2;

            if (DownedBossSystem.downedDoG)
                stacks = 3;

            return new PlagueChargeStats(stacks, 15, 0.05f, 0.05f);
        }

        private bool AnyEarlyBossOrMinibossDowned()
        {
            return
                NPC.downedSlimeKing ||
                NPC.downedBoss1 ||
                NPC.downedBoss2 ||
                NPC.downedBoss3 ||
                NPC.downedQueenBee ||
                NPC.downedGoblins ||
                NPC.downedFrost ||
                NPC.downedPirates ||
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
                DownedBossSystem.downedMauler;
        }

        private static bool DownedAnyMechBoss() => NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        private float GetBreakthroughLeftProjectileSpeedMultiplier()
        {
            if (DownedBossSystem.downedDoG)
                return 3f;

            if (DownedBossSystem.downedPolterghast)
                return 2f;

            if (NPC.downedMoonlord)
                return 1.66f;

            if (NPC.downedGolemBoss)
                return 1.33f;

            return 1f;
        }

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }
    }
}
