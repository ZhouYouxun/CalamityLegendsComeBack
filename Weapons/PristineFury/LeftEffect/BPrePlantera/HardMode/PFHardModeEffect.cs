using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFHardModeEffect
    {
        private const int FireInterval = 5;
        private const int CatCount = 3;
        private const float FireSpeed = 12.8f;
        private const float Fan = 0.18f;
        private const float DamageMultiplier = 0.48f;
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
            PFLeftEffectRules.FireSpread(
                holdout,
                ModContent.ProjectileType<PFHardMode_MeowCreature>(),
                CatCount,
                Fan,
                FireSpeed,
                0.35f,
                DamageMultiplier,
                Recoil,
                14,
                new Color(255, 142, 66),
                0.9f,
                15f);
        }
    }
}
