using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class TwistingNetherEffect : DefaultEffect
    {
        public override int EffectID => 33;

        public override int AmmoType => ModContent.ItemType<TwistingNether>();

        public override Color ThemeColor => new Color(30, 0, 40);
        public override Color StartColor => new Color(120, 40, 160);
        public override Color EndColor => new Color(5, 0, 10);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 1.3f;

        // ===== 自定义计数器（禁止用localAI）=====
        private float lemniscateAngle;
        private int targetIndex = -1;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = 5;
            projectile.timeLeft = 500;
            projectile.velocity *= 2.5f;

            lemniscateAngle = 0f;
            targetIndex = -1;
        }
        public override bool EnableDefaultSlowdown => false;
        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 1. 额外加速 =====
            //projectile.velocity *= 1.023f;

            // ===== 2. 获取目标 =====
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].active)
            {
                float maxDist = 800f;
                NPC found = null;

                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;

                    float dist = Vector2.Distance(projectile.Center, npc.Center);
                    if (dist < maxDist)
                    {
                        maxDist = dist;
                        found = npc;
                    }
                }

                if (found != null)
                    targetIndex = found.whoAmI;
            }

            // ===== 3. ♾️ 伯努利双纽线追踪 =====
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[targetIndex];

                if (target.active)
                {
                    lemniscateAngle += MathHelper.TwoPi / 60f;

                    // ===== Hex 轨迹（完全照搬 EternityHex 行为）=====

                    // 时间推进
                    lemniscateAngle += MathHelper.TwoPi / 60f;

                    // 伯努利双纽线
                    float scale = 2f / (3f - (float)Math.Cos(2 * lemniscateAngle));

                    float radius = 80f; // 你可以调大小
                                      
                    // ===== 主轴旋转（随时间旋转整个∞）=====
                    float axisRotation = Main.GlobalTimeWrappedHourly * 0.6f; // 旋转速度可调

                    Vector2 rawOffset = scale * new Vector2(
                        (float)Math.Cos(lemniscateAngle),
                        (float)Math.Sin(2f * lemniscateAngle) / 2f
                    ) * radius;

                    // 👉 整体旋转
                    Vector2 offset = rawOffset.RotatedBy(axisRotation);

                    // 👉 直接锁位置（关键！！！）
                    projectile.Center = target.Center + offset;

                    // 👉 手动算velocity（只为了旋转 & 特效）
                    projectile.velocity = offset.SafeNormalize(Vector2.UnitX) * projectile.velocity.Length();


                }
            }

            {
                // ===== 4. 飞行特效（重做版：Hex / Nadir / InfiniteDarkness 混合黑暗风）=====
                Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);
                float t = Main.GlobalTimeWrappedHourly * 6f;

                // ① 主尾流：黑色高速裂流
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spawnPos = projectile.Center
                            - forward * Main.rand.NextFloat(6f, 20f)
                            + right * Main.rand.NextFloat(-8f, 8f);

                        Vector2 velocity = (-forward).RotatedByRandom(0.55f) * Main.rand.NextFloat(1.4f, 5.6f);

                        Particle altSpark = new AltSparkParticle(
                            spawnPos,
                            velocity,
                            false,
                            Main.rand.Next(10, 18),
                            Main.rand.NextFloat(0.7f, 1.2f),
                            Main.rand.NextBool(3) ? Color.Black : new Color(35, 15, 45)
                        );
                        GeneralParticleHandler.SpawnParticle(altSpark);
                    }
                }

                // ② 黑烟拖尾层
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 smokePos = projectile.Center
                            - forward * Main.rand.NextFloat(4f, 16f)
                            + right * Main.rand.NextFloat(-10f, 10f);

                        Vector2 smokeVel = (-forward).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.15f, 1.4f);

                        Particle smoke = new HeavySmokeParticle(
                            smokePos,
                            smokeVel,
                            Main.rand.NextBool(2) ? Color.Black : new Color(40, 20, 55),
                            Main.rand.Next(16, 28),
                            Main.rand.NextFloat(0.45f, 0.9f),
                            0.42f,
                            Main.rand.NextFloat(-0.06f, 0.06f),
                            false
                        );
                        GeneralParticleHandler.SpawnParticle(smoke);
                    }
                }

                // ③ ProvidenceMarkParticle 点缀（染成黑色）
                if (Main.rand.NextBool(3))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spawnPos = projectile.Center
                            - forward * Main.rand.NextFloat(2f, 14f)
                            + right * Main.rand.NextFloat(-12f, 12f);

                        Vector2 velocity = (-forward).RotatedByRandom(0.65f) * Main.rand.NextFloat(1.5f, 4.8f);

                        Particle spark = new CustomSpark(
                            spawnPos,
                            velocity,
                            "CalamityMod/Particles/ProvidenceMarkParticle",
                            false,
                            Main.rand.Next(16, 24),
                            Main.rand.NextFloat(0.85f, 1.2f),
                            Color.Lerp(Color.Black, new Color(35, 35, 35), 0.5f + 0.5f * (float)Math.Sin(t + i * 0.9f)),
                            new Vector2(Main.rand.NextFloat(1.1f, 1.45f), Main.rand.NextFloat(0.28f, 0.5f)),
                            true,
                            false,
                            Main.rand.NextFloat(-0.08f, 0.08f),
                            false,
                            false,
                            0.08f
                        );
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }

                // ④ 双螺旋虚空尘带（围绕飞行轴扭动）
                if (Main.rand.NextBool(2))
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float wave = (float)Math.Sin(t * 1.8f + projectile.identity * 0.37f + side * 1.2f);
                        Vector2 ribbonOffset = right * wave * 10f * side;
                        Vector2 dustPos = projectile.Center - forward * Main.rand.NextFloat(2f, 10f) + ribbonOffset;
                        Vector2 dustVel = (-forward * Main.rand.NextFloat(0.8f, 2.4f) + right * side * Main.rand.NextFloat(0.2f, 1.1f));

                        int dust = Dust.NewDust(dustPos, 1, 1, DustID.Shadowflame, dustVel.X, dustVel.Y, 0, default, Main.rand.NextFloat(1.0f, 1.5f));
                        Main.dust[dust].noGravity = true;
                    }
                }

                // ⑤ Hex感环绕火花（沿当前双纽线相位做摆动）
                if (Main.rand.NextBool(4))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float phase = lemniscateAngle + i * MathHelper.TwoPi / 3f;
                        Vector2 orbitOffset = new Vector2((float)Math.Cos(phase), (float)Math.Sin(phase)) * Main.rand.NextFloat(6f, 14f);
                        Vector2 spawnPos = projectile.Center + orbitOffset;
                        Vector2 velocity = (orbitOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.4f, 1.6f)) - forward * Main.rand.NextFloat(0.6f, 1.8f);

                        int dust = Dust.NewDust(spawnPos, 1, 1, DustID.PurpleTorch, velocity.X, velocity.Y, 0, default, Main.rand.NextFloat(0.9f, 1.35f));
                        Main.dust[dust].noGravity = true;
                    }
                }

                // ⑥ 中心呼吸暗核
                if (Main.rand.NextBool(5))
                {
                    Particle pulse = new CustomPulse(
                        projectile.Center,
                        Vector2.Zero,
                        Main.rand.NextBool(2) ? Color.Black : new Color(28, 8, 36),
                        "CalamityMod/Particles/SmallBloom",
                        Vector2.One,
                        Main.rand.NextFloat(-0.12f, 0.12f),
                        Main.rand.NextFloat(0.28f, 0.45f),
                        0f,
                        Main.rand.Next(10, 16),
                        false
                    );
                    GeneralParticleHandler.SpawnParticle(pulse);
                }
            }
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }


        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 找最近敌人（优先锁定目标）=====
            NPC target = null;

            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC npc = Main.npc[targetIndex];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                    target = npc;
            }

            // 如果锁定目标无效，就找最近的
            if (target == null)
            {
                float maxDist = 800f;
                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;

                    float dist = Vector2.Distance(projectile.Center, npc.Center);
                    if (dist < maxDist)
                    {
                        maxDist = dist;
                        target = npc;
                    }
                }
            }

            // ===== 如果有目标，执行终结斩 + 特效 =====
            if (target != null)
            {
                Vector2 forward = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);
                float t = Main.GlobalTimeWrappedHourly * 7f;

                // ===== 终结斩（3倍伤害）=====
                int damage = (int)(projectile.damage * 3f);

                NPC.HitInfo hit = new NPC.HitInfo
                {
                    Damage = damage,
                    Knockback = projectile.knockBack,
                    HitDirection = projectile.direction,
                    Crit = Main.rand.NextBool()
                };

                target.StrikeNPC(hit);

                // ===== 以下为原 OnHitNPC 特效，整体搬迁 =====

                // ① 中心暗核脉冲
                for (int i = 0; i < 3; i++)
                {
                    Particle pulse = new CustomPulse(
                        target.Center,
                        Vector2.Zero,
                        i == 0 ? Color.Black : new Color(35, 12, 48),
                        i == 2 ? "CalamityMod/Particles/LargeBloom" : "CalamityMod/Particles/SmallBloom",
                        Vector2.One,
                        Main.rand.NextFloat(-0.22f, 0.22f),
                        0.6f + i * 0.35f,
                        0f,
                        18 + i * 8,
                        false
                    );
                    GeneralParticleHandler.SpawnParticle(pulse);
                }

                // ② 贯穿火花
                for (int i = 0; i < 14; i++)
                {
                    Vector2 dir = forward.RotatedByRandom(0.95f) * Main.rand.NextFloat(-6.5f, 6.5f);

                    Particle spark = new CustomSpark(
                        target.Center + Main.rand.NextVector2Circular(10f, 10f),
                        dir,
                        "CalamityMod/Particles/GlowSpark2",
                        false,
                        Main.rand.Next(12, 20),
                        Main.rand.NextFloat(0.02f, 0.05f),
                        Main.rand.NextBool(3) ? Color.Black : new Color(40, 18, 60),
                        new Vector2(Main.rand.NextFloat(1.05f, 1.65f), Main.rand.NextFloat(0.26f, 0.6f)),
                        false,
                        shrinkSpeed: 1.06f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // ③ ProvidenceMark
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spawnPos = target.Center + Main.rand.NextVector2Circular(18f, 18f);
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5.2f);

                    Particle spark = new CustomSpark(
                        spawnPos,
                        velocity,
                        "CalamityMod/Particles/ProvidenceMarkParticle",
                        false,
                        Main.rand.Next(16, 24),
                        Main.rand.NextFloat(0.85f, 1.2f),
                        Color.Lerp(Color.Black, new Color(35, 35, 35), 0.5f + 0.5f * (float)Math.Sin(t + i * 0.9f)),
                        new Vector2(Main.rand.NextFloat(1.1f, 1.45f), Main.rand.NextFloat(0.28f, 0.5f)),
                        true,
                        false,
                        Main.rand.NextFloat(-0.08f, 0.08f),
                        false,
                        false,
                        0.08f
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // ④ 黑烟
                for (int i = 0; i < 12; i++)
                {
                    Vector2 smokeVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.8f, 4.5f);

                    Particle smoke = new HeavySmokeParticle(
                        target.Center + Main.rand.NextVector2Circular(8f, 8f),
                        smokeVel,
                        Main.rand.NextBool(2) ? Color.Black : new Color(45, 20, 55),
                        Main.rand.Next(22, 38),
                        Main.rand.NextFloat(0.7f, 1.2f),
                        0.5f,
                        Main.rand.NextFloat(-0.08f, 0.08f),
                        false
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);
                }

                // ⑤ 双环 Dust
                int ringCount = 30;
                for (int i = 0; i < ringCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / ringCount;

                    Vector2 vel1 = angle.ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f);
                    int dust1 = Dust.NewDust(target.Center, 1, 1, DustID.Shadowflame, vel1.X, vel1.Y, 0, default, Main.rand.NextFloat(1.1f, 1.7f));
                    Main.dust[dust1].noGravity = true;

                    Vector2 vel2 = angle.ToRotationVector2().RotatedBy(MathHelper.Pi / 4f) * Main.rand.NextFloat(1.8f, 5.1f);
                    int dust2 = Dust.NewDust(target.Center + right * 6f, 1, 1, DustID.PurpleTorch, vel2.X, vel2.Y, 0, default, Main.rand.NextFloat(0.95f, 1.45f));
                    Main.dust[dust2].noGravity = true;
                }

                // ⑥ 裂流
                for (int i = 0; i < 16; i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    Vector2 dir = (forward + right * side * Main.rand.NextFloat(0.35f, 1.1f)).SafeNormalize(Vector2.UnitX);
                    Vector2 vel = dir * Main.rand.NextFloat(2.5f, 7.5f);

                    Particle altSpark = new AltSparkParticle(
                        target.Center + Main.rand.NextVector2Circular(12f, 12f),
                        vel,
                        false,
                        Main.rand.Next(12, 20),
                        Main.rand.NextFloat(0.85f, 1.35f),
                        Main.rand.NextBool(3) ? Color.Black : new Color(32, 10, 45)
                    );
                    GeneralParticleHandler.SpawnParticle(altSpark);
                }

                // ⑦ inward吸回
                for (int i = 0; i < 18; i++)
                {
                    Vector2 spawnPos = target.Center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(36f, 90f);
                    Vector2 vel = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.4f, 6f);

                    int dust = Dust.NewDust(spawnPos, 1, 1, DustID.Shadowflame, vel.X, vel.Y, 0, default, Main.rand.NextFloat(1f, 1.5f));
                    Main.dust[dust].noGravity = true;
                }
            }
        }



    }
}