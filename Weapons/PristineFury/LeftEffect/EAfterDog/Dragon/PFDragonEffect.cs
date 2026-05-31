using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFDragonEffect
    {
        private static readonly SoundStyle S12KFireSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/S12K开火")
        {
            Volume = 0.82f,
            PitchVariance = 0.05f,
            MaxInstances = 6
        };

        private const int FireInterval = 14;
        private const int PelletCount = 5;
        private const float Fan = 0.26f;
        private const float FireSpeed = 12.8f;
        private const float DamageMultiplier = 0.48f;
        private const float Recoil = 15f;

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
            FireDragonBreath(holdout);
        }

        private static void FireDragonBreath(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 muzzle = holdout.GunTipPosition + direction * 16f;
            int damage = holdout.GetScaledDamage(DamageMultiplier);

            for (int i = 0; i < PelletCount; i++)
            {
                float ratio = PelletCount == 1 ? 0.5f : i / (float)(PelletCount - 1);
                float spread = MathHelper.Lerp(-Fan, Fan, ratio) + Main.rand.NextFloat(-0.085f, 0.085f);
                float speed = FireSpeed * Main.rand.NextFloat(0.72f, 1.28f);
                Vector2 velocity = direction.RotatedBy(spread) * speed;
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + direction * (i * 2f),
                    velocity,
                    ModContent.ProjectileType<PFDragon_Flame>(),
                    damage,
                    holdout.Projectile.knockBack * 0.65f,
                    holdout.Projectile.owner,
                    i,
                    holdout.LeftBurstIndex + Main.rand.NextFloat(1000f));

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.LeftBurstIndex += PelletCount;
            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 1.18f);
            SoundEngine.PlaySound(S12KFireSound, muzzle);
        }
    }
}
