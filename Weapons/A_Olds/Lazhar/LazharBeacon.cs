using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 雷达锁定信标 (LazharBeacon) 弹幕
    /// 由武器右键发射。
    /// 物理特性：高初速 (18f)，不受重力影响，接触敌怪时会对其施加 10秒 的“雷达锁定”减益，并播放锁敌声响，触发全息锁定波动圈后自我销毁。
    /// </summary>
    public class LazharBeacon : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        // 借用灾厄的常用发光光流贴图，通过 PreDraw 强制渲染为亮橘红色/金色
        public override string Texture => "CalamityMod/Projectiles/LaserProj";

        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true; // 撞击墙壁破碎
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 1; // 双倍物理更新，提升手感
        }

        public override void AI()
        {
            // 淡入渲染
            if (Projectile.alpha > 0)
                Projectile.alpha = Math.Max(0, Projectile.alpha - 50);

            // 飞行过程中散发出电离尾焰粒子
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(
                    Projectile.position, 
                    Projectile.width, 
                    Projectile.height, 
                    DustID.CopperCoin, 
                    -Projectile.velocity.X * 0.2f, 
                    -Projectile.velocity.Y * 0.2f, 
                    100, 
                    default, 
                    1.1f
                );
                d.noGravity = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0.05f); // 散发橘红色氛围光
            timer++;
        }

        /// <summary>
        /// 击中NPC时施加“雷达锁定”Buff，并生成声光配合特效
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 给NPC施加 600 帧（10秒）的雷达锁定Buff
            target.AddBuff(ModContent.BuffType<LazharTargetDebuff>(), 600);

            // 播放锁定成功的高频声纳哔哔声 (Radar locked style)
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.85f, Pitch = 0.5f }, target.Center);
            SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { Volume = 0.4f, Pitch = -0.3f }, target.Center);

            if (!Main.dedServ)
            {
                // 生成一圈向外扩张的科技雷达扫描圈 (BloomCircle 扁化扩散)
                Vector2 impactPoint = Projectile.Center;
                
                // 强烈爆发粒子
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        impactPoint,
                        vel,
                        false,
                        16,
                        Main.rand.NextFloat(0.4f, 0.7f),
                        Color.OrangeRed,
                        true,
                        true
                    ));
                }

                // 全息冲击波环 (SquishyLightParticle 模拟环状散失)
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    impactPoint,
                    Vector2.Zero,
                    1.2f,
                    Color.Lerp(Color.Gold, Color.OrangeRed, 0.5f),
                    20
                ));
            }
        }

        /// <summary>
        /// 撞击墙壁时破碎，播放高频碎裂电磁音效
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 播放电磁撞击碎裂音
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.45f, Pitch = 0.4f }, Projectile.Center);

            if (!Main.dedServ)
            {
                // 物块碰撞粒子
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 1f);
                    d.velocity = oldVelocity.RotatedByRandom(0.5f) * -0.4f;
                    d.noGravity = true;
                }
            }
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 信标发光色：暖金红
            Color glowColor = Color.Lerp(Color.Gold, Color.OrangeRed, 0.4f) with { A = 0 } * Projectile.Opacity;

            // 绘制运动拖尾，使其在高速飞行中表现为一个拖着流光带的信标探头
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float trailOpacity = (float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length;
                
                Main.spriteBatch.Draw(
                    tex,
                    oldDrawPos,
                    null,
                    glowColor * trailOpacity * 0.5f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (1f - (float)i / Projectile.oldPos.Length * 0.4f),
                    SpriteEffects.None,
                    0f
                );
            }

            // 绘制探头核心本体
            Main.spriteBatch.Draw(
                tex,
                drawPos,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.2f,
                SpriteEffects.None,
                0f
            );

            // 绘制高亮核心叠层
            Main.spriteBatch.Draw(
                tex,
                drawPos,
                null,
                glowColor * 1.5f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.1f,
                SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}
