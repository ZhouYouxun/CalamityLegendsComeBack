using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFIdleEffect
    {
        private const int FireInterval = 3;
        private const float FireSpeed = 27f;
        private const float Spread = 0f;
        private const float DamageMultiplier = 0.58f;
        private const float Recoil = 2.2f;

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
                PristineFuryMarkHelper.GetColor(holdout.CurrentMark),
                0.62f,
                14f);
        }
    }
}
