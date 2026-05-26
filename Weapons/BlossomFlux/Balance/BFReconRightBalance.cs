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
