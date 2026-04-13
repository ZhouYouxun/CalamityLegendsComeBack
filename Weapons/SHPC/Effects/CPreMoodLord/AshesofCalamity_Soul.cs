using CalamityMod.Dusts;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class AshesofCalamity_Soul : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;

            // ===== 现在是友方弹幕 =====
            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 80;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {


        }

        public override void AI()
        {
            // ===== 1. 帧动画 =====
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 9)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 6)
                Projectile.frame = 0;

            // ===== 2. 淡入 =====
            if (Projectile.alpha > 5)
                Projectile.alpha -= 15;
            if (Projectile.alpha < 5)
                Projectile.alpha = 5;

            // ===== 3. 朝向与旋转 =====
            // 这里保留原本那套朝向逻辑，让不可见弹幕的“特效方向感”完全跟着速度走
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() +
                                  (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) -
                                  MathHelper.ToRadians(90f) * Projectile.direction;

            // ===== 4. 常态加速 =====
            // 用户要求：获得一个常态加速
            Projectile.velocity *= 1.03f;

            // ===== 5. 红色照明 =====
            Lighting.AddLight(Projectile.Center, 0.65f, 0f, 0f);

            // ===== 6. Brimstone Dust =====
            // 这是原本风格里很基础的一层“红色邪火尘”
            // 数量极少，只作为底味，不让画面太脏
            Dust brimDust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(32f, 32f),
                (int)CalamityDusts.Brimstone,
                null,
                170,
                default,
                1.1f
            );
            brimDust.noGravity = true;
            brimDust.velocity *= 0.5f;
            brimDust.velocity += Projectile.velocity * 0.1f;

            // ===== 7. Metaball 主体 =====
            // 这一层是整个弹幕“像灾厄飞矢”的核心体积感来源
            // 并不是在画贴图，而是在持续往弹幕周围喂高密度 metaball 粒子
            // 让它在视觉上融合成一团持续前冲的红色能量体
            for (int i = 0; i < 1; i++)
            {
                CalamitasMetaball.SpawnParticle(
                    Projectile.Center + Projectile.velocity,
                    Main.rand.NextVector2Circular(2f, 2f),
                    64f
                );
            }

            // ===== 8. 双螺旋辉光球 =====
            // 这是你额外要求加上的部分
            // 做法是：取弹幕前进方向的垂线，使用 sin 让两侧偏移量周期变化
            // 一正一反两个位置，就形成了围绕弹道的“双螺旋”观感
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            float sine = (float)Math.Sin(Projectile.timeLeft * 0.575f / MathHelper.Pi);
            Vector2 helixOffset = right * sine * 16f;

            // 左侧辉光球
            GlowOrbParticle orbA = new GlowOrbParticle(
                Projectile.Center + helixOffset,
                Vector2.Zero,
                false,
                5,
                0.9f,
                Color.Red,
                true,
                false,
                true
            );
            GeneralParticleHandler.SpawnParticle(orbA);

            // 右侧辉光球
            GlowOrbParticle orbB = new GlowOrbParticle(
                Projectile.Center - helixOffset,
                Vector2.Zero,
                false,
                5,
                0.9f,
                Color.Red,
                true,
                false,
                true
            );
            GeneralParticleHandler.SpawnParticle(orbB);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ===== 命中敌人时，补一层“飞行期间同款”的视觉反馈 =====
            // 不搞大爆炸，不搞新体系，就沿用：
            // 1）红色 metaball
            // 2）红色双螺旋/双侧辉光球
            // 这样味道最统一

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.Pi / 2f);

            // ===== 命中点的 metaball 团 =====
            for (int i = 0; i < 2; i++)
            {
                CalamitasMetaball.SpawnParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    58f
                );
            }

            // ===== 命中点的双侧辉光球 =====
            Vector2 hitOffset = right * 12f;

            GlowOrbParticle hitOrbA = new GlowOrbParticle(
                target.Center + hitOffset,
                Vector2.Zero,
                false,
                6,
                1f,
                Color.Red,
                true,
                false,
                true
            );
            GeneralParticleHandler.SpawnParticle(hitOrbA);

            GlowOrbParticle hitOrbB = new GlowOrbParticle(
                target.Center - hitOffset,
                Vector2.Zero,
                false,
                6,
                1f,
                Color.Red,
                true,
                false,
                true
            );
            GeneralParticleHandler.SpawnParticle(hitOrbB);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(250, 50, 50, Projectile.alpha);
        }

        public override void OnKill(int timeLeft)
        {
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}