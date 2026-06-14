using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEvilT2Effect
    {
        private const int FireInterval = 18;

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
                4.8f,
                0.075f,
                0.98f,
                4.8f,
                14,
                PristineFuryMarkHelper.GetColor(holdout.CurrentMark),
                0.9f,
                16f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/BrainOfCthulhu/BoC_Rev_BloodShot") { Volume = 0.65f, PitchVariance = 0.1f }, holdout.GunTipPosition);
        }
    }
}
