using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPlanteraEffect
    {
        private const int FireInterval = 3;
        private const float FireSpeed = 15.2f;
        private const float Spread = 0.045f;
        private const float DamageMultiplier = 0.62f;
        private const float Recoil = 3.2f;

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
                ModContent.ProjectileType<PFPlantera_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                10,
                new Color(94, 242, 108),
                0.72f,
                15f);
        }
    }
}
