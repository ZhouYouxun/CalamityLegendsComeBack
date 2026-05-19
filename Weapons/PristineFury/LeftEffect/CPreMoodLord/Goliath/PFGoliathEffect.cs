using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFGoliathEffect
    {
        private const int FireInterval = 19;
        private const float FireSpeed = 7.4f;
        private const float Spread = 0.09f;
        private const float DamageMultiplier = 0.62f;
        private const float Recoil = 5.2f;

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
                ModContent.ProjectileType<PFGoliath_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                14,
                new Color(139, 242, 73),
                0.92f,
                15f);
        }
    }
}
