using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFBrimstoneElementalEffect
    {
        private const int LaserInterval = 120;
        private const float DamageMultiplier = 1.35f;
        private const float Recoil = 8.5f;

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
                holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.58f);
        }

        private static void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            int laser = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 18f,
                direction,
                ModContent.ProjectileType<PFBrimstoneElemental_Laser>(),
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(laser, holdout.CurrentMark);

            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(20);
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 1.3f);
        }

        private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            if (Main.dedServ)
                return;

            charge = MathHelper.Clamp(charge, 0f, 1f);
            Color theme = PristineFuryMarkHelper.GetColor(holdout.CurrentMark);
            Vector2 direction = holdout.AimDirection;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            float spiral = holdout.LeftTimer * MathHelper.Lerp(0.06f, 0.32f, charge);
            float radius = MathHelper.Lerp(36f, 7f, charge);
            Vector2 orbit = side * ((float)System.Math.Cos(spiral) * radius) +
                direction * ((float)System.Math.Sin(spiral * 1.7f) * radius * 0.36f);
            Vector2 center = holdout.GunTipPosition + direction * MathHelper.Lerp(10f, 24f, charge);
            int interval = System.Math.Max(2, (int)System.MathF.Round(MathHelper.Lerp(7f, 2f, charge)));

            if (holdout.LeftTimer % interval == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + orbit,
                    -orbit.SafeNormalize(direction) * MathHelper.Lerp(0.9f, 4.8f, charge),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.16f, 0.42f) * MathHelper.Lerp(0.8f, 1.75f, charge),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.08f, 0.5f)),
                    true,
                    false,
                    true));
            }

            if (holdout.LeftTimer % 12 == 0)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, direction, theme * (0.35f + charge * 0.55f), Vector2.One, direction.ToRotation(), 0.02f, MathHelper.Lerp(0.16f, 0.58f, charge), 16));

            if (Main.rand.NextFloat() < 0.18f + charge * 0.45f)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + orbit.RotatedByRandom(0.2f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
                    -orbit.SafeNormalize(direction) * Main.rand.NextFloat(1.2f, 5.4f + charge * 3f),
                    40,
                    Color.Lerp(theme, Color.White, charge * 0.35f),
                    Main.rand.NextFloat(0.6f, 1.2f) * MathHelper.Lerp(0.75f, 1.8f, charge));
                dust.noGravity = true;
            }
        }
    }
}
