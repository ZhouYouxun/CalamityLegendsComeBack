using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPolterghastEffect
    {
        private const int AccelerationFrames = 300;
        private const int SlowInterval = 8;
        private const int FastInterval = 3;
        private const float FireSpeed = 15.5f;
        private const float DamageMultiplier = 0.58f;
        private const float Recoil = 3.8f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftChargeTimer = System.Math.Min(AccelerationFrames, holdout.LeftChargeTimer + 1);
            int interval = (int)System.MathF.Round(MathHelper.Lerp(SlowInterval, FastInterval, holdout.LeftChargeTimer / (float)AccelerationFrames));
            holdout.LeftTimer++;
            if (holdout.LeftTimer < interval)
                return;

            holdout.LeftTimer = 0;

            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFPolterghast_Flame>(),
                FireSpeed,
                0.03f,
                DamageMultiplier,
                Recoil,
                10,
                Color.DodgerBlue,
                0.75f,
                16f);
        }
    }
}
