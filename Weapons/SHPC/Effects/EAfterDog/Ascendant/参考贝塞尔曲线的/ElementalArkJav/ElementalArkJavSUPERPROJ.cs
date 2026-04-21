using CalamityMod.Projectiles.BaseProjectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.ID;
using CalamityMod.Projectiles.Melee;
using CalamityMod;
using CalamityMod.Particles;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkJavSUPERPROJ : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJBlade";
        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";
        private int frameCounter = 0;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制弹幕的残影效果
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 80;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // 允许与方块碰撞
            Projectile.extraUpdates = 1; // 额外更新次数
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为14帧
        }
        private float initialRotation; // 添加一个字段来存储初始朝向

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner]; // 获取弹幕的拥有者（玩家）

            if (Projectile.ai[1] == 0) // 阶段 0：蓄力阶段
            {
                
                // 会转动的
                //{
                //    Vector2 ownerCenter = owner.RotatedRelativePoint(owner.MountedCenter, true);
                //    Vector2 directionToMouse = (Main.MouseWorld - ownerCenter).SafeNormalize(Vector2.Zero);

                //    // 计算旋转角度，使投射物朝向鼠标
                //    Projectile.rotation = directionToMouse.ToRotation();
                //    if (Projectile.spriteDirection == -1)
                //        Projectile.rotation += MathHelper.PiOver2;
                //    else
                //        Projectile.rotation += MathHelper.PiOver4;

                //    // 设置投射物的位置为玩家的中心，并考虑方向
                //    //Projectile.Center = ownerCenter + directionToMouse * 40f; // 保持适当的偏移以模拟长矛握在手中的效果
                //    Projectile.Center = ownerCenter - directionToMouse * 35f; // 保持适当的偏移以模拟长矛握在手中的效果
                //    owner.heldProj = Projectile.whoAmI;

                //    // 增加蓄力计时器
                //    frameCounter++; // 每帧增加计数器，记录蓄力的持续时间
                //    if (frameCounter >= 60) // 如果蓄力持续了 60 帧（1 秒）
                //    {
                //        // 更新方向，使其跟随最新的鼠标位置
                //        Projectile.velocity = directionToMouse * 16f; // 设置初始速度，确保蓄力结束后直接朝鼠标方向冲刺

                //        Projectile.ai[1] = 1; // 切换到阶段 1（减速阶段）
                //        frameCounter = 0; // 重置计数器
                //    }
                //}

                // 不会转动的
                {
                    Vector2 ownerCenter = owner.RotatedRelativePoint(owner.MountedCenter, true);
                    Vector2 directionToMouse = (Main.MouseWorld - ownerCenter).SafeNormalize(Vector2.Zero);

                    // 仅在蓄力阶段开始时设定一次投射物的位置和方向
                    if (frameCounter == 0)
                    {
                        Projectile.Center = ownerCenter - directionToMouse * 35f; // 保持适当的偏移以模拟长矛握在手中的效果
                        Projectile.rotation = directionToMouse.ToRotation(); // 设置初始旋转方向
                        owner.heldProj = Projectile.whoAmI;
                        // 在武器出现时记录初始朝向
                        initialRotation = Projectile.AngleTo(Main.MouseWorld) + MathHelper.PiOver4;

                    }
          
                    Projectile.rotation = initialRotation;
                    Projectile.Center = owner.MountedCenter;
                    //Projectile.Center = ownerCenter - directionToMouse * 35f; // 保持适当的偏移以模拟长矛握在手中的效果
                    owner.heldProj = Projectile.whoAmI;
                    // 增加蓄力计时器
                    frameCounter++; // 每帧增加计数器，记录蓄力的持续时间
                    if (frameCounter >= 60) // 如果蓄力持续了 60 帧（1 秒）
                    {
                        // 更新方向，使其朝向最新的鼠标位置
                        Projectile.velocity = directionToMouse * 16f; // 设置初始速度，确保蓄力结束后直接朝鼠标方向冲刺

                        Projectile.ai[1] = 1; // 切换到阶段 1（减速阶段）
                        frameCounter = 0; // 重置计数器
                    }

                }

                // 每 2 帧生成“等离子体喷射”粒子特效
                if (frameCounter % 2 == 0)
                {
                    // 使用更可见、更浓烈色彩
                    Color[] plasmaColors = { Color.Yellow, Color.Orange, Color.OrangeRed, Color.LightYellow, Color.LightSkyBlue };
                    Color smokeColor = plasmaColors[Main.rand.Next(plasmaColors.Length)];

                    // 保持锐角喷射方向
                    float baseAngle = Projectile.rotation - MathHelper.PiOver4;
                    // 小范围动态扰动（仅 ±3°）
                    float waveOffset = (float)Math.Sin(Main.GameUpdateCount * 0.4f) * MathHelper.ToRadians(3f);
                    float spawnAngle = baseAngle + waveOffset;

                    Vector2 direction = spawnAngle.ToRotationVector2();
                    float spawnDistance = Main.rand.NextFloat(80f, 120f);
                    Vector2 spawnPosition = Projectile.Center + direction * spawnDistance;
                    Vector2 velocity = direction * Main.rand.NextFloat(15f, 25f); // 基础喷射速度

                    // 🌀 重型浓烈烟雾（等离子体核心喷射）
                    Particle smoke = new HeavySmokeParticle(
                        spawnPosition,
                        velocity * 1.2f,
                        smokeColor,
                        60,
                        Projectile.scale * Main.rand.NextFloat(0.5f, 1.8f),
                        1.2f,
                        MathHelper.ToRadians(1f),
                        false,
                        0f,
                        true
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);

                    // === 高速扩散散射特效（CritSpark + Dust 独立循环） ===
                    int scatterPoints = 6; // 控制数量

                    for (int i = 0; i < scatterPoints; i++)
                    {
                        float scatterAngle = baseAngle + MathHelper.ToRadians(Main.rand.NextFloat(-60f, 60f));
                        Vector2 scatterDirection = scatterAngle.ToRotationVector2();

                        float scatterDistance = Main.rand.NextFloat(80f, 120f);
                        Vector2 scatterPosition = Projectile.Center + scatterDirection * scatterDistance;
                        Vector2 scatterVelocity = scatterDirection * Main.rand.NextFloat(30f, 50f); // 更高速散射

                        // ⚡ CritSpark 高亮闪光点
                        if (Main.rand.NextBool(1))
                        {
                            Particle critSpark = new CritSpark(
                                scatterPosition,
                                scatterVelocity * 0.8f,
                                Color.White,
                                Color.Yellow,
                                Main.rand.NextFloat(1.50f, 1.95f),
                                Main.rand.Next(16, 24),
                                0.08f,
                                0f // 关闭 Bloom
                            );
                            GeneralParticleHandler.SpawnParticle(critSpark);
                        }


                        // ✨ 小型 GenericBloom 等离子体喷射特效（频率降低 1/3，方向修正 180°）
                        if (frameCounter % 6 == 0) // 频率降低到当前的 1/3
                        {
                            // 左右偏移 ±20°，整体转 180° 修正方向
                            float correctedAngle = MathHelper.ToRadians(180) + MathHelper.ToRadians(Main.rand.Next(-20, 20));
                            Vector2 directio1n = correctedAngle.ToRotationVector2();

                            Vector2 spawnPositi2on = Projectile.Center + directio1n * Main.rand.NextFloat(40f, 60f);

                            Color particleColor = Main.rand.NextBool() ? Color.OrangeRed : Color.Yellow;
                            float particleScale = Main.rand.NextFloat(0.3f, 0.6f); // 小型

                            // 小型 GenericBloom（发散）
                            GeneralParticleHandler.SpawnParticle(
                                new GenericBloom(
                                    spawnPositi2on,
                                    directio1n * Main.rand.NextFloat(6f, 10f),
                                    particleColor,
                                    particleScale,
                                    Main.rand.Next(20, 30)
                                )
                            );
                        }


                    }

                }






            }
            else if (Projectile.ai[1] == 1) // 阶段 1：减速阶段
            {
                // 调整弹幕旋转方向与其运动方向一致
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4; // 弹幕的旋转方向跟随其运动方向

                // 添加光效
                Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 0.55f); // 在弹幕位置添加橙色光效

                // 逐渐减速
                //Projectile.velocity *= 0.99f; // 每帧减少弹幕的速度

                // 每 25 帧生成一个随机方向的 ElementalArkJavEonBolt 弹幕
                frameCounter++; // 计数器每帧增加
                if (frameCounter % 25 == 0) // 每 25 帧触发一次
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi); // 随机生成一个角度
                    Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 5f; // 设定弹幕的速度
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity * 2.5f,
                        ModContent.ProjectileType<ElementalArkJavEonBolt>(), (int)(Projectile.damage * 2.5), Projectile.knockBack, Projectile.owner); // 生成新的弹幕
                }

                // 随机生成粒子效果
                if (Main.rand.NextBool(3)) // 以一定的概率生成粒子
                {
                    float leftAngle = MathHelper.ToRadians(180 + Main.rand.Next(-20, 20)); // 左方偏移 -20 到 20 度
                    float rightAngle = MathHelper.ToRadians(Main.rand.Next(-20, 20)); // 右方偏移 -20 到 20 度

                    Vector2 leftDirection = new Vector2((float)Math.Cos(leftAngle), (float)Math.Sin(leftAngle)); // 左方向的单位向量
                    Vector2 rightDirection = new Vector2((float)Math.Cos(rightAngle), (float)Math.Sin(rightAngle)); // 右方向的单位向量

                    Vector2 leftPosition = Projectile.Center + leftDirection * 20f; // 左方粒子的生成位置
                    Vector2 rightPosition = Projectile.Center + rightDirection * 20f; // 右方粒子的生成位置

                    Color particleColor = Main.rand.NextBool() ? Color.OrangeRed : Color.Yellow; // 随机选择粒子的颜色
                    float particleScale = Main.rand.NextFloat(0.2f, 0.5f); // 随机设定粒子的缩放比例

                    GeneralParticleHandler.SpawnParticle(new StrongBloom(leftPosition, leftDirection * Main.rand.NextFloat(2f, 4f), particleColor, particleScale, Main.rand.Next(20) + 10)); // 生成左方粒子
                    GeneralParticleHandler.SpawnParticle(new GenericBloom(rightPosition, rightDirection * Main.rand.NextFloat(2f, 4f), particleColor, particleScale, Main.rand.Next(20) + 10)); // 生成右方粒子
                }

                // 当弹幕速度足够低时进入爆炸阶段
                if (Projectile.velocity.Length() < 0.5f) // 如果速度降到一定阈值以下
                {
                    Projectile.ai[1] = 2; // 进入爆炸阶段
                }
            }
            else if (Projectile.ai[1] == 2) // 阶段 2：爆炸阶段
            {
                // 调用 OnKill() 方法的逻辑来实现爆炸效果
                OnKill(Projectile.timeLeft); // 调用爆炸效果的方法
                Projectile.Kill(); // 杀死弹幕，触发爆炸效果
            }
        }

        public override bool? CanDamage()
        {
            // 仅在蓄力阶段结束（frameCounter >= 60）后才能造成伤害
            return frameCounter >= 60;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++) // 在 5 个方向发射 ElementalArkJavBlast
            {
                // 计算每个方向的角度
                float angle = MathHelper.ToRadians(72 * i); // 每个方向相隔 72 度

                // 计算终点位置：从中心点出发，沿该角度方向延伸 25 格 * 16 像素
                Vector2 endPosition = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (25 * 16);

                // 计算从终点位置到中心点的速度向量
                Vector2 velocity = (Projectile.Center - endPosition).SafeNormalize(Vector2.Zero) * 6f; // 确保速度方向正确并且长度为 6f

                // 在终点位置发射弹幕，目标为原始位置
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), endPosition, velocity,
                    ModContent.ProjectileType<ElementalArkJavBlast>(), (int)(Projectile.damage * 10.0), Projectile.knockBack, Projectile.owner); // 生成从终点射向中心的弹幕
            }
        }





    }
}
