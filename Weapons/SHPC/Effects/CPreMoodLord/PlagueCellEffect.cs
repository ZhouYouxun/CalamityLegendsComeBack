using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class PlagueCellEffect : DefaultEffect
    {
        public override int EffectID => 18;
        public override int AmmoType => ModContent.ItemType<PlagueCellCanister>();

        // ===== 瘟疫主题色 =====
        public override Color ThemeColor => new Color(40, 120, 40);
        public override Color StartColor => new Color(110, 220, 90);
        public override Color EndColor => new Color(20, 55, 20);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        public override void AI(Projectile projectile, Player owner)
        {
            SpawnFlightEffects(projectile);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 给命中的敌人顺手挂上瘟疫相关debuff
            target.AddBuff(ModContent.BuffType<Plague>(), 180);
            target.AddBuff(BuffID.Venom, 180);
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ================= 大范围瘟疫爆炸伤害 =================
            int explosionIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile explosion = Main.projectile[explosionIndex];
            explosion.width = 260;
            explosion.height = 260;

            // ================= 核心贴图爆闪 =================
            Color explosionColor = Color.DarkGreen;
            Particle blastRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                explosionColor,
                "CalamityLegendsComeBack/Weapons/SHPC/Effects/CPreMoodLord/BiologicalHazards",
                Vector2.One * 0.33f,
                Main.rand.NextFloat(-10f, 10f),
                0.075f,
                0.40f,
                30
            );
            GeneralParticleHandler.SpawnParticle(blastRing);

            // ================= 生物危害图标主体 =================
            SpawnBiohazardDeathEffect(projectile);

            // ================= 外围瘟疫炸散 Dust =================
            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi / 18f * i;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 10f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                    velocity,
                    0,
                    Color.DarkGreen,
                    Main.rand.NextFloat(1.15f, 1.75f)
                );
                dust.noGravity = true;
            }

            for (int i = 0; i < 26; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextBool() ? DustID.TerraBlade : DustID.GreenTorch,
                    velocity,
                    0,
                    Main.rand.NextBool() ? Color.DarkGreen : Color.LimeGreen,
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }

            // ================= 爆散 3~9 个瘟疫蜜蜂（改为 BasicPlagueBee） =================
            int beeCount = Main.rand.Next(3, 10);

            for (int i = 0; i < beeCount; i++)
            {
                float angle = MathHelper.TwoPi * i / beeCount + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 dir = angle.ToRotationVector2();

                Vector2 beeVelocity =
                    dir * Main.rand.NextFloat(7f, 13f) +
                    Main.rand.NextVector2Circular(1.2f, 1.2f);

                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(6f, 16f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    beeVelocity,
                    ModContent.ProjectileType<BasicPlagueBee>(),
                    (int)(projectile.damage * 0.42f),
                    0f,
                    projectile.owner,
                    0f,     // ai[0]：默认30帧后开始追踪伤害
                    90f,    // ai[1]：瘟疫debuff时长
                    2f      // ai[2]：粒子细节等级
                );
            }

            // ================= 音效 =================
            SoundEngine.PlaySound(SoundID.Item14, projectile.Center);
        }

        // ================= 飞行特效 =================
        private void SpawnFlightEffects(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            // 1. 深绿尾烟：核心拖尾
            if (Main.rand.NextBool(2))
            {
                Color smokeColor = Color.Lerp(
                    new Color(18, 30, 18),
                    new Color(70, 125, 45),
                    Main.rand.NextFloat()
                );

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    projectile.Center - forward * Main.rand.NextFloat(4f, 10f) + Main.rand.NextVector2Circular(4f, 4f),
                    back * Main.rand.NextFloat(0.6f, 2.4f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    smokeColor,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.35f, 0.6f),
                    0.7f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // 2. 绿色病原亮点：在飞行轨迹周围抖动
            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    projectile.Center - forward * Main.rand.NextFloat(2f, 6f) + Main.rand.NextVector2Circular(3f, 3f),
                    back * Main.rand.NextFloat(0.15f, 0.55f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(StartColor, Color.LimeGreen, Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(orb);
            }

            // 3. 少量瘟疫 Dust：提高颗粒质感
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - forward * Main.rand.NextFloat(4f, 9f) + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.GemEmerald,
                    back.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.2f),
                    0,
                    Main.rand.NextBool() ? Color.LimeGreen : Color.DarkGreen,
                    Main.rand.NextFloat(0.85f, 1.25f)
                );
                dust.noGravity = true;
            }

            // 4. 微弱污染照明
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.22f);
        }

        // ================= 生物危害图标式死亡特效 =================
        private void SpawnBiohazardDeathEffect(Projectile projectile)
        {
            Vector2 center = projectile.Center;

            // ===== 中心核心环 =====
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 dir = angle.ToRotationVector2();

                Dust dust = Dust.NewDustPerfect(
                    center + dir * 10f,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                    dir * Main.rand.NextFloat(0.8f, 2f),
                    0,
                    Color.Lerp(Color.LimeGreen, Color.White, 0.25f),
                    Main.rand.NextFloat(1.0f, 1.45f)
                );
                dust.noGravity = true;
            }

            // ===== 三个“月牙”结构 =====
            float mainRadius = 30f;          // 三个瓣中心距离主体中心的距离
            float crescentRadius = 18f;      // 每个瓣自身的半径
            float crescentThickness = 7f;    // 月牙厚度
            float arcSpan = MathHelper.ToRadians(210f); // 月牙张角
            int arcPoints = 22;

            for (int lobe = 0; lobe < 3; lobe++)
            {
                float baseAngle = MathHelper.TwoPi / 3f * lobe - MathHelper.PiOver2;
                Vector2 lobeCenter = center + baseAngle.ToRotationVector2() * mainRadius;

                // 外弧
                for (int i = 0; i < arcPoints; i++)
                {
                    float t = i / (float)(arcPoints - 1);
                    float angle = baseAngle + MathHelper.Pi + MathHelper.Lerp(-arcSpan * 0.5f, arcSpan * 0.5f, t);

                    Vector2 outer = lobeCenter + angle.ToRotationVector2() * crescentRadius;
                    Vector2 dir = (outer - center).SafeNormalize(Vector2.UnitY);

                    Dust outerDust = Dust.NewDustPerfect(
                        outer,
                        Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                        dir * Main.rand.NextFloat(1.5f, 3.5f),
                        0,
                        Main.rand.NextBool() ? Color.LimeGreen : new Color(170, 255, 120),
                        Main.rand.NextFloat(1.0f, 1.45f)
                    );
                    outerDust.noGravity = true;
                }

                // 内弧：制造“月牙缺口”
                for (int i = 0; i < arcPoints - 4; i++)
                {
                    float t = i / (float)(arcPoints - 5);
                    float angle = baseAngle + MathHelper.Pi + MathHelper.Lerp(-arcSpan * 0.38f, arcSpan * 0.38f, t);

                    Vector2 inner = lobeCenter + angle.ToRotationVector2() * (crescentRadius - crescentThickness);
                    Vector2 dir = (inner - center).SafeNormalize(Vector2.UnitY);

                    Dust innerDust = Dust.NewDustPerfect(
                        inner,
                        DustID.TerraBlade,
                        dir * Main.rand.NextFloat(0.8f, 2.2f),
                        0,
                        Color.DarkGreen,
                        Main.rand.NextFloat(0.9f, 1.2f)
                    );
                    innerDust.noGravity = true;
                }

                // 每个瓣再加一个脉冲，增强形体感
                float pulseScale = Main.rand.NextFloat(0.24f, 0.34f);
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    lobeCenter,
                    (lobeCenter - center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.4f, 1.2f),
                    Color.Lerp(Color.LimeGreen, Color.Green, Main.rand.NextFloat()) * 0.8f,
                    new Vector2(1f, 1f),
                    pulseScale - 0.18f,
                    pulseScale,
                    0f,
                    18
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // ===== 外圈污染冲击 =====
            for (int i = 0; i < 3; i++)
            {
                CustomPulse blastRing = new CustomPulse(
                    center,
                    Vector2.Zero,
                    Color.Lerp(StartColor, EndColor, i / 2f),
                    "CalamityMod/Particles/FlameExplosion",
                    Vector2.One,
                    Main.rand.NextFloat(-8f, 8f),
                    0.04f + i * 0.012f,
                    0.22f + i * 0.06f,
                    16 + i * 5
                );
                GeneralParticleHandler.SpawnParticle(blastRing);
            }
        }








    }
}