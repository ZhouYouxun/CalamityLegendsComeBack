using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFAuroraEffect
    {
        private const int FireInterval = 5;
        private const float FireSpeed = 7.5f;
        private const float Spread = 0.08f;
        private const float DamageMultiplier = 0.5f;
        private const float Recoil = 4.6f;

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
                ModContent.ProjectileType<PFAurora_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                12,
                new Color(126, 210, 255),
                0.82f,
                15f);
        }
    }
}
