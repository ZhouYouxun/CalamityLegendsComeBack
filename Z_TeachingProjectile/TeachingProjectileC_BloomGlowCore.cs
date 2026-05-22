using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileC_BloomGlowCore : ModProjectile
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

            // 1.强烈光晕（StrongBloom）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle strongBloom = new StrongBloom(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.LimeGreen, // 是否受重力影响
                1.8f, // 生命周期，单位是帧
                40 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(strongBloom);

            // 2.普通光晕（GenericBloom）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle genericBloom = new GenericBloom(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.GreenYellow, // 是否受重力影响
                1.3f, // 生命周期，单位是帧
                36, // 缩放大小
                true, // 颜色
                true // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(genericBloom);

            // 3.绽放光粒（BloomParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bloomParticle = new BloomParticle(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.LightGreen, // 是否受重力影响
                0.4f, // 生命周期，单位是帧
                2.0f, // 缩放大小
                45, // 颜色
                true // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(bloomParticle);

            // 4.光晕圆环（BloomRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bloomRing = new BloomRing(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.ForestGreen, // 是否受重力影响
                1.6f, // 生命周期，单位是帧
                38 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(bloomRing);

            // 5.辉光球（GlowOrbParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle glowOrbParticle = new GlowOrbParticle(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                false, // 是否受重力影响
                20, // 生命周期，单位是帧
                0.9f, // 缩放大小
                Color.Red, // 颜色
                true, // 拉伸或压缩比例
                false, // 开关参数
                true // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(glowOrbParticle);

            // 6.扁平辉光（FlatGlow）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle flatGlow = new FlatGlow(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Cyan, // 是否受重力影响
                forward.ToRotation(), // 生命周期，单位是帧
                new Vector2(0.2f, 1.4f), // 缩放大小
                new Vector2(0.02f, 2.4f), // 颜色
                20 // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(flatGlow);
        }
    }
}
