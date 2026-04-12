using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash
{
    internal static class BrinyBaron_SkillDashTornado_FlightEffects
    {
        private const string GlowBladeTexture = "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade";
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
            float pulseRotation = forward.ToRotation();
            float sideWave = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 18f + projectile.identity * 0.27f);

            for (int i = 0; i < 3; i++)
            {
                Particle pulse = new DirectionalPulseRing(
                    tip - forward * (6f + i * 4f),
                    projectile.velocity * (0.08f + i * 0.02f),
                    Color.Lerp(new Color(55, 175, 255), Color.White, 0.18f),
                    new Vector2(0.85f, 2.55f),
                    pulseRotation,
                    0.28f + i * 0.02f,
                    0.05f,
                    18 - i * 2);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int i = 0; i < 3; i++)
            {
                Particle customLine = new CustomSpark(
                    tip - forward * (8f + i * 2.5f) + right * sideWave * (1.5f + i * 0.35f),
                    projectile.velocity * (0.03f + i * 0.01f),
                    GlowBladeTexture,
                    false,
                    4,
                    0.17f,
                    new Color(145, 235, 255) * 0.95f,
                    new Vector2(0.58f, 2f),
                    glowCenter: true,
                    shrinkSpeed: 0.95f,
                    glowCenterScale: 0.9f,
                    glowOpacity: 0.7f);
                GeneralParticleHandler.SpawnParticle(customLine);
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

            Particle customLine = new CustomSpark(
                tip + right * sideWave * 1.9f,
                projectile.velocity * 0.03f,
                GlowBladeTexture,
                false,
                2,
                0.16f,
                new Color(160, 242, 255) * 0.96f,
                new Vector2(0.56f, 2.15f),
                glowCenter: true,
                shrinkSpeed: 1.2f,
                glowCenterScale: 0.92f,
                glowOpacity: 0.72f);
            GeneralParticleHandler.SpawnParticle(customLine);


        
            Particle centerFlare = new CustomSpark(
                projectile.Center, // 从弹幕中心释放，作为冲刺本体的核心闪耀
                projectile.velocity * 0.02f, // 轻微跟随本体前推，避免静止贴图发死
                "CalamityLegendsComeBack/Texture/KsTexture/window_04", // 新增 flare 贴图路径
                false, // 不受重力影响
                10, // 存活 X 帧
                0.26f, // 保留现有 CustomSpark 的基础大小
                new Color(160, 242, 255) * 1.96f, // 保留现有颜色和强度
                new Vector2(0.56f, 2.15f), // 保留现有纵向拉伸比例
                glowCenter: true, // 保留中心高亮
                shrinkSpeed: 1.2f, // 保留快速收缩速度
                glowCenterScale: 0.92f, // 保留中心发光范围
                glowOpacity: 0.72f); // 保留中心发光透明度
            GeneralParticleHandler.SpawnParticle(centerFlare);

            SpawnOuterWake(projectile, tip, forward, right, oceanPhase, wakeDrift, wakeSpread, 11.5f, stateTimer % 2 == 0, stateTimer % 3 == 0);
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

            Particle customLine = new CustomSpark(
                tip - forward * 9f + right * (float)System.Math.Sin(oceanPhase) * 1.8f,
                projectile.velocity * 0.04f,
                GlowBladeTexture,
                false,
                10,
                0.12f,
                new Color(120, 220, 255) * 0.7f,
                new Vector2(0.45f, 1.4f),
                glowCenter: true,
                glowCenterScale: 0.9f,
                glowOpacity: 0.6f,
                shrinkSpeed: 0.7f);
            GeneralParticleHandler.SpawnParticle(customLine);

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
