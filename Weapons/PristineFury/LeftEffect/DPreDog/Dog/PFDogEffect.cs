using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFDogEffect
    {
        private const int ChargeFrames = 180;
        private const int ScatterCount = 12;
        private const float ScatterSpeed = 23.5f;
        private const float ScatterFan = 0.76f;
        private const float DamageMultiplier = 1.36f;
        private const float Recoil = 58f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftChargeTimer = Math.Min(ChargeFrames, holdout.LeftChargeTimer + 1);
            float charge = holdout.LeftChargeTimer / (float)ChargeFrames;
            EnsureChargeOrb(holdout, charge);
            SpawnChargeEffects(holdout, charge);

            if (holdout.LeftChargeTimer < ChargeFrames)
                return;

            FireScatter(holdout);
            holdout.LeftChargeTimer = 0;
            holdout.LeftTimer = 0;
            holdout.LeftAuxTimer = 0;
        }

        private static void FireScatter(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 muzzle = holdout.GunTipPosition + direction * 12f;
            int damage = holdout.GetScaledDamage(DamageMultiplier);

            for (int i = 0; i < ScatterCount; i++)
            {
                float ratio = ScatterCount == 1 ? 0.5f : i / (float)(ScatterCount - 1);
                float spread = MathHelper.Lerp(-ScatterFan, ScatterFan, ratio);
                Vector2 velocity = direction.RotatedBy(spread + Main.rand.NextFloat(-0.035f, 0.035f)) * ScatterSpeed * Main.rand.NextFloat(0.94f, 1.08f);

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + direction * (i % 3) * 4f,
                    velocity,
                    ModContent.ProjectileType<PFDog_Flame>(),
                    damage,
                    holdout.Projectile.knockBack * 1.35f,
                    holdout.Projectile.owner,
                    i,
                    ScatterCount);

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.LeftBurstIndex += ScatterCount;
            holdout.ApplyRecoil(Recoil);
            holdout.Owner.velocity -= direction * 7.5f;
            holdout.Owner.Calamity().GeneralScreenShakePower = System.Math.Max(holdout.Owner.Calamity().GeneralScreenShakePower, 9f);
            holdout.TriggerMuzzleFlash(30);
            holdout.SpawnMuzzleBurst(new Color(160, 100, 255), 1.55f);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.82f, Pitch = -0.18f, MaxInstances = 2 }, muzzle);
        }

        private static void EnsureChargeOrb(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            int type = ModContent.ProjectileType<PFDog_ChargeOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != holdout.Projectile.owner || projectile.type != type || projectile.ai[0] != holdout.Projectile.whoAmI)
                    continue;

                projectile.ai[1] = charge;
                projectile.timeLeft = 2;
                PFLeftEffectRules.ApplyTheme(i, holdout.CurrentMark);
                return;
            }

            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition,
                Vector2.Zero,
                type,
                0,
                0f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI,
                charge);

            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
        }

        private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout, float charge)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 muzzle = holdout.GunTipPosition + direction * 10f;
            Color theme = Color.Lerp(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), Color.White, charge * 0.38f);
            Lighting.AddLight(muzzle, theme.ToVector3() * (0.35f + charge * 0.95f));

            if (Main.rand.NextFloat() < 0.46f + charge * 0.38f)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(40f + charge * 74f, 40f + charge * 74f);
                Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.2f, 6.2f) * (0.5f + charge);

                Dust dust = Dust.NewDustPerfect(muzzle + offset, ModContent.DustType<SquashDust>());
                dust.velocity = velocity;
                dust.color = theme;
                dust.scale = Main.rand.NextFloat(0.9f, 1.45f) * (0.8f + charge * 0.7f);
                dust.noGravity = true;
                dust.fadeIn = 1.8f + charge * 2f;
            }

            if (holdout.LeftChargeTimer == ChargeFrames)
            {
                Particle ring = new DirectionalPulseRing(muzzle, Vector2.Zero, theme, Vector2.One, direction.ToRotation(), 0.1f, 1.4f, 24);
                GeneralParticleHandler.SpawnParticle(ring);
            }
        }
    }
}
