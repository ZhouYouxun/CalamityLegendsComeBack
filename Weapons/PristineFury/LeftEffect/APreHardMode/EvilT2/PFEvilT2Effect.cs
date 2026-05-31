using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEvilT2Effect
    {
        private const int BurstCount = 6;
        private const int BurstCooldown = 20;
        private const int ShotInterval = 3;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            int requiredDelay = holdout.LeftAuxTimer <= 0 ? BurstCooldown : ShotInterval;
            if (holdout.LeftTimer < requiredDelay)
                return;

            holdout.LeftTimer = 0;
            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFEvilT2_Flame>(),
                10.8f,
                0.045f,
                0.82f,
                5.2f,
                14,
                PristineFuryMarkHelper.GetColor(holdout.CurrentMark),
                0.9f,
                16f);

            holdout.LeftAuxTimer++;
            if (holdout.LeftAuxTimer >= BurstCount)
                holdout.LeftAuxTimer = 0;
        }
    }
}
