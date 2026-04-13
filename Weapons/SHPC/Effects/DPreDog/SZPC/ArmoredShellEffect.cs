using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    internal class ArmoredShellEffect : DefaultEffect
    {
        public override int EffectID => 40;

        public override int AmmoType => ModContent.ItemType<ArmoredShell>();

        // 取自贴图本身的蓝紫主体 + 青蓝高光 + 深蓝暗部
        public override Color ThemeColor => new Color(118, 110, 151);
        public override Color StartColor => new Color(99, 183, 220);
        public override Color EndColor => new Color(31, 28, 65);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // VolterionOrb 实际只认 5 种类型
        private const int OrbTypeCount = 5;

        // 每圈实际喷出的球数量
        private const int OrbProjectileCount = 12;

        // 圆环整体前冲速度
        private const float MiddleForwardSpeed = 24f;
        private const float SideForwardSpeedFactor = 0.7f;

        // 圆环自身膨胀速度
        private const float RingExpandSpeed = 12f;

        // 特效层级数量
        private const int OuterFxCountPerRing = 16;
        private const int InnerFxCountPerRing = 10;

        // ================= 出生时 =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 不在这里直接杀，先记录第一帧标记，等 AI 第一帧时再自杀
            projectile.GetGlobalProjectile<ArmoredShell_GP>().firstFrame = true;
        }

        // ================= 每帧 AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            ArmoredShell_GP gp = projectile.GetGlobalProjectile<ArmoredShell_GP>();

            // 第一帧直接自杀，确保前面的 Effect 同步都已经稳定完成
            if (gp.firstFrame)
            {
                gp.firstFrame = false;
                projectile.Kill();
                return;
            }
        }

        // ================= 死亡时 =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseDirection = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 先放死亡特效
            SpawnRingBurstFX(projectile.Center, baseDirection);

            // 只由拥有者真正生成后续弹幕，避免多人环境重复生成
            if (projectile.owner != Main.myPlayer)
                return;

            if (Main.zenithWorld)
            {
                SpawnOrbRing(projectile, baseDirection, 0f, MiddleForwardSpeed);
                SpawnOrbRing(projectile, baseDirection, (MathHelper.Pi / 6f), MiddleForwardSpeed * SideForwardSpeedFactor);
                SpawnOrbRing(projectile, baseDirection, -(MathHelper.Pi / 6f), MiddleForwardSpeed * SideForwardSpeedFactor);
            }
            else
            {
                SpawnOrbRing(projectile, baseDirection, 0f, MiddleForwardSpeed);
            }
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 120);
        }

        // ================= 生成一个“整体前进 + 自身成环膨胀”的弹幕环 =================
        private static void SpawnOrbRing(Projectile projectile, Vector2 baseDirection, float angleOffset, float forwardSpeed)
        {
            Vector2 ringDirection = baseDirection.RotatedBy(angleOffset);
            Vector2 forwardVelocity = ringDirection * forwardSpeed;

            for (int i = 0; i < OrbProjectileCount; i++)
            {
                float angle = MathHelper.TwoPi / OrbProjectileCount * i;

                // 自身向四周均匀膨胀
                Vector2 radialVelocity = angle.ToRotationVector2() * RingExpandSpeed;
                Vector2 finalVelocity = forwardVelocity + radialVelocity;

                Projectile orb = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    finalVelocity,
                    ModContent.ProjectileType<ArmoredShell_Ball>(),
                    (int)(projectile.damage * 0.1f),
                    projectile.knockBack,
                    projectile.owner,
                    i % OrbTypeCount // 只传 0~4，避免超范围类型
                );

                // 用有规律的角度排列，而不是纯随机
                orb.rotation = angle + ringDirection.ToRotation();
            }
        }

        // ================= 死亡时的圆环喷射特效 =================
        private static void SpawnRingBurstFX(Vector2 center, Vector2 baseDirection)
        {
            SpawnSingleRingFX(center, baseDirection, 0f, 6.6f);

            if (Main.zenithWorld)
            {
                SpawnSingleRingFX(center, baseDirection, (MathHelper.Pi / 6f), 6.6f * SideForwardSpeedFactor);
                SpawnSingleRingFX(center, baseDirection, -(MathHelper.Pi / 6f), 6.6f * SideForwardSpeedFactor);
            }
        }

        private static void SpawnSingleRingFX(Vector2 center, Vector2 baseDirection, float angleOffset, float forwardSpeed)
        {
            Vector2 ringDirection = baseDirection.RotatedBy(angleOffset);
            Vector2 normal = ringDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 forwardVelocity = ringDirection * forwardSpeed;

            // 外层：更宽的椭圆闪电环
            SpawnLayeredBoltDustRing(
                center,
                forwardVelocity,
                ringDirection,
                normal,
                OuterFxCountPerRing,
                7.8f,
                4.6f,
                4.9f,
                1.65f,
                2.8f,
                0f,
                3f
            );

            // 内层：相位错开的第二层，形成双层咬合感
            SpawnLayeredBoltDustRing(
                center,
                forwardVelocity * 0.85f,
                ringDirection,
                normal,
                InnerFxCountPerRing,
                4.9f,
                2.8f,
                3.3f,
                1.2f,
                2.15f,
                MathHelper.Pi / InnerFxCountPerRing,
                2f
            );

            // 中心再补一层向前喷射的束流，让整体不只是“一个圆”
            SpawnForwardArcJets(center, forwardVelocity, ringDirection, normal);
        }

        // ================= 带数学秩序感的分层闪电环 =================
        private static void SpawnLayeredBoltDustRing(
            Vector2 center,
            Vector2 forwardVelocity,
            Vector2 axisX,
            Vector2 axisY,
            int pointCount,
            float majorAxis,
            float minorAxis,
            float radialSpeed,
            float tangentSpeed,
            float dustSpeed,
            float phase,
            float harmonic)
        {
            for (int i = 0; i < pointCount; i++)
            {
                float angle = MathHelper.TwoPi / pointCount * i + phase;

                // 三次谐波调制，让每个点不是机械等强度，而是有节奏起伏
                float wave = 0.78f + 0.22f * (0.5f + 0.5f * MathF.Sin(angle * harmonic + phase * 2f));

                // 椭圆主形
                Vector2 ellipseVector =
                    axisX * MathF.Cos(angle) * majorAxis +
                    axisY * MathF.Sin(angle) * minorAxis;

                // 椭圆切线，用来制造“喷射感”而不是纯放射
                Vector2 tangent =
                    -axisX * MathF.Sin(angle) * majorAxis +
                     axisY * MathF.Cos(angle) * minorAxis;

                Vector2 radialDirection = ellipseVector.SafeNormalize(axisX);
                Vector2 tangentDirection = tangent.SafeNormalize(axisY);

                // 左右交替旋拧，形成双股缠绕感
                float spinSign = i % 2 == 0 ? 1f : -1f;

                Vector2 finalVelocity =
                    forwardVelocity +
                    radialDirection * (radialSpeed * wave) +
                    tangentDirection * (tangentSpeed * spinSign);

                Color color = VolterionOrb.GetColor(i % OrbTypeCount);

                // 小闪电主体
                BoltParticle bolt = new BoltParticle(
                    center + radialDirection * 2f,
                    finalVelocity,
                    false,
                    12 + (i % 4) * 2,
                    0.24f + 0.07f * wave,
                    color,
                    new Vector2(0.42f + 0.12f * wave, 0.82f + 0.16f * wave),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);

                // 外沿 Dust：描出椭圆外轮廓
                Dust shellDust = Dust.NewDustPerfect(
                    center + ellipseVector * 0.18f,
                    DustID.FireworksRGB,
                    finalVelocity * 0.45f + radialDirection * dustSpeed * 0.3f
                );
                shellDust.noGravity = true;
                shellDust.noLight = true;
                shellDust.scale = 1.05f + 0.18f * wave;
                shellDust.fadeIn = 1.1f;
                shellDust.color = Color.Lerp(color, Color.White, 0.28f);

                // 中轴 Dust：补齐中心向外的喷发轨迹
                Dust coreDust = Dust.NewDustPerfect(
                    center,
                    DustID.FireworksRGB,
                    forwardVelocity * 0.35f + tangentDirection * dustSpeed * spinSign + radialDirection * (1.15f + 0.35f * wave)
                );
                coreDust.noGravity = true;
                coreDust.noLight = true;
                coreDust.scale = 0.86f + 0.14f * wave;
                coreDust.fadeIn = 1.02f;
                coreDust.color = color;
            }
        }

        // ================= 中心向前的弧束喷流 =================
        private static void SpawnForwardArcJets(Vector2 center, Vector2 forwardVelocity, Vector2 forward, Vector2 normal)
        {
            for (int i = -2; i <= 2; i++)
            {
                float t = i / 2f;

                // 两侧略分开，中间更强，形成一个向前推进的喷口
                Vector2 offset = normal * (5f * t);
                Vector2 jetVelocity =
                    forwardVelocity * 0.62f +
                    forward * (3.6f - MathF.Abs(t) * 0.65f) +
                    normal * (1.9f * t);

                Color color = Color.Lerp(
                    VolterionOrb.GetColor((i + 10) % OrbTypeCount),
                    Color.White,
                    0.22f
                );

                BoltParticle bolt = new BoltParticle(
                    center + offset,
                    jetVelocity,
                    false,
                    11 + Math.Abs(i),
                    0.22f,
                    color,
                    new Vector2(0.36f, 0.76f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);

                Dust dust = Dust.NewDustPerfect(
                    center + offset * 0.4f,
                    DustID.FireworksRGB,
                    jetVelocity * 0.42f
                );
                dust.noGravity = true;
                dust.noLight = true;
                dust.scale = 0.95f;
                dust.fadeIn = 1.08f;
                dust.color = color;
            }
        }
    }

    // ================= 每个弹幕独立记录第一帧状态 =================
    public class ArmoredShell_GP : GlobalProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}