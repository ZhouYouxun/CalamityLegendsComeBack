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
            projectile.timeLeft = 300;

            // 初速度大幅提升
            projectile.velocity *= 2.5f;
        }
        private int hitCount;
        private int postHitTimer;
        public override void AI(Projectile projectile, Player owner)
        {
            NPC target = projectile.Center.ClosestNPCAt(1200f);

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (target != null)
            {
                Vector2 desiredDir = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

                // ================= 角速度逻辑 =================
                float maxTurn;

                if (hitCount == 0)
                {
                    // 第一次命中前
                    maxTurn = MathHelper.ToRadians(2.7f);
                }
                else
                {
                    // 命中后：5.5 → 0.9（25帧线性下降）
                    float t = MathHelper.Clamp(postHitTimer / 25f, 0f, 1f);

                    float deg = MathHelper.Lerp(5.5f, 0.9f, t);
                    maxTurn = MathHelper.ToRadians(deg);

                    postHitTimer++;
                }

                float currentRot = projectile.velocity.ToRotation();
                float targetRot = desiredDir.ToRotation();

                float newRot = currentRot.AngleTowards(targetRot, maxTurn);

                // ===== 随机扰动 =====
                newRot += Main.rand.NextFloat(-0.08f, 0.08f);

                float speed = projectile.velocity.Length();

                // ===== 速度稳定 =====
                speed = MathHelper.Lerp(speed, 16f, 0.08f);

                projectile.velocity = newRot.ToRotationVector2() * speed;
            }

            // ================= 速度层 =================
            projectile.velocity *= 1.025f; // 常驻

            if (hitCount > 0)
            {
                projectile.velocity *= 1.005f; // 第二层加速
            }
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {

            hitCount++;
            postHitTimer = 0;


            //// ===== 吸血 =====
            //int heal = damageDone / 10;
            //if (heal > 0)
            //{
            //    owner.statLife += heal;
            //    owner.HealEffect(heal);
            //}

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




        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            // ================= 1.主圆环（放射爆散） =================
            int count = 12;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;

                Vector2 dir = angle.ToRotationVector2();
                Vector2 vel = dir * Main.rand.NextFloat(2f, 5f);

                SquishyLightParticle particle = new(
                    center,
                    vel,
                    1.2f,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.3f),
                    18
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // ================= 2.椭圆 Dust X环 =================
            int dustCount = 24;

            float longAxis = 70f;   // ⭐ 长轴（你可以自己调）
            float shortAxis = 30f;  // ⭐ 短轴（你可以自己调）
            float speed = 3f;

            for (int i = 0; i < dustCount; i++)
            {
                float t = MathHelper.TwoPi * i / dustCount;

                // 椭圆1
                Vector2 ellipse1 = new Vector2(
                    (float)Math.Cos(t) * longAxis,
                    (float)Math.Sin(t) * shortAxis
                );

                // 椭圆2（旋转90°形成X）
                Vector2 ellipse2 = ellipse1.RotatedBy(MathHelper.Pi / 2f);

                Vector2 dir1 = ellipse1.SafeNormalize(Vector2.UnitY);
                Vector2 dir2 = ellipse2.SafeNormalize(Vector2.UnitY);

                Vector2 vel1 = dir1 * speed;
                Vector2 vel2 = dir2 * speed;

                // Dust1
                Dust d1 = Dust.NewDustPerfect(
                    center + ellipse1,
                    Main.rand.NextBool() ? 107 : 110,
                    vel1,
                    120,
                    Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1.0f, 2.2f)
                );
                d1.noGravity = true;

                // Dust2
                Dust d2 = Dust.NewDustPerfect(
                    center + ellipse2,
                    Main.rand.NextBool() ? 107 : 110,
                    vel2,
                    120,
                    Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1.0f, 2.2f)
                );
                d2.noGravity = true;
            }
        }







    }
}