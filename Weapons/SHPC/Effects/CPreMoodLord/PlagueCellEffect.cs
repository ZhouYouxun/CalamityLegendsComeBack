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
using System.Collections.Generic;

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






            // 尝试画一个生物危害图标？
            {

                // ================= 超大生物危害图标 Dust =================
                {
                    Vector2 center = projectile.Center;

                    // 整体尺寸直接放大到原来两倍以上
                    float coreRadius = 22f;          // 中心圆环半径
                    float lobeCenterRadius = 78f;    // 三个大瓣中心距离
                    float lobeOuterRadius = 46f;     // 每个大瓣外弧半径
                    float lobeInnerRadius = 24f;     // 每个大瓣内切空半径
                    float arcSpan = MathHelper.ToRadians(228f);

                    int corePoints = 24;
                    int outerArcPoints = 34;
                    int innerArcPoints = 24;

                    // ===== 中心核心环 =====
                    for (int i = 0; i < corePoints; i++)
                    {
                        float angle = MathHelper.TwoPi * i / corePoints;
                        Vector2 dir = angle.ToRotationVector2();
                        Vector2 pos = center + dir * coreRadius;

                        Dust dust = Dust.NewDustPerfect(
                            pos,
                            Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                            dir * Main.rand.NextFloat(1.2f, 2.8f),
                            0,
                            Color.Lerp(Color.LimeGreen, Color.White, 0.22f),
                            Main.rand.NextFloat(1.25f, 1.65f)
                        );
                        dust.noGravity = true;
                    }

                    // ===== 中心圆内部再补一圈，保证图标核心更清晰 =====
                    for (int i = 0; i < 14; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 14f;
                        Vector2 dir = angle.ToRotationVector2();
                        Vector2 pos = center + dir * 12f;

                        Dust dust = Dust.NewDustPerfect(
                            pos,
                            DustID.TerraBlade,
                            dir * Main.rand.NextFloat(0.4f, 1.4f),
                            0,
                            Color.DarkGreen,
                            Main.rand.NextFloat(1.0f, 1.25f)
                        );
                        dust.noGravity = true;
                    }

                    // ===== 三个大月牙瓣 =====
                    for (int lobe = 0; lobe < 3; lobe++)
                    {
                        float baseAngle = MathHelper.TwoPi / 3f * lobe - MathHelper.PiOver2;
                        Vector2 lobeCenter = center + baseAngle.ToRotationVector2() * lobeCenterRadius;

                        // 外弧：亮色，形成清晰大瓣轮廓
                        for (int i = 0; i < outerArcPoints; i++)
                        {
                            float t = i / (float)(outerArcPoints - 1);
                            float angle = baseAngle + MathHelper.Pi + MathHelper.Lerp(-arcSpan * 0.5f, arcSpan * 0.5f, t);

                            Vector2 pos = lobeCenter + angle.ToRotationVector2() * lobeOuterRadius;
                            Vector2 dir = (pos - center).SafeNormalize(Vector2.UnitY);

                            Dust dust = Dust.NewDustPerfect(
                                pos,
                                Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                                dir * Main.rand.NextFloat(2.0f, 4.6f),
                                0,
                                Main.rand.NextBool() ? Color.LimeGreen : new Color(190, 255, 150),
                                Main.rand.NextFloat(1.25f, 1.7f)
                            );
                            dust.noGravity = true;
                        }

                        // 内弧：深色，挖出“月牙缺口”
                        for (int i = 0; i < innerArcPoints; i++)
                        {
                            float t = i / (float)(innerArcPoints - 1);
                            float angle = baseAngle + MathHelper.Pi + MathHelper.Lerp(-arcSpan * 0.36f, arcSpan * 0.36f, t);

                            Vector2 pos = lobeCenter + angle.ToRotationVector2() * lobeInnerRadius;
                            Vector2 dir = (pos - center).SafeNormalize(Vector2.UnitY);

                            Dust dust = Dust.NewDustPerfect(
                                pos,
                                DustID.TerraBlade,
                                dir * Main.rand.NextFloat(0.8f, 2.2f),
                                0,
                                Color.DarkGreen,
                                Main.rand.NextFloat(1.0f, 1.3f)
                            );
                            dust.noGravity = true;
                        }

                        // 每个大瓣根部再补一个小亮点，增强“生物危害”标识辨识度
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 dir = (lobeCenter - center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.28f);
                            Vector2 pos = center + dir * Main.rand.NextFloat(34f, 52f);

                            Dust dust = Dust.NewDustPerfect(
                                pos,
                                DustID.WhiteTorch,
                                dir * Main.rand.NextFloat(0.8f, 2.0f),
                                0,
                                Color.Lerp(Color.White, Color.LimeGreen, Main.rand.NextFloat(0.35f, 0.75f)),
                                Main.rand.NextFloat(1.1f, 1.45f)
                            );
                            dust.noGravity = true;
                        }
                    }

                    // ===== 最外圈少量扩散颗粒，给整体增加“爆开”感，但不破坏图标 =====
                    for (int i = 0; i < 20; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 20f;
                        Vector2 dir = angle.ToRotationVector2();
                        Vector2 pos = center + dir * Main.rand.NextFloat(92f, 118f);

                        Dust dust = Dust.NewDustPerfect(
                            pos,
                            Main.rand.NextBool() ? DustID.GreenTorch : DustID.WhiteTorch,
                            dir * Main.rand.NextFloat(1.2f, 3.2f),
                            0,
                            Main.rand.NextBool() ? Color.DarkGreen : Color.LimeGreen,
                            Main.rand.NextFloat(0.95f, 1.35f)
                        );
                        dust.noGravity = true;
                    }
                }
            }











            // ================= 搜索最近最多3个敌人并标记 =================
            float searchRadius = 10f * 16f;
            List<NPC> targets = new();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float dist = Vector2.Distance(projectile.Center, npc.Center);
                if (dist <= searchRadius)
                    targets.Add(npc);
            }

            // 按距离排序
            targets.Sort((a, b) =>
                Vector2.Distance(projectile.Center, a.Center)
                    .CompareTo(Vector2.Distance(projectile.Center, b.Center))
            );

            // 最多取3个
            int spawnCount = Math.Min(3, targets.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                NPC target = targets[i];

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PlagueCell_Marked>(),
                    projectile.damage,
                    0f,
                    projectile.owner,
                    target.whoAmI // 用 ai[0] 传目标
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