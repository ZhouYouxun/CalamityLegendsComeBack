using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFGoliathEffect
    {
        private const int SalvoCount = 9;
        private const int ShotSpacing = 3;
        private const int SalvoCooldown = 20;
        private const float FireSpeed = 10.8f;
        private const float DamageMultiplier = 0.56f;
        private const float Recoil = 3.8f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (holdout.LeftChargeTimer > 0)
            {
                holdout.LeftChargeTimer--;
                return;
            }

            if (holdout.LeftAuxTimer <= 0)
            {
                holdout.LeftAuxTimer = SalvoCount;
                holdout.LeftTimer = ShotSpacing;
                SpawnCrosshair(holdout);
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < ShotSpacing)
                return;

            holdout.LeftTimer = 0;
            holdout.LeftAuxTimer--;
            FireMissile(holdout);

            if (holdout.LeftAuxTimer <= 0)
                holdout.LeftChargeTimer = SalvoCooldown;
        }

        private static void SpawnCrosshair(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.myPlayer != holdout.Projectile.owner)
                return;

            int crosshair = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GetMouseWorld(),
                Vector2.Zero,
                ModContent.ProjectileType<PFGoliath_MouseCrosshair>(),
                0,
                0f,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(crosshair, holdout.CurrentMark);
        }

        private static void FireMissile(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 target = holdout.GetMouseWorld();
            Vector2 direction = holdout.AimDirection.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f));
            Vector2 velocity = direction * FireSpeed + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.6f, 1.6f);
            int missile = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 16f,
                velocity,
                ModContent.ProjectileType<PFGoliath_HiveNukeMissile>(),
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                target.X,
                target.Y);
            PFLeftEffectRules.ApplyTheme(missile, holdout.CurrentMark);
            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(14);
            holdout.SpawnMuzzleBurst(new Color(139, 242, 73), 0.86f);
        }
    }
}
