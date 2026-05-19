using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFSlimeGodEffect
    {
        private const int FireInterval = 28;
        private const int BurstCount = 3;
        private const float Fan = 0.23f;
        private const float FireSpeed = 5.6f;
        private const float DamageMultiplier = 0.72f;
        private const float Recoil = 7.4f;

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
                ModContent.ProjectileType<PFSlimeGod_Flame>(),
                BurstCount,
                Fan,
                FireSpeed,
                0.25f,
                DamageMultiplier,
                Recoil,
                16,
                new Color(133, 133, 224),
                0.95f,
                16f);
        }
    }
}
