using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofSnowEffect : DefaultEffect
    {
        public override int EffectID => 6;

        public override int AmmoType => ModContent.ItemType<EssenceofEleum>();

        // 冰蓝主题（从图提取）
        public override Color ThemeColor => new Color(120, 220, 255);
        public override Color StartColor => new Color(200, 240, 255);
        public override Color EndColor => new Color(80, 160, 255);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 初速度强化
            projectile.velocity *= 1.5f;

            // 生命周期缩短
            projectile.timeLeft = 40;

            // 提高更新频率（更丝滑）
            projectile.extraUpdates = 1;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 霜冻效果（原版）
            target.AddBuff(BuffID.Frostburn, 180); // 3秒
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 计算前进方向 =====
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ===== 生成液氮区域弹幕（先生成占位）=====
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                forward * projectile.velocity.Length() * 2f, // 速度 = 原速度 ×2
                ModContent.ProjectileType<EssenceofSnow_N2>(), // ⚠️ 你后面实现
                (int)(projectile.damage * 1.25f),
                projectile.knockBack,
                projectile.owner
            );






            // ================= 自定义前向喷射光粒子 =================
            //Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // 椭圆参数（横向扩散 + 前向拉伸）
            float ellipseX = 12f;
            float ellipseY = 6f;

            // 数学节奏
            float t = Main.GameUpdateCount * 0.18f;

            // ===== 前向喷射 SquishyLightParticle =====
            int particleCount = 28;

            for (int i = 0; i < particleCount; i++)
            {
                float progress = i / (float)(particleCount - 1);

                // 椭圆随机点（不是一个点！）
                Vector2 ellipseOffset =
                    right * Main.rand.NextFloat(-ellipseY, ellipseY) +
                    forward * Main.rand.NextFloat(-ellipseX, ellipseX) * 0.25f;

                Vector2 spawnPos = projectile.Center + ellipseOffset;

                // 数学结构喷射（逐渐展开角度）
                float angleSpread = MathHelper.Lerp(-0.6f, 0.6f, progress);
                float wave = (float)Math.Sin(t + progress * MathHelper.Pi) * 0.25f;

                Vector2 dir = forward.RotatedBy(angleSpread + wave);

                Vector2 velocity = dir * Main.rand.NextFloat(6f, 13f);

                float scale = Main.rand.NextFloat(0.9f, 1.5f);

                Color color = Color.Lerp(
                    new Color(120, 220, 255),
                    Color.White,
                    Main.rand.NextFloat()
                );

                SquishyLightParticle particle = new(
                    spawnPos,
                    velocity,
                    scale,
                    color,
                    Main.rand.Next(28, 45)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // ================= 轻型烟雾（跟随喷射） =================
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = forward.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f, 4f);

                Particle smokeL = new HeavySmokeParticle(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 6f),
                    vel,
                    Color.WhiteSmoke,
                    18,
                    Main.rand.NextFloat(0.9f, 1.6f),
                    0.35f,
                    Main.rand.NextFloat(-1f, 1f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smokeL);
            }

            // ================= 冰雪 Dust =================
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel =
                    forward.RotatedByRandom(0.7f) *
                    Main.rand.NextFloat(2f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                    vel,
                    0,
                    Color.Lerp(new Color(180, 240, 255), Color.White, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.6f)
                );

                dust.noGravity = true;
            }



        }










    }
}