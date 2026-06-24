using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// 轨道卫星激光打击 (LazharOrbitalStrike) 弹幕
    /// 当攻击被雷达锁定的目标时触发。
    /// 物理特性：
    /// - 瞬间定位于目标NPC，实时黏附追踪。
    /// - 寿命较短 (15帧)，逐渐虚无淡出。
    /// - 碰撞盒拓展：通过 Colliding 覆写，将其检测线由天顶 (Y轴-1200像素) 垂直拉回至敌怪，对击中线上的所有怪造成贯穿伤害。
    /// - 渲染特性：在像素着色层绘制一根巨大无比的白炽金色光柱，具有极强的破坏性视觉冲击。
    /// </summary>
    public class LazharOrbitalStrike : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        // 光柱由顶点着色器手绘，本身没有贴图实体
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private const int MaxLifetime = 15;

        public override void SetStaticDefaults()
        {
            // 设定较大的缓存，确保光柱上下绘制连接完整
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = MaxLifetime;
            Projectile.penetrate = -1; // 贯穿打击
            Projectile.tileCollide = false; // 穿过地形，来自太空港的俯冲激光
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // 每只怪仅能被这次卫星打击造成一次伤害
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];

            // 实时黏附追踪处于锁定标记状态的NPC，防止光柱与跑动的BOSS偏离
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC npc = Main.npc[targetIndex];
                if (npc.active && !npc.friendly)
                {
                    Projectile.Center = npc.Center;
                }
            }

            // 首帧生成时，进行爆点声光演出和屏幕微震
            if (timer == 0)
            {
                // 震地低音 Exo 爆炸音效
                SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaExplosionSound with { Volume = 0.85f, Pitch = -0.15f }, Projectile.Center);
                
                // 触发玩家中等屏幕抖动
                Player owner = Main.player[Projectile.owner];
                if (owner.active && Projectile.owner == Main.myPlayer)
                {
                    owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 6f);
                }

                // 客户端生成巨大的环形金色能量粒子和飞溅火花
                if (!Main.dedServ)
                {
                    SpawnExplosionParticles();
                }
            }

            timer++;
        }

        /// <summary>
        /// 自定义线型垂直碰撞箱
        /// 从 NPC Center 垂直向上延伸 1200 像素，对线宽 32 像素范围内的所有敌怪进行贯穿检测
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 startPoint = Projectile.Center - new Vector2(0f, 1200f);
            Vector2 endPoint = Projectile.Center;
            float lineWidth = 32f * Projectile.scale;

            bool isColliding = Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), 
                targetHitbox.Size(), 
                startPoint, 
                endPoint, 
                lineWidth, 
                ref _
            );

            if (isColliding)
                return true;

            return false;
        }

        /// <summary>
        /// 卫星光流落地的爆点火焰和膨胀能量圈效果
        /// </summary>
        private void SpawnExplosionParticles()
        {
            Vector2 explosionCenter = Projectile.Center;

            // 生成向上冲腾的金色高亮火球粒子群
            for (int i = 0; i < 12; i++)
            {
                Vector2 sparkVel = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, -1f));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    explosionCenter + Main.rand.NextVector2Circular(24f, 12f),
                    sparkVel,
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 1f),
                    Color.Lerp(Color.Gold, Color.OrangeRed, Main.rand.NextFloat(0.1f, 0.5f)),
                    true,
                    true
                ));
            }

            // 产生水平扩散的强能爆闪圆环
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                explosionCenter,
                Vector2.Zero,
                2f,
                Color.Gold,
                16
            ));
        }

        /// <summary>
        /// 轨道光柱顶点渲染
        /// 从天空直到敌怪身体，绘制一根粗壮宏伟的白金色全息光幕柱
        /// </summary>
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            // 构建自上而下的垂直节点数组
            Vector2[] beamPoints = new Vector2[12];
            float maxBeamHeight = 1200f;
            
            for (int i = 0; i < beamPoints.Length; i++)
            {
                float ratio = (float)i / (beamPoints.Length - 1);
                // 顶部节点朝高空Y坐标偏移，底部贴合敌怪中心
                beamPoints[i] = Projectile.Center - new Vector2(0f, maxBeamHeight * (1f - ratio));
            }

            // ── 外圈：超厚暖橙色极光光柱 (使用 ScarletDevilStreak 形成波动能量环流) ──
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak")
            );

            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    OffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                beamPoints.Length * 2
            );

            // ── 内核：炽热白光离子柱 (使用 SylvestaffStreak 表现核心聚能) ──
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak")
            );

            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(
                    CoreWidthFunction,
                    CoreColorFunction,
                    OffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                beamPoints.Length * 2
            );
        }

        // 光柱横向波动幅度 (由于卫星光轴稳定性高，波动较小，仅做轻微能量震颤)
        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float shimmer = (float)Math.Sin(completion * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 20f) * 1.5f;
            return Vector2.UnitX * shimmer;
        }

        // 外圈光幕粗细函数 (随着弹幕timeLeft的衰减，光幕迅速变细淡出)
        private float WidthFunction(float completion, Vector2 _)
        {
            float progress = (float)Projectile.timeLeft / MaxLifetime;
            // 轨道打击是一根均匀的光柱，头部和底部做微小的收束以显得柔和
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 32f * progress * (0.8f + taper * 0.2f) * Projectile.scale;
        }

        // 外圈光柱颜色函数 (高热度的金黄色向橙红色渐变)
        private Color ColorFunction(float completion, Vector2 _)
        {
            float progress = (float)Projectile.timeLeft / MaxLifetime;
            Color coreColor = Color.Gold;
            Color outerColor = Color.OrangeRed;
            
            Color color = Color.Lerp(coreColor, outerColor, completion * 0.5f) * progress;
            color.A = 0; // 加法混合渲染
            return color;
        }

        // 内核白光粗细函数
        private float CoreWidthFunction(float completion, Vector2 _)
        {
            float progress = (float)Projectile.timeLeft / MaxLifetime;
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 14f * progress * (0.9f + taper * 0.1f) * Projectile.scale;
        }

        // 内核白光颜色函数 (白炽高亮)
        private Color CoreColorFunction(float completion, Vector2 _)
        {
            float progress = (float)Projectile.timeLeft / MaxLifetime;
            Color color = Color.Lerp(Color.White, Color.Gold, completion * 0.3f) * progress;
            color.A = 0;
            return color;
        }
    }
}

