using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class LivingShardEffect : DefaultEffect
    {
        public override int EffectID => 15;

        // 生命碎片（你自己替换真实ID）
        public override int AmmoType => ModContent.ItemType<LivingShard>();


        // ===== 三主题色（纯绿色系）=====
        public override Color ThemeColor => new Color(120, 255, 120);
        public override Color StartColor => new Color(180, 255, 180);
        public override Color EndColor => new Color(60, 200, 120);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 穿透
            projectile.penetrate = 6;

            // 初速度大幅提升
            projectile.velocity *= 2.5f;
        }

        // ================= AI（诡异追踪）=================
        public override void AI(Projectile projectile, Player owner)
        {
            NPC target = projectile.Center.ClosestNPCAt(1200f);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (target != null)
            {
                Vector2 desiredDir = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

                // ===== 限制角速度（核心诡异感）=====
                float maxTurn = MathHelper.ToRadians(3.2f);

                float currentRot = projectile.velocity.ToRotation();
                float targetRot = desiredDir.ToRotation();

                float newRot = currentRot.AngleTowards(targetRot, maxTurn);

                // ===== 随机扰动（关键）=====
                newRot += Main.rand.NextFloat(-0.08f, 0.08f);

                float speed = projectile.velocity.Length();

                // ===== 速度保持稳定 =====
                speed = MathHelper.Lerp(speed, 16f, 0.08f);

                projectile.velocity = newRot.ToRotationVector2() * speed;
            }

            // 抵消默认减速
            projectile.velocity *= 1.020408f;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ===== 吸血 =====
            int heal = damageDone / 10;
            if (heal > 0)
            {
                owner.statLife += heal;
                owner.HealEffect(heal);
            }

            // ===== 释放额外弹幕 =====
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<LivingShard_Healing>(),
                projectile.damage,
                0f,
                projectile.owner
            );
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            // ===== 魔法阵：主圆 =====
            int count = 36;
            float radius = 60f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;

                Vector2 pos = center + angle.ToRotationVector2() * radius;

                Particle p = new PointParticle(
                    pos,
                    Vector2.Zero,
                    false,
                    20,
                    1.3f,
                    Color.Lerp(StartColor, EndColor, i / (float)count)
                );
                GeneralParticleHandler.SpawnParticle(p);
            }

            // ===== 内圈旋转结构 =====
            int inner = 18;
            for (int i = 0; i < inner; i++)
            {
                float angle = MathHelper.TwoPi * i / inner + Main.rand.NextFloat(0.2f);

                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f);

                Particle spark = new SparkParticle(
                    center,
                    vel,
                    false,
                    40,
                    1.1f,
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.3f))
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 外扩脉冲 =====
            for (int i = 0; i < 4; i++)
            {
                float rot = MathHelper.TwoPi / 4 * i;

                Particle pulse = new DirectionalPulseRing(
                    center,
                    rot.ToRotationVector2() * 2f,
                    ThemeColor,
                    new Vector2(1.2f, 2.8f),
                    rot,
                    0.15f,
                    0.05f,
                    18
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // ===== 环形 Dust（补充结构）=====
            for (int i = 0; i < 40; i++)
            {
                float angle = MathHelper.TwoPi * i / 40f;

                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 8f);

                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.GreenTorch,
                    vel,
                    0,
                    default,
                    1.4f
                );
                d.noGravity = true;
            }
        }
    }

  
}