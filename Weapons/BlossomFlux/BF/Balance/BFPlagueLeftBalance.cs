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
