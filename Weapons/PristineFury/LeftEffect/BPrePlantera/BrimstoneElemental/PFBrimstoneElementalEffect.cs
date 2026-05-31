using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFBrimstoneElementalEffect
    {
        private const int LaserInterval = 120;
        private const float DamageMultiplier = 1.35f;
        private const float Recoil = 8.5f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer >= LaserInterval)
            {
                holdout.LeftTimer = 0;
                Fire(holdout);
            }

            if (holdout.LeftTimer % 8 == 0)
                holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.58f);
        }

        private static void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            int laser = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 18f,
                direction,
                ModContent.ProjectileType<PFBrimstoneElemental_Laser>(),
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(laser, holdout.CurrentMark);

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(20);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 1.3f);
        }
    }
}
