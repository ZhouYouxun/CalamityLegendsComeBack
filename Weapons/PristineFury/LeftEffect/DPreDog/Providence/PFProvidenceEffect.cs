using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFProvidenceEffect
    {
        private const int PrimaryInterval = 3;
        private const int SecondaryInterval = 24;
        private const float PrimarySpeed = 11.2f;
        private const float SecondarySpeed = 4.2f;
        private const float DamageMultiplier = 1.18f;
        private const float Recoil = 4.2f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            holdout.LeftAuxTimer++;

            if (holdout.LeftAuxTimer >= SecondaryInterval)
            {
                holdout.LeftAuxTimer = 0;
                FireSecondary(holdout);
            }

            if (holdout.LeftTimer < PrimaryInterval)
                return;

            holdout.LeftTimer = 0;
            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFProvidence_Flame>(),
                PrimarySpeed,
                0.035f,
                DamageMultiplier,
                Recoil,
                12,
                new Color(255, 220, 118),
                0.92f,
                15f,
                0f);
        }

        private static void FireSecondary(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.RotatedBy(Main.rand.NextFloat(-0.14f, 0.14f));
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 18f,
                direction * SecondarySpeed,
                ModContent.ProjectileType<PFProvidence_Flame>(),
                holdout.GetScaledDamage(DamageMultiplier * 0.32f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                1f,
                holdout.LeftBurstIndex++);
            holdout.SpawnMuzzleBurst(new Color(255, 220, 118), 0.72f);
        }
    }
}
