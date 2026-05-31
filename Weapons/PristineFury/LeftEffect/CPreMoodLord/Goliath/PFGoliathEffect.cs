using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFGoliathEffect
    {
        private const int ChargeFrames = 30;
        private const int FireInterval = 5;
        private const int DronesPerVolley = 2;
        private const float FireSpeed = 11.6f;
        private const float ScatterFan = 0.24f;
        private const float DamageMultiplier = 0.24f;
        private const float Recoil = 1.25f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (justPressed)
            {
                holdout.LeftChargeTimer = 0;
                holdout.LeftTimer = 0;
                holdout.LeftAuxTimer = 0;
            }

            if (holdout.LeftChargeTimer < ChargeFrames)
            {
                holdout.LeftChargeTimer++;
                SpawnChargeEffects(holdout, holdout.LeftChargeTimer / (float)ChargeFrames);
                return;
            }

            SpawnSwarmWakeEffects(holdout);

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            FireReapers(holdout);
        }

        private static void FireReapers(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.myPlayer != holdout.Projectile.owner)
                return;

            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 origin = holdout.Owner.MountedCenter + direction * 24f;
            int damage = holdout.GetScaledDamage(DamageMultiplier);

            for (int i = 0; i < DronesPerVolley; i++)
            {
                float centeredIndex = i - (DronesPerVolley - 1f) * 0.5f;
                float spread = centeredIndex * 0.1f + Main.rand.NextFloat(-ScatterFan, ScatterFan);
                Vector2 velocity = direction.RotatedBy(spread) * FireSpeed * Main.rand.NextFloat(0.92f, 1.14f);
                Vector2 spawnPosition = origin + Main.rand.NextVector2Circular(22f, 16f) - direction * Main.rand.NextFloat(0f, 18f);

                int drone = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<PFGoliath_ReaperDrone>(),
                    damage,
                    holdout.Projectile.knockBack * 0.55f,
                    holdout.Projectile.owner,
                    Main.rand.NextBool() ? -1f : 1f,
                    Main.rand.NextFloat(MathHelper.TwoPi));

                PFLeftEffectRules.ApplyTheme(drone, holdout.CurrentMark);
            }

            holdout.LeftBurstIndex += DronesPerVolley;
            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(8);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.58f);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.22f, Pitch = 0.46f, PitchVariance = 0.16f }, origin);
        }

        private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 origin = holdout.Owner.MountedCenter + direction * 22f;
            Color theme = Color.Lerp(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), new Color(180, 255, 80), charge * 0.22f);
            Lighting.AddLight(origin, theme.ToVector3() * (0.25f + charge * 0.75f));

            if (Main.rand.NextFloat() < 0.38f + charge * 0.42f)
            {
                Vector2 offset = -direction * Main.rand.NextFloat(16f, 58f) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-24f, 24f);
                Vector2 velocity = -offset.SafeNormalize(direction) * Main.rand.NextFloat(1.8f, 4.8f + charge * 1.8f);
                Particle spark = new GlowOrbParticle(
                    origin + offset,
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.22f, 0.44f) * (0.8f + charge),
                    Color.Lerp(theme, new Color(180, 255, 80), Main.rand.NextFloat(0.05f, 0.22f)),
                    true,
                    false,
                    true);

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private static void SpawnSwarmWakeEffects(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 origin = holdout.Owner.MountedCenter + direction * Main.rand.NextFloat(10f, 38f) + Main.rand.NextVector2Circular(18f, 14f);
            Color theme = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Particle mote = new SquishyLightParticle(
                origin,
                -direction * Main.rand.NextFloat(0.4f, 1.2f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                Main.rand.NextFloat(0.22f, 0.42f),
                Color.Lerp(theme, new Color(180, 255, 80), Main.rand.NextFloat(0.08f, 0.32f)),
                Main.rand.Next(10, 18));

            GeneralParticleHandler.SpawnParticle(mote);
        }
    }
}
