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
            float damageReduction)
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

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                heal = 7;
                maxTime = 15 * 60;
                defense = 8;
                regen = 3;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
            {
                defense = 12;
                regen = 5;
                damageReduction = 0.10f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
            {
                heal = 10;
                maxTime = 20 * 60;
                defense = 15;
                regen = 7;
                regenTime = 4;
                damageReduction = 0.12f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                regen = 20;
                missingQuarterRegen = 10;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                heal = 15;
                maxTime = 25 * 60;
                defense = 20;
                damageReduction = 0.10f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                maxTime = 30 * 60;
                defense = 25;
                damageReduction = 0.20f;
            }

            // -------------------- 内部固定参数 --------------------
            // 闪现冷却、标记闪现冷却、闪现窗口、窗口上限、单次绿叶时间。
            return new BFRecoveryLeftStats(heal, 120, 75, 5 * 60, 2, 5 * 60, maxTime, volleyPauseFrames, defense, regen, missingQuarterRegen, regenTime, damageReduction);
        }
    }
}
