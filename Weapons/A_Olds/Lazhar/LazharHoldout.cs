using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 拉扎尔射线 (Lazhar) 的手持弹幕。
    /// 接管玩家持枪动作，提供高精度的旋转指向、三重爆破后坐力动画、极其华丽的枪体外圈描边和粒子星芒。
    /// ai[0] = 攻击模式 (0 = 左键三连发，1 = 右键发射锁定信标)
    /// </summary>
    public class LazharHoldout : ModProjectile
    {
        // 枪体使用透明材质占位，真实枪体在 PreDraw 中通过矩阵变换以顶点级别手动绘制
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 攻击模式属性
        private int AttackMode => (int)Projectile.ai[0];

        // 运行时内部计时与状态
        private int timer;
        private float recoilOffset;
        private float gunRotation;
        private int spriteDir;
        private int energyCoreGlowTime;
        private int energyCoreGlowBurst;

        // 手持弹幕绘制参数
        private const float GunSpriteWidth = 72f;
        private const float GunSpriteHeight = 32f;
        private const float MuzzleDistance = 42f; // 枪口相对于旋转中心的距离

        /// <summary>
        /// 能量核心中心在世界坐标中的物理位置 (用于绘制发光晶核)
        /// </summary>
        private Vector2 EnergyCorePosition
        {
            get
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                int facing = spriteDir == 0 ? 1 : spriteDir;
                // 枪身中部位置作为储能晶核
                return Projectile.Center + direction * 10f - direction.RotatedBy(MathHelper.PiOver2) * 2f * facing;
            }
        }

        /// <summary>
        /// 枪尖物理世界坐标 (用于精确发射子弹和喷射枪口火焰)
        /// </summary>
        private Vector2 MuzzlePosition => Projectile.Center + gunRotation.ToRotationVector2() * MuzzleDistance;

        public override void SetStaticDefaults()
        {
            // 防止高分辨率或缩放时，屏幕边缘弹幕因剔除而裁剪绘制
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36; // 与 Lazhar.cs 物品使用时间严格同步
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // 检查玩家和武器手持状态，防止越界或失效
            if (!owner.active || owner.dead || owner.HeldItem.ModItem is not Lazhar)
            {
                Projectile.Kill();
                return;
            }

            // 更新手持弹幕空间变换
            UpdateTransform(owner);

            // 网络同步，确保联机状态下枪管指向准确
            if (Projectile.owner == Main.myPlayer && timer % 3 == 0)
                Projectile.netUpdate = true;

            // 根据不同的攻击模式，执行发射器与后坐力协调
            if (AttackMode == 0)
            {
                ExecuteLeftClickBurst(owner);
            }
            else
            {
                ExecuteRightClickBeacon(owner);
            }

            // 粒子状态与发光计时递增
            if (energyCoreGlowBurst > 0)
                energyCoreGlowBurst--;

            energyCoreGlowTime++;
            timer++;
            Projectile.timeLeft = 2; // 维持手持状态，生命由物品动画结束或 AI 主动裁决
        }

        /// <summary>
        /// 更新枪管朝向与玩家手臂指向，确保完美的视线对齐
        /// </summary>
        private void UpdateTransform(Player owner)
        {
            Vector2 aimWorld = Projectile.owner == Main.myPlayer
                ? Main.MouseWorld
                : owner.Calamity().mouseWorld;

            Vector2 armPos = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 aimDir = (aimWorld - armPos).SafeNormalize(Vector2.UnitX * owner.direction);

            gunRotation = aimDir.ToRotation();
            spriteDir = Math.Sign(aimDir.X);
            if (spriteDir == 0)
                spriteDir = owner.direction;

            owner.ChangeDir(spriteDir);
            owner.heldProj = Projectile.whoAmI;

            // 枪身旋转轴偏移，贴合玩家手部
            Projectile.Center = armPos + aimDir * 24f;
            Projectile.velocity = aimDir;

            // 强制玩家使用手持施法动画
            owner.itemRotation = (float)Math.Atan2(aimDir.Y * owner.direction, aimDir.X * owner.direction);
            owner.itemTime = 2;
            owner.itemAnimation = 2;
        }

        /// <summary>
        /// 左键 3 连发爆发现线控制核心
        /// 在第 0, 8, 16 帧分别发射一枚高速高伤追踪射线，每次发射都会重置并触发枪体后坐力
        /// </summary>
        private void ExecuteLeftClickBurst(Player owner)
        {
            // 后坐力自然衰减
            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.15f);

            // 每隔 8 帧发射一次，共 3 次 (0, 8, 16)
            if (timer == 0 || timer == 8 || timer == 16)
            {
                // 设置核心爆发亮度
                energyCoreGlowBurst = 12;

                // 重置后坐力偏移到最大值 (14 像素)
                recoilOffset = 14f;

                // 播放清脆的 Exo 激光发射音效，带有微弱的音调偏移增强质感
                SoundEngine.PlaySound(CommonCalamitySounds.ExoLaserShootSound with { Volume = 0.65f, PitchVariance = 0.12f, MaxInstances = 8 }, Projectile.Center);

                if (Projectile.owner == Main.myPlayer)
                {
                    var lazharPlayer = owner.GetModPlayer<LazharPlayer>();
                    bool overloaded = lazharPlayer.OverloadReady;
                    float overloadArg = overloaded ? 1f : 0f;

                    // 枪管前部发射射线
                    Vector2 shootVelocity = gunRotation.ToRotationVector2() * 32f;
                    int projDamage = (int)(Projectile.damage * 0.95f);
                    
                    // 将过载标志传递给 Projectile.ai[1]
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        MuzzlePosition,
                        shootVelocity,
                        ModContent.ProjectileType<LazharLaser>(),
                        projDamage,
                        Projectile.knockBack,
                        Projectile.owner,
                        0f,
                        overloadArg
                    );

                    // 枪口小范围屏幕抖动 (过载时抖动更强)
                    owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, overloaded ? 4.5f : 2f);
                }

                // 在客户端生成炫目枪口金色粒子
                if (!Main.dedServ)
                {
                    var lazharPlayer = owner.GetModPlayer<LazharPlayer>();
                    SpawnMuzzleFlashParticles(lazharPlayer.OverloadReady ? Color.White : Color.Gold);
                }
            }

            // 3 连发加上合理的收枪尾帧后，自动销毁，并清除过载充能
            if (timer >= 28)
            {
                var lazharPlayer = owner.GetModPlayer<LazharPlayer>();
                if (lazharPlayer.OverloadReady)
                {
                    lazharPlayer.OverloadReady = false;
                    lazharPlayer.LockedHitCount = 0;
                }
                Projectile.Kill();
            }
        }

        /// <summary>
        /// 右键发射锁定信标控制器
        /// </summary>
        private void ExecuteRightClickBeacon(Player owner)
        {
            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.1f);

            if (timer == 0)
            {
                energyCoreGlowBurst = 18;
                recoilOffset = 22f; // 右键信标冲击力更大，后坐力强烈

                // 播放电磁聚能释放音效
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);

                if (Projectile.owner == Main.myPlayer)
                {
                    Vector2 shootVelocity = gunRotation.ToRotationVector2() * 18f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        MuzzlePosition,
                        shootVelocity,
                        ModContent.ProjectileType<LazharBeacon>(),
                        (int)(Projectile.damage * 0.2f), // 信标本身伤害较低
                        1f,
                        Projectile.owner
                    );
                }

                if (!Main.dedServ)
                {
                    // 右键生成高能橘红色/金色粒子，彰显锁定功能
                    SpawnMuzzleFlashParticles(Color.Orange);
                }
            }

            // 收枪尾帧结束后销毁
            if (timer >= 24)
            {
                Projectile.Kill();
            }
        }

        /// <summary>
        /// 产生枪口金色/橘红色发光粒子和气态膨胀粒子
        /// </summary>
        private void SpawnMuzzleFlashParticles(Color themeColor)
        {
            Vector2 muzzle = MuzzlePosition;
            Vector2 fireDirection = gunRotation.ToRotationVector2();

            // 生成球状光晕粒子，向前喷吐
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = fireDirection.RotatedByRandom(0.18f) * Main.rand.NextFloat(4f, 10f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    muzzle + fireDirection * Main.rand.NextFloat(0f, 3f),
                    vel,
                    false,
                    8,
                    Main.rand.NextFloat(0.4f, 0.7f),
                    Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.3f, 0.8f)),
                    true,
                    true
                ));
            }

            // 生成被拉伸的扁状强光粒子
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                muzzle,
                fireDirection * Main.rand.NextFloat(2f, 4f),
                Main.rand.NextFloat(0.3f, 0.5f),
                Color.Lerp(themeColor, Color.White, 0.45f),
                Main.rand.Next(6, 12)
            ));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.HeldItem.ModItem is not Lazhar)
                return false;

            // 获取枪体贴图
            Texture2D tex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/A_Olds/Lazhar/拉扎尔射线").Value;
            Vector2 origin = tex.Size() * 0.5f;

            Color themeColor = Color.Gold;

            // 考虑后坐力物理拉扯和屏幕偏移
            Vector2 aimDir = gunRotation.ToRotationVector2();
            Vector2 drawCenter = Projectile.Center - aimDir * recoilOffset - Main.screenPosition;

            // 翻转朝向
            SpriteEffects flip = spriteDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (owner.gravDir == -1f)
                flip ^= SpriteEffects.FlipVertically;

            // 发光缩放和呼吸节奏
            float idlePulse = 0.5f + 0.5f * (float)Math.Sin(timer * 0.15f);
            float burstPulse = MathHelper.Clamp(energyCoreGlowBurst / 18f, 0f, 1f);

            // 14 边界发光，提供流光溢彩的厚重金属科技感
            Color outlineColor = (Color.Lerp(themeColor, Color.White, 0.55f) with { A = 0 }) 
                * (0.6f + idlePulse * 0.4f + burstPulse * 0.9f);
            float outlineDistance = 1.8f + idlePulse * 2.2f + burstPulse * 3.8f;

            // 绘制描边
            for (int i = 0; i < 14; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, outlineColor, gunRotation, origin, 1f, flip, 0);
            }

            // 绘制核心内圈高亮
            Color innerOutlineColor = (Color.White with { A = 0 }) * (0.4f + burstPulse * 0.6f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1f + burstPulse * 1.5f);
                Main.EntitySpriteDraw(tex, drawCenter + offset, null, innerOutlineColor, gunRotation, origin, 1.02f + burstPulse * 0.03f, flip, 0);
            }

            // 绘制真实枪体
            Main.EntitySpriteDraw(tex, drawCenter, null, Color.White, gunRotation, origin, 1f, flip, 0);

            // 绘制能量核心发光区
            DrawEnergyCore(themeColor, flip, -aimDir * recoilOffset);

            return false;
        }

        /// <summary>
        /// 绘制处于枪膛中部的储能晶核，提供科技脉冲和十字光芒星效
        /// </summary>
        private void DrawEnergyCore(Color themeColor, SpriteEffects flip, Vector2 recoilVec)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkle = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D simpleStar = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;

            float pulse = MathHelper.Clamp(energyCoreGlowBurst / 18f, 0f, 1f);
            Color coreWhite = Color.Lerp(themeColor, Color.White, 0.5f);

            Vector2 corePos = EnergyCorePosition + recoilVec - Main.screenPosition;
            float time = energyCoreGlowTime;

            // ── 常驻圆环光晕 ──
            float breath = 0.08f + 0.04f * (float)Math.Sin(time * 0.1f);
            Main.EntitySpriteDraw(
                bloom,
                corePos,
                null,
                Color.Lerp(themeColor, coreWhite, 0.25f) with { A = 0 } * (0.3f + pulse * 0.6f),
                0f,
                bloom.Size() * 0.5f,
                new Vector2(1f, 0.4f) * (breath + pulse * 0.7f),
                flip
            );

            // ── 十字发光晶体 ──
            float starRot = gunRotation + time * 0.025f;
            float starScale = 0.04f + pulse * 0.07f;
            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    simpleStar,
                    corePos,
                    null,
                    coreWhite with { A = 0 } * (0.35f + pulse * 0.55f),
                    starRot + i * MathHelper.PiOver4,
                    simpleStar.Size() * 0.5f,
                    new Vector2(0.3f, 1.2f) * starScale,
                    flip
                );
            }

            // ── 双向放射长臂粒子效果 ──
            for (int i = 0; i < 4; i++)
            {
                float mult = 1f - 0.15f * i;
                if (pulse > 0f)
                {
                    Main.EntitySpriteDraw(
                        bloom,
                        corePos + Main.rand.NextVector2Circular(1.5f, 1.5f) * pulse,
                        null,
                        Color.Lerp(themeColor, Color.White, i * 0.2f) with { A = 0 } * pulse * 0.7f,
                        Main.rand.NextFloat(-3f, 3f),
                        bloom.Size() * 0.5f,
                        new Vector2(1f, 0.3f) * 0.6f * pulse * mult,
                        flip
                    );
                }

                for (int dir = -1; dir <= 1; dir += 2)
                {
                    float sine = MathHelper.Lerp((float)Math.Sin(Main.GlobalTimeWrappedHourly * 15f), (1f - pulse) * dir, 0.8f);
                    Vector2 scale = new Vector2(0.25f, 0.8f * sine * dir) * (Main.rand.NextFloat(2f, 3.5f) * mult + pulse * 1f);
                    float rotation = gunRotation + time * pulse * Math.Max(i - 1, 0) * 0.15f + MathHelper.PiOver4 * dir;

                    Main.EntitySpriteDraw(
                        sparkle,
                        corePos,
                        null,
                        Color.Lerp(themeColor, Color.White, i * 0.2f) with { A = 0 } * (0.4f + pulse * 0.6f),
                        rotation,
                        sparkle.Size() * 0.5f,
                        scale,
                        flip
                    );
                }
            }
        }
    }
}
