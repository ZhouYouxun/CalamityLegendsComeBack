using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Sounds;
using CalamityThrowingSpear.Weapons.ChangedWeapons.EAfterDog.DragonRageC;
using Terraria.Audio;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using CalamityMod.Projectiles.Melee;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkJavFragments05 : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJFragment";

        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";
        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private float progress;
        public int Time = 0;
        public bool isCurveUpwards; // 新增变量，用于确定曲线方向

        public List<Particle> Particles;

        //public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/ElementalArkJavFragments05";
        //public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/ElementalArkJavBlade05";


        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制弹幕的残影效果
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);

            // 绘制星空链条粒子效果
            if (Particles != null)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                foreach (Particle particle in Particles)
                    particle.CustomDraw(Main.spriteBatch);
                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }


        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 0) // 初始化位置
            {
                Player player = Main.player[Projectile.owner]; // 获取玩家对象
                startPoint = player.Center; // 将起点设置为玩家的中心位置
                endPoint = Main.MouseWorld; // 终点为鼠标位置

                // 计算起点到终点的方向
                Vector2 direction = (endPoint - startPoint).SafeNormalize(Vector2.Zero);

                // 获取与方向垂直的向量
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

                // 设置偏移量大小，控制曲线的凹陷程度，保持固定
                float offsetMagnitude = 300f;

                // 设置控制点的位置，沿垂直于方向的偏移
                controlPoint = (startPoint + endPoint) / 2 + perpendicular * offsetMagnitude * (isCurveUpwards ? 1f : -1f);

                Projectile.ai[0] = 1;
            }


            // 贝塞尔曲线进度
            progress += 1f / Projectile.timeLeft;
            if (progress >= 1f)
            {
                Projectile.Kill();
                return;
            }

            // 计算贝塞尔曲线位置
            Vector2 bezierPosition = Vector2.Lerp(Vector2.Lerp(startPoint, controlPoint, progress), Vector2.Lerp(controlPoint, endPoint, progress), progress);
            Projectile.Center = bezierPosition;

            // 旋转和粒子效果
            Projectile.rotation += 0.2f;
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.75f);
            Time++;


            {
                // 黄金螺旋 · 狂野增强版
                if (Main.rand.NextBool(2))
                {
                    float time = Main.GameUpdateCount * 0.12f;
                    float goldenAngle = MathHelper.ToRadians(137.5f);
                    int spiralPoints = 5; // 增加爆发感

                    for (int i = 0; i < spiralPoints; i++)
                    {
                        float angle = time + goldenAngle * i;
                        float radius = 48f + 16f * (float)Math.Sin(time + i * 0.5f); // 扩大范围

                        Vector2 offset = angle.ToRotationVector2() * radius;

                        // CritSpark
                        Particle critSpark = new CritSpark(
                            Projectile.Center + offset,
                            offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.5f, 4.5f),
                            Color.White,
                            Color.Gold,
                            Main.rand.NextFloat(1.2f, 2.0f),
                            Main.rand.Next(18, 28),
                            0.14f,
                            3
                        );
                        GeneralParticleHandler.SpawnParticle(critSpark);

                        // Spark
                        Particle spark = new SparkParticle(
                            Projectile.Center + offset * 0.7f,
                            offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.8f, 3.2f),
                            false,
                            24,
                            Main.rand.NextFloat(0.9f, 1.5f),
                            Color.Gold
                        );
                        GeneralParticleHandler.SpawnParticle(spark);

                        // Dust
                        Vector2 dustVelocity = offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.8f, 3.0f);
                        Dust goldDust = Dust.NewDustPerfect(
                            Projectile.Center + offset * 0.85f,
                            DustID.GoldCoin,
                            dustVelocity,
                            100,
                            Color.White,
                            Main.rand.NextFloat(1.5f, 2.2f)
                        );
                        goldDust.noGravity = true;
                    }
                }

                // 光环特效保持不变
                if (Time % 3 == 0)
                {
                    Vector2 particleOffset = new Vector2(13.5f * Projectile.direction, 0);
                    particleOffset.X += Main.rand.NextFloat(-3f, 3f);
                    Vector2 particlePosition = Projectile.Center + particleOffset + Projectile.velocity * 0.5f;
                    Particle Smear = new CircularSmearVFX(
                        particlePosition,
                        Color.White * Main.rand.NextFloat(0.78f, 0.85f),
                        Main.rand.NextFloat(-8, 8),
                        Main.rand.NextFloat(1.2f, 1.3f)
                    );
                    GeneralParticleHandler.SpawnParticle(Smear);
                }

                // 狂野随机方向动态释放的 3选1增强替换
                if (Main.rand.NextBool(2))
                {
                    float time = Main.GameUpdateCount * 0.2f;
                    float dynamicAngle = time + Main.rand.NextFloat(MathHelper.TwoPi);
                    float dynamicRadius = Main.rand.NextFloat(24f, 48f);
                    Vector2 dynamicOffset = dynamicAngle.ToRotationVector2() * dynamicRadius;
                    Vector2 particlePosition = Projectile.Center + dynamicOffset;
                    Vector2 dynamicVelocity = dynamicOffset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.5f, 4.5f);

                    int particleType = Main.rand.Next(3);
                    switch (particleType)
                    {
                        case 0:
                            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                                particlePosition,
                                dynamicVelocity * 0.6f,
                                Color.Gold,
                                Main.rand.NextFloat(0.36f, 0.72f),
                                Main.rand.Next(20, 30)
                            ));
                            break;

                        case 1:
                            GeneralParticleHandler.SpawnParticle(new SparkParticle(
                                particlePosition,
                                dynamicVelocity * 0.8f,
                                false,
                                22,
                                Main.rand.NextFloat(0.8f, 1.4f),
                                Color.Yellow
                            ));
                            break;

                        case 2:
                            GeneralParticleHandler.SpawnParticle(new CritSpark(
                                particlePosition,
                                dynamicVelocity,
                                Color.White,
                                Color.Gold,
                                Main.rand.NextFloat(1.0f, 1.6f),
                                Main.rand.Next(16, 24),
                                0.12f,
                                3
                            ));
                            break;
                    }
                }

            }


        }



        //public override void OnKill(int timeLeft)
        //{
        //    // 初始化粒子列表
        //    if (Particles == null)
        //        Particles = new List<Particle>();

        //    Player player = Main.player[Projectile.owner];
        //    Vector2 startPosition = Projectile.Center;
        //    Vector2 endPosition = player.Center;
        //    Vector2 sizeVector = (endPosition - startPosition).SafeNormalize(Vector2.Zero) * 300f; // 链条长度限制为300像素

        //    // 清空旧粒子，生成新的星空链条
        //    Particles.Clear();
        //    float constellationColorHue = Main.rand.NextFloat(); // 初始色调
        //    Vector2 previousStar = startPosition;
        //    Vector2 offset;

        //    // 创建星空链条效果的粒子
        //    for (float i = 0; i < 1f; i += Main.rand.NextFloat(0.2f, 0.5f))
        //    {
        //        constellationColorHue = (constellationColorHue + 0.16f) % 1f;
        //        Color constellationColor = Main.hslToRgb(constellationColorHue, 1f, 0.8f);
        //        offset = Main.rand.NextFloat(-50f, 50f) * sizeVector.RotatedBy(MathHelper.PiOver2);

        //        // 生成星星粒子
        //        Particle star = new GenericSparkle(startPosition + sizeVector * i + offset, Vector2.Zero, Color.White, constellationColor, Main.rand.NextFloat(1f, 1.5f), 20, 0f, 3f);
        //        Particles.Add(star);

        //        // 生成连接线条粒子
        //        Particle line = new BloomLineVFX(previousStar, startPosition + sizeVector * i + offset - previousStar, 0.8f, constellationColor * 0.75f, 20, true);
        //        Particles.Add(line);

        //        previousStar = startPosition + sizeVector * i + offset;
        //    }

        //    // 在终点生成星星粒子和最终的连接线
        //    Particle finalStar = new GenericSparkle(endPosition, Vector2.Zero, Color.White, Color.Cyan, Main.rand.NextFloat(1f, 1.5f), 20, 0f, 3f);
        //    Particles.Add(finalStar);
        //    Particle finalLine = new BloomLineVFX(previousStar, endPosition - previousStar, 0.8f, Color.Cyan * 0.75f, 20, true);
        //    Particles.Add(finalLine);

        //}


        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 startPosition = Projectile.Center;
            Vector2 directionToPlayer = (player.Center - startPosition).SafeNormalize(Vector2.Zero);

            // 生成指向玩家的 ArkOfTheCosmos_BlastAttack 弹幕
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), startPosition, directionToPlayer * 10f,
                ModContent.ProjectileType<ElementalArkJavBlast>(), (Projectile.damage)*2, Projectile.knockBack, Projectile.owner);
        }


    }
}
