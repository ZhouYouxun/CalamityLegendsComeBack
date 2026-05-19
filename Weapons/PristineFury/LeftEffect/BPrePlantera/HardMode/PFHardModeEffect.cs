using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFHardModeEffect
    {
        private const int FireInterval = 6;
        private const float FireSpeed = 9.8f;
        private const float Spread = 0.12f;
        private const float DamageMultiplier = 0.82f;
        private const float Recoil = 5.4f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFHardMode_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                14,
                Color.DeepSkyBlue,
                0.9f,
                15f);
        }
    }
}
