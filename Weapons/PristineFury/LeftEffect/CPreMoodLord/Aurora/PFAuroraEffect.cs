using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFAuroraEffect
    {
        private const float DamageMultiplier = 3.2f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            EnsureMuzzleOrb(holdout);
            holdout.LeftTimer++;
        }

        private static void EnsureMuzzleOrb(NewLegendPristineFuryHoldOut holdout)
        {
            int orbType = ModContent.ProjectileType<PFAurora_MuzzleOrb>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != holdout.Projectile.owner || projectile.type != orbType || (int)projectile.ai[0] != holdout.Projectile.whoAmI)
                    continue;

                projectile.timeLeft = 2;
                projectile.damage = holdout.GetScaledDamage(DamageMultiplier);
                projectile.knockBack = holdout.Projectile.knockBack;
                PFLeftEffectRules.ApplyTheme(projectile.whoAmI, holdout.CurrentMark);
                return;
            }

            int orb = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + holdout.AimDirection * 7f,
                Vector2.Zero,
                orbType,
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI);
            PFLeftEffectRules.ApplyTheme(orb, holdout.CurrentMark);
        }
    }
}
