using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using CalamityMod.Particles;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Buffs.DamageOverTime;
using static CalamityThrowingSpear.CTSLightingBoltsSystem;
using Terraria.Audio;

namespace CalamityThrowingSpear.Weapons.ChangedWeapons.CPreMoodLord.AstralPikeC
{
    public class AstralPikeJavPROJ : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityThrowingSpear/Weapons/ChangedWeapons/CPreMoodLord/AstralPikeC/AstralPikeJav";
        public new string LocalizationCategory => "Projectiles.ChangedWeapons.CPreMoodLord";
        private bool hasBounced = false; // 记录是否已经反弹过一次
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1; // 只允许一次伤害
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true; // 允许与方块碰撞
            Projectile.extraUpdates = 1; // 额外更新次数
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为14帧
        }

        public override void AI()
        {
            // 保持弹幕旋转（对于倾斜走向的弹幕而言）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Lighting - 添加深橙色光源，光照强度为 0.55
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 0.55f);

            // 弹幕保持直线运动并逐渐加速
            Projectile.velocity *= 1.01f;

            // 如果已经反弹过一次，开始追踪敌人
            if (hasBounced)
            {
                // 追踪逻辑
                CalamityUtils.HomeInOnNPC(Projectile, true, 450f, 24f, 30f);
            }

            // 每隔一定时间产生轨迹
            if (Main.rand.NextBool(5))
            {
                float sideOffset = Main.rand.NextFloat(-1f, 1f);
                Vector2 trailPos = Projectile.Center + Vector2.UnitY.RotatedBy(Projectile.rotation) + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * sideOffset;

                float trailScale = Main.rand.NextFloat(0.8f, 1.2f);
                Color trailColor = Main.rand.NextBool() ? Color.Orange : Color.LightBlue;

                Particle trail = new SparkParticle(trailPos, Projectile.velocity * 0.2f, false, 60, trailScale, trailColor);
                GeneralParticleHandler.SpawnParticle(trail);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 反弹一次后继续穿过方块
            if (!hasBounced)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact with { Volume = 1.2f, Pitch = -0.0f }, Projectile.Center);

                hasBounced = true;
                //Projectile.penetrate = -1; // 禁止进一步反弹，允许穿过方块
                Projectile.tileCollide = false; // 不在允许与方块碰撞

                // 调用召唤彗星的逻辑，召唤2颗彗星（不再召唤）
                NPC target = FindClosestNPC(1800f); // 寻找最近的敌人，1000像素范围内
                if (target != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        //SummonComet(target);
                    }
                }

                // 第一次反弹后粒子效果
                CreateBounceParticles();
                CTSLightingBoltsSystem.Spawn_CelestialBurst(Projectile.Center);
                CTSLightingBoltsSystem.Spawn_SpectralWhispers(Projectile.Center);
                
                return false; // 不销毁弹幕，继续运行
            }

            return true; // 如果已经反弹过，则直接穿过
        }

        private void CreateBounceParticles()
        {
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.OrangeTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default(Color), 1.5f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default(Color), 1.5f);
            }
        }

        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float closestDistance = maxDetectDistance;
            foreach (NPC npc in Main.npc)
            {
                if (npc.CanBeChasedBy(Projectile))
                {
                    float distance = Vector2.Distance(Projectile.Center, npc.Center * 5);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNPC = npc;
                    }
                }
            }
            return closestNPC;
        }

        private void SummonComet(NPC npc)
        {
            Player player = Main.player[npc.target];
            Vector2 targetPosition = npc.Center;
            float radius = 50f * 16f;
            float arrowSpeed = 10f;

            float randomAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawnPosition = targetPosition + radius * new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle));

            Vector2 direction = targetPosition - spawnPosition;
            direction.Normalize();
            float speedX = direction.X * arrowSpeed * 6f + Main.rand.Next(-120, 121) * 0.01f;
            float speedY = direction.Y * arrowSpeed * 6f + Main.rand.Next(-120, 121) * 0.01f;

            int newDamage = (int)(Projectile.damage * 0.33f); // 伤害为主弹幕的33%

            // 生成彗星弹幕，速度为原来的两倍
            Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPosition, new Vector2(speedX, speedY), ModContent.ProjectileType<AstralPikeSTAR>(), newDamage, 0, player.whoAmI);
        }

        public override void OnKill(int timeLeft)
        {
            // 产生爆炸效果
            Projectile explosion = Main.projectile[
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Summon.SummonAstralExplosion>(),
                    (int)(Projectile.damage * 0.5f),
                    Projectile.knockBack,
                    Main.myPlayer
                )
            ];
            // 将伤害类型更改为近战
            explosion.DamageType = DamageClass.Melee;

            //// 创建线性粒子特效，橙色
            //int points = 25;
            //float radians = MathHelper.TwoPi / points;
            //Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
            //float rotRando = Main.rand.NextFloat(0.1f, 2.5f);
            //for (int k = 0; k < points; k++)
            //{
            //    Vector2 velocity = spinningPoint.RotatedBy(radians * k).RotatedBy(-0.45f * rotRando);
            //    LineParticle subTrail = new LineParticle(Projectile.Center + velocity * 20.5f, velocity * 15, false, 30, 0.75f, Color.Orange); // 将颜色改为橙色
            //    GeneralParticleHandler.SpawnParticle(subTrail);
            //}

            // 死亡时召唤 4 颗朝自己飞来的彗星
            for (int i = 0; i < 4; i++)
            {
                SummonCometAtSelf();
            }


            // 创建线性粒子特效，朝向正上方、正下方、正左方和正右方，每个方向三条平行线
            int linesPerDirection = 5; // 每个方向有三条平行线
            float lineOffset = 3f; // 粒子间距为1像素
            float middleSpeedMultiplier = 1.2f; // 中间的粒子比两边的快一点
            float sideSpeedMultiplier = 1.0f; // 两边粒子的速度

            Vector2[] directions = new Vector2[] {
                new Vector2(0, -1), // 正上方
                new Vector2(0, 1),  // 正下方
                new Vector2(-1, 0), // 正左方
                new Vector2(1, 0)   // 正右方
            };

            // 遍历四个方向
            foreach (Vector2 direction in directions)
            {
                // 中间的线性粒子
                Vector2 middleVelocity = direction * middleSpeedMultiplier * 15f; // 中间粒子速度
                LineParticle middleParticle = new LineParticle(Projectile.Center, middleVelocity, false, 30, 0.75f, Color.Orange);
                GeneralParticleHandler.SpawnParticle(middleParticle);

                // 两边的线性粒子
                for (int i = -1; i <= 1; i += 2) // i = -1 和 1，分别生成两边的粒子
                {
                    Vector2 sideOffset = new Vector2(direction.Y, direction.X) * lineOffset * i; // 计算偏移
                    Vector2 sideVelocity = direction * sideSpeedMultiplier * 15f; // 两边粒子速度
                    LineParticle sideParticle = new LineParticle(Projectile.Center + sideOffset, sideVelocity, false, 30, 0.75f, Color.Orange);
                    GeneralParticleHandler.SpawnParticle(sideParticle);
                }
            }


            // 随机扩散的橙色粒子特效，反向抛射
            int numRandomParticles = 20;
            for (int i = 0; i < numRandomParticles; i++)
            {
                Vector2 randomVelocity = -Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30)) * Main.rand.NextFloat(0.5f, 2f);
                Dust randomDust = Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch, randomVelocity, 0, Color.Orange, Main.rand.NextFloat(1.0f, 2.5f));
                randomDust.noGravity = true;
                randomDust.fadeIn = 1f;
                randomDust.scale = Main.rand.NextFloat(1f, 2.5f); // 随机大小
                randomDust.rotation = Main.rand.NextFloat(MathHelper.TwoPi); // 随机旋转
                randomDust.velocity *= Main.rand.NextFloat(0.8f, 1.5f); // 随机速度
            }


            {
                Vector2 center = Projectile.Center;

                // ✦ 1. 中心爆炸核心光尘
                for (int i = 0; i < 18; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                    int dustType = Main.rand.NextBool() ? DustID.OrangeTorch : DustID.BlueTorch;

                    Dust d = Dust.NewDustPerfect(center, dustType, velocity, 150, default, Main.rand.NextFloat(1.5f, 2.1f));
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }

                // ✦ 2. 外圈高速星尘放射（Dust 长线扩散）
                for (int i = 0; i < 36; i++)
                {
                    float angle = MathHelper.TwoPi * i / 36f;
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 velocity = dir * Main.rand.NextFloat(4f, 8f);

                    int dustType = (i % 2 == 0) ? DustID.OrangeTorch : DustID.BlueTorch;
                    Dust d = Dust.NewDustPerfect(center, dustType, velocity, 100, default, Main.rand.NextFloat(1.2f, 1.6f));
                    d.noGravity = true;
                    d.fadeIn = 1.1f;
                }

                // ✦ 3. 星芒旋涡式线性粒子轨迹（SparkParticle 旋涡状）
                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f;
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 spawnPos = center + dir * Main.rand.NextFloat(4f, 10f);
                    Vector2 vel = dir.RotatedBy(MathHelper.PiOver4) * Main.rand.NextFloat(1.5f, 3f);

                    Color trailColor = Main.rand.NextBool(2) ? Color.LightBlue : Color.Orange;
                    float scale = Main.rand.NextFloat(0.8f, 1.4f);

                    Particle p = new SparkParticle(spawnPos, vel, false, 40, scale, trailColor);
                    GeneralParticleHandler.SpawnParticle(p);
                }

                // ✦ 4. 中心高亮粒子 + 放射光（Spark 核心加光晕）
                for (int i = 0; i < 8; i++)
                {
                    Vector2 spawnPos = center + Main.rand.NextVector2Circular(6f, 6f);
                    Vector2 vel = -Projectile.velocity.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(0.3f, 1.2f);

                    Particle p = new SparkParticle(spawnPos, vel, false, 60, Main.rand.NextFloat(1.6f, 2.2f), Color.White);
                    GeneralParticleHandler.SpawnParticle(p);
                }
            }
        }
        private void SummonCometAtSelf()
        {
            Vector2 targetPosition = Projectile.Center;
            float radius = 50f * 16f;
            float arrowSpeed = 10f;

            float randomAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawnPosition = targetPosition + radius * randomAngle.ToRotationVector2();

            Vector2 direction = targetPosition - spawnPosition;
            direction.Normalize();
            Vector2 velocity = direction * arrowSpeed * 6f + Main.rand.NextVector2Circular(1.2f, 1.2f);

            int newDamage = (int)(Projectile.damage * 0.33f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<AstralPikeSTAR>(),
                newDamage,
                0,
                Main.myPlayer
            );
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff> (), 300); // 幻星感染
        }


    }
}
