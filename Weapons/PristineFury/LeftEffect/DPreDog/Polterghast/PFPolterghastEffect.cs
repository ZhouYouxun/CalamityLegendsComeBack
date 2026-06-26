using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPolterghastEffect
    {
        private const int AccelerationFrames = 180;
        private const int SlowInterval = 10;
        private const int FastInterval = 6;
        private const float DamageMultiplier = 0.60f;
        private const float Recoil = 3.4f;

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
            float spread = MathHelper.Lerp(MathHelper.ToRadians(38f), MathHelper.ToRadians(22f), progress);
            Vector2 muzzle = holdout.GunTipPosition + direction * 18f;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            int shotCount = Main.rand.Next(3, 5);

            // Polterghast mark visuals must reference Alpha Draconis, Galileo Gladius,
            // Halley's Inferno, Vega, and Crescent Moon: same source family, different shape.
            for (int i = 0; i < shotCount; i++)
            {
                float laneOffset = Main.rand.NextFloat(-18f, 18f) + ((holdout.LeftBurstIndex & 1) == 0 ? -1f : 1f) * Main.rand.NextFloat(2f, 8f);
                float speed = Main.rand.NextFloat(9f, 24f);
                float homingDelay = Main.rand.NextFloat(10f, 44f);

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + side * laneOffset + Main.rand.NextVector2Circular(5f, 5f),
                    direction.RotatedBy(Main.rand.NextFloat(-spread, spread)) * speed,
                    ModContent.ProjectileType<PFPolterghast_StarBolt>(),
                    holdout.GetScaledDamage(DamageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    homingDelay,
                    holdout.LeftBurstIndex + Main.rand.NextFloat(1000f));
                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
                holdout.LeftBurstIndex++;
            }

            if (holdout.LeftBurstIndex % 3 == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Polterghast/PolterghastPhantomSpawn") { Volume = 0.38f, Pitch = 0.2f, PitchVariance = 0.1f }, holdout.GunTipPosition);
            }

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(7);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.72f);
        }
    }
}
