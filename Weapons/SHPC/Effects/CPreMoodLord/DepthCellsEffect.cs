using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects
{
    public class DepthCellsEffect : DefaultEffect
    {
        public override int EffectID => 17;
        public override int AmmoType => ModContent.ItemType<DepthCells>();

        // ===== 深渊暗色主题 =====
        public override Color ThemeColor => new Color(26, 34, 52);
        public override Color StartColor => new Color(52, 74, 112);
        public override Color EndColor => new Color(10, 14, 24);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // ===== 每个弹幕各自的状态表 =====
        private readonly Dictionary<int, bool> stuckState = new();
        private readonly Dictionary<int, int> stuckTargetIndex = new();
        private readonly Dictionary<int, int> hitCountOnCurrentTarget = new();
        private readonly Dictionary<int, int> bounceCount = new();
        private readonly Dictionary<int, int> stickVisualTimer = new();
        private readonly Dictionary<int, float> orbitAngle = new();

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;

            // 为了支持持续多段伤害和连续弹跳，这里直接给无限穿透
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;

            stuckState[id] = false;
            stuckTargetIndex[id] = -1;
            hitCountOnCurrentTarget[id] = 0;
            bounceCount[id] = 0;
            stickVisualTimer[id] = 0;
            orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI(Projectile projectile, Player owner)
        {
            int id = projectile.whoAmI;

            EnsureStateExists(id);

            // ===== 常规飞行时：暗色深渊尾迹 =====
            if (!stuckState[id])
            {
                projectile.rotation = projectile.velocity.ToRotation();

                // 深渊重烟，参考 Typhons / Tenebreus 的同源粒子，但释放方式改成“贴后拖尾”
                if (Main.rand.NextBool(2))
                {
                    Color smokeColor = Color.Lerp(
                        new Color(16, 22, 36),
                        new Color(40, 62, 104),
                        Main.rand.NextFloat()
                    );

                    HeavySmokeParticle smoke = new HeavySmokeParticle(
                        projectile.Center - projectile.velocity * 0.8f + Main.rand.NextVector2Circular(6f, 6f),
                        projectile.velocity * Main.rand.NextFloat(-0.08f, -0.18f),
                        smokeColor,
                        Main.rand.Next(20, 34),
                        Main.rand.NextFloat(0.4f, 0.7f),
                        0.9f
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);
                }

                // 深渊水刃感碎屑，参考 TenebreusTidesJavWaterSword 的同源粒子，但改成前后混合散逸
                if (Main.rand.NextBool(3))
                {
                    Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
                    Vector2 spawnPos = projectile.Center - forward * Main.rand.NextFloat(4f, 12f) + Main.rand.NextVector2Circular(4f, 4f);
                    Vector2 velocity = Main.rand.NextVector2Circular(1.8f, 1.8f) - forward * Main.rand.NextFloat(0.4f, 1.2f);

                    WaterFlavoredParticle waterShard = new WaterFlavoredParticle(
                        spawnPos,
                        velocity,
                        false,
                        Main.rand.Next(12, 22),
                        Main.rand.NextFloat(0.65f, 0.95f),
                        Color.Lerp(new Color(22, 34, 64), new Color(70, 110, 170), Main.rand.NextFloat())
                    );
                    GeneralParticleHandler.SpawnParticle(waterShard);
                }

                // 微弱深渊幽光
                Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.18f);
                return;
            }

            // ===== 粘附状态：锁在敌人身上 =====
            int targetIndex = stuckTargetIndex[id];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                ReleaseAndDashToNext(projectile, owner, -1);
                return;
            }

            NPC target = Main.npc[targetIndex];
            if (!target.active || !target.CanBeChasedBy(projectile))
            {
                ReleaseAndDashToNext(projectile, owner, targetIndex);
                return;
            }

            stickVisualTimer[id]++;
            //orbitAngle[id] += 0.18f;

            Vector2 stickDir = (projectile.Center - target.Center).SafeNormalize(Vector2.UnitY);
            Vector2 orbitOffset = stickDir * 18f;
            
            projectile.Center = target.Center + orbitOffset;
            projectile.velocity = Vector2.Zero;
            projectile.rotation += 0.22f;

            // 粘附期间持续释放深渊特效
            SpawnStuckAbyssEffects(projectile, target);

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.12f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int id = projectile.whoAmI;

            EnsureStateExists(id);

            // ================= 命中特效（强化版） =================

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 1. 深渊反冲烟（方向性）
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = -forward * Main.rand.NextFloat(1.2f, 3.5f) + Main.rand.NextVector2Circular(1.2f, 1.2f);

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    target.Center + Main.rand.NextVector2Circular(8f, 8f),
                    vel,
                    Color.Lerp(new Color(10, 14, 22), new Color(36, 58, 96), Main.rand.NextFloat()),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.5f, 0.9f),
                    1f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // 2. 撕裂水刃（前后结构）
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel =
                    forward * Main.rand.NextFloat(1f, 3f) +
                    Main.rand.NextVector2Circular(1.5f, 1.5f);

                WaterFlavoredParticle shard = new WaterFlavoredParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    vel,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.7f, 1.1f),
                    Color.Lerp(new Color(26, 40, 74), new Color(72, 110, 164), Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            // 3. 暗蓝闪烁（补层）
            for (int i = 0; i < 5; i++)
            {
                AltSparkParticle spark = new AltSparkParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    false,
                    12,
                    Main.rand.NextFloat(0.9f, 1.4f),
                    Color.Lerp(new Color(20, 30, 60), new Color(80, 120, 180), Main.rand.NextFloat()) * 0.5f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 4. 原版Dust层（增强质感）
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? DustID.Water : DustID.BlueTorch,
                    Main.rand.NextVector2Circular(2f, 2f),
                    120,
                    Color.Lerp(new Color(16, 24, 40), new Color(56, 86, 132), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.4f)
                );
                dust.noGravity = true;
            }

            // ================= 你要求的爆环 =================

            CustomPulse blastRing1 = new(
                projectile.Center,
                Vector2.Zero,
                Color.Blue,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10, 10),
                0.05f,
                0.35f,
                15
            );
            GeneralParticleHandler.SpawnParticle(blastRing1);

            CustomPulse blastRing2 = new(
                projectile.Center,
                Vector2.Zero,
                Color.Blue,
                "CalamityMod/Particles/FlameExplosion",
                new Vector2(1f, 0.5f),
                0f,
                0.05f,
                0.35f,
                20
            );
            GeneralParticleHandler.SpawnParticle(blastRing2);

            // ================= Debuff =================

            target.AddBuff(ModContent.BuffType<CrushDepth>(), 320);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 320);

            // ================= 原逻辑（必须保留） =================

            if (!stuckState[id] || stuckTargetIndex[id] != target.whoAmI)
            {
                stuckState[id] = true;
                stuckTargetIndex[id] = target.whoAmI;
                hitCountOnCurrentTarget[id] = 1;
                stickVisualTimer[id] = 0;
                orbitAngle[id] = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.velocity = Vector2.Zero;
                projectile.netUpdate = true;
                return;
            }

            hitCountOnCurrentTarget[id]++;

            if (hitCountOnCurrentTarget[id] >= 4)
            {
                int currentTarget = target.whoAmI;

                if (bounceCount[id] < 3)
                {
                    bounceCount[id]++;
                    hitCountOnCurrentTarget[id] = 0;
                    ReleaseAndDashToNext(projectile, owner, currentTarget);
                }
                else
                {
                    projectile.Kill();
                }
            }
        }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            int id = projectile.whoAmI;

            // 收尾也给一点深渊散灭感
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.2f, 2.2f);

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    Color.Lerp(new Color(12, 18, 28), new Color(48, 72, 116), Main.rand.NextFloat()),
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.45f, 0.75f),
                    0.9f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            ClearState(id);
        }

        // ========================= 工具区 =========================

        private void EnsureStateExists(int id)
        {
            if (!stuckState.ContainsKey(id))
                stuckState[id] = false;
            if (!stuckTargetIndex.ContainsKey(id))
                stuckTargetIndex[id] = -1;
            if (!hitCountOnCurrentTarget.ContainsKey(id))
                hitCountOnCurrentTarget[id] = 0;
            if (!bounceCount.ContainsKey(id))
                bounceCount[id] = 0;
            if (!stickVisualTimer.ContainsKey(id))
                stickVisualTimer[id] = 0;
            if (!orbitAngle.ContainsKey(id))
                orbitAngle[id] = 0f;
        }

        private void ClearState(int id)
        {
            stuckState.Remove(id);
            stuckTargetIndex.Remove(id);
            hitCountOnCurrentTarget.Remove(id);
            bounceCount.Remove(id);
            stickVisualTimer.Remove(id);
            orbitAngle.Remove(id);
        }

        private void ReleaseAndDashToNext(Projectile projectile, Player owner, int excludeTargetWhoAmI)
        {
            int id = projectile.whoAmI;

            NPC nextTarget = FindNextTarget(projectile, excludeTargetWhoAmI);

            // 先脱离当前粘附状态
            stuckState[id] = false;
            stuckTargetIndex[id] = -1;
            stickVisualTimer[id] = 0;

            if (nextTarget == null)
            {
                projectile.Kill();
                return;
            }

            // 起跳时做一个暗色爆散
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f);

                WaterFlavoredParticle waterShard = new WaterFlavoredParticle(
                    projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.6f, 0.9f),
                    Color.Lerp(new Color(18, 28, 52), new Color(70, 110, 165), Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(waterShard);
            }

            // 快速冲向下一个目标
            Vector2 dashDirection = (nextTarget.Center - projectile.Center).SafeNormalize(Vector2.UnitX);
            projectile.velocity = dashDirection * 22f;
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.netUpdate = true;
        }

        private NPC FindNextTarget(Projectile projectile, int excludeTargetWhoAmI)
        {
            NPC result = null;
            float maxDistance = 900f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.CanBeChasedBy(projectile))
                    continue;

                if (npc.whoAmI == excludeTargetWhoAmI)
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    result = npc;
                }
            }

            return result;
        }

        private void SpawnHitAbyssEffects(Projectile projectile, NPC target)
        {
            // 1) 命中瞬间的深渊重烟
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.8f, 1.8f);

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    Color.Lerp(new Color(10, 14, 22), new Color(36, 58, 96), Main.rand.NextFloat()),
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.45f, 0.8f),
                    1f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // 2) 同源的暗色水刃碎片，但改成“从命中点往外喷”
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);

                WaterFlavoredParticle shard = new WaterFlavoredParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.65f, 1f),
                    Color.Lerp(new Color(26, 40, 74), new Color(72, 110, 164), Main.rand.NextFloat())
                );
                GeneralParticleHandler.SpawnParticle(shard);
            }

            // 3) 少量暗蓝火花，补一层“深渊撕裂感”
            for (int i = 0; i < 4; i++)
            {
                AltSparkParticle spark = new AltSparkParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false,
                    12,
                    Main.rand.NextFloat(0.8f, 1.2f),
                    Color.Lerp(new Color(20, 30, 60), new Color(80, 120, 180), Main.rand.NextFloat()) * 0.4f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 4) 再补一点原版 Dust，让层次更像“深渊液态黑雾”
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool() ? DustID.Water : DustID.BlueTorch,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    120,
                    Color.Lerp(new Color(16, 24, 40), new Color(56, 86, 132), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.9f, 1.2f)
                );
                dust.noGravity = true;
            }
        }

        private void SpawnStuckAbyssEffects(Projectile projectile, NPC target)
        {
            // 粘附期间做成“围绕宿主表面爬行的深渊烟流”
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(18f, 18f);

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    target.Center + offset,
                    offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.2f, 0.8f),
                    Color.Lerp(new Color(10, 14, 22), new Color(30, 48, 86), Main.rand.NextFloat()),
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.35f, 0.6f),
                    0.85f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2Circular(16f, 16f);

                AltSparkParticle spark = new AltSparkParticle(
                    target.Center + offset,
                    Main.rand.NextVector2Circular(0.5f, 0.5f),
                    false,
                    10,
                    Main.rand.NextFloat(0.75f, 1f),
                    Color.Lerp(new Color(24, 36, 66), new Color(78, 118, 176), Main.rand.NextFloat()) * 0.35f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
