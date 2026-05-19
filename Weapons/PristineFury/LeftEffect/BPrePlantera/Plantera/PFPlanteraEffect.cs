using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPlanteraEffect
    {
        private const int ChargeFrames = 52;
        private const int BloomCount = 4;
        private const float Fan = 0.36f;
        private const float FireSpeed = 11.6f;
        private const float DamageMultiplier = 0.94f;
        private const float Recoil = 10.5f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                if (justReleased && holdout.LeftChargeTimer >= ChargeFrames)
                    Fire(holdout);

                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftChargeTimer++;
            holdout.LeftTimer++;
            if (holdout.LeftTimer % 8 == 0)
                holdout.SpawnMuzzleBurst(new Color(94, 242, 108), 0.45f + holdout.LeftChargeTimer / (float)ChargeFrames);
        }

        private static void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            PFLeftEffectRules.FireSpread(
                holdout,
                ModContent.ProjectileType<PFPlantera_Flame>(),
                BloomCount,
                Fan,
                FireSpeed,
                0.35f,
                DamageMultiplier,
                Recoil,
                18,
                new Color(94, 242, 108),
                1.1f,
                18f);
        }
    }
}
