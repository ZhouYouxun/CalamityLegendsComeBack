using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog
{
    internal class NightmareFuel_ARC : ModProjectile
    {
        // 自定义状态：禁止用 localAI 当计数器
        private bool initialized;
        private int timer;
        private int lockedTargetIndex = -1;
        private bool hasSpawnedScythe;
        private Color arcColor = Color.MediumOrchid;

        // 曲线方向：-1 左弧，0 直线，1 右弧
        private int CurveDirection => (int)MathHelper.Clamp(Projectile.ai[0], -1f, 1f);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; // 交汇前无限贯穿，这里直接给无限贯穿
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 12; // 你要求的 ex = 12
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // 左右双色：模仿魔王剑的双色风格
            if (CurveDirection < 0)
                arcColor = Color.MediumOrchid;
            else if (CurveDirection > 0)
                arcColor = Color.Lerp(Color.DeepPink, Color.Orange, 0.5f);
            else
                arcColor = Color.Lerp(Color.MediumOrchid, Color.BlueViolet, 0.5f);

            lockedTargetIndex = FindInitialTarget(1600f);

            initialized = true;
        }

        public override void AI()
        {
            if (!initialized)
                return;

            timer++;

            Vector2 safeVel = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ===== 轨迹逻辑 =====
            // 1. 一开始有锁定目标：用“追踪 + 曲线偏转”混合
            // 2. 一开始没锁定：保持原路前进，只做 Apoctosis 式左右分弧 / 中间直走
            if (lockedTargetIndex >= 0 && lockedTargetIndex < Main.maxNPCs && Main.npc[lockedTargetIndex].active && !Main.npc[lockedTargetIndex].dontTakeDamage && Main.npc[lockedTargetIndex].life > 0)
            {
                NPC target = Main.npc[lockedTargetIndex];
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(safeVel);

                // Apoctosis 风格的左右弧：持续小角度偏转
                float curveAngle = 0.0205f * CurveDirection;

                // 距离越近，曲线越收，逐渐汇向目标
                float distance = Vector2.Distance(Projectile.Center, target.Center);
                float curveFade = Utils.GetLerpValue(120f, 520f, distance, true);
                Vector2 curvedDirection = toTarget.RotatedBy(curveAngle * curveFade);

                // 平滑转向，避免硬拐弯
                Vector2 finalDirection = Vector2.Lerp(safeVel, curvedDirection, 0.13f).SafeNormalize(safeVel);

                // 轻微加速，保证弧线推进感
                float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.004f, 8f, 21f);
                Projectile.velocity = finalDirection * speed;
            }
            else
            {
                // 没锁定就原路前进，中间直走，左右持续缓慢弯曲
                if (CurveDirection != 0)
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(0.0175f * CurveDirection);
                }

                Projectile.velocity *= 1.0025f;
                if (Projectile.velocity.Length() > 18f)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 18f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // ===== 飞行冷紫/魔焰特效 =====
            Lighting.AddLight(Projectile.Center, arcColor.ToVector3() * 0.55f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            float t = Main.GameUpdateCount * 0.22f + Projectile.whoAmI * 0.37f;

            // 冷焰弧线粒子
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float wave = (float)Math.Sin(t + i * 1.4f) * 7f;

                Vector2 spawnPos = Projectile.Center - forward * 24f + right * wave * side;
                Vector2 vel = -forward * Main.rand.NextFloat(0.8f, 2.2f) + right * side * Main.rand.NextFloat(0.1f, 0.55f);

                SquishyLightParticle particle = new(
                    spawnPos,
                    vel,
                    Main.rand.NextFloat(0.8f, 1.25f),
                    Color.Lerp(arcColor, Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
                    Main.rand.Next(16, 26)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 雾状拖尾
            if (timer % 2 == 0)
            {
                Vector2 pos = Projectile.Center - forward * Main.rand.NextFloat(10f, 30f) + right * Main.rand.NextFloat(-10f, 10f);
                Vector2 vel = -forward * Main.rand.NextFloat(0.5f, 1.5f) + right * Main.rand.NextFloat(-0.25f, 0.25f);

                Particle mist = new MediumMistParticle(
                    pos,
                    vel,
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.55f, 0.85f),
                    Main.rand.NextFloat(90f, 140f)
                );

                GeneralParticleHandler.SpawnParticle(mist);
            }

            // 补一点 dust，强化魔王剑的双色味道
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    -forward * Main.rand.NextFloat(0.2f, 1.1f),
                    0,
                    Main.rand.NextBool() ? arcColor : Color.BlueViolet,
                    Main.rand.NextFloat(1.0f, 1.35f)
                );
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中后只生成一次镰刀
            if (hasSpawnedScythe)
                return;

            hasSpawnedScythe = true;

            // 从目标下方生成镰刀，先往上切
            Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), Main.rand.NextFloat(220f, 320f));
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 16f;

            int scytheIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<NightmareFuel_Scythe>(),
                (int)(Projectile.damage * 0.8f),
                Projectile.knockBack,
                Projectile.owner
            );

            if (scytheIndex >= 0 && scytheIndex < Main.maxProjectiles)
            {
                Projectile scythe = Main.projectile[scytheIndex];
                scythe.rotation = scythe.velocity.ToRotation();
            }

            // 命中爆裂特效
            for (int i = 0; i < 12; i++)
            {
                Vector2 burstVel = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.8f) * Main.rand.NextFloat(3f, 12f);

                SquishyLightParticle particle = new(
                    target.Center,
                    burstVel,
                    Main.rand.NextFloat(0.9f, 1.4f),
                    Main.rand.NextBool() ? arcColor : Color.MediumOrchid,
                    Main.rand.Next(18, 30)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 8; i++)
            {
                Particle mist = new MediumMistParticle(
                    target.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.9f) * Main.rand.NextFloat(1.2f, 4.5f),
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.65f, 0.95f),
                    Main.rand.NextFloat(110f, 180f)
                );

                GeneralParticleHandler.SpawnParticle(mist);
            }
        }

        private int FindInitialTarget(float maxDistance)
        {
            int targetIndex = -1;
            float nearestDistance = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance > nearestDistance)
                    continue;

                targetIndex = i;
                nearestDistance = distance;
            }

            return targetIndex;
        }
    }

    internal class NightmareFuel_Scythe : ModProjectile
    {
        // 自定义状态：禁止 localAI 计数
        private bool initialized;
        private int timer;
        private bool gainedHoming;
        private int homingDelay;
        private Color scytheColor = Color.BlueViolet;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialized = true;
            scytheColor = Main.rand.NextBool() ? Color.MediumOrchid : Color.Lerp(Color.DeepPink, Color.Orange, 0.5f);
        }

        public override void AI()
        {
            if (!initialized)
                return;

            timer++;

            // 初始阶段：从下往上切
            if (!gainedHoming)
            {
                Projectile.velocity *= 0.992f;
            }
            else
            {
                if (homingDelay > 0)
                {
                    homingDelay--;
                }
                else
                {
                    int targetIndex = FindTarget(1400f);
                    if (targetIndex != -1)
                    {
                        NPC target = Main.npc[targetIndex];
                        Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);

                        float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.015f, 9f, 20f);
                        Vector2 desiredVelocity = toTarget * speed;

                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.085f);
                    }
                    else
                    {
                        Projectile.velocity *= 1.004f;
                    }
                }
            }

            Projectile.rotation += 0.32f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);

            Lighting.AddLight(Projectile.Center, scytheColor.ToVector3() * 0.48f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            // 镰刀拖尾
            for (int i = 0; i < 2; i++)
            {
                Vector2 vel = -forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.8f, 2.8f);

                SquishyLightParticle particle = new(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    vel,
                    Main.rand.NextFloat(0.75f, 1.2f),
                    Main.rand.NextBool() ? scytheColor : Color.White,
                    Main.rand.Next(18, 28)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (timer % 2 == 0)
            {
                Particle mist = new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    -forward.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.4f, 1.5f),
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.55f, 0.85f),
                    Main.rand.NextFloat(80f, 130f)
                );

                GeneralParticleHandler.SpawnParticle(mist);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 镰刀第一次命中后才获得追踪能力
            if (!gainedHoming)
            {
                gainedHoming = true;
                homingDelay = 6;
                Projectile.penetrate = -1;
                Projectile.velocity *= 0.75f;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 burstVel = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.95f) * Main.rand.NextFloat(2.5f, 9f);

                SquishyLightParticle particle = new(
                    target.Center,
                    burstVel,
                    Main.rand.NextFloat(0.9f, 1.3f),
                    Main.rand.NextBool() ? scytheColor : Color.MediumOrchid,
                    Main.rand.Next(16, 26)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private int FindTarget(float maxDistance)
        {
            int targetIndex = -1;
            float nearestDistance = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance > nearestDistance)
                    continue;

                targetIndex = i;
                nearestDistance = distance;
            }

            return targetIndex;
        }
    }
}