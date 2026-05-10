using CalamityMod;

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
            int flashCount = 4;
            int heal = 10;
            float chargeDr = 0f;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                flashCount = 5;
                heal = 12;
            }

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                flashCount = 6;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                flashCount = 7;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                chargeFrames = 4 * 60;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
                heal = 15;

            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
                flashCount = 9;

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
