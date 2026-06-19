using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFDragonEffect
    {
        private static readonly SoundStyle DragonFireSound = new("CalamityMod/Sounds/Custom/Yharon/YharonFireball", 3)
        {
            Volume = 1.44f,
            PitchVariance = 0.22f,
            MaxInstances = 6
        };

        private static readonly SoundStyle FrenzyStartSound = new("CalamityMod/Sounds/Item/DragonsBreathStrongStart")
        {
            Volume = 0.85f,
            Pitch = 0.18f
        };

        private const int WarmupFrames = 120;
        private const int RampFrames = 120;
        private const int FrenzyFrames = 120;
        private const int RestFrames = 70;
        private const int PelletCount = 2;
        private const float Fan = 0.08726646f; // 5 degrees.
        private const float FireSpeed = 15.5f;
        private const float DamageMultiplier = 0.55f;
        private const float Recoil = 15f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (justPressed)
            {
                holdout.LeftTimer = 0;
                holdout.LeftChargeTimer = 0;
                holdout.LeftAuxTimer = 0;
            }

            holdout.LeftChargeTimer++;
            int cycleLength = WarmupFrames + RampFrames + FrenzyFrames + RestFrames;
            if (holdout.LeftChargeTimer >= cycleLength)
            {
                holdout.LeftChargeTimer = 0;
                holdout.LeftTimer = 0;
                holdout.LeftAuxTimer = 0;
            }

            if (holdout.LeftChargeTimer == WarmupFrames + RampFrames)
                SoundEngine.PlaySound(FrenzyStartSound, holdout.GunTipPosition);

            if (holdout.LeftChargeTimer >= WarmupFrames + RampFrames + FrenzyFrames)
                return;

            int fireInterval = GetFireInterval(holdout.LeftChargeTimer);
            holdout.LeftTimer++;
            if (holdout.LeftTimer < fireInterval)
                return;

            holdout.LeftTimer = 0;
            FireDragonBreath(holdout);
        }

        private static int GetFireInterval(int cycleTimer)
        {
            if (cycleTimer < WarmupFrames)
            {
                float completion = cycleTimer / (float)WarmupFrames;
                return (int)MathHelper.Lerp(30f, 14f, completion);
            }

            if (cycleTimer < WarmupFrames + RampFrames)
            {
                float completion = (cycleTimer - WarmupFrames) / (float)RampFrames;
                return (int)MathHelper.Lerp(12f, 5f, completion);
            }

            return 3;
        }

        private static void FireDragonBreath(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 muzzle = holdout.GunTipPosition + direction * 16f;
            int damage = holdout.GetScaledDamage(DamageMultiplier);

            for (int i = 0; i < PelletCount; i++)
            {
                float ratio = PelletCount == 1 ? 0.5f : i / (float)(PelletCount - 1);
                float spread = MathHelper.Lerp(-Fan, Fan, ratio);
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
            SoundEngine.PlaySound(DragonFireSound, muzzle);
        }
    }
}
