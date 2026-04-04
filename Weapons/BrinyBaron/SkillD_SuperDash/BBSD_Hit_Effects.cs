using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    // Owns all enemy-hit feedback for the dash.
    // The burst now keeps flowing forward along the dash vector instead of exploding equally in every direction.
    internal static class BBSD_Hit_Effects
    {
        internal static void SpawnImpactStars(Projectile projectile, Vector2 aimDirection, float goldenAngle, float supportStarImpactDamageFactor, float supportStarDashDamageFactor, Vector2 impactCenter, bool majorImpact)
        {
            if (Main.myPlayer != projectile.owner)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(aimDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            int starCount = majorImpact ? 8 : 4;
            float damageFactor = majorImpact ? supportStarImpactDamageFactor : supportStarDashDamageFactor;
            int starDamage = System.Math.Max(1, (int)(projectile.damage * damageFactor));

            for (int i = 0; i < starCount; i++)
            {
                float t = (i + 0.5f) / starCount;
                float spiralAngle = i * goldenAngle;
                float spiralRadius = MathHelper.Lerp(4f, 18f, (float)System.Math.Sqrt(t));

                Vector2 spiralOffset =
                    right * ((float)System.Math.Sin(spiralAngle) * spiralRadius) +
                    forward * ((float)System.Math.Cos(spiralAngle) * spiralRadius * 0.4f);

                Vector2 launchDirection = (
                    forward * 1.2f +
                    right * ((float)System.Math.Sin(spiralAngle) * 0.38f) +
                    spiralOffset.SafeNormalize(forward) * 0.3f +
                    Main.rand.NextVector2Circular(0.06f, 0.06f)).SafeNormalize(forward);

                Vector2 launchVelocity = launchDirection * Main.rand.NextFloat(10f, 16f);
                Vector2 spawnPosition = impactCenter + spiralOffset + Main.rand.NextVector2Circular(2f, 2f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    spawnPosition,
                    launchVelocity,
                    ModContent.ProjectileType<BBSD_Star>(),
                    starDamage,
                    projectile.knockBack * 0.35f,
                    projectile.owner);

                BBSD_Fly_Effects.SpawnSupportStarLaunchEffects(spawnPosition, launchVelocity, majorImpact ? 1f : 0.76f);
            }
        }

        internal static void SpawnImpactSlash(Projectile projectile, Vector2 aimDirection, float impactSlashDamageFactor, float impactSlashScale, Vector2 impactCenter, bool isDashing)
        {
            if (Main.myPlayer != projectile.owner || !isDashing)
                return;

            Vector2 slashVelocity = projectile.velocity.SafeNormalize(aimDirection) * 8f;
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                impactCenter,
                slashVelocity,
                ModContent.ProjectileType<BBSwing_Slash>(),
                System.Math.Max(1, (int)(projectile.damage * impactSlashDamageFactor)),
                projectile.knockBack * 0.45f,
                projectile.owner,
                impactSlashScale,
                Main.rand.NextFloat(-0.2f, 0.2f));
        }

        internal static void ApplyImpactScreenShake(Vector2 impactCenter, float shakePower)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(impactCenter, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = System.Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);
        }

        internal static void PlayImpactSounds(Vector2 impactCenter)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 1.15f,
                Pitch = -0.26f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Splash with
            {
                Volume = 1.1f,
                Pitch = -0.12f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Item88 with
            {
                Volume = 0.62f,
                Pitch = -0.34f
            }, impactCenter);
        }

        internal static void SpawnImpactBurst(Projectile projectile, Player owner, Vector2 aimDirection, int timer, Vector2 impactCenter, bool majorImpact)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(aimDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float intensity = majorImpact ? 1f : 0.72f;

            SpawnImpactFan(impactCenter, forward, intensity, majorImpact);
            SpawnImpactPulse(impactCenter, forward, intensity, majorImpact);
            SpawnImpactOrbs(impactCenter, forward, right, intensity, timer);
            SpawnImpactDust(impactCenter, forward, right, intensity, majorImpact);
        }

        // The impact cone stays narrow and directional so the dash keeps "carrying through" the target.
        private static void SpawnImpactFan(Vector2 impactCenter, Vector2 forward, float intensity, bool majorImpact)
        {
            float[] branchAngles = { -0.42f, -0.18f, 0f, 0.18f, 0.42f };
            float[] branchWeights = { 0.62f, 0.8f, 1f, 0.8f, 0.62f };

            for (int i = 0; i < branchAngles.Length; i++)
            {
                int lineCount = majorImpact && i == 2 ? 2 : 1;

                for (int j = 0; j < lineCount; j++)
                {
                    float local = lineCount == 1 ? 0f : j * 2f - 1f;
                    Vector2 direction = forward.RotatedBy(branchAngles[i] + local * 0.05f);

                    Particle line = new CustomSpark(
                        impactCenter + forward * Main.rand.NextFloat(0f, 6f) + direction.RotatedBy(MathHelper.PiOver2) * local * 2f,
                        direction * Main.rand.NextFloat(10f, 17f) * (0.74f + branchWeights[i] * 0.34f) * intensity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        Main.rand.Next(8, 12),
                        Main.rand.NextFloat(0.055f, 0.085f) * (0.8f + branchWeights[i] * 0.25f) * intensity,
                        Color.Lerp(new Color(95, 215, 255), Color.White, 0.34f + branchWeights[i] * 0.24f) * 0.78f,
                        new Vector2(1.8f + branchWeights[i] * 0.5f, 0.42f + branchWeights[i] * 0.05f),
                        shrinkSpeed: 0.7f);
                    GeneralParticleHandler.SpawnParticle(line);
                }
            }
        }

        private static void SpawnImpactPulse(Vector2 impactCenter, Vector2 forward, float intensity, bool majorImpact)
        {
            DirectionalPulseRing frontPulse = new DirectionalPulseRing(
                impactCenter + forward * 4f,
                forward * (0.35f + intensity * 0.14f),
                Color.Lerp(new Color(85, 205, 255), Color.White, 0.42f),
                new Vector2(0.5f + intensity * 0.08f, 1.32f + intensity * 0.4f),
                forward.ToRotation(),
                0.12f + intensity * 0.03f,
                0.015f,
                majorImpact ? 14 : 11);
            GeneralParticleHandler.SpawnParticle(frontPulse);

            if (majorImpact)
            {
                DirectionalPulseRing secondPulse = new DirectionalPulseRing(
                    impactCenter - forward * 2f,
                    forward * 0.2f,
                    new Color(80, 195, 255) * 0.82f,
                    new Vector2(0.44f, 1.05f),
                    forward.ToRotation(),
                    0.1f,
                    0.012f,
                    10);
                GeneralParticleHandler.SpawnParticle(secondPulse);
            }
        }

        private static void SpawnImpactOrbs(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, int timer)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = i / 2f;
                    float wave = (float)System.Math.Sin(timer * 0.18f + side * 0.9f + i * 0.75f);
                    Vector2 position =
                        impactCenter +
                        forward * MathHelper.Lerp(4f, 20f, t) +
                        right * side * (3f + 8f * t + wave * 1.6f);

                    GlowOrbParticle orb = new GlowOrbParticle(
                        position,
                        forward * Main.rand.NextFloat(0.08f, 0.4f) + right * side * Main.rand.NextFloat(0.02f, 0.12f),
                        false,
                        6,
                        0.5f + (1f - t) * 0.18f * intensity,
                        Color.Lerp(new Color(85, 200, 255), Color.White, 0.28f + t * 0.3f),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }
        }

        private static void SpawnImpactDust(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int dustCount = majorImpact ? 12 : 8;

            for (int i = 0; i < dustCount; i++)
            {
                float spread = MathHelper.Lerp(-0.46f, 0.46f, i / (float)System.Math.Max(1, dustCount - 1));
                Vector2 velocity =
                    forward.RotatedBy(spread) * Main.rand.NextFloat(5f, 11f) * intensity +
                    right * spread * 3.2f;

                Dust water = Dust.NewDustPerfect(
                    impactCenter + right * spread * 5f,
                    DustID.Water,
                    velocity,
                    100,
                    new Color(85, 200, 255),
                    Main.rand.NextFloat(0.75f, 1.12f) * intensity);
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        impactCenter + forward * 2f,
                        DustID.Frost,
                        velocity * 0.65f,
                        100,
                        new Color(215, 248, 255),
                        Main.rand.NextFloat(0.68f, 0.96f) * intensity);
                    frost.noGravity = true;
                }
            }

            int critCount = majorImpact ? 4 : 2;
            for (int i = 0; i < critCount; i++)
            {
                CritSpark spark = new CritSpark(
                    impactCenter + forward * Main.rand.NextFloat(2f, 10f) + right * Main.rand.NextFloat(-4f, 4f),
                    forward.RotatedBy(Main.rand.NextFloat(-0.18f, 0.18f)) * Main.rand.NextFloat(4f, 7f),
                    Color.White,
                    Color.LightBlue,
                    Main.rand.NextFloat(0.55f, 0.82f) * intensity,
                    10 + Main.rand.Next(4));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
