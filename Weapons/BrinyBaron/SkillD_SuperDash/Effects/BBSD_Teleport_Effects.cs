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

            Vector2 right = dashDirection.RotatedBy(MathHelper.PiOver2);
            float burstScale = 0.18f + strikeIndex / (float)Math.Max(1, totalStrikes) * 0.06f;

            for (int i = 0; i < 2; i++)
            {
                DirectionalPulseRing portal = new DirectionalPulseRing(
                    startPos,
                    dashDirection * 0.16f,
                    Color.Lerp(new Color(92, 206, 255), Color.White, 0.28f),
                    new Vector2(0.44f + i * 0.08f, 1.48f + i * 0.18f),
                    dashDirection.ToRotation() + i * MathHelper.PiOver2,
                    burstScale + i * 0.03f,
                    0.02f,
                    14 + i * 2);
                GeneralParticleHandler.SpawnParticle(portal);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Particle edge = new CustomSpark(
                    startPos + right * side * 28f,
                    dashDirection * Main.rand.NextFloat(1.8f, 3.6f) + right * side * Main.rand.NextFloat(1.4f, 2.6f),
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    9,
                    0.18f,
                    new Color(160, 244, 255) * 1.15f,
                    new Vector2(0.56f, 2.4f),
                    glowCenter: true,
                    shrinkSpeed: 0.82f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.7f);
                GeneralParticleHandler.SpawnParticle(edge);
            }

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    startPos + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    dashDirection.RotatedByRandom(0.7f) * Main.rand.NextFloat(2f, 6f),
                    100,
                    new Color(170, 238, 255),
                    Main.rand.NextFloat(1f, 1.35f));
                dust.noGravity = true;
            }
        }

        internal static void SpawnTeleportHoldEffects(Projectile projectile, Vector2 targetCenter, int phaseTimer, int windupFrames)
        {
            if (Main.dedServ)
                return;

            float readiness = Utils.GetLerpValue(0f, windupFrames, phaseTimer, true);
            Vector2 forward = projectile.velocity.SafeNormalize((projectile.rotation - MathHelper.PiOver4).ToRotationVector2());

            Particle centerFlare = new CustomSpark(
                projectile.Center,
                forward * 0.1f,
                "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                false,
                8,
                0.24f + readiness * 0.06f,
                new Color(165, 243, 255) * 1.5f,
                new Vector2(0.62f, 2.4f),
                glowCenter: true,
                shrinkSpeed: 1.05f,
                glowCenterScale: 0.92f,
                glowOpacity: 0.72f);
            GeneralParticleHandler.SpawnParticle(centerFlare);

            if (phaseTimer % 2 == 0)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(new Color(110, 220, 255), Color.White, 0.2f + readiness * 0.2f),
                    new Vector2(0.35f, 1.05f + readiness * 0.35f),
                    (targetCenter - projectile.Center).ToRotation(),
                    0.09f + readiness * 0.03f,
                    0.016f,
                    10);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        internal static void SpawnAbortEffects(Vector2 center, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    center,
                    direction * 0.1f,
                    new Color(95, 198, 255),
                    new Vector2(0.42f + i * 0.08f, 1.2f),
                    direction.ToRotation(),
                    0.12f,
                    0.018f,
                    12);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int i = 0; i < 10; i++)
            {
                Dust mist = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    Main.rand.NextVector2Circular(4.5f, 4.5f),
                    100,
                    new Color(150, 230, 255),
                    Main.rand.NextFloat(0.9f, 1.25f));
                mist.noGravity = true;
            }
        }
    }
}
