using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal static class BBSD_Charge_Effects
    {
        private const string GlowBladeTexture = "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade";

        private static Vector2 BladeForward(Projectile projectile) => (projectile.rotation - MathHelper.PiOver4).ToRotationVector2();

        private static Vector2 WeaponTip(Projectile projectile) => projectile.Center + BladeForward(projectile) * (46f * projectile.scale);

        internal static void SpawnChargeEffects(Projectile projectile, Player owner, Vector2 focusPoint, NPC target, float chargeCompletion, int timer)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = BladeForward(projectile);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = WeaponTip(projectile);
            Vector2 searchAnchor = target?.Center ?? focusPoint;
            float wave = (float)Math.Sin(timer * 0.25f);
            float intensity = 0.4f + chargeCompletion * 0.9f;

            SpawnBladeCore(projectile, tip, forward, right, timer, intensity);
            SpawnMouseSearchHalo(projectile, focusPoint, searchAnchor, timer, chargeCompletion);

            if (target is not null)
                SpawnTargetSense(target, timer, chargeCompletion);

            if (timer % 6 == 0)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    tip + forward * (6f + chargeCompletion * 8f),
                    forward * (0.22f + chargeCompletion * 0.16f),
                    Color.Lerp(new Color(88, 196, 255), Color.White, 0.28f + chargeCompletion * 0.22f),
                    new Vector2(0.38f + chargeCompletion * 0.12f, 1.18f + chargeCompletion * 0.42f),
                    forward.ToRotation(),
                    0.11f + chargeCompletion * 0.03f,
                    0.018f,
                    10 + (int)(chargeCompletion * 5f));
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            if (Main.rand.NextBool(2))
            {
                Dust frost = Dust.NewDustPerfect(
                    tip - forward * Main.rand.NextFloat(2f, 9f) + right * wave * Main.rand.NextFloat(1.6f, 3.8f),
                    Main.rand.NextBool() ? DustID.Frost : DustID.Water,
                    forward * Main.rand.NextFloat(0.65f, 1.9f) + right * Main.rand.NextFloat(-0.45f, 0.45f),
                    100,
                    new Color(180, 245, 255),
                    Main.rand.NextFloat(0.8f, 1.15f) * intensity);
                frost.noGravity = true;
            }
        }

        private static void SpawnBladeCore(Projectile projectile, Vector2 tip, Vector2 forward, Vector2 right, int timer, float intensity)
        {
            float sideWave = (float)Math.Sin(timer * 0.42f + projectile.identity * 0.35f);

            Particle bladeSpark = new CustomSpark(
                tip + right * sideWave * 2.2f,
                projectile.velocity * 0.02f + forward * 0.28f,
                GlowBladeTexture,
                false,
                6,
                0.18f + intensity * 0.02f,
                new Color(162, 242, 255) * 1.05f,
                new Vector2(0.58f, 2.32f),
                glowCenter: true,
                shrinkSpeed: 0.86f,
                glowCenterScale: 0.95f,
                glowOpacity: 0.78f);
            GeneralParticleHandler.SpawnParticle(bladeSpark);

            Particle centerFlare = new CustomSpark(
                projectile.Center,
                projectile.velocity * 0.02f,
                "CalamityLegendsComeBack/Texture/KsTexture/window_04",
                false,
                10,
                0.24f + intensity * 0.03f,
                new Color(150, 232, 255) * 1.55f,
                new Vector2(0.56f, 2.1f),
                glowCenter: true,
                shrinkSpeed: 1.05f,
                glowCenterScale: 0.92f,
                glowOpacity: 0.68f);
            GeneralParticleHandler.SpawnParticle(centerFlare);
        }

        private static void SpawnMouseSearchHalo(Projectile projectile, Vector2 focusPoint, Vector2 searchAnchor, int timer, float chargeCompletion)
        {
            float orbitRadius = MathHelper.Lerp(34f, 18f, chargeCompletion);
            float orbitAngle = timer * 0.11f;
            Vector2 orbitOffset = orbitAngle.ToRotationVector2() * orbitRadius;

            GlowOrbParticle focusOrb = new GlowOrbParticle(
                searchAnchor + orbitOffset,
                orbitOffset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 0.4f,
                false,
                12,
                0.52f + chargeCompletion * 0.14f,
                Color.Lerp(new Color(92, 214, 255), Color.White, 0.25f + chargeCompletion * 0.2f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(focusOrb);

            if (timer % 4 == 0)
            {
                DirectionalPulseRing focusRing = new DirectionalPulseRing(
                    focusPoint,
                    Vector2.Zero,
                    Color.Lerp(new Color(66, 190, 255), Color.White, 0.2f + chargeCompletion * 0.24f),
                    new Vector2(0.5f, 0.8f + chargeCompletion * 0.18f),
                    orbitAngle,
                    0.1f + chargeCompletion * 0.04f,
                    0.015f,
                    12);
                GeneralParticleHandler.SpawnParticle(focusRing);
            }

            if (timer % 3 == 0)
            {
                Vector2 linkVelocity = (searchAnchor - focusPoint).SafeNormalize(Vector2.Zero) * 1.2f;
                Dust trace = Dust.NewDustPerfect(
                    Vector2.Lerp(focusPoint, searchAnchor, 0.35f + 0.2f * (float)Math.Sin(timer * 0.08f)),
                    DustID.GemSapphire,
                    linkVelocity + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    100,
                    new Color(120, 235, 255),
                    Main.rand.NextFloat(0.8f, 1.12f));
                trace.noGravity = true;
            }
        }

        private static void SpawnTargetSense(NPC target, int timer, float chargeCompletion)
        {
            if (timer % 5 == 0)
            {
                DirectionalPulseRing targetRing = new DirectionalPulseRing(
                    target.Center,
                    Vector2.Zero,
                    Color.Lerp(new Color(140, 240, 255), Color.White, 0.35f),
                    new Vector2(0.56f, 1.2f),
                    timer * 0.08f,
                    0.13f + chargeCompletion * 0.03f,
                    0.018f,
                    11);
                GeneralParticleHandler.SpawnParticle(targetRing);
            }

            if (Main.rand.NextBool(2))
            {
                Dust highlight = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.42f, target.height * 0.42f),
                    DustID.Clentaminator_Cyan,
                    Main.rand.NextVector2Circular(0.7f, 0.7f),
                    100,
                    Color.White,
                    Main.rand.NextFloat(1f, 1.25f));
                highlight.noGravity = true;
            }
        }
    }
}
