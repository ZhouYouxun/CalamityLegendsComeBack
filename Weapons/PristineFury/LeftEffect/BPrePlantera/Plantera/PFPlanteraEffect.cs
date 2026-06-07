using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPlanteraEffect
    {
        private const int FireInterval = 13;
        private const int OrbCount = 3;

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
            FireLightOrbs(holdout);
        }

        private static void FireLightOrbs(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < OrbCount; i++)
            {
                float spread = MathHelper.Lerp(-0.14f, 0.14f, i / (float)(OrbCount - 1));
                Vector2 spawnPosition = holdout.GunTipPosition + direction * 14f + side * ((i - 1) * 7f);
                Vector2 velocity = direction.RotatedBy(spread + Main.rand.NextFloat(-0.025f, 0.025f)) * Main.rand.NextFloat(5.6f, 6.8f);
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<PFPlantera_Flame>(),
                    holdout.GetScaledDamage(0.82f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    i);
                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.ApplyRecoil(10f);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(new Color(74, 255, 92), 1.15f);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.62f, Pitch = 0.18f }, holdout.GunTipPosition);
        }
    }
}
