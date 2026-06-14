using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFIdleEffect
    {
        private const int FireInterval = 4;
        private const float FireSpeed = 9.6f;
        private const float Spread = 0.08f;
        private const float DamageMultiplier = 0.78f;
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
                PristineFuryMarkHelper.GetColor(holdout.CurrentMark),
                0.62f,
                14f);

            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.55f, Pitch = 0.12f, PitchVariance = 0.1f }, holdout.GunTipPosition);
        }
    }
}
