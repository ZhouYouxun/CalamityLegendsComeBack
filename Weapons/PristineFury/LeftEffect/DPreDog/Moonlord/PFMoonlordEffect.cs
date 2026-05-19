using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFMoonlordEffect
    {
        private const int FireInterval = 22;
        private const float FireSpeed = 8f;
        private const float Spread = 0.04f;
        private const float DamageMultiplier = 0.74f;
        private const float Recoil = 5.8f;

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
                ModContent.ProjectileType<PFMoonlord_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                16,
                Color.LightGreen,
                0.9f,
                15f);
        }
    }
}
