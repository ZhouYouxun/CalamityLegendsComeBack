using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEvilT2Effect
    {
        private const int FireInterval = 10;
        private const float FireSpeed = 8.8f;
        private const float Spread = 0.08f;
        private const float DamageMultiplier = 0.54f;
        private const float Recoil = 3.8f;

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
                ModContent.ProjectileType<PFEvilT2_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                12,
                new Color(111, 38, 164),
                0.76f,
                13f);
        }
    }
}
