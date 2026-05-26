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
