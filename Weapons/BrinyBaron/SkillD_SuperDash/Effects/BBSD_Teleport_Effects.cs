using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Teleport_Effects
    {
        internal static void SpawnTeleportBurst(Vector2 startPos, Vector2 targetCenter, Vector2 dashDirection, int strikeIndex, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = dashDirection.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float strikeRatio = strikeIndex / (float)Math.Max(1, totalStrikes);
            Color bladeColor = Color.Lerp(new Color(150, 238, 255), Color.White, 0.22f + strikeRatio * 0.18f);
            Color orbLeftColor = new Color(72, 188, 255);
            Color orbRightColor = new Color(190, 245, 255);

            Particle bladeSpark = new CustomSpark(
                startPos + forward * 14f,
                forward * 9.5f,
                "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                false,
                12,
                0.34f,
                bladeColor * 1.2f,
                new Vector2(0.72f, 5.1f),
                glowCenter: true,
                shrinkSpeed: 0.84f,
                glowCenterScale: 1.04f,
                glowOpacity: 0.82f);
            GeneralParticleHandler.SpawnParticle(bladeSpark);

            for (int i = 0; i < 14; i++)
            {
                float t = i / 13f;
                float spreadAngle = MathHelper.Lerp(-0.72f, 0.72f, t);
                float curve = (float)Math.Sin(t * MathHelper.Pi);
                Vector2 edgePos =
                    startPos +
                    forward * MathHelper.Lerp(18f, 88f, t) +
                    right * spreadAngle * (34f + 18f * curve);
                Vector2 edgeVelocity =
                    forward.RotatedBy(spreadAngle * 0.58f) * MathHelper.Lerp(2.2f, 7.8f, 0.45f + 0.55f * curve) +
                    right * spreadAngle * Main.rand.NextFloat(0.15f, 0.9f);

                Dust dust = Dust.NewDustPerfect(
                    edgePos,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    edgeVelocity,
                    100,
                    Color.Lerp(orbLeftColor, orbRightColor, t),
                    Main.rand.NextFloat(0.95f, 1.4f));
                dust.noGravity = true;

                GlowOrbParticle orb = new GlowOrbParticle(
                    edgePos,
                    edgeVelocity * 0.18f,
                    false,
                    5 + Main.rand.Next(2),
                    0.74f + curve * 0.26f,
                    t < 0.5f ? orbLeftColor : orbRightColor,
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);

                if (i % 2 == 0)
                {
                    Particle spark = new CritSpark(
                        edgePos,
                        edgeVelocity * 0.9f,
                        Color.White,
                        Color.Lerp(new Color(145, 220, 255), Color.White, curve * 0.3f),
                        0.9f + curve * 0.35f,
                        16 + Main.rand.Next(4),
                        0.14f,
                        1.28f);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            Particle shrinkingPulse = new DirectionalPulseRing(
                startPos - forward * 8f,
                -forward * 0.16f,
                Color.Lerp(new Color(120, 220, 255), Color.White, 0.34f),
                new Vector2(2.8f, 8.8f),
                forward.ToRotation(),
                Main.rand.NextFloat(3.6f, 5.4f),
                0.15f,
                14);
            GeneralParticleHandler.SpawnParticle(shrinkingPulse);
        }

        internal static void SpawnTeleportHoldEffects(Projectile projectile, Vector2 targetCenter, int phaseTimer, int windupFrames)
        {
        }

        internal static void SpawnAbortEffects(Vector2 center, Vector2 direction)
        {
        }
    }
}
