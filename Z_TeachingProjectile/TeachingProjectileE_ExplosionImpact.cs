using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileE_ExplosionImpact : ModProjectile
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

            // 1.细节爆炸（DetailedExplosion）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle detailedExplosion = new DetailedExplosion(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.OrangeRed * 0.9f, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                Main.rand.NextFloat(-0.3f, 0.3f), // 缩放大小
                0f, // 颜色
                0.28f, // 拉伸或压缩比例
                16, // 开关参数
                true // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(detailedExplosion);

            // 2.火焰爆炸（FlameExplosion）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle flameExplosion = new FlameExplosion(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Orange, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                Main.rand.NextFloat(-0.4f, 0.4f), // 缩放大小
                0.1f, // 颜色
                0.9f, // 拉伸或压缩比例
                20, // 开关参数
                0.9f // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(flameExplosion);

            // 3.等离子爆炸（PlasmaExplosion）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle plasmaExplosion = new PlasmaExplosion(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Cyan, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                Main.rand.NextFloat(-0.4f, 0.4f), // 缩放大小
                0.05f, // 初始大小
                0.18f, // 最终大小
                18 // 持续时间
            );
            GeneralParticleHandler.SpawnParticle(plasmaExplosion);

            // 4.撞击粒子（ImpactParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle impactParticle = new ImpactParticle(
                Projectile.Center, // 生成位置
                0.08f, // 初始速度
                18, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                Color.White // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(impactParticle);

            // 5.Boss咆哮波（BossRoar）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bossRoar = new BossRoar(
                Projectile.Center, // 生成位置
                Color.Red, // 初始速度
                0f, // 是否受重力影响
                0.2f, // 生命周期，单位是帧
                1.8f, // 缩放大小
                40, // 颜色
                0.8f // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(bossRoar);
        }
    }
}
