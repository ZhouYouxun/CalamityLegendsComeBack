using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFMoonlordEffect
    {
        private const int WarmupFrames = 180;
        private const int SuperLaserPauseFrames = 46;
        private const int SlowInterval = 24;
        private const int FastInterval = 4;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (holdout.LeftAuxTimer > 0)
            {
                holdout.LeftAuxTimer--;
                if (holdout.LeftAuxTimer == 0)
                {
                    FireSuperLaser(holdout);
                    holdout.LeftChargeTimer = 0;
                }
                return;
            }

            if (holdout.LeftChargeTimer >= WarmupFrames)
            {
                holdout.LeftAuxTimer = SuperLaserPauseFrames;
                holdout.LeftTimer = 0;
                return;
            }

            holdout.LeftChargeTimer++;
            float warmup = holdout.LeftChargeTimer / (float)WarmupFrames;
            int interval = (int)System.MathF.Round(MathHelper.Lerp(SlowInterval, FastInterval, warmup));
            holdout.LeftTimer++;
            if (holdout.LeftTimer < interval)
                return;

            holdout.LeftTimer = 0;
            FireLunarFlare(holdout);
        }

        private static void FireLunarFlare(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            const int flareCount = 3;
            for (int i = 0; i < flareCount; i++)
            {
                float spread = MathHelper.Lerp(-0.12f, 0.12f, flareCount == 1 ? 0.5f : i / (float)(flareCount - 1));
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    holdout.GunTipPosition + direction * 15f,
                    direction.RotatedBy(spread) * 21f,
                    ModContent.ProjectileType<PFMoonlord_Flame>(),
                    holdout.GetScaledDamage(0.86f),
                    holdout.Projectile.knockBack * 0.6f,
                    holdout.Projectile.owner,
                    i,
                    holdout.LeftBurstIndex + Main.rand.NextFloat(1000f));

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.ApplyRecoil(3.2f);
            holdout.TriggerMuzzleFlash(9);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.7f);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.34f, Pitch = 0.32f }, holdout.GunTipPosition);
        }

        private static void FireSuperLaser(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 18f,
                direction,
                ModContent.ProjectileType<PFMoonlord_SolarLaser>(),
                holdout.GetScaledDamage(5.3f),
                holdout.Projectile.knockBack * 1.8f,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);

            holdout.ApplyRecoil(28f);
            holdout.TriggerMuzzleFlash(30);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 2.8f);
            holdout.Owner.Calamity().GeneralScreenShakePower = Math.Max(holdout.Owner.Calamity().GeneralScreenShakePower, 12f);
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.92f, Pitch = -0.42f }, holdout.GunTipPosition);
        }
    }
}
