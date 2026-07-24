using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    internal static class BrinyBaron_SkillDashTornado_FlightEffects
    {
        private const float FrontAnchorDistance = 16f * 3f;

        public static Vector2 GetFrontAnchor(Projectile projectile, Vector2 fallbackDirection)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(fallbackDirection);
            if (forward == Vector2.Zero)
                forward = fallbackDirection.SafeNormalize(Vector2.UnitX);

            return projectile.Center + forward * FrontAnchorDistance;
        }

        public static void SpawnDashStartEffects(Projectile projectile, Vector2 fallbackDirection)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(fallbackDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = GetFrontAnchor(projectile, fallbackDirection);
            float sideWave = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 18f + projectile.identity * 0.27f);

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    tip - forward * Main.rand.NextFloat(4f, 13f) + right * sideWave * Main.rand.NextFloat(1f, 3f),
                    projectile.velocity * 0.025f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.05f, 0.095f),
                    Color.Lerp(new Color(95, 205, 255), Color.White, Main.rand.NextFloat(0.1f, 0.3f))));
            }

            SpawnOuterWake(projectile, tip, forward, right, 0f, 0.85f, 5.6f, 15f, true, true);
        }

        public static void SpawnDashFlightEffects(Projectile projectile, Vector2 fallbackDirection, float bladeRotation, float oceanPhase, int stateTimer)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(fallbackDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = GetFrontAnchor(projectile, fallbackDirection);
            float sideWave = (float)System.Math.Sin(oceanPhase * 1.4f);
            float wakeSpread = 3.8f + 2.4f * (0.5f + 0.5f * (float)System.Math.Sin(oceanPhase * 1.6f + 0.4f));
            float wakeDrift = 0.72f + 0.28f * (0.5f + 0.5f * (float)System.Math.Cos(oceanPhase * 1.25f));

            if (stateTimer % 2 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    tip - forward * 9f + right * sideWave * 1.45f,
                    projectile.velocity * 0.085f,
                    Color.Lerp(new Color(80, 195, 255), Color.White, 0.16f),
                    new Vector2(0.88f, 2.5f),
                    bladeRotation - MathHelper.PiOver4,
                    0.22f,
                    0.03f,
                    10);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // Spawn BlossomFlux-style orbiting ocean swirl visual projectile anchored in world space
            if (stateTimer % 5 == 0 && Main.myPlayer == projectile.owner)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BrinyBaron_DashOceanSwirl>(),
                    0,
                    0f,
                    projectile.owner);
            }

            SpawnOuterWake(projectile, tip, forward, right, oceanPhase, wakeDrift * 1.18f, wakeSpread * 1.14f, 11.5f, stateTimer % 2 == 0, stateTimer % 2 == 0);
        }

        public static void SpawnReboundFlightEffects(Projectile projectile, Vector2 fallbackDirection, float bladeRotation, float oceanPhase, int stateTimer)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(fallbackDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = GetFrontAnchor(projectile, fallbackDirection);

            if (stateTimer % 3 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    tip - forward * 8f,
                    projectile.velocity * 0.08f,
                    new Color(90, 190, 255),
                    new Vector2(0.7f, 1.8f),
                    bladeRotation - MathHelper.PiOver4,
                    0.18f,
                    0.03f,
                    12);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            SpawnOuterWake(projectile, tip, forward, right, oceanPhase * 0.8f, 0.5f, 2.8f, 10f, stateTimer % 2 == 0, stateTimer % 4 == 0);
        }

        private static void SpawnOuterWake(Projectile projectile, Vector2 tip, Vector2 forward, Vector2 right, float phase, float lateralDrift, float spread, float backOffset, bool emitDust, bool emitBubble)
        {
            float crest = 0.5f + 0.5f * (float)System.Math.Sin(phase * 1.4f + projectile.identity * 0.11f);
            float swell = 0.5f + 0.5f * (float)System.Math.Cos(phase * 1.05f + projectile.identity * 0.19f);
            float lanePush = 0.85f + crest * 0.7f;
            float backLift = 0.1f + swell * 0.22f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 wingOffset = right * side * spread * (0.85f + crest * 0.35f);
                Vector2 spawnPos = tip - forward * backOffset + wingOffset;
                Vector2 wakeVelocity = projectile.velocity * 0.018f + right * side * lateralDrift * lanePush - forward * backLift;

                if (emitDust)
                {
                    Dust water = Dust.NewDustPerfect(
                        spawnPos,
                        DustID.Water,
                        wakeVelocity * Main.rand.NextFloat(1.15f, 1.45f) + Main.rand.NextVector2Circular(0.16f, 0.16f),
                        100,
                        new Color(110, 210, 255),
                        Main.rand.NextFloat(0.84f, 1.06f));
                    water.noGravity = true;
                    water.fadeIn = 1.02f + crest * 0.08f;

                    if (Main.rand.NextBool(2))
                    {
                        Dust frost = Dust.NewDustPerfect(
                            spawnPos - forward * 3f + right * side * spread * 0.12f,
                            DustID.Frost,
                            wakeVelocity * 0.72f + right * side * 0.22f,
                            100,
                            new Color(205, 248, 255),
                            Main.rand.NextFloat(0.72f, 0.9f));
                        frost.noGravity = true;
                    }
                }

                if (emitBubble)
                {
                    Gore bubble = Gore.NewGorePerfect(
                        projectile.GetSource_FromAI(),
                        spawnPos + right * side * (1.4f + crest * 0.9f),
                        projectile.velocity * 0.2f + wakeVelocity * 0.85f + Main.rand.NextVector2Circular(0.35f, 0.35f),
                        Main.rand.NextBool(3) ? 412 : 411);
                    bubble.timeLeft = 8 + Main.rand.Next(6);
                    bubble.scale = Main.rand.NextFloat(0.6f, 1f) * (1.05f + crest * 0.35f);
                }
            }

            if (emitBubble && Main.rand.NextBool(2))
            {
                Gore centerBubble = Gore.NewGorePerfect(
                    projectile.GetSource_FromAI(),
                    tip - forward * (backOffset - 2f),
                    projectile.velocity * 0.18f + right * (float)System.Math.Sin(phase * 1.7f) * 0.55f + Main.rand.NextVector2Circular(0.22f, 0.22f),
                    Main.rand.NextBool(3) ? 412 : 411);
                centerBubble.timeLeft = 7 + Main.rand.Next(5);
                centerBubble.scale = Main.rand.NextFloat(0.52f, 0.82f) * (1.02f + swell * 0.26f);
            }
        }
    }
}
