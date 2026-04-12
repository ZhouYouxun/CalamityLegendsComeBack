using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal static class BBSwing_Wave_Effect
    {
        public static void SpawnFlightEffects(Projectile projectile, int lifeTimer, int spawnStage, float stageIntensity, float initialSpeed)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float currentSpeed = projectile.velocity.Length();
            float speedRatio = initialSpeed <= 0.001f ? 0f : MathHelper.Clamp(currentSpeed / initialSpeed, 0f, 1f);

            float visualRadius = projectile.width * 0.5f;
            float t = Main.GameUpdateCount * 0.2f;
            float sway = (float)Math.Sin(t * (2.4f + spawnStage * 0.2f)) * MathHelper.Lerp(3f, 9f + spawnStage * 2f, 1f - speedRatio);

            Vector2 wakeAnchor = projectile.Center - forward * MathHelper.Lerp(visualRadius * 0.08f, visualRadius * 0.22f, speedRatio);
            float edgeDistance = visualRadius * MathHelper.Lerp(0.74f, 0.94f, speedRatio);
            float fillDistance = visualRadius * 0.88f;

            int edgeInterval = Math.Max(1, 3 - spawnStage);
            if (lifeTimer % edgeInterval == 0)
            {
                int edgeBursts = spawnStage >= 2 ? 2 : 1;
                for (int burst = 0; burst < edgeBursts; burst++)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 edgePos = wakeAnchor + right * side * (edgeDistance + sway * 0.3f) - forward * burst * visualRadius * 0.08f;
                        Vector2 edgeVelocity =
                            -forward * MathHelper.Lerp(1.8f, 4.8f + spawnStage * 0.8f, speedRatio) +
                            right * side * MathHelper.Lerp(0.55f, 1.85f + spawnStage * 0.2f, speedRatio);

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
            }

            if (lifeTimer % 2 == 0)
            {
                int dustCount = 2 + spawnStage;
                for (int i = 0; i < dustCount; i++)
                {
                    float band = (float)Math.Sqrt(Main.rand.NextFloat());
                    float sideBias = Main.rand.NextFloatDirection();
                    Vector2 dustPos =
                        wakeAnchor +
                        right * sideBias * fillDistance * MathHelper.Lerp(0.18f, 1f, band) -
                        forward * Main.rand.NextFloat(visualRadius * 0.04f, visualRadius * 0.24f);

                    Vector2 dustVelocity =
                        -forward * Main.rand.NextFloat(1f, MathHelper.Lerp(2.4f, 4.8f + spawnStage * 0.7f, speedRatio)) +
                        right * sideBias * Main.rand.NextFloat(0.1f, 1.25f + spawnStage * 0.12f);

                    Dust wakeDust = Dust.NewDustPerfect(
                        dustPos,
                        Main.rand.NextBool(4) ? DustID.Frost : DustID.Water,
                        dustVelocity,
                        0,
                        Color.Lerp(new Color(105, 205, 255), new Color(215, 248, 255), Main.rand.NextFloat(0.15f, 0.9f)),
                        MathHelper.Lerp(0.88f, 1.25f, speedRatio) * Main.rand.NextFloat(0.92f, 1.12f));
                    wakeDust.noGravity = true;
                }
            }

            int driftInterval = Math.Max(2, 4 - spawnStage);
            if (speedRatio < 0.78f && lifeTimer % driftInterval == 0)
            {
                float driftBand = Main.rand.NextFloatDirection();
                Vector2 driftPos =
                    wakeAnchor +
                    right * driftBand * visualRadius * Main.rand.NextFloat(0.25f, 0.84f) -
                    forward * Main.rand.NextFloat(visualRadius * 0.12f, visualRadius * 0.34f) +
                    right * sway * 0.4f;

                Vector2 driftVelocity =
                    -forward * MathHelper.Lerp(0.5f, 1.55f + spawnStage * 0.2f, speedRatio) +
                    right * driftBand * Main.rand.NextFloat(0.08f, 0.5f);

                GlowOrbParticle slowOrb = new GlowOrbParticle(
                    driftPos,
                    driftVelocity,
                    false,
                    Main.rand.Next(10, 16),
                    MathHelper.Lerp(0.35f, 0.62f, 1f - speedRatio),
                    Color.Lerp(new Color(80, 170, 255), new Color(220, 250, 255), 1f - speedRatio),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(slowOrb);

                Dust slowDust = Dust.NewDustPerfect(
                    driftPos + Main.rand.NextVector2Circular(visualRadius * 0.08f, visualRadius * 0.08f),
                    DustID.Water,
                    driftVelocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.8f, 1.15f),
                    0,
                    new Color(195, 245, 255),
                    MathHelper.Lerp(0.85f, 1.16f, 1f - speedRatio));
                slowDust.noGravity = true;
            }

            if (spawnStage >= 1 && lifeTimer % 3 == 0)
            {
                // =========================
                // 位置：整体后移一点（形成“滞后感”）
                // =========================
                Vector2 sparkPos =
                    projectile.Center
                    - forward * visualRadius * 0.18f // 👉 向后拖（核心：滞后感来源）
                    + right * Main.rand.NextFloatDirection() * visualRadius * Main.rand.NextFloat(0.35f, 0.7f); // 👉 横向分布范围（收窄）

                // =========================
                // 速度：更收敛、更贴波面
                // =========================
                Vector2 sparkVelocity =
                    -forward * Main.rand.NextFloat(2.0f, 3.6f) * stageIntensity // 👉 主方向速度（减弱，避免太炸）
                    + right * Main.rand.NextFloatDirection() * Main.rand.NextFloat(0.2f, 0.7f); // 👉 横向扰动（减弱）

                GeneralParticleHandler.SpawnParticle(
                    new GlowSparkParticle(
                        sparkPos, // 📍 生成位置（已做“滞后”处理）

                        sparkVelocity, // 🚀 粒子速度（决定朝向 + 拉伸方向）

                        false, // 🧲 是否受重力（false = 完全能量体）

                        Main.rand.Next(6, 9), // ⏳ 生命周期（原8~12 → 缩短，让更干净）

                        0.055f * stageIntensity, // 📏 尺寸（原0.09 → 缩小）

                        Color.Lerp(
                            new Color(120, 220, 255), // 基础青色
                            Color.White,
                            0.22f // 👉 白色混合比例（原0.35 → 降低亮度）
                        ),

                        new Vector2(
                            3.6f, // 👉 横向宽度（变大一点，让它不是点）
                            0.28f // 👉 纵向厚度（压扁 → 形成“线/带”）
                        ),

                        true // ✨ 是否发光（保持true作为主体）
                    )
                );
            }
        }

        public static void SpawnHitEffects(Projectile projectile, int spawnStage, float stageIntensity)
        {
            Vector2 pos = projectile.Center;
            float radius = projectile.width * 0.4f;

            for (int i = 0; i < 4 + spawnStage * 2; i++)
            {
                Vector2 spawnPos = pos + Main.rand.NextVector2Circular(radius, radius);
                Vector2 vel = -projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.42f) * Main.rand.NextFloat(2.4f, 5.6f);

                Dust impactDust = Dust.NewDustPerfect(
                    spawnPos,
                    Main.rand.NextBool() ? DustID.Frost : DustID.Water,
                    vel,
                    0,
                    Color.Lerp(new Color(95, 195, 255), Color.White, Main.rand.NextFloat(0.25f, 0.8f)),
                    Main.rand.NextFloat(0.95f, 1.25f) * stageIntensity);
                impactDust.noGravity = true;
            }
        }
    }
}
