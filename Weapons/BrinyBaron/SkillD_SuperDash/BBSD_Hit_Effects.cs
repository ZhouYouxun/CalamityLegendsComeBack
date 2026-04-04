using System;
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
    // Owns all enemy-hit feedback for the super dash.
    // The visuals are intentionally biased toward the current facing direction so the impact keeps flowing with the dash.
    internal static class BBSD_Hit_Effects
    {
        internal static void SpawnImpactStars(Projectile projectile, Vector2 aimDirection, float goldenAngle, float supportStarImpactDamageFactor, float supportStarDashDamageFactor, Vector2 impactCenter, bool majorImpact)
        {
            if (Main.myPlayer != projectile.owner)
                return;

            Vector2 forward = projectile.rotation.ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            int starCount = majorImpact ? 8 : 4;
            float damageFactor = majorImpact ? supportStarImpactDamageFactor : supportStarDashDamageFactor;
            int starDamage = Math.Max(1, (int)(projectile.damage * damageFactor));

            for (int i = 0; i < starCount; i++)
            {
                float t = (i + 0.5f) / starCount;
                float spiralAngle = i * goldenAngle;
                float spiralRadius = MathHelper.Lerp(4f, 20f, (float)Math.Sqrt(t));

                Vector2 spiralOffset =
                    right * ((float)Math.Sin(spiralAngle) * spiralRadius) +
                    forward * ((float)Math.Cos(spiralAngle) * spiralRadius * 0.35f);

                Vector2 launchDirection = (
                    forward * 1.2f +
                    right * ((float)Math.Sin(spiralAngle) * 0.42f) +
                    spiralOffset.SafeNormalize(forward) * 0.35f +
                    Main.rand.NextVector2Circular(0.08f, 0.08f)).SafeNormalize(forward);

                Vector2 launchVelocity = launchDirection * Main.rand.NextFloat(10f, 17f);
                Vector2 spawnPosition = impactCenter + spiralOffset + Main.rand.NextVector2Circular(2f, 2f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    spawnPosition,
                    launchVelocity,
                    ModContent.ProjectileType<BBSD_Star>(),
                    starDamage,
                    projectile.knockBack * 0.35f,
                    projectile.owner);

                BBSD_Fly_Effects.SpawnSupportStarLaunchEffects(spawnPosition, launchVelocity, majorImpact ? 1.15f : 0.82f);
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
                Math.Max(1, (int)(projectile.damage * impactSlashDamageFactor)),
                projectile.knockBack * 0.45f,
                projectile.owner,
                impactSlashScale,
                Main.rand.NextFloat(-0.26f, 0.26f));
        }

        internal static void ApplyImpactScreenShake(Vector2 impactCenter, float shakePower)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(impactCenter, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);
        }

        internal static void PlayImpactSounds(Vector2 impactCenter)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 1.2f,
                Pitch = -0.22f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.05f,
                Pitch = -0.34f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Splash with
            {
                Volume = 1.15f,
                Pitch = -0.16f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Item88 with
            {
                Volume = 0.75f,
                Pitch = -0.32f
            }, impactCenter);
        }

        internal static void SpawnImpactBurst(Projectile projectile, Player owner, Vector2 aimDirection, int timer, Vector2 impactCenter, bool majorImpact)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.rotation.ToRotationVector2();
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float intensity = majorImpact ? 1f : 0.7f;

            SpawnImpactForwardBloom(impactCenter, forward, right, intensity, majorImpact);
            SpawnImpactDustGeometry(owner, projectile, forward, right, timer, impactCenter, intensity, majorImpact);
            SpawnImpactParticleGeometry(owner, impactCenter, forward, right, intensity, majorImpact);
            SpawnImpactAftermath(projectile, impactCenter, forward, right, intensity, majorImpact);
        }

        private static void SpawnImpactForwardBloom(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            float[] branchAngles =
            {
                -0.48f,
                0f,
                0.48f
            };

            float[] branchWeights =
            {
                0.68f,
                1f,
                0.68f
            };

            for (int branchIndex = 0; branchIndex < branchAngles.Length; branchIndex++)
            {
                float branchAngle = branchAngles[branchIndex];
                float branchWeight = branchWeights[branchIndex];
                int lineCount = majorImpact ? (branchIndex == 1 ? 5 : 3) : (branchIndex == 1 ? 3 : 2);

                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    float localT = lineCount == 1 ? 0f : lineIndex / (float)(lineCount - 1);
                    float centered = localT * 2f - 1f;
                    Vector2 direction = forward.RotatedBy(branchAngle + centered * 0.055f);

                    Particle line = new CustomSpark(
                        impactCenter + forward * Main.rand.NextFloat(0f, 8f) + right * centered * 3f,
                        direction * Main.rand.NextFloat(10f, 18f) * (0.72f + branchWeight * 0.42f) * intensity,
                        "CalamityMod/Particles/BloomLineSoftEdge",
                        false,
                        Main.rand.Next(13, 20),
                        Main.rand.NextFloat(0.1f, 0.18f) * (0.8f + branchWeight * 0.8f) * intensity,
                        Color.Lerp(new Color(105, 220, 255), Color.White, 0.4f + 0.25f * branchWeight) * 0.9f,
                        new Vector2(1.95f + branchWeight * 1.35f, 0.36f + branchWeight * 0.06f),
                        shrinkSpeed: 0.68f);
                    GeneralParticleHandler.SpawnParticle(line);
                }
            }
        }

        private static void SpawnImpactDustGeometry(Player owner, Projectile projectile, Vector2 forward, Vector2 right, int timer, Vector2 impactCenter, float intensity, bool majorImpact)
        {
            int coneCount = majorImpact ? 22 : 14;
            for (int i = 0; i < coneCount; i++)
            {
                float t = i / (float)Math.Max(1, coneCount - 1);
                float centered = t * 2f - 1f;
                float centralFocus = 1f - Math.Abs(centered);
                float wave = (float)Math.Sin(t * MathHelper.TwoPi * 2.2f + timer * 0.16f) * 0.04f;
                float coneOffset = MathHelper.Lerp(-0.58f, 0.58f, t) + wave;
                float speed = MathHelper.Lerp(8f, 24f, (float)Math.Sqrt(Math.Max(0f, centralFocus))) * intensity;
                Vector2 jetVelocity =
                    forward.RotatedBy(coneOffset) * speed +
                    right * centered * 2f +
                    owner.velocity * 0.14f;

                Dust water = Dust.NewDustPerfect(
                    impactCenter + right * centered * 5f,
                    DustID.Water,
                    jetVelocity,
                    100,
                    new Color(75, 180, 255),
                    Main.rand.NextFloat(1.05f, 1.6f) * intensity);
                water.noGravity = true;
                water.fadeIn = 1.12f;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        impactCenter + forward * 2f,
                        DustID.Frost,
                        jetVelocity * 0.72f,
                        100,
                        new Color(210, 248, 255),
                        Main.rand.NextFloat(0.9f, 1.25f) * intensity);
                    frost.noGravity = true;
                }
            }
        }

        private static void SpawnImpactParticleGeometry(Player owner, Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int pulseCount = majorImpact ? 3 : 2;
            for (int i = 0; i < pulseCount; i++)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    impactCenter - forward * (2f + i * 4f),
                    forward * (0.42f + i * 0.16f),
                    Color.Lerp(new Color(70, 190, 255), Color.White, 0.28f + i * 0.18f),
                    new Vector2(0.55f + i * 0.12f, 1.4f + i * 0.45f),
                    forward.ToRotation(),
                    0.14f + i * 0.04f,
                    0.016f,
                    11 + i * 3);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            int helixPoints = majorImpact ? 12 : 8;
            for (int arm = 0; arm < 2; arm++)
            {
                float sign = arm == 0 ? 1f : -1f;
                for (int i = 0; i < helixPoints; i++)
                {
                    float t = i / (float)Math.Max(1, helixPoints - 1);
                    float theta = sign * t * MathHelper.TwoPi * 0.9f;
                    float radius = MathHelper.Lerp(4f, 26f, t) * intensity;
                    Vector2 position =
                        impactCenter +
                        forward * MathHelper.Lerp(4f, 28f, t) +
                        right * (float)Math.Sin(theta) * radius;

                    GlowOrbParticle orb = new GlowOrbParticle(
                        position,
                        forward * Main.rand.NextFloat(0.15f, 0.8f) + right * sign * Main.rand.NextFloat(0.05f, 0.3f),
                        false,
                        7,
                        0.8f + (1f - t) * 0.3f * intensity,
                        Color.Lerp(new Color(70, 185, 255), Color.White, 0.3f + 0.4f * t),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }

            int sparkCount = majorImpact ? 7 : 4;
            for (int i = 0; i < sparkCount; i++)
            {
                float side = MathHelper.Lerp(-0.42f, 0.42f, i / (float)Math.Max(1, sparkCount - 1));
                CritSpark spark = new CritSpark(
                    impactCenter + forward * Main.rand.NextFloat(2f, 9f) + right * side * 8f,
                    forward.RotatedBy(side) * Main.rand.NextFloat(5f, 9f) * intensity + owner.velocity * 0.08f,
                    Color.White,
                    Color.LightBlue,
                    Main.rand.NextFloat(0.9f, 1.25f) * intensity,
                    13 + Main.rand.Next(6));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private static void SpawnImpactAftermath(Projectile projectile, Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int smokeCount = majorImpact ? 5 : 3;
            for (int i = 0; i < smokeCount; i++)
            {
                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    impactCenter + Main.rand.NextVector2Circular(6f, 6f),
                    forward * Main.rand.NextFloat(0.5f, 2.2f) + right * Main.rand.NextFloat(-1.5f, 1.5f),
                    Color.WhiteSmoke,
                    18 + Main.rand.Next(5),
                    Main.rand.NextFloat(0.9f, 1.35f) * intensity,
                    0.35f,
                    Main.rand.NextFloat(-0.3f, 0.3f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            int bloodCount = majorImpact ? 5 : 3;
            for (int i = 0; i < bloodCount; i++)
            {
                Vector2 bloodVelocity =
                    forward.RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f)) * Main.rand.NextFloat(4f, 10f) * intensity +
                    right * Main.rand.NextFloat(-1.8f, 1.8f);

                BloodParticle blood = new BloodParticle(
                    impactCenter + Main.rand.NextVector2Circular(6f, 6f),
                    bloodVelocity,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.72f, 1.1f) * intensity,
                    Color.Lerp(new Color(90, 12, 26), new Color(170, 32, 55), Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(blood);
            }

            int bubbleCount = majorImpact ? 6 : 3;
            for (int i = 0; i < bubbleCount; i++)
            {
                Gore bubble = Gore.NewGorePerfect(
                    projectile.GetSource_FromAI(),
                    impactCenter + Main.rand.NextVector2Circular(8f, 8f),
                    forward * Main.rand.NextFloat(0.8f, 2.8f) + Main.rand.NextVector2Circular(1.1f, 1.1f),
                    411);
                bubble.timeLeft = 6 + Main.rand.Next(7);
                bubble.scale = Main.rand.NextFloat(0.6f, 0.85f) * intensity;
                bubble.type = Main.rand.NextBool(3) ? 412 : 411;
            }
        }
    }
}
