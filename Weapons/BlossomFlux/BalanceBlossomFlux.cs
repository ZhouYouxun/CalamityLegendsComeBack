using Terraria;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    public class BalanceBlossomFlux
    {
        public static readonly string[] StageNames =
        {
            "初始 / Initial",
            "击败克苏鲁之眼 / Eye of Cthulhu",
            "击败蜂王 / Queen Bee",
            "击败血肉墙 / Wall of Flesh",
            "击败任意机械 Boss / Any Mechanical Boss",
            "击败世纪之花 / Plantera",
            "击败瘟疫使者歌莉娅 / Plaguebringer Goliath",
            "击败月亮领主 / Moon Lord",
            "击败噬魂幽花 / Polterghast",
            "击败神明吞噬者 / Devourer of Gods"
        };

        // 左键基础伤害。数组顺序与 StageNames 完全一致。
        private static readonly int[] LeftClickBaseDamage =
        {
            10, // 初始 / Initial
            12, // 击败克苏鲁之眼 / Eye of Cthulhu
            17, // 击败蜂王 / Queen Bee
            27, // 击败血肉墙 / Wall of Flesh
            40, // 击败任意机械 Boss / Any Mechanical Boss
            54, // 击败世纪之花 / Plantera
            82, // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath
            96, // 击败月亮领主 / Moon Lord
            155, // 击败噬魂幽花 / Polterghast
            185 // 击败神明吞噬者 / Devourer of Gods
        };

        // 右键基础伤害。数组顺序与 StageNames 完全一致。
        private static readonly int[] RightClickBaseDamage =
        {
            10, // 初始 / Initial
            12, // 击败克苏鲁之眼 / Eye of Cthulhu
            17, // 击败蜂王 / Queen Bee
            27, // 击败血肉墙 / Wall of Flesh
            40, // 击败任意机械 Boss / Any Mechanical Boss
            54, // 击败世纪之花 / Plantera
            82, // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath
            96, // 击败月亮领主 / Moon Lord
            155, // 击败噬魂幽花 / Polterghast
            185 // 击败神明吞噬者 / Devourer of Gods
        };

        public int GetCompletedStageIndex() => BlossomFluxProgression.StageIndex;

        public int GetLeftClickBaseDamage() => GetValueForStage(LeftClickBaseDamage, GetCompletedStageIndex());

        public int GetRightClickBaseDamage() => GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());

        private static int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            // ### 歼灭普攻
            // 歼灭普攻从空中降下轰炸箭，下面这些就是平衡组常用调整项。
            //
            // 初始 / Initial：每次降下 4 支箭，发射间隔 20 帧，每支箭爆炸 1 次。
            int minCount = 4;
            int maxCount = 4;
            int interval = 20;
            int explosionLimit = 1;
            float radius = 1f;
            float speed = 1f;

            // 击败世纪之花 / Plantera：最大箭数变为 5。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                maxCount = 5;

            // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath：固定 5 支箭，发射间隔变为 17 帧。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
            {
                minCount = 5;
                maxCount = 5;
                interval = 17;
            }

            // 击败月亮领主 / Moon Lord：5-6 支箭，发射间隔变为 16 帧，每支箭爆炸 2 次。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                minCount = 5;
                maxCount = 6;
                interval = 16;
                explosionLimit = 2;
            }

            // 击败噬魂幽花 / Polterghast：爆炸范围变为 1.25 倍，基础弹速变为 1.18 倍。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                radius = 1.25f;
                speed = 1.18f;
            }

            // 击败神明吞噬者 / Devourer of Gods：固定 6 支箭，发射间隔变为 14 帧，每支箭爆炸 3 次。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                minCount = 6;
                maxCount = 6;
                interval = 14;
                explosionLimit = 3;
            }

            // -------------------- 内部返回结构 --------------------
            return new BFBombardLeftStats(minCount, maxCount, interval, explosionLimit, radius, speed);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal readonly struct BFBombardRightStats
    {
        public readonly int ChargeFrames;
        public readonly float ExplosionSize;
        public readonly float SkyRainMultiplier;

        public BFBombardRightStats(int chargeFrames, float explosionSize, float skyRainMultiplier)
        {
            ChargeFrames = chargeFrames;
            ExplosionSize = explosionSize;
            SkyRainMultiplier = skyRainMultiplier;
        }
    }

    internal static class BFBombardRightBalance
    {
        public static BFBombardRightStats GetStats()
        {
            float size = 190f;
            float skyRainMultiplier = 1f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                size += 55f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
                size += 55f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
                skyRainMultiplier = 1.5f;

            return new BFBombardRightStats(60, size, skyRainMultiplier);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int useTime = 15;
            int useInterval = 15;
            float shotsPerSecond = 4f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.EyeOfCthulhu))
            {
                useTime = 10;
                useInterval = 10;
                shotsPerSecond = 6f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.QueenBee))
            {
                useTime = 12;
                useInterval = 6;
                shotsPerSecond = 10f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                useTime = 12;
                useInterval = 4;
                shotsPerSecond = 15f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
            {
                useTime = 15;
                useInterval = 3;
                shotsPerSecond = 20f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
            {
                useTime = 10;
                useInterval = 2;
                shotsPerSecond = 30f;
            }

            return new BFBreakthroughLeftStats(useTime, useInterval, shotsPerSecond, GetProjectileSpeedMultiplier());
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
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int framesPerArrow = 45;
            int maxArrows = 3;
            int penetrate = 5;
            bool noFalloff = true;
            float speedMult = 1f;
            float damagePerChargeStack = 0f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.EyeOfCthulhu))
                framesPerArrow = 40;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.QueenBee))
                maxArrows = 4;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                framesPerArrow = 35;
                maxArrows = 5;
                penetrate = 9;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                maxArrows = 6;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                penetrate = 15;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                maxArrows = 7;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                penetrate = -1;
                noFalloff = true;
                framesPerArrow = 30;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                penetrate = -1;
                speedMult = 1.65f;
                damagePerChargeStack = 0.05f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                speedMult = 2.15f;
                framesPerArrow = 24;
            }

            return new BFBreakthroughRightStats(framesPerArrow, maxArrows, penetrate, noFalloff, speedMult, damagePerChargeStack);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int initial = 10 * 60;
            int stack = 5 * 60;
            int max = 30 * 60;
            bool betsysCurse = false;
            bool astral = false;
            bool wither = false;
            bool whisper = false;
            bool absorber = false;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                initial = 15 * 60;
                stack = 10 * 60;
                max = 50 * 60;
                betsysCurse = true;
                astral = true;
                wither = true;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                initial = 20 * 60;
                stack = 15 * 60;
                max = 70 * 60;
                whisper = true;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                initial = 30 * 60;
                stack = 20 * 60;
                max = 90 * 60;
                absorber = true;
            }

            return new BFPlagueLeftStats(initial, stack, max, betsysCurse, astral, wither, whisper, absorber);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int stacks = 1;
            float markMultiplier = 1f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                markMultiplier = 1.5f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
                stacks = 2;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                stacks = 3;
                markMultiplier = 2f;
            }

            return new BFPlagueRightStats(stacks, 15, 0.05f, markMultiplier);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal static class BFReconLeftBalance
    {
        // ### 侦察普攻
        // 命中标记持续 30 帧。
        public const int MarkDuration = 30;

        // 追踪优化：缩短启动延迟，提高转向响应，让弹幕能绕更小的弧追踪，但不做锐角折返。
        public const int HomingDelayFrames = 18;
        public const float HomingTurnResponsiveness = 0.22f;
        public const float PriorityHomingTurnResponsiveness = 0.34f;
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int chargeFrames = 90;
            int markDuration = 15 * 60;
            int effectTier = 0;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                markDuration = 20 * 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                chargeFrames = 75;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                markDuration = 25 * 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                chargeFrames = 60;
                effectTier = 1;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
                markDuration = 30 * 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                chargeFrames = 45;
                effectTier = 2;
            }

            return new BFReconRightStats(chargeFrames, markDuration, effectTier);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            // ### 复苏普攻
            // 复苏普攻每轮发射 4 组，然后停顿片刻。停顿按成长均匀缩短：
            // 初始 / Initial：一轮后停顿 60 帧
            // 击败血肉墙 / Wall of Flesh：一轮后停顿 40 帧
            // 击败任意机械 Boss / Any Mechanical Boss：一轮后停顿 20 帧
            // 击败世纪之花 / Plantera：一轮后停顿 0 帧
            int volleyPauseFrames = 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
                volleyPauseFrames = 40;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                volleyPauseFrames = 20;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                volleyPauseFrames = 0;

            // ### 复苏命中与生态增益
            // 以下数值是平衡组常用调整项：治疗量、绿叶持续时间、防御、生命恢复和减伤。
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

            // -------------------- 内部固定参数 --------------------
            // 闪现冷却、标记闪现冷却、闪现窗口、窗口上限、单次绿叶时间。
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
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
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
            int chargeFrames = 5 * 60;
            int flashCount = 7;
            int heal = 10;
            float chargeDr = 0f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                heal = 12;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                chargeFrames = 4 * 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                heal = 15;

            if (DownedBossSystem.downedYharon)
            {
                heal = 20;
                chargeFrames = 3 * 60;
                chargeDr = 0.30f;
            }

            return new BFRecoveryRightStats(chargeFrames, flashCount, heal, chargeDr);
        }
    }
}

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal enum BlossomFluxProgressionStage
    {
        Start = 0,
        EyeOfCthulhu = 1,
        QueenBee = 2,
        WallOfFlesh = 3,
        MechBoss = 4,
        Plantera = 5,
        PlaguebringerGoliath = 6,
        MoonLord = 7,
        Polterghast = 8,
        DevourerOfGods = 9
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

            if (NPC.downedQueenBee)
                stage = BlossomFluxProgressionStage.QueenBee;

            if (Main.hardMode)
                stage = BlossomFluxProgressionStage.WallOfFlesh;

            if (DownedAnyMechBoss())
                stage = BlossomFluxProgressionStage.MechBoss;

            if (NPC.downedPlantBoss)
                stage = BlossomFluxProgressionStage.Plantera;

            if (DownedBossSystem.downedPlaguebringer)
                stage = BlossomFluxProgressionStage.PlaguebringerGoliath;

            if (NPC.downedMoonlord)
                stage = BlossomFluxProgressionStage.MoonLord;

            if (DownedBossSystem.downedPolterghast)
                stage = BlossomFluxProgressionStage.Polterghast;

            if (DownedBossSystem.downedDoG)
                stage = BlossomFluxProgressionStage.DevourerOfGods;

            return stage;
        }
    }
}
