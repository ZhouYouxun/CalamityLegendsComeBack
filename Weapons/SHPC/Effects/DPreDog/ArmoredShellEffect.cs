using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class ArmoredShellEffect : DefaultEffect
    {
        public override int EffectID => 40;

        public override int AmmoType => ModContent.ItemType<ArmoredShell>();

        // 取自你发来的贴图：蓝紫主体 + 青蓝高光 + 深蓝暗部
        public override Color ThemeColor => new Color(118, 110, 151);
        public override Color StartColor => new Color(99, 183, 220);
        public override Color EndColor => new Color(31, 28, 65);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // VolterionOrb 一共正好 5 种 OrbType
        private const int OrbCountPerRing = 5;

        // 圆环“整体飞行速度”
        private const float MiddleForwardSpeed = 24f;
        private const float SideForwardSpeedFactor = 0.7f;

        // 圆环“膨胀速度”
        private const float RingExpandSpeed = 12f;

        // OnKill 里的额外圆环特效数量
        private const int FxCountPerRing = 12;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseDirection = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 先放 OnKill 特效，只在死亡时触发
            SpawnRingBurstFX(projectile.Center, baseDirection);

            // 只由弹幕拥有者真正生成新弹幕，避免多人下重复生成
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

        // ================= 生成一个会“整体前进 + 自身成环膨胀”的 VolterionOrb 圆环 =================
        private static void SpawnOrbRing(Projectile projectile, Vector2 baseDirection, float angleOffset, float forwardSpeed)
        {
            Vector2 ringDirection = baseDirection.RotatedBy(angleOffset);
            Vector2 forwardVelocity = ringDirection * forwardSpeed;

            for (int i = 0; i < 12; i++) // 一圈多少个？
            {
                float angle = MathHelper.TwoPi / 12 * i; // 平分一圈的角度
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
                    i // OrbType：0~4，直接继承 VolterionShot 那套逻辑
                );

                // 让每颗球自带一点随机转角，和原版 VolterionShot 发射 VolterionOrb 的感觉一致
                orb.rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            }
        }

        // ================= OnKill 专属的圆环喷射特效 =================
        private static void SpawnRingBurstFX(Vector2 center, Vector2 baseDirection)
        {
            SpawnSingleRingFX(center, baseDirection, 0f, 6f);

            if (Main.zenithWorld)
            {
                SpawnSingleRingFX(center, baseDirection, (MathHelper.Pi / 6f), 6f * SideForwardSpeedFactor);
                SpawnSingleRingFX(center, baseDirection, -(MathHelper.Pi / 6f), 6f * SideForwardSpeedFactor);
            }
        }

        private static void SpawnSingleRingFX(Vector2 center, Vector2 baseDirection, float angleOffset, float forwardSpeed)
        {
            Vector2 ringDirection = baseDirection.RotatedBy(angleOffset);
            Vector2 forwardVelocity = ringDirection * forwardSpeed;

            for (int i = 0; i < FxCountPerRing; i++)
            {
                float angle = MathHelper.TwoPi / FxCountPerRing * i;
                Vector2 radialVelocity = angle.ToRotationVector2() * 4.5f;
                Vector2 finalVelocity = forwardVelocity + radialVelocity;

                Color color = VolterionOrb.GetColor(i % 5);

                // 细一点的闪电粒子，别太张扬
                BoltParticle bolt = new BoltParticle(
                    center,
                    finalVelocity,
                    false,
                    12,
                    0.28f,
                    color,
                    new Vector2(0.45f, 0.85f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);

                // 再补一点 Dust，让圆环轮廓更清楚
                Dust dust = Dust.NewDustPerfect(center, DustID.FireworksRGB, finalVelocity * 0.55f);
                dust.noGravity = true;
                dust.noLight = true;
                dust.scale = 1.05f;
                dust.color = color;
            }
        }
    }
}