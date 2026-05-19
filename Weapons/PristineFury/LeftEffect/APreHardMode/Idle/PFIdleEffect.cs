using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFIdleEffect
    {
        private const int FireInterval = 3;
        private const float FireSpeed = 11.6f;
        private const float Spread = 0.035f;
        private const float DamageMultiplier = 0.46f;
        private const float Recoil = 2.8f;

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
                ModContent.ProjectileType<PFIdle_Flame>(),
                FireSpeed,
                Spread,
                DamageMultiplier,
                Recoil,
                9,
                new Color(255, 146, 62),
                0.62f,
                14f);
        }
    }
}
