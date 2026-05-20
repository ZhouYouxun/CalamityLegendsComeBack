using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFProvidenceEffect
    {
        private const int StartupFrames = 18;
        private const int FireInterval = 64;
        private const float MortarSpeed = 12.6f;
        private const float DamageMultiplier = 1.72f;
        private const float Recoil = 25f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (justPressed)
                holdout.LeftTimer = FireInterval - StartupFrames;

            holdout.LeftTimer++;
            SpawnChargeEffects(holdout);

            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            FireMortar(holdout);
        }

        private static void FireMortar(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.SafeNormalize(-Vector2.UnitY * holdout.Owner.gravDir);
            Vector2 muzzle = holdout.GunTipPosition + direction * 10f;
            Vector2 target = holdout.GetMouseWorld();

            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction.RotatedByRandom(MathHelper.ToRadians(2.5f)) * MortarSpeed,
                ModContent.ProjectileType<PFProvidence_NukeOfBliss>(),
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                target.X,
                target.Y);

            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            holdout.LeftBurstIndex++;
            holdout.ApplyRecoil(Recoil);
            holdout.TriggerMuzzleFlash(22);
            holdout.SpawnMuzzleBurst(new Color(255, 226, 116), 1.2f);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LauncherHeavyShot") { Volume = 0.72f, PitchVariance = 0.14f }, muzzle);
        }

        private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.dedServ)
                return;

            float charge = Utils.GetLerpValue(0f, FireInterval, holdout.LeftTimer, true);
            Vector2 direction = holdout.AimDirection.SafeNormalize(-Vector2.UnitY * holdout.Owner.gravDir);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 muzzle = holdout.GunTipPosition + direction * MathHelper.Lerp(-2f, 9f, charge);
            Color gold = new(255, 210, 82);
            Color white = Color.Lerp(gold, Color.White, 0.55f);

            Lighting.AddLight(muzzle, gold.ToVector3() * (0.22f + charge * 0.55f));

            if (holdout.LeftTimer % 4 == 0)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.GoldFlame);
                dust.velocity = direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 2.8f) + right * Main.rand.NextFloat(-0.7f, 0.7f);
                dust.scale = Main.rand.NextFloat(0.85f, 1.28f) * (0.75f + charge * 0.45f);
                dust.noGravity = true;
            }

            if (holdout.LeftTimer % 10 == 0)
            {
                Particle ring = new DirectionalPulseRing(
                    muzzle,
                    direction * Main.rand.NextFloat(0.3f, 1.3f),
                    Color.Lerp(gold, white, charge) * 0.72f,
                    Vector2.One,
                    direction.ToRotation(),
                    0.04f + charge * 0.04f,
                    0.22f + charge * 0.18f,
                    18);

                GeneralParticleHandler.SpawnParticle(ring);
            }
        }
    }
}
