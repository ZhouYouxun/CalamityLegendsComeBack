using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFSlimeGodEffect
    {
        private const int FireInterval = 28;
        private const int MinBurstCount = 6;
        private const int MaxBurstCount = 8;
        private const float Fan = MathHelper.Pi / 12f;
        private const float FireSpeed = 5.6f;
        private const float DamageMultiplier = 0.72f;
        private const float Recoil = 7.4f;

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
            FireRandomBurst(holdout);
        }

        private static void FireRandomBurst(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 baseDirection = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + baseDirection * 16f;
            int count = Main.rand.Next(MinBurstCount, MaxBurstCount + 1);
            int projectileType = ModContent.ProjectileType<PFSlimeGod_Flame>();

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = baseDirection.RotatedBy(Main.rand.NextFloat(-Fan, Fan));
                float speed = FireSpeed * Main.rand.NextFloat(0.25f, 0.65f);
                Vector2 spawnOffset = baseDirection * Main.rand.NextFloat(0f, 8f);

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + spawnOffset,
                    direction * speed,
                    projectileType,
                    holdout.GetScaledDamage(DamageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    i,
                    holdout.LeftBurstIndex + i);

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.LeftBurstIndex += count;
            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(16);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.95f);
        }
    }
}
