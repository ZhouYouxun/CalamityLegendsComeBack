using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentNebulaEffect : DefaultEffect
    {
        public override int EffectID => 23;

        // 占位
        public override int AmmoType => ItemID.FragmentNebula;

        // ===== 星云主题色 =====
        public override Color ThemeColor => new Color(180, 80, 255);
        public override Color StartColor => new Color(220, 140, 255);
        public override Color EndColor => new Color(120, 40, 200);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 简单追踪 =====
            NPC target = projectile.Center.ClosestNPCAt(900f);

            if (target != null)
            {
                Vector2 desiredDir = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

                float currentRot = projectile.velocity.ToRotation();
                float targetRot = desiredDir.ToRotation();

                float newRot = currentRot.AngleTowards(targetRot, MathHelper.ToRadians(4f));

                float speed = projectile.velocity.Length();

                // 稍微拉到稳定速度
                speed = MathHelper.Lerp(speed, 14f, 0.08f);

                projectile.velocity = newRot.ToRotationVector2() * speed;
            }

            // 抵消默认减速
            projectile.velocity *= 1.03f;

            // ===== 星云拖尾 =====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.PurpleCrystalShard,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    default,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );
                d.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    Color.MediumPurple,
                    Main.rand.NextFloat(1.0f, 1.5f)
                );
                d.noGravity = true;
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= Main.rand.NextFloat(0.5f, 2.5f);
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;
            Vector2 baseVelocity = projectile.velocity;
            Vector2 forward = baseVelocity.SafeNormalize(Vector2.UnitY);

            Color mainColor = ThemeColor;
            Color startColor = StartColor;
            Color endColor = EndColor;
            Color coreColor = Color.White;

            // ===== 数学常量 =====
            float goldenAngle = MathHelper.TwoPi * 0.38196601125f; // 黄金角
            float baseRadius = 54f; // 整体基础半径，明显放大
            float outerRadius = 120f; // 最外层法阵半径，至少三倍宏伟
            float spiralScale = 8.5f;

            // =========================================================
            // 1. 内核：黄金螺旋 Squishy 爆闪
            // =========================================================
            for (int i = 0; i < 34; i++)
            {
                float t = i + 1f;
                float angle = goldenAngle * t;
                float radius = spiralScale * (float)Math.Sqrt(t);

                Vector2 offset = angle.ToRotationVector2() * radius;

                // 阿基米德/黄金螺旋切向速度
                Vector2 radialDir = offset.SafeNormalize(Vector2.UnitX);
                Vector2 tangentDir = radialDir.RotatedBy(MathHelper.Pi / 2f);
                Vector2 velocity = radialDir * (1.2f + t * 0.06f) + tangentDir * (2.2f + t * 0.035f);

                float scale = 0.9f + t * 0.018f;
                int lifetime = 24 + (i % 10);
                Color particleColor = Color.Lerp(startColor, coreColor, 0.32f + 0.68f * (i / 33f));

                SquishyLightParticle particle = new(
                    center + offset,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // =========================================================
            // 2. 主法阵：傅里叶半径调制环
            // r(θ)=R*(1+a cos 3θ+b sin 5θ)
            // =========================================================
            int ringCount = 48;
            for (int i = 0; i < ringCount; i++)
            {
                float theta = MathHelper.TwoPi * i / ringCount;

                float modulation =
                    1f
                    + 0.22f * (float)Math.Cos(3f * theta)
                    + 0.14f * (float)Math.Sin(5f * theta);

                float radius = outerRadius * modulation;
                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * radius;

                // 对 r(θ) 求导得到局部切线方向，形成“阵纹流动”
                float dr =
                    outerRadius *
                    (-0.22f * 3f * (float)Math.Sin(3f * theta)
                    + 0.14f * 5f * (float)Math.Cos(5f * theta));

                Vector2 tangent = new Vector2(
                    dr * (float)Math.Cos(theta) - radius * (float)Math.Sin(theta),
                    dr * (float)Math.Sin(theta) + radius * (float)Math.Cos(theta)
                ).SafeNormalize(Vector2.UnitY);

                // 节点十字星
                CritSpark spark = new CritSpark(
                    pos,
                    tangent * 5.8f + dir * 1.8f,
                    Color.Lerp(coreColor, startColor, 0.45f),
                    Color.Lerp(mainColor, endColor, 0.45f),
                    1.25f,
                    24
                );
                GeneralParticleHandler.SpawnParticle(spark);

                // 法阵线段笔划
                AltSparkParticle sparkLine = new AltSparkParticle(
                    pos - tangent * 18f,
                    tangent * 0.42f,
                    false,
                    14,
                    1.7f,
                    Color.Lerp(startColor, mainColor, 0.5f) * 0.28f
                );
                GeneralParticleHandler.SpawnParticle(sparkLine);

                // 节点 dust
                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.Electric,
                    dir * 1.8f + tangent * 1.2f,
                    0,
                    Color.Lerp(startColor, mainColor, 0.4f),
                    1.45f
                );
                d.noGravity = true;
            }

            // =========================================================
            // 3. 副法阵：李萨如曲线 / Lissajous
            // x=A sin(3t+δ), y=B sin(4t)
            // =========================================================
            int lissajousCount = 54;
            float A = 108f;
            float B = 84f;
            float delta = MathHelper.Pi / 2f;
            for (int i = 0; i < lissajousCount; i++)
            {
                float t = MathHelper.TwoPi * i / lissajousCount;

                Vector2 pos = center + new Vector2(
                    A * (float)Math.Sin(3f * t + delta),
                    B * (float)Math.Sin(4f * t)
                );

                // 导数作为切线方向
                Vector2 deriv = new Vector2(
                    3f * A * (float)Math.Cos(3f * t + delta),
                    4f * B * (float)Math.Cos(4f * t)
                ).SafeNormalize(Vector2.UnitX);

                PointParticle point = new PointParticle(
                    pos,
                    deriv * 4.8f,
                    false,
                    18,
                    1.18f,
                    Color.Lerp(mainColor, startColor, 0.55f)
                );
                GeneralParticleHandler.SpawnParticle(point);

                AltSparkParticle spark = new AltSparkParticle(
                    pos - deriv * 10f,
                    deriv * 0.38f,
                    false,
                    12,
                    1.45f,
                    Color.Lerp(endColor, startColor, 0.5f) * 0.24f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // =========================================================
            // 4. 玫瑰线外花瓣：r = a cos(kθ)，并叠加速度方向投影
            // =========================================================
            int roseCount = 42;
            int k = 7;
            float roseRadius = 132f;
            for (int i = 0; i < roseCount; i++)
            {
                float theta = MathHelper.TwoPi * i / roseCount;
                float rose = roseRadius * (float)Math.Cos(k * theta);
                Vector2 dir = theta.ToRotationVector2();

                // 在弹幕前进方向上做一次投影偏置，让图案既对称又有朝向
                float projection = Vector2.Dot(dir, forward) * 18f;
                Vector2 pos = center + dir * rose + forward * projection;

                Vector2 normal = dir.RotatedBy(MathHelper.Pi / 2f);
                Vector2 velocity = normal * 4.2f + dir * 2.4f;

                CritSpark petalSpark = new CritSpark(
                    pos,
                    velocity,
                    Color.Lerp(coreColor, startColor, 0.5f),
                    Color.Lerp(mainColor, endColor, 0.6f),
                    1.05f,
                    20
                );
                GeneralParticleHandler.SpawnParticle(petalSpark);

                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.PurpleTorch,
                    velocity * 0.55f,
                    0,
                    Color.Lerp(mainColor, endColor, 0.45f),
                    1.35f
                );
                d.noGravity = true;
                d.fadeIn = 0.45f;
            }

            // =========================================================
            // 5. 斯托克斯式“环流感”：两组反向旋涡
            // 用切向速度制造环流，形成有序+无序共存
            // =========================================================
            int vortexCount = 36;
            for (int i = 0; i < vortexCount; i++)
            {
                float theta = MathHelper.TwoPi * i / vortexCount;
                Vector2 dir = theta.ToRotationVector2();

                float radiusA = 42f + i * 2.2f;
                float radiusB = 58f + i * 1.65f;

                Vector2 posA = center + dir * radiusA;
                Vector2 posB = center - dir * radiusB;

                Vector2 tangentA = dir.RotatedBy(MathHelper.Pi / 2f);
                Vector2 tangentB = dir.RotatedBy(-(MathHelper.Pi / 2f));

                SquishyLightParticle particleA = new(
                    posA,
                    tangentA * (3.2f + i * 0.05f),
                    0.82f + i * 0.01f,
                    Color.Lerp(startColor, coreColor, 0.35f),
                    18 + (i % 8)
                );
                GeneralParticleHandler.SpawnParticle(particleA);

                SquishyLightParticle particleB = new(
                    posB,
                    tangentB * (2.8f + i * 0.05f),
                    0.78f + i * 0.012f,
                    Color.Lerp(mainColor, endColor, 0.45f),
                    18 + (i % 7)
                );
                GeneralParticleHandler.SpawnParticle(particleB);
            }

            // =========================================================
            // 6. 前向主爆轰：沿速度方向的锥形多层喷发
            // =========================================================
            int forwardBurstCount = 28;
            for (int i = 0; i < forwardBurstCount; i++)
            {
                float lerp = i / (float)(forwardBurstCount - 1);
                float angleOffset = MathHelper.Lerp(-(MathHelper.Pi / 3f), MathHelper.Pi / 3f, lerp);
                Vector2 dir = forward.RotatedBy(angleOffset);

                float speed = MathHelper.Lerp(7f, 18f, (float)Math.Sin(lerp * MathHelper.Pi));
                Vector2 velocity = dir * speed;

                PointParticle point = new PointParticle(
                    center + dir * Main.rand.NextFloat(6f, 22f),
                    velocity,
                    false,
                    20,
                    1.25f,
                    Color.Lerp(coreColor, startColor, 0.4f + 0.5f * (1f - Math.Abs(lerp - 0.5f) * 2f))
                );
                GeneralParticleHandler.SpawnParticle(point);

                CritSpark spark = new CritSpark(
                    center,
                    velocity * 0.85f,
                    coreColor,
                    Color.Lerp(startColor, mainColor, 0.55f),
                    1.15f,
                    18
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // =========================================================
            // 7. 补充 dust：用极坐标+波形做层层外扩
            // =========================================================
            int dustCount = 72;
            for (int i = 0; i < dustCount; i++)
            {
                float theta = MathHelper.TwoPi * i / dustCount;
                float wave = 0.5f + 0.5f * (float)Math.Sin(6f * theta);
                float radius = baseRadius + 110f * wave;

                Vector2 dir = theta.ToRotationVector2();
                Vector2 pos = center + dir * radius;

                Vector2 velocity = dir * (3.5f + 4.5f * wave) + dir.RotatedBy(MathHelper.Pi / 2f) * 1.8f;

                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.Electric,
                    velocity,
                    0,
                    Color.Lerp(startColor, endColor, wave * 0.55f),
                    1.25f + wave * 0.85f
                );
                d.noGravity = true;
                d.fadeIn = 0.35f;
            }

            // =========================================================
            // 8. 核心终闪：高亮白核
            // =========================================================
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 13f);

                SquishyLightParticle particle = new(
                    center,
                    velocity,
                    Main.rand.NextFloat(1.2f, 1.9f),
                    Color.Lerp(coreColor, startColor, Main.rand.NextFloat(0.2f, 0.55f)),
                    Main.rand.Next(18, 30)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            Lighting.AddLight(center, ThemeColor.ToVector3() * 1.85f);
        }









    }
}