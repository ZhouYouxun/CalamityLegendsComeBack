using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPolterghastEffect
    {
        private const int BurstCooldown = 39;
        private const int BurstCount = 5;
        private const int BurstSpacing = 5;
        private const float FireSpeed = 12f;
        private const float DamageMultiplier = 0.58f;
        private const float Recoil = 4.4f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (holdout.LeftChargeTimer > 0)
            {
                holdout.LeftChargeTimer--;
                return;
            }

            if (holdout.LeftAuxTimer <= 0)
            {
                holdout.LeftAuxTimer = BurstCount;
                holdout.LeftTimer = 0;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < BurstSpacing)
                return;

            holdout.LeftTimer = 0;
            holdout.LeftAuxTimer--;

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

            if (holdout.LeftAuxTimer <= 0)
                holdout.LeftChargeTimer = BurstCooldown;
        }
    }
}
