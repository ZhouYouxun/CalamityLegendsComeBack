using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPrimeEffect
    {
        private const int FireInterval = 4;
        private const float FireSpeed = 7.25f;
        private const float Spread = 0.035f;
        private const float DamageMultiplier = 0.62f;
        private const float Recoil = 4.8f;

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
                ModContent.ProjectileType<PFPrime_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                12,
                new Color(255, 206, 92),
                0.86f,
                15f);

            SoundEngine.PlaySound(SoundID.Item31 with { Volume = 0.42f, Pitch = 0.35f, PitchVariance = 0.15f }, holdout.GunTipPosition);
        }
    }
}
