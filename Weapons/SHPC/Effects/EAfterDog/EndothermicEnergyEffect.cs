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

            if (!state.IsRetreating)
            {
                // ================= 普通飞行冷系尾迹 =================
                float t = Main.GameUpdateCount * 0.16f;

                // 数学双侧冷光
                for (int i = 0; i < 1; i++)
                {
                    float side = i == 0 ? -1f : 1f;
                    float wave = (float)Math.Sin(t + projectile.identity * 0.37f + i * 1.2f) * 4.5f;

                    Vector2 spawnPos = projectile.Center - safeDir * 6f + right * wave * side;
                    Vector2 vel = -safeDir * Main.rand.NextFloat(0.6f, 1.8f) + right * side * Main.rand.NextFloat(0.1f, 0.45f);

                    Color particleColor = Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.25f, 0.75f));
                    float scale = Main.rand.NextFloat(0.7f, 1.15f);
                    int lifetime = Main.rand.Next(20, 32);

                    SquishyLightParticle particle = new(
                        spawnPos,
                        vel,
                        scale,
                        particleColor,
                        lifetime
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }

                // 旋转雾环，频率别太高，保持“冷而轻”
                if (Main.rand.NextBool(2))
                {
                    float angle = t + projectile.identity * 0.21f;
                    float radius = Main.rand.NextFloat(4f, 8f);

                    Vector2 pos = projectile.Center - safeDir * Main.rand.NextFloat(4f, 9f) + angle.ToRotationVector2() * radius;
                    Vector2 vel = -safeDir * Main.rand.NextFloat(0.3f, 1.0f) + right * (float)Math.Sin(angle * 1.6f) * 0.45f;

                    Particle mist = new MediumMistParticle(
                        pos,
                        vel,
                        Color.White,
                        Color.Transparent,
                        Main.rand.NextFloat(0.5f, 0.9f),
                        Main.rand.NextFloat(120f, 180f)
                    );

                    GeneralParticleHandler.SpawnParticle(mist);
                }

                // 少量冰尘补冷感
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(
                        projectile.Center - safeDir * 5f + Main.rand.NextVector2Circular(3f, 3f),
                        Main.rand.NextBool() ? DustID.IceTorch : DustID.GemDiamond,
                        -safeDir * Main.rand.NextFloat(0.2f, 1.2f),
                        0,
                        Color.Lerp(StartColor, Color.White, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.9f, 1.25f)
                    );
                    dust.noGravity = true;
                }
            }
            else
            {
                // ================= 命中后：快速后撤并减速 =================
                state.RetreatTimer++;

                // 后撤减速
                projectile.velocity *= 0.92f;

                float retreatProgress = Utils.GetLerpValue(0f, 18f, state.RetreatTimer, true);
                float t = Main.GameUpdateCount * 0.22f + projectile.identity * 0.31f;

                // 后撤方向：和原前方相反
                Vector2 retreatDir = -state.OriginalForward;
                Vector2 retreatRight = retreatDir.RotatedBy(MathHelper.Pi / 2f);

                // 冷光尾迹：椭圆后喷
                for (int i = 0; i < 3; i++)
                {
                    float progress = i / 2f;
                    float spread = MathHelper.Lerp(-0.55f, 0.55f, progress);
                    float wave = (float)Math.Sin(t + progress * MathHelper.Pi) * 0.18f;

                    Vector2 spawnPos =
                        projectile.Center
                        - retreatDir * Main.rand.NextFloat(2f, 8f)
                        + retreatRight * Main.rand.NextFloat(-5f, 5f);

                    Vector2 vel =
                        retreatDir.RotatedBy(spread + wave) * Main.rand.NextFloat(2.5f, 6.5f) +
                        retreatRight * Main.rand.NextFloat(-0.4f, 0.4f);

                    Color particleColor = Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.35f, 0.9f));
                    float scale = Main.rand.NextFloat(0.8f, 1.3f);
                    int lifetime = Main.rand.Next(26, 40);

                    SquishyLightParticle particle = new(
                        spawnPos,
                        vel,
                        scale,
                        particleColor,
                        lifetime
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }

                // 数学感雾流：以后撤方向为轴心做冷雾卷吸
                if (state.RetreatTimer % 2 == 0)
                {
                    int amount = 2;
                    for (int i = 0; i < amount; i++)
                    {
                        float ratio = i / (float)Math.Max(1, amount - 1);
                        float angle = MathHelper.Lerp(-0.9f, 0.9f, ratio) + (float)Math.Sin(t + ratio * 4f) * 0.12f;

                        Vector2 pos =
                            projectile.Center
                            - retreatDir * Main.rand.NextFloat(3f, 10f)
                            + retreatRight * (float)Math.Sin(t * 1.35f + i * 1.7f) * 6f;

                        Vector2 vel =
                            retreatDir.RotatedBy(angle) * Main.rand.NextFloat(1.0f, 2.6f) +
                            retreatRight * (float)Math.Cos(t + i) * 0.22f;

                        Particle mist = new MediumMistParticle(
                            pos,
                            vel,
                            Color.White,
                            Color.Transparent,
                            MathHelper.Lerp(0.55f, 0.9f, retreatProgress),
                            Main.rand.NextFloat(140f, 210f)
                        );

                        GeneralParticleHandler.SpawnParticle(mist);
                    }
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

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicState state = GetState(projectile);

            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);

            // 只在第一次命中时触发“后撤→再散射”
            if (state.IsRetreating)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // 记录原始正前方，后面散射要用
            state.IsRetreating = true;
            state.RetreatTimer = 0;
            state.OriginalForward = forward;

            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.friendly = false;

            // 往正后方快速飞行
            projectile.velocity = -forward * 18f;

            // ================= 命中时：往前喷射扇形冷爆 =================
            float t = Main.GameUpdateCount * 0.19f;

            // 冷光扇形：带波浪扰动的数学展开
            int particleCount = 18;
            for (int i = 0; i < particleCount; i++)
            {
                float progress = i / (float)(particleCount - 1);
                float angleSpread = MathHelper.Lerp(-0.72f, 0.72f, progress);
                float wave = (float)Math.Sin(t + progress * MathHelper.Pi * 2f) * 0.14f;

                Vector2 ellipseOffset =
                    right * Main.rand.NextFloat(-10f, 10f) +
                    forward * Main.rand.NextFloat(-4f, 6f);

                Vector2 spawnPos = target.Center + ellipseOffset;
                Vector2 velocity = forward.RotatedBy(angleSpread + wave) * Main.rand.NextFloat(4.5f, 10f);

                Color particleColor = Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.25f, 0.9f));
                float scale = Main.rand.NextFloat(0.85f, 1.45f);
                int lifetime = Main.rand.Next(24, 38);

                SquishyLightParticle particle = new(
                    spawnPos,
                    velocity,
                    scale,
                    particleColor,
                    lifetime
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 雾状扇面：玫瑰节奏 + 轻微旋涡
            int mistCount = 10;
            for (int i = 0; i < mistCount; i++)
            {
                float progress = i / (float)(mistCount - 1);
                float angle = MathHelper.Lerp(-0.58f, 0.58f, progress);
                float rose = 1f + 0.22f * (float)Math.Sin(progress * MathHelper.TwoPi * 3f + t);

                Vector2 pos =
                    target.Center +
                    right * MathHelper.Lerp(-14f, 14f, progress) +
                    forward * Main.rand.NextFloat(-4f, 8f);

                Vector2 vel =
                    forward.RotatedBy(angle) * Main.rand.NextFloat(1.2f, 3.2f) * rose +
                    right * (float)Math.Sin(t + i * 0.7f) * 0.18f;

                Particle mist = new MediumMistParticle(
                    pos,
                    vel,
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.65f, 1.0f),
                    Main.rand.NextFloat(150f, 220f)
                );

                GeneralParticleHandler.SpawnParticle(mist);
            }

            // 前喷冰晶 Dust
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.Lerp(-0.65f, 0.65f, i / 15f);
                Vector2 dustVel = forward.RotatedBy(angle) * Main.rand.NextFloat(2f, 6f);

                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.IceTorch : DustID.GemDiamond,
                    dustVel,
                    0,
                    Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.25f, 0.85f)),
                    Main.rand.NextFloat(1.0f, 1.45f)
                );
                dust.noGravity = true;
            }
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 清理当前弹幕实例状态，避免残留
            projectileStates.Remove(projectile.whoAmI);
        }
    }
}