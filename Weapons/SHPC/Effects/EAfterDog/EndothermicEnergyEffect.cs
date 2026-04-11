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
            public bool PendingShadowRelease;
            public int MarkedTargetIndex = -1;
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
            state.PendingShadowRelease = false;
            state.MarkedTargetIndex = -1;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            Vector2 velocity = projectile.velocity;
            Vector2 safeDir = velocity.SafeNormalize(Vector2.UnitX);
            Vector2 forward = safeDir;
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // 冷色照明
            Lighting.AddLight(projectile.Center, Color.Lerp(StartColor, ThemeColor, 0.5f).ToVector3() * 0.52f);

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

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }
        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            EndothermicState state = GetState(projectile);

            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 180);

            state.PendingShadowRelease = true;
            state.MarkedTargetIndex = target.whoAmI;

            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            EndothermicState state = GetState(projectile);

            if (state.PendingShadowRelease && Main.npc.IndexInRange(state.MarkedTargetIndex))
            {
                NPC target = Main.npc[state.MarkedTargetIndex];
                if (target.active && target.CanBeChasedBy(projectile))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        float distance = Main.rand.NextFloat(170f, 512f);
                        Vector2 spawnOffset = angle.ToRotationVector2() * distance;

                        Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            target.Center + spawnOffset,
                            Vector2.Zero,
                            ModContent.ProjectileType<EndothermicEnergy_Shadow>(),
                            (int)(projectile.damage * 0.42f),
                            projectile.knockBack,
                            projectile.owner,
                            0f,
                            target.whoAmI,
                            angle
                        );
                    }
                }
            }

            // 清理当前弹幕实例状态，避免残留
            projectileStates.Remove(projectile.whoAmI);
        }
    }
}
