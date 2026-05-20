using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFMoonlordEffect
    {
        private const int FireInterval = 15;
        private const int NebulaBlaze1ID = 634;
        private const int NebulaBlaze2ID = 635;
        private const int StardustSoldierLaserID = 537;
        private const float Recoil = 5.8f;

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

            switch (holdout.LeftBurstIndex++ % 4)
            {
                case 0:
                    FireSolar(holdout);
                    break;
                case 1:
                    FireVortex(holdout);
                    break;
                case 2:
                    FireNebula(holdout);
                    break;
                default:
                    FireStardust(holdout);
                    break;
            }
        }

        private static void FireSolar(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 12f;
            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction,
                ModContent.ProjectileType<PFMoonlord_SolarLaser>(),
                holdout.GetScaledDamage(1.05f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);

            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            ApplyShotFeedback(holdout, new Color(255, 184, 54), 1.08f, Recoil + 1.6f, SoundID.Item33);
        }

        private static void FireVortex(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 15f;
            const int count = 4;
            const float fan = 0.42f;

            for (int i = 0; i < count; i++)
            {
                float ratio = count == 1 ? 0.5f : i / (float)(count - 1);
                float rotation = MathHelper.Lerp(-fan, fan, ratio);
                Vector2 velocity = direction.RotatedBy(rotation) * 13.5f;

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + direction * (i * 3f),
                    velocity,
                    ModContent.ProjectileType<PFMoonlord_VortexScorpioRocket>(),
                    holdout.GetScaledDamage(0.58f),
                    holdout.Projectile.knockBack * 0.8f,
                    holdout.Projectile.owner,
                    13.5f,
                    i);

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            ApplyShotFeedback(holdout, new Color(78, 255, 170), 0.92f, Recoil, SoundID.Item11);
        }

        private static void FireNebula(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 14f;
            for (int i = 0; i < 3; i++)
            {
                float spread = MathHelper.Lerp(-0.14f, 0.14f, i / 2f);
                int type = i == 1 ? NebulaBlaze2ID : NebulaBlaze1ID;
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle,
                    direction.RotatedBy(spread) * 12.2f,
                    type,
                    holdout.GetScaledDamage(0.54f),
                    holdout.Projectile.knockBack * 0.6f,
                    holdout.Projectile.owner);

                NormalizeVanillaProjectile(projectileIndex);
            }

            ApplyShotFeedback(holdout, new Color(255, 98, 230), 0.92f, Recoil * 0.82f, SoundID.Item88);
        }

        private static void FireStardust(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 16f,
                direction * 16f,
                StardustSoldierLaserID,
                holdout.GetScaledDamage(0.78f),
                holdout.Projectile.knockBack * 0.7f,
                holdout.Projectile.owner);

            NormalizeVanillaProjectile(projectileIndex);
            ApplyShotFeedback(holdout, new Color(100, 220, 255), 0.96f, Recoil, SoundID.Item12);
        }

        private static void NormalizeVanillaProjectile(int projectileIndex)
        {
            if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
                return;

            Projectile projectile = Main.projectile[projectileIndex];
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.DamageType = DamageClass.Ranged;
            projectile.netUpdate = true;
        }

        private static void ApplyShotFeedback(NewLegendPristineFuryHoldOut holdout, Color color, float scale, float recoil, SoundStyle sound)
        {
            holdout.ApplyRecoil(recoil);
            holdout.TriggerMuzzleFlash(14);
            holdout.SpawnMuzzleBurst(color, scale);
            SoundEngine.PlaySound(sound with { Volume = 0.55f, PitchVariance = 0.12f }, holdout.GunTipPosition);
        }
    }
}
