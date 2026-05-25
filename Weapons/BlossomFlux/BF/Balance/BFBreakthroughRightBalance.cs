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
            bool noFalloff = false;
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
