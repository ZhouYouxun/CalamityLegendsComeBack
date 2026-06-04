using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPolterghastEffect
    {
        private const int AccelerationFrames = 300;
        private const int SlowInterval = 8;
        private const int FastInterval = 3;
        private const float FireSpeed = 15.5f;
        private const float DamageMultiplier = 0.58f;
        private const float Recoil = 3.8f;

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

            FireConvergingPair(holdout);
        }

        private static void FireConvergingPair(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            float progress = holdout.LeftChargeTimer / (float)AccelerationFrames;
            float spread = MathHelper.Lerp(MathHelper.ToRadians(10f), MathHelper.ToRadians(1f), progress);

            for (int side = -1; side <= 1; side += 2)
            {
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    holdout.GunTipPosition + direction * 14f + direction.RotatedBy(MathHelper.PiOver2) * side * 3f,
                    direction.RotatedBy(spread * side) * FireSpeed,
                    ModContent.ProjectileType<PFPolterghast_Flame>(),
                    holdout.GetScaledDamage(DamageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner);
                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(10);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.75f);
        }
    }
}
