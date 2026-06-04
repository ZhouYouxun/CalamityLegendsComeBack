using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPlanteraEffect
    {
        private const int FireInterval = 8;

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
            FireRecursiveLightning(holdout);
        }

        private static void FireRecursiveLightning(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 14f,
                direction * 7.2f,
                ModContent.ProjectileType<PFPlantera_Flame>(),
                holdout.GetScaledDamage(1.3f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);

            holdout.ApplyRecoil(10f);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(new Color(74, 255, 92), 1.15f);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.62f, Pitch = 0.18f }, holdout.GunTipPosition);
        }
    }
}
