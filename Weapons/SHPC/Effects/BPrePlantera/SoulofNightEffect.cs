using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class SoulofNightEffect : DefaultEffect
    {
        public override int EffectID => 10;
        public override int AmmoType => ItemID.SoulofNight;

        // ===== 暗紫色 =====
        public override Color ThemeColor => new Color(90, 0, 120);
        public override Color StartColor => new Color(140, 40, 180);
        public override Color EndColor => new Color(40, 0, 60);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ===== 箭头形6发 =====
            for (int i = 0; i < 6; i++)
            {
                float offset = (i - 2.5f) * 0.25f;

                Vector2 dir = forward.RotatedBy(offset);

                // 中间更快，两侧更慢
                float speed = MathHelper.Lerp(16f, 10f, Math.Abs(offset) / 0.625f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    dir * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    1 // 套用第1套预设
                );
            }


            {

                // ================= 暗影交叉爆散特效 =================

                // 以前进方向为基准
                forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

                // ===== 1. 中心先来一点暗影核心爆闪 =====
                for (int i = 0; i < 6; i++)
                {
                    Vector2 burstVel = Main.rand.NextVector2Circular(2.2f, 2.2f);

                    SquishyLightParticle core = new(
                        projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        burstVel,
                        Main.rand.NextFloat(0.7f, 1.0f),
                        Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.2f, 0.7f)),
                        Main.rand.Next(20, 28)
                    );
                    GeneralParticleHandler.SpawnParticle(core);
                }

                // ===== 2. 两条“叉叉”弧线主体 =====
                // 思路：
                // 一条从左往右滑过去
                // 一条从右往左滑过去
                // 都朝前喷，因此会在前方中央交叉穿过彼此
                int arcCount = 11;
                float forwardStep = 8f;   // 每段往前推进多少
                float sideStrength = 22f; // 左右展开强度

                for (int i = 0; i < arcCount; i++)
                {
                    float t = i / (float)(arcCount - 1); // 0~1
                    float forwardOffset = MathHelper.Lerp(6f, 88f, t);

                    // 抛物线侧向量：两端大，中间小，这样更像交叉弧线
                    float sideAmount = (float)Math.Sin(t * MathHelper.Pi) * sideStrength;

                    // ===== 弧线A：从左往右 =====
                    Vector2 posA = projectile.Center + forward * forwardOffset - right * sideAmount;
                    Vector2 velA =
                        forward * Main.rand.NextFloat(1.8f, 3.2f) +
                        right * MathHelper.Lerp(1.8f, -1.8f, t);

                    SquishyLightParticle arcA = new(
                        posA,
                        velA,
                        MathHelper.Lerp(0.72f, 0.38f, t),
                        Color.Lerp(ThemeColor, StartColor, 0.35f + 0.35f * (1f - t)),
                        Main.rand.Next(18, 26)
                    );
                    GeneralParticleHandler.SpawnParticle(arcA);

                    // ===== 弧线B：从右往左 =====
                    Vector2 posB = projectile.Center + forward * forwardOffset + right * sideAmount;
                    Vector2 velB =
                        forward * Main.rand.NextFloat(1.8f, 3.2f) +
                        right * MathHelper.Lerp(-1.8f, 1.8f, t);

                    SquishyLightParticle arcB = new(
                        posB,
                        velB,
                        MathHelper.Lerp(0.72f, 0.38f, t),
                        Color.Lerp(ThemeColor, StartColor, 0.35f + 0.35f * (1f - t)),
                        Main.rand.Next(18, 26)
                    );
                    GeneralParticleHandler.SpawnParticle(arcB);
                }

                // ===== 3. 用 Spark 补“切割感”和交叉中心的锐利感 =====
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 13f;
                    float forwardOffset = MathHelper.Lerp(10f, 84f, t);

                    // 越靠近中段越集中，突出“X”交叉中心
                    float crossTightness = (float)Math.Sin(t * MathHelper.Pi) * 10f;

                    Vector2 sparkPos1 = projectile.Center + forward * forwardOffset - right * crossTightness;
                    Vector2 sparkPos2 = projectile.Center + forward * forwardOffset + right * crossTightness;

                    Particle spark1 = new SparkParticle(
                        sparkPos1,
                        forward * Main.rand.NextFloat(2.5f, 4.8f) + right * Main.rand.NextFloat(-1.2f, 1.2f),
                        false,
                        Main.rand.Next(16, 24),
                        Main.rand.NextFloat(0.7f, 1.0f),
                        Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.35f, 0.8f))
                    );
                    GeneralParticleHandler.SpawnParticle(spark1);

                    Particle spark2 = new SparkParticle(
                        sparkPos2,
                        forward * Main.rand.NextFloat(2.5f, 4.8f) + right * Main.rand.NextFloat(-1.2f, 1.2f),
                        false,
                        Main.rand.Next(16, 24),
                        Main.rand.NextFloat(0.7f, 1.0f),
                        Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.35f, 0.8f))
                    );
                    GeneralParticleHandler.SpawnParticle(spark2);
                }

                // ===== 4. 在交叉中心再补一点更亮的切点 =====
                Vector2 crossCenter = projectile.Center + forward * 44f;
                for (int i = 0; i < 5; i++)
                {
                    SquishyLightParticle crossFlash = new(
                        crossCenter + Main.rand.NextVector2Circular(6f, 6f),
                        Main.rand.NextVector2Circular(1.2f, 1.2f),
                        Main.rand.NextFloat(0.45f, 0.65f),
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.15f, 0.35f)),
                        Main.rand.Next(14, 20)
                    );
                    GeneralParticleHandler.SpawnParticle(crossFlash);
                }

            }






        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }
        public override void AI(Projectile projectile, Player owner) { }
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
    }
}