namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal readonly struct BFBreakthroughRightStats
    {
        public readonly int FramesPerArrow;
        public readonly int MaxLoadedArrows;
        public readonly int Penetrate;
        public readonly bool IgnorePenetrationDamageFalloff;
        public readonly float ProjectileSpeedMultiplier;

        public BFBreakthroughRightStats(int framesPerArrow, int maxLoadedArrows, int penetrate, bool ignorePenetrationDamageFalloff, float projectileSpeedMultiplier)
        {
            FramesPerArrow = framesPerArrow;
            MaxLoadedArrows = maxLoadedArrows;
            Penetrate = penetrate;
            IgnorePenetrationDamageFalloff = ignorePenetrationDamageFalloff;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
        }
    }

    internal static class BFBreakthroughRightBalance
    {
        public static BFBreakthroughRightStats GetStats()
        {
            int framesPerArrow = 45;
            int maxArrows = 3;
            int penetrate = 4;
            bool noFalloff = false;
            float speedMult = 1f;

            if (BlossomFluxProgression.DownedAnyBossOrMiniboss())
                penetrate = 5;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.EyeOfCthulhu))
                framesPerArrow = 40;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.QueenBee))
                maxArrows = 4;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                framesPerArrow = 35;
                maxArrows = 5;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                maxArrows = 6;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                penetrate = 7;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                maxArrows = 7;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                penetrate = 15;
                noFalloff = true;
                framesPerArrow = 30;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                penetrate = -1;
                speedMult = 1.65f;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
                speedMult = 2.15f;

            return new BFBreakthroughRightStats(framesPerArrow, maxArrows, penetrate, noFalloff, speedMult);
        }
    }
}
