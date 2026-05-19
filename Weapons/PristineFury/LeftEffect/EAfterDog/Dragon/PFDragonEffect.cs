using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFDragonEffect
    {
        private const int FireInterval = 12;
        private const int BurstCount = 2;
        private const float Fan = 0.18f;
        private const float FireSpeed = 7.2f;
        private const float DamageMultiplier = 0.48f;
        private const float Recoil = 12f;

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
            PFLeftEffectRules.FireSpread(
                holdout,
                ModContent.ProjectileType<PFDragon_Flame>(),
                BurstCount,
                Fan,
                FireSpeed,
                1.1f,
                DamageMultiplier,
                Recoil,
                22,
                new Color(255, 108, 50),
                1.15f,
                18f);
        }
    }
}
