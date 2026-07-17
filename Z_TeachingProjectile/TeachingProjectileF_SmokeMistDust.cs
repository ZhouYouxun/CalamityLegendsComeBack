using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileF_SmokeMistDust : ModProjectile
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

            // 1.重型烟雾（HeavySmokeParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle heavySmokeParticle = new HeavySmokeParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                Color.Gray, // 是否受重力影响
                35, // 生命周期，单位是帧
                1.0f, // 缩放大小
                0.8f, // 颜色
                0.02f, // 拉伸或压缩比例
                false, // 开关参数
                0f, // 开关参数
                false, // 旋转角度或方向
                false // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(heavySmokeParticle);

            //// 2.小型烟雾（SmallSmokeParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle smallSmokeParticle = new SmallSmokeParticle(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(0.8f, 0.8f), // 初始速度
            //    Color.WhiteSmoke, // 是否受重力影响
            //    Color.Gray, // 生命周期，单位是帧
            //    0.7f, // 缩放大小
            //    0.6f, // 颜色
            //    0.02f, // 拉伸或压缩比例
            //    false // 开关参数
            //);
            //GeneralParticleHandler.SpawnParticle(smallSmokeParticle);

            // 3.定时烟雾（TimedSmokeParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle timedSmokeParticle = new TimedSmokeParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(0.8f, 0.8f), // 初始速度
                Color.WhiteSmoke, // 是否受重力影响
                Color.DarkGray, // 生命周期，单位是帧
                0.9f, // 缩放大小
                0.65f, // 颜色
                32, // 拉伸或压缩比例
                0.02f // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(timedSmokeParticle);

            //// 4.塔纳托斯烟雾（ThanatosSmokeParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle thanatosSmokeParticle = new ThanatosSmokeParticle(
            //    Projectile.Center, // 生成位置
            //    30, // 初始速度
            //    1.0f, // 是否受重力影响
            //    0.8f, // 生命周期，单位是帧
            //    forward.ToRotation() // 缩放大小
            //);
            //GeneralParticleHandler.SpawnParticle(thanatosSmokeParticle);

            //// 5.中型雾气（MediumMistParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle mediumMistParticle = new MediumMistParticle(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    Color.LightBlue, // 是否受重力影响
            //    Color.Transparent, // 生命周期，单位是帧
            //    1.0f, // 缩放大小
            //    0.7f, // 颜色
            //    0.02f // 拉伸或压缩比例
            //);
            //GeneralParticleHandler.SpawnParticle(mediumMistParticle);

            //// 6.透明混合中型雾（MediumMistParticleAlphaBlend）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle mediumMistParticleAlphaBlend = new MediumMistParticleAlphaBlend(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    Color.LightGreen, // 是否受重力影响
            //    Color.Transparent, // 生命周期，单位是帧
            //    1.0f, // 缩放大小
            //    0.55f, // 颜色
            //    0.02f // 拉伸或压缩比例
            //);
            //GeneralParticleHandler.SpawnParticle(mediumMistParticleAlphaBlend);

            // 7.瘟疫湿雾（PlagueHumidifierMist）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle plagueHumidifierMist = new PlagueHumidifierMist(
                Projectile.Center, // 生成位置
                35, // 初始速度
                1.0f, // 是否受重力影响
                Main.rand.NextVector2Circular(1f, 1f) // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(plagueHumidifierMist);


            //// 8.沙尘粒子（SandyDustParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle sandyDustParticle = new SandyDustParticle(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    Color.SandyBrown, // 是否受重力影响
            //    1.0f, // 生命周期，单位是帧
            //    30, // 缩放大小
            //    0.03f, // 颜色
            //    new Vector2(0f, 0.04f) // 拉伸或压缩比例
            //);
            //GeneralParticleHandler.SpawnParticle(sandyDustParticle); 这就是一个Dust特效，没别的

            //// 9.弹幕伪尘（ArianeFakeDust）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle arianeFakeDust = new ArianeFakeDust(
            //    Projectile, // 生成位置
            //    Projectile.Center, // 初始速度
            //    Main.rand.NextVector2Circular(1f, 1f), // 是否受重力影响
            //    Color.White, // 生命周期，单位是帧
            //    1.0f, // 缩放大小
            //    28, // 颜色
            //    0.03f, // 拉伸或压缩比例
            //    false // 开关参数
            //);
            //GeneralParticleHandler.SpawnParticle(arianeFakeDust);

            //// 10.辉光伪尘（FakeGlowDust）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle fakeGlowDust = new FakeGlowDust(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    Color.Cyan, // 是否受重力影响
            //    1.0f, // 生命周期，单位是帧
            //    28, // 缩放大小
            //    0.03f, // 颜色
            //    false, // 拉伸或压缩比例
            //    true, // 开关参数
            //    new Vector2(0f, 0.02f) // 开关参数
            //);
            //GeneralParticleHandler.SpawnParticle(fakeGlowDust); 这也就是一个Dust特效，没别的
        }
    }
}
