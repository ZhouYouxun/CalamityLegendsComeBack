using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Strike_Effects
    {
        internal static void SpawnStrikeLaunchEffects(Projectile projectile, Vector2 strikeStart, Vector2 targetCenter, Vector2 dashDirection, int strikeIndex)
        {
            if (Main.dedServ)
                return;

            Particle bladeSpark = new CustomSpark(
                strikeStart,
                dashDirection * 5.2f,
                "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                false,
                8,
                0.2f,
                new Color(165, 245, 255) * 1.15f,
                new Vector2(0.58f, 2.65f),
                glowCenter: true,
                shrinkSpeed: 0.95f,
                glowCenterScale: 0.96f,
                glowOpacity: 0.72f);
            GeneralParticleHandler.SpawnParticle(bladeSpark);

            DirectionalPulseRing launch = new DirectionalPulseRing(
                strikeStart,
                dashDirection * 0.5f,
                Color.Lerp(new Color(115, 224, 255), Color.White, 0.26f),
                new Vector2(0.42f, 1.82f),
                dashDirection.ToRotation(),
                0.16f,
                0.02f,
                12);
            GeneralParticleHandler.SpawnParticle(launch);
        }

        internal static void SpawnStrikeTravelEffects(Projectile projectile, Vector2 previousCenter, Vector2 currentCenter, Vector2 dashDirection, int phaseTimer, int strikeIndex)
        {
            if (Main.dedServ)
                return;

            Vector2 right = dashDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 midPoint = Vector2.Lerp(previousCenter, currentCenter, 0.5f);

            Particle core = new CustomSpark(
                projectile.Center,
                projectile.velocity * 0.02f,
                "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                false,
                6,
                0.22f,
                new Color(165, 244, 255) * 1.9f,
                new Vector2(0.6f, 2.2f),
                glowCenter: true,
                shrinkSpeed: 1.1f,
                glowCenterScale: 0.94f,
                glowOpacity: 0.72f);
            GeneralParticleHandler.SpawnParticle(core);

            for (int side = -1; side <= 1; side += 2)
            {
                if ((phaseTimer + (side > 0 ? 0 : 1)) % 2 != 0)
                    continue;

                Particle wing = new CustomSpark(
                    midPoint + right * side * Main.rand.NextFloat(8f, 18f),
                    projectile.velocity * 0.03f + right * side * Main.rand.NextFloat(0.45f, 1.2f),
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    5,
                    0.16f,
                    new Color(150, 235, 255) * 0.92f,
                    new Vector2(0.54f, 2.4f),
                    glowCenter: true,
                    shrinkSpeed: 1.15f,
                    glowCenterScale: 0.9f,
                    glowOpacity: 0.68f);
                GeneralParticleHandler.SpawnParticle(wing);
            }

            if (phaseTimer % 2 == 0)
            {
                DirectionalPulseRing trail = new DirectionalPulseRing(
                    currentCenter,
                    projectile.velocity * 0.05f,
                    new Color(105, 214, 255),
                    new Vector2(0.36f, 1.5f),
                    dashDirection.ToRotation(),
                    0.12f,
                    0.018f,
                    8);
                GeneralParticleHandler.SpawnParticle(trail);
            }

            if (Main.rand.NextBool(2))
            {
                Dust splash = Dust.NewDustPerfect(
                    midPoint + right * Main.rand.NextFloat(-16f, 16f),
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    projectile.velocity * 0.05f + right * Main.rand.NextFloat(-1.25f, 1.25f),
                    100,
                    new Color(180, 244, 255),
                    Main.rand.NextFloat(0.92f, 1.18f));
                splash.noGravity = true;
            }
        }

        internal static void SpawnStrikeImpactEffects(Projectile projectile, Vector2 impactCenter, Vector2 dashDirection, int strikeIndex, int totalStrikes)
        {
            if (Main.dedServ)
                return;

            Vector2 right = dashDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing impact = new DirectionalPulseRing(
                    impactCenter,
                    dashDirection * (0.18f + i * 0.05f),
                    Color.Lerp(new Color(255, 220, 130), Color.White, 0.25f),
                    new Vector2(0.56f + i * 0.08f, 1.48f + i * 0.18f),
                    dashDirection.ToRotation() + i * 0.1f,
                    0.12f + i * 0.02f,
                    0.022f,
                    12 + i * 2);
                GeneralParticleHandler.SpawnParticle(impact);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Particle finisher = new CustomSpark(
                    impactCenter + right * side * 18f,
                    dashDirection * Main.rand.NextFloat(2.2f, 4.8f) + right * side * Main.rand.NextFloat(1.8f, 3.4f),
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    10,
                    0.22f,
                    new Color(255, 240, 185) * 1.08f,
                    new Vector2(0.64f, 2.9f),
                    glowCenter: true,
                    shrinkSpeed: 0.86f,
                    glowCenterScale: 1f,
                    glowOpacity: 0.76f);
                GeneralParticleHandler.SpawnParticle(finisher);
            }

            for (int i = 0; i < 12; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    impactCenter,
                    Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Frost,
                    dashDirection.RotatedByRandom(0.8f) * Main.rand.NextFloat(2.4f, 7.8f),
                    100,
                    new Color(255, 228, 150),
                    Main.rand.NextFloat(1.05f, 1.45f));
                burst.noGravity = true;
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
