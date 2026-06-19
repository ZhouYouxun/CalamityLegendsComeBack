using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFBrimstoneElementalEffect
    {
        private const int LaserInterval = 50;
        private const float DamageMultiplier = 0.52f;
        private const float Recoil = 7.5f;
        private const int BallCount = 5;
        private static readonly Color BlueTheme = new Color(60, 160, 255);

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            SpawnChargeEffects(holdout, holdout.LeftTimer / (float)LaserInterval);
            if (holdout.LeftTimer >= LaserInterval)
            {
                holdout.LeftTimer = 0;
                Fire(holdout);
            }

            if (holdout.LeftTimer % 8 == 0)
                holdout.SpawnMuzzleBurst(BlueTheme, 0.55f);
        }

        private static void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 18f;
            int damage = holdout.GetScaledDamage(DamageMultiplier);
            int projType = ModContent.ProjectileType<PFBrimstoneElemental_StarBolt>();

            for (int i = 0; i < BallCount; i++)
            {
                float spread = MathHelper.Lerp(-MathHelper.Pi / 9f, MathHelper.Pi / 9f, i / (float)(BallCount - 1)) + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 vel = direction.RotatedBy(spread) * Main.rand.NextFloat(9f, 14f);
                int proj = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + Main.rand.NextVector2Circular(7f, 7f),
                    vel,
                    projType,
                    damage,
                    holdout.Projectile.knockBack * 0.5f,
                    holdout.Projectile.owner);
                PFLeftEffectRules.ApplyTheme(proj, holdout.CurrentMark);
            }

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(BlueTheme, 1.25f);
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.65f, Pitch = 0.1f }, muzzle);
        }

        private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            if (Main.dedServ)
                return;

            charge = MathHelper.Clamp(charge, 0f, 1f);
            Vector2 direction = holdout.AimDirection;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            float spiral = holdout.LeftTimer * MathHelper.Lerp(0.06f, 0.32f, charge);
            float radius = MathHelper.Lerp(36f, 7f, charge);
            Vector2 orbit = side * ((float)System.Math.Cos(spiral) * radius) +
                direction * ((float)System.Math.Sin(spiral * 1.7f) * radius * 0.36f);
            Vector2 center = holdout.GunTipPosition + direction * MathHelper.Lerp(10f, 24f, charge);
            int interval = System.Math.Max(2, (int)System.MathF.Round(MathHelper.Lerp(7f, 2f, charge)));

            Color blue = Color.Lerp(BlueTheme, Color.White, Main.rand.NextFloat(0.08f, 0.42f) * charge);
            Lighting.AddLight(center, BlueTheme.ToVector3() * (0.25f + charge * 0.88f));

            if (holdout.LeftTimer % interval == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + orbit,
                    -orbit.SafeNormalize(direction) * MathHelper.Lerp(0.9f, 4.8f, charge),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.16f, 0.42f) * MathHelper.Lerp(0.8f, 1.75f, charge),
                    blue,
                    true,
                    false,
                    true));
            }

            if (holdout.LeftTimer % 12 == 0)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, direction, BlueTheme * (0.35f + charge * 0.55f), Vector2.One, direction.ToRotation(), 0.02f, MathHelper.Lerp(0.16f, 0.58f, charge), 16));

            if (Main.rand.NextFloat() < 0.18f + charge * 0.45f)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + orbit.RotatedByRandom(0.2f),
                    DustID.BlueTorch,
                    -orbit.SafeNormalize(direction) * Main.rand.NextFloat(1.2f, 5.4f + charge * 3f),
                    40,
                    Color.Lerp(BlueTheme, Color.White, charge * 0.35f),
                    Main.rand.NextFloat(0.6f, 1.2f) * MathHelper.Lerp(0.75f, 1.8f, charge));
                dust.noGravity = true;
            }
        }
    }
}
