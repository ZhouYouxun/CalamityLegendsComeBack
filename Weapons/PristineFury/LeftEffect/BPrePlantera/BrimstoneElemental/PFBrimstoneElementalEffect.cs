using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFBrimstoneElementalEffect
    {
        private const int ChargeFrames = 45;
        private const float FireSpeed = 18.2f;
        private const float DamageMultiplier = 1.55f;
        private const float Recoil = 11f;

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
            if (holdout.LeftTimer % 5 == 0)
                holdout.SpawnMuzzleBurst(new Color(246, 55, 64), 0.5f + holdout.LeftChargeTimer / 110f);
        }

        private static void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFBrimstoneElemental_Flame>(),
                FireSpeed,
                0.025f,
                DamageMultiplier,
                Recoil,
                20,
                new Color(246, 55, 64),
                1.25f,
                20f);
        }
    }
}
