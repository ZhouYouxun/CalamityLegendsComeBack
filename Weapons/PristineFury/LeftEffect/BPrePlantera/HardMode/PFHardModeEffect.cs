using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFHardModeEffect
    {
        private const int FireInterval = 40;

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
                ModContent.ProjectileType<PFHardMode_HeavyFireball>(),
                10.5f,
                0.035f,
                1.65f,
                16f,
                22,
                PristineFuryMarkHelper.GetColor(holdout.CurrentMark),
                1.3f,
                20f);

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.85f, Pitch = -0.15f, PitchVariance = 0.12f }, holdout.GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.65f, Pitch = 0.15f, PitchVariance = 0.1f }, holdout.GunTipPosition);
        }
    }
}
