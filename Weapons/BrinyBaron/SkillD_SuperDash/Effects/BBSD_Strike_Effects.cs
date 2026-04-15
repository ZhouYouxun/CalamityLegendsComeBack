using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Strike_Effects
    {
        internal static void SpawnStrikeLaunchEffects(Projectile projectile, Vector2 strikeStart, Vector2 targetCenter, Vector2 dashDirection, int strikeIndex)
        {
        }

        internal static void SpawnStrikeTravelEffects(Projectile projectile, Vector2 previousCenter, Vector2 currentCenter, Vector2 dashDirection, int phaseTimer, int strikeIndex)
        {
        }

        internal static void SpawnStrikeImpactEffects(Projectile projectile, Vector2 impactCenter, Vector2 dashDirection, int strikeIndex, int totalStrikes)
        {
            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (Main.myPlayer == projectile.owner)
            {
                float[] slashOffsets = { -0.28f, 0f, 0.28f };
                foreach (float rotationOffset in slashOffsets)
                {
                    Vector2 slashVelocity = forward.RotatedBy(rotationOffset) * 7f;
                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        impactCenter + slashVelocity * 0.65f,
                        slashVelocity,
                        ModContent.ProjectileType<BBSwing_Slash>(),
                        Math.Max(1, projectile.damage),
                        projectile.knockBack,
                        projectile.owner,
                        1f,
                        Main.rand.NextFloat(-0.12f, 0.12f));
                }

                Vector2 starSpawnCenter = impactCenter - forward * 84f;
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    float spread = MathHelper.Lerp(-0.26f, 0.26f, t);
                    Vector2 spawnPos = starSpawnCenter + right * MathHelper.Lerp(-42f, 42f, t);
                    Vector2 starVelocity = forward.RotatedBy(spread) * Main.rand.NextFloat(7.4f, 10.6f);

                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        spawnPos,
                        starVelocity,
                        ModContent.ProjectileType<BBSD_Star>(),
                        Math.Max(1, projectile.damage),
                        projectile.knockBack,
                        projectile.owner);
                }
            }

            if (Main.dedServ)
                return;

            float burst = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 14f + strikeIndex * 0.8f);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float speedRatio = i / 5f;
                    Vector2 edgePos =
                        impactCenter -
                        forward * MathHelper.Lerp(10f, 58f, speedRatio) +
                        right * side * MathHelper.Lerp(8f, 36f, speedRatio);
                    Vector2 edgeVelocity =
                        forward * MathHelper.Lerp(0.6f, 3.8f, speedRatio) +
                        right * side * MathHelper.Lerp(0.8f, 2.6f, speedRatio);

                    GlowOrbParticle wakeOrb = new GlowOrbParticle(
                        edgePos,
                        edgeVelocity,
                        false,
                        Main.rand.Next(9, 15),
                        MathHelper.Lerp(0.4f, 0.76f, speedRatio) * (1f + burst * 0.08f),
                        side < 0 ? new Color(70, 180, 255) : new Color(185, 245, 255),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(wakeOrb);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Color pulseColor = i switch
                {
                    0 => new Color(80, 205, 255),
                    1 => new Color(170, 240, 255),
                    _ => new Color(255, 245, 205)
                };

                Particle bolt = new CustomPulse(
                    impactCenter,
                    Vector2.Zero,
                    pulseColor,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                    new Vector2(1.2f + i * 0.2f, 1f + i * 0.12f),
                    Main.rand.NextFloat(-10f, 10f),
                    0.03f + i * 0.012f,
                    0.16f + i * 0.04f,
                    16 + i * 3);
                GeneralParticleHandler.SpawnParticle(bolt);
            }

            for (int i = 0; i < 18; i++)
            {
                Dust burstDust = Dust.NewDustPerfect(
                    impactCenter,
                    Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Frost,
                    forward.RotatedByRandom(0.95f) * Main.rand.NextFloat(2.8f, 8.6f),
                    100,
                    Color.Lerp(new Color(110, 214, 255), new Color(255, 230, 165), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1f, 1.42f));
                burstDust.noGravity = true;
            }
        }

        internal static void SpawnFinalBurst(Vector2 center, Vector2 direction, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                DirectionalPulseRing wave = new DirectionalPulseRing(
                    center,
                    direction * 0.16f,
                    Color.Lerp(new Color(120, 220, 255), Color.White, 0.32f),
                    new Vector2(0.5f + i * 0.1f, 1.65f + i * 0.22f),
                    direction.ToRotation() + i * MathHelper.PiOver4,
                    0.16f + i * 0.02f,
                    0.02f,
                    14 + i * 2);
                GeneralParticleHandler.SpawnParticle(wave);
            }

            for (int i = 0; i < Math.Max(14, totalStrikes); i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Water,
                    Main.rand.NextVector2Circular(8f, 8f),
                    100,
                    new Color(215, 245, 255),
                    Main.rand.NextFloat(1.1f, 1.55f));
                burst.noGravity = true;
            }
        }
    }
}
