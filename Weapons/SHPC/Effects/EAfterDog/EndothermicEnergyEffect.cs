using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog
{
    internal class EndothermicEnergyEffect : DefaultEffect
    {
        public override int EffectID => 34;

        public override int AmmoType => ModContent.ItemType<EndothermicEnergy>();

        public override Color ThemeColor => new Color(120, 170, 255);
        public override Color StartColor => new Color(220, 240, 255);
        public override Color EndColor => new Color(30, 60, 130);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // 关闭默认减速
        public override bool EnableDefaultSlowdown => false;

        // 每个弹幕实例独立状态，绝不占用 ai / localAI
        private class EndothermicState
        {
            public bool IsRetreating;
            public int RetreatTimer;
            public Vector2 OriginalForward;
        }

        private readonly Dictionary<int, EndothermicState> projectileStates = new();

        // 获取或创建当前弹幕的独立状态
        private EndothermicState GetState(Projectile projectile)
        {
            if (!projectileStates.TryGetValue(projectile.whoAmI, out EndothermicState state))
            {
                state = new EndothermicState();
                projectileStates[projectile.whoAmI] = state;
            }

            return state;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 初速略微强化一点，让“冷矢”更利落
            projectile.velocity *= 1.06f;
            projectile.penetrate = 3;

            EndothermicState state = GetState(projectile);
            state.IsRetreating = false;
            state.RetreatTimer = 0;
            state.OriginalForward = projectile.velocity.SafeNormalize(Vector2.UnitX);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            EndothermicState state = GetState(projectile);

            Vector2 velocity = projectile.velocity;
            Vector2 safeDir = velocity.SafeNormalize(Vector2.UnitX);

            // 注意：这里的 forward 在不同阶段含义不同
            // 正常飞行：forward = 当前前方
            // 后撤阶段：forward = 原始命中前方向（用于“最终朝原前方散射”）
            Vector2 forward = state.IsRetreating ? state.OriginalForward : safeDir;
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // 冷色照明
            Lighting.AddLight(projectile.Center, Color.Lerp(StartColor, ThemeColor, 0.5f).ToVector3() * 0.52f);

            // ================= 当 timeLeft 剩余 100 时，启动后撤与散射流程 =================
            if (!state.IsRetreating && projectile.timeLeft <= 100)
            {
                Vector2 triggerForward = projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 triggerRight = triggerForward.RotatedBy(MathHelper.Pi / 2f);
                float burstTime = (float)Main.GameUpdateCount * 0.19f + projectile.identity * 0.27f;

                state.IsRetreating = true;
                state.RetreatTimer = 0;
                state.OriginalForward = triggerForward;

                projectile.tileCollide = false;
                projectile.penetrate = -1;
                projectile.friendly = false;

                // 往正后方快速飞行
                projectile.velocity = -triggerForward * 18f;

                // ================= 启动瞬间：极寒数学扇爆 =================

                // 少量核心冷光，数量降低
                for (int i = 0; i < 6; i++)
                {
                    float progress = i / 5f;
                    float angleSpread = MathHelper.Lerp(-0.52f, 0.52f, progress);
                    float wave = (float)Math.Sin(burstTime + progress * MathHelper.TwoPi) * 0.08f;

                    Vector2 spawnPos =
                        projectile.Center +
                        triggerRight * MathHelper.Lerp(-8f, 8f, progress) +
                        triggerForward * Main.rand.NextFloat(-3f, 5f);

                    Vector2 particleVelocity =
                        triggerForward.RotatedBy(angleSpread + wave) * Main.rand.NextFloat(4.2f, 8.4f);

                    SquishyLightParticle particle = new(
                        spawnPos,
                        particleVelocity,
                        Main.rand.NextFloat(0.72f, 1.05f),
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.35f, 0.85f)),
                        Main.rand.Next(20, 30)
                    );
                    GeneralParticleHandler.SpawnParticle(particle);
                }

                // 主体使用 GlowSpark 做数学扇切
                int glowCount = 14;
                for (int i = 0; i < glowCount; i++)
                {
                    float progress = i / (float)(glowCount - 1);
                    float angle = MathHelper.Lerp(-0.72f, 0.72f, progress);
                    float wave = (float)Math.Sin(burstTime * 1.65f + progress * MathHelper.TwoPi * 1.5f) * 0.12f;

                    Vector2 spawnPos =
                        projectile.Center +
                        triggerRight * MathHelper.Lerp(-10f, 10f, progress) +
                        triggerForward * Main.rand.NextFloat(-2f, 4f);

                    Vector2 sparkVelocity =
                        triggerForward.RotatedBy(angle + wave) * Main.rand.NextFloat(4.8f, 9.6f) +
                        triggerRight * (float)Math.Sin(burstTime + progress * 6f) * 0.35f;

                    GlowSparkParticle spark = new GlowSparkParticle(
                        spawnPos,
                        sparkVelocity,
                        false,
                        Main.rand.Next(9, 13),
                        Main.rand.NextFloat(0.020f, 0.032f),
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.18f, 0.58f)),
                        new Vector2(1.9f, 1f),
                        true,
                        false,
                        1.15f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // 冰尘扇面：占比提高，强化极寒质感
                int dustCount = 24;
                for (int i = 0; i < dustCount; i++)
                {
                    float progress = i / (float)(dustCount - 1);
                    float angle = MathHelper.Lerp(-0.78f, 0.78f, progress);
                    float rose = 1f + 0.16f * (float)Math.Sin(progress * MathHelper.TwoPi * 3f + burstTime);

                    Vector2 dustVelocity =
                        triggerForward.RotatedBy(angle) * Main.rand.NextFloat(2.8f, 7.2f) * rose +
                        triggerRight * (float)Math.Cos(burstTime + i * 0.35f) * 0.24f;

                    Dust dust = Dust.NewDustPerfect(
                        projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                        Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                        dustVelocity,
                        0,
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.20f, 0.78f)),
                        Main.rand.NextFloat(1.0f, 1.42f)
                    );
                    dust.noGravity = true;
                }
            }

            // 更新阶段用最新的状态重新定义 forward / right
            forward = state.IsRetreating ? state.OriginalForward : projectile.velocity.SafeNormalize(Vector2.UnitX);
            right = forward.RotatedBy(MathHelper.Pi / 2f);

            if (!state.IsRetreating)
            {
                // ================= 普通飞行：减少光粒子，提升 GlowSpark + Dust 比例 =================
                float t = (float)Main.GameUpdateCount * 0.18f + projectile.identity * 0.31f;

                // 低频冷光主核：只保留少量
                if (Main.rand.NextBool(4))
                {
                    float wave = (float)Math.Sin(t * 1.15f) * 4.2f;
                    Vector2 spawnPos = projectile.Center - forward * Main.rand.NextFloat(3f, 7f) + right * wave;
                    Vector2 vel = -forward * Main.rand.NextFloat(0.45f, 1.15f) + right * (float)Math.Cos(t * 1.4f) * 0.16f;

                    SquishyLightParticle particle = new(
                        spawnPos,
                        vel,
                        Main.rand.NextFloat(0.62f, 0.95f),
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.18f, 0.55f)),
                        Main.rand.Next(18, 26)
                    );
                    GeneralParticleHandler.SpawnParticle(particle);
                }

                // 双侧数学切线：以 GlowSpark 为主
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float phase = t + i * 1.13f;
                    float lateral = (float)Math.Sin(phase * 1.35f) * 5.2f;

                    Vector2 spawnPos = projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + right * lateral * side;
                    Vector2 sparkVelocity =
                        (-forward + right * side * 0.42f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 3.9f) +
                        right * side * (float)Math.Cos(phase) * 0.22f;

                    GlowSparkParticle spark = new GlowSparkParticle(
                        spawnPos,
                        sparkVelocity,
                        false,
                        Main.rand.Next(8, 11),
                        Main.rand.NextFloat(0.016f, 0.024f),
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.15f, 0.50f)),
                        new Vector2(1.8f, 1f),
                        true,
                        false,
                        1.08f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // 冰霜 Dust：用数学摆动把尾迹填密
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float phase = t * 0.92f + i * 2.1f;
                    Vector2 dustPos =
                        projectile.Center
                        - forward * Main.rand.NextFloat(4f, 10f)
                        + right * (float)Math.Sin(phase) * Main.rand.NextFloat(2.5f, 5.5f) * side;

                    Vector2 dustVel =
                        (-forward).RotatedBy((float)Math.Sin(phase * 1.4f) * 0.14f) * Main.rand.NextFloat(0.9f, 2.4f) +
                        right * side * Main.rand.NextFloat(0.08f, 0.36f);

                    Dust dust = Dust.NewDustPerfect(
                        dustPos,
                        Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                        dustVel,
                        0,
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.28f, 0.72f)),
                        Main.rand.NextFloat(0.95f, 1.28f)
                    );
                    dust.noGravity = true;
                }

                // 少量冷雾，保留但降低存在感
                if (Main.rand.NextBool(3))
                {
                    float angle = t * 0.85f;
                    float radius = Main.rand.NextFloat(3f, 6f);

                    Vector2 pos = projectile.Center - forward * Main.rand.NextFloat(4f, 8f) + angle.ToRotationVector2() * radius;
                    Vector2 vel = -forward * Main.rand.NextFloat(0.25f, 0.75f) + right * (float)Math.Sin(angle * 1.5f) * 0.18f;

                    Particle mist = new MediumMistParticle(
                        pos,
                        vel,
                        Color.White,
                        Color.Transparent,
                        Main.rand.NextFloat(0.42f, 0.68f),
                        Main.rand.NextFloat(110f, 155f)
                    );

                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }
            else
            {
                // ================= 后撤阶段：时间到后触发，不再由命中直接触发 =================
                state.RetreatTimer++;

                // 后撤减速
                projectile.velocity *= 0.92f;

                float retreatProgress = Utils.GetLerpValue(0f, 18f, state.RetreatTimer, true);
                float t = (float)Main.GameUpdateCount * 0.24f + projectile.identity * 0.31f;

                // 后撤方向：和原前方相反
                Vector2 retreatDir = -state.OriginalForward;
                Vector2 retreatRight = retreatDir.RotatedBy(MathHelper.Pi / 2f);

                // 冷光尾迹：减少 Squishy 数量
                if (state.RetreatTimer % 2 == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float progress = i / 1f;
                        float spread = MathHelper.Lerp(-0.28f, 0.28f, progress);
                        float wave = (float)Math.Sin(t + progress * MathHelper.Pi) * 0.12f;

                        Vector2 spawnPos =
                            projectile.Center
                            - retreatDir * Main.rand.NextFloat(2f, 7f)
                            + retreatRight * Main.rand.NextFloat(-4f, 4f);

                        Vector2 vel =
                            retreatDir.RotatedBy(spread + wave) * Main.rand.NextFloat(2.2f, 5.4f) +
                            retreatRight * Main.rand.NextFloat(-0.28f, 0.28f);

                        SquishyLightParticle particle = new(
                            spawnPos,
                            vel,
                            Main.rand.NextFloat(0.72f, 1.06f),
                            Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.32f, 0.82f)),
                            Main.rand.Next(20, 30)
                        );

                        GeneralParticleHandler.SpawnParticle(particle);
                    }
                }

                // 双侧冷切 GlowSpark：占比提高
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float phase = t * 1.12f + i * 1.6f;

                    Vector2 spawnPos =
                        projectile.Center
                        - retreatDir * Main.rand.NextFloat(2f, 9f)
                        + retreatRight * (float)Math.Sin(phase) * Main.rand.NextFloat(3f, 7f) * side;

                    Vector2 sparkVel =
                        (retreatDir + retreatRight * side * 0.55f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 4.2f) +
                        retreatRight * side * (float)Math.Cos(phase * 1.35f) * 0.28f;

                    GlowSparkParticle spark = new GlowSparkParticle(
                        spawnPos,
                        sparkVel,
                        false,
                        Main.rand.Next(8, 12),
                        Main.rand.NextFloat(0.018f, 0.028f),
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.18f, 0.52f)),
                        new Vector2(1.9f, 1f),
                        true,
                        false,
                        1.10f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // 极寒 Dust：数量提高，做成椭圆后喷
                for (int i = 0; i < 3; i++)
                {
                    float ratio = i / 2f;
                    float angle = MathHelper.Lerp(-0.42f, 0.42f, ratio) + (float)Math.Sin(t + ratio * 4f) * 0.08f;

                    Vector2 dustPos =
                        projectile.Center
                        - retreatDir * Main.rand.NextFloat(3f, 10f)
                        + retreatRight * (float)Math.Sin(t * 1.28f + i * 1.3f) * 5.5f;

                    Vector2 dustVel =
                        retreatDir.RotatedBy(angle) * Main.rand.NextFloat(1.4f, 3.2f) +
                        retreatRight * (float)Math.Cos(t + i) * 0.20f;

                    Dust dust = Dust.NewDustPerfect(
                        dustPos,
                        Main.rand.NextBool(2) ? DustID.IceTorch : DustID.GemDiamond,
                        dustVel,
                        0,
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.22f, 0.78f)),
                        Main.rand.NextFloat(0.95f, 1.35f)
                    );
                    dust.noGravity = true;
                }

                // 后撤到一定距离后，朝“原前方”扇形散射
                if (state.RetreatTimer >= 16 || projectile.velocity.Length() <= 4.5f)
                {
                    Vector2 splitForward = state.OriginalForward;
                    int splitCount = 5;
                    float angleStep = MathHelper.ToRadians(8f);

                    for (int i = -(splitCount / 2); i <= splitCount / 2; i++)
                    {
                        Vector2 shootVelocity = splitForward.RotatedBy(i * angleStep) * 10f;

                        Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            projectile.Center,
                            shootVelocity,
                            ModContent.ProjectileType<EndothermicEnergy_Split>(),
                            (int)(projectile.damage * 0.35f),
                            projectile.knockBack,
                            projectile.owner
                        );
                    }

                    projectile.Kill();
                }
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            EndothermicState state = GetState(projectile);

            // 后撤阶段本体不再造成伤害
            if (state.IsRetreating)
            {
                modifiers.SourceDamage *= 0f;
            }
        }
        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            EndothermicState state = GetState(projectile);

            // 已经进入后撤阶段就不再触发
            if (state.IsRetreating)
                return true;

            // ================= 模拟“命中触发” =================

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            state.IsRetreating = true;
            state.RetreatTimer = 0;
            state.OriginalForward = forward;

            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.friendly = false;

            // 直接压缩到触发阶段
            if (projectile.timeLeft > 100)
                projectile.timeLeft = 100;

            // 往反方向弹开（比NPC命中略弱一点）
            projectile.velocity = -forward * 14f;

            return false; // ❗不执行默认反弹/销毁
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicState state = GetState(projectile);

            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);

            // 第一次命中时，直接把剩余时间压到100
            if (!state.IsRetreating && projectile.timeLeft > 100)
                projectile.timeLeft = 100;
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 清理当前弹幕实例状态，避免残留
            projectileStates.Remove(projectile.whoAmI);
        }
    }
}