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
using CalamityMod.Sounds;
using CalamityThrowingSpear.Weapons.ChangedWeapons.EAfterDog.DragonRageC;
using Terraria.Audio;
using static tModPorter.ProgressUpdate;
using System.Net;
using CalamityMod.Particles;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkJavBlade : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJBlade";
        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";
        private Vector2 startPoint;
        private Vector2 controlPoint;
        private Vector2 endPoint;
        private float progress;
        private bool directionUpwards;
        private bool initialDirectionUpwards; // 保存初始方向
        public int Time = 0;
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
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; 
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // 允许与方块碰撞
            Projectile.extraUpdates = 1; // 额外更新次数
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为44帧
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (Projectile.ai[0] == 0) // 初始化位置
            {
                startPoint = player.Center;
                endPoint = Main.MouseWorld;
                directionUpwards = Projectile.localAI[0] == 1f;
                initialDirectionUpwards = directionUpwards; // 保存初始方向

                Vector2 direction = (endPoint - startPoint).SafeNormalize(Vector2.Zero);
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                float offsetMagnitude = 300f;

                // 初始控制点计算，结合鼠标位置与玩家左右关系
                bool isMouseRightOfPlayer = endPoint.X > startPoint.X;
                controlPoint = (startPoint + endPoint) / 2 + perpendicular * offsetMagnitude * (directionUpwards ? 1f : -1f) * (isMouseRightOfPlayer ? 1f : -1f);

                Projectile.ai[0] = 1;
            }

            progress += 1f / Projectile.timeLeft;

            if (progress >= 1f)
            {
                if (endPoint == player.Center) // 如果当前终点是玩家位置，则销毁
                {
                    Projectile.Kill();
                    return;
                }

                // 更新起点与终点
                startPoint = endPoint;
                endPoint = player.Center;
                directionUpwards = !directionUpwards;

                // 重新计算控制点，确保回旋时反向
                Vector2 direction = (endPoint - startPoint).SafeNormalize(Vector2.Zero);
                Vector2 perpendicular = new Vector2(-direction.Y, -direction.X); // direction.X之前没有-，导致返回的不正确，现在有了
                float offsetMagnitude = 300f;

                // 判断初始方向与鼠标左右关系
                bool isMouseRightOfPlayer = startPoint.X > endPoint.X; // 重新评估相对位置
                controlPoint = (startPoint + endPoint) / 2 + perpendicular * offsetMagnitude * (directionUpwards ? 1f : -1f) * (isMouseRightOfPlayer ? 1f : -1f);

                progress = 0f;
            }

            // 计算贝塞尔曲线位置
            Vector2 bezierPosition = Vector2.Lerp(
                Vector2.Lerp(startPoint, controlPoint, progress),
                Vector2.Lerp(controlPoint, endPoint, progress),
                progress
            );
            Projectile.Center = bezierPosition;

            // 添加旋转和光效
            Projectile.rotation += 0.2f;
            Lighting.AddLight(Projectile.Center, Color.DeepSkyBlue.ToVector3() * 0.75f);

            {

                if (Main.rand.NextBool(2))
                {
                    // 黄金螺旋特效
                    float time = Main.GameUpdateCount * 0.08f; // 慢速旋转黄金角
                    float goldenAngle = MathHelper.ToRadians(137.5f); // 黄金角
                    int spiralPoints = 4; // 更多点充实大范围

                    for (int i = 0; i < spiralPoints; i++)
                    {
                        float angle = time + goldenAngle * i;
                        float radius = 36f + 12f * (float)Math.Sin(time + i); // 扩大半径范围

                        Vector2 offset = angle.ToRotationVector2() * radius;

                        // CritSpark (加大尺寸和速度)
                        Particle critSpark = new CritSpark(
                            Projectile.Center + offset,
                            offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.5f, 3.5f),
                            Color.White,
                            Color.LightYellow,
                            Main.rand.NextFloat(1.2f, 1.8f),
                            Main.rand.Next(18, 26),
                            0.12f,
                            3
                        );
                        GeneralParticleHandler.SpawnParticle(critSpark);

                        // Spark (加大尺寸和速度)
                        Particle spark = new SparkParticle(
                            Projectile.Center + offset * 0.7f,
                            offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.0f, 2.0f),
                            false,
                            20,
                            Main.rand.NextFloat(0.8f, 1.4f),
                            Color.White
                        );
                        GeneralParticleHandler.SpawnParticle(spark);

                        // Dust (金色)
                        Vector2 dustVelocity = offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 2.4f);
                        Dust goldDust = Dust.NewDustPerfect(
                            Projectile.Center + offset * 0.9f,
                            DustID.GoldCoin,
                            dustVelocity,
                            100,
                            Color.White,
                            Main.rand.NextFloat(1.2f, 1.8f)
                        );
                        goldDust.noGravity = true;
                    }
                }

                if (Time % 3 == 0)
                {
                    // 保留 CircularSmearVFX 不变
                    Vector2 particleOffset = new Vector2(13.5f * Projectile.direction, 0);
                    particleOffset.X += Main.rand.NextFloat(-3f, 3f);
                    Vector2 particlePosition = Projectile.Center + particleOffset + Projectile.velocity * 0.5f;

                    Particle Smear = new CircularSmearVFX(
                        particlePosition,
                        Color.LightYellow * Main.rand.NextFloat(0.78f, 0.85f),
                        Main.rand.NextFloat(-8, 8),
                        Main.rand.NextFloat(1.2f, 1.3f)
                    );
                    GeneralParticleHandler.SpawnParticle(Smear);
                }



            }

            Time++;

            //if (Time % 15 == 0)
            //{
            //    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/VividClarityBeamAppear")
            //    {
            //        Pitch = 0.3f,
            //        Volume = 1.6f
            //    }, Projectile.Center);
            //}

        }




    }
}