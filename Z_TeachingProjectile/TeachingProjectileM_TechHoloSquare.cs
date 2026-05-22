using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileM_TechHoloSquare : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            NPC target = Main.npc[0];
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Projectile.velocity.LengthSquared() < 0.01f)
                forward = Vector2.UnitX;

            // 1.方形粒子（SquareParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle squareParticle = new SquareParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.Cyan, // 颜色
                0.1f // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(squareParticle);

            // 2.辉光方块（GlowSquareParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle glowSquareParticle = new GlowSquareParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.DeepSkyBlue, // 颜色
                true, // 拉伸或压缩比例
                0.1f // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(glowSquareParticle);

            // 3.科技全息方块（TechyHoloysquareParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle techyHoloysquareParticle = new TechyHoloysquareParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                1.0f, // 是否受重力影响
                Color.Cyan, // 生命周期，单位是帧
                30, // 缩放大小
                0.9f // 颜色
            );
            GeneralParticleHandler.SpawnParticle(techyHoloysquareParticle);

            // 4.纳米粒子（NanoParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle nanoParticle = new NanoParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                Color.Cyan, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                30, // 缩放大小
                false, // 颜色
                true, // 拉伸或压缩比例
                true, // 开关参数
                new Vector2(0f, 0.02f) // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(nanoParticle);

            // 5.毁灭者准星预警（DestroyerReticleTelegraph）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle destroyerReticleTelegraph = new DestroyerReticleTelegraph(
                target, // 生成位置
                Color.Red, // 初始速度
                0.2f, // 是否受重力影响
                1.2f, // 生命周期，单位是帧
                40 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(destroyerReticleTelegraph);

            // 6.毁灭者火花预警（DestroyerSparkTelegraph）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle destroyerSparkTelegraph = new DestroyerSparkTelegraph(
                target, // 生成位置
                Color.Red, // 初始速度
                Color.Orange, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                40, // 缩放大小
                0.02f, // 颜色
                1.2f // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(destroyerSparkTelegraph);
        }
    }
}
