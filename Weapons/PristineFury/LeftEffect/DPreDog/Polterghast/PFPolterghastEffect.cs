using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPolterghastEffect
    {
        private const int AccelerationFrames = 180;
        private const int SlowInterval = 5;
        private const int FastInterval = 2;
        private const float FireSpeed = 20.3f;
        private const float DamageMultiplier = 0.36f;
        private const float Recoil = 1.8f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftChargeTimer = System.Math.Min(AccelerationFrames, holdout.LeftChargeTimer + 1);
            int interval = (int)System.MathF.Round(MathHelper.Lerp(SlowInterval, FastInterval, holdout.LeftChargeTimer / (float)AccelerationFrames));
            holdout.LeftTimer++;
            if (holdout.LeftTimer < interval)
                return;

            holdout.LeftTimer = 0;

            FireMachineGunShot(holdout);
        }

        private static void FireMachineGunShot(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            float progress = holdout.LeftChargeTimer / (float)AccelerationFrames;
            float spread = MathHelper.Lerp(MathHelper.ToRadians(2.4f), MathHelper.ToRadians(0.35f), progress);
            Vector2 muzzle = holdout.GunTipPosition + direction * 18f;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            float laneOffset = ((holdout.LeftBurstIndex & 1) == 0 ? -1f : 1f) * MathHelper.Lerp(2.6f, 1f, progress);

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                muzzle + side * laneOffset,
                direction.RotatedBy(Main.rand.NextFloat(-spread, spread)) * FireSpeed,
                    ModContent.ProjectileType<PFPolterghast_Flame>(),
                    holdout.GetScaledDamage(DamageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner);
                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            holdout.LeftBurstIndex++;

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(7);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.48f);
        }
    }
}
