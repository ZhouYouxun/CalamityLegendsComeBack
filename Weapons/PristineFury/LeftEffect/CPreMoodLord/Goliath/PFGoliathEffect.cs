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
        private const float FireSpeed = 16.4f;
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
            int shotIndex = SalvoCount - holdout.LeftAuxTimer;
            holdout.LeftAuxTimer--;
            FireMissile(holdout, shotIndex);

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

        private static void FireMissile(NewLegendPristineFuryHoldOut holdout, int shotIndex)
        {
            Vector2 target = holdout.GetMouseWorld();
            float ratio = SalvoCount == 1 ? 0.5f : shotIndex / (float)(SalvoCount - 1);
            float spread = MathHelper.Lerp(-MathHelper.ToRadians(135f), MathHelper.ToRadians(135f), ratio);
            Vector2 direction = holdout.AimDirection.RotatedBy(spread + Main.rand.NextFloat(-0.04f, 0.04f));
            Vector2 velocity = direction * FireSpeed * Main.rand.NextFloat(0.94f, 1.12f);
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
