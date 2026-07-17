using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileH_WaterFoamOcean : ModProjectile
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

            // 1.水味粒子（WaterFlavoredParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle waterFlavoredParticle = new WaterFlavoredParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.LightBlue // 颜色
            );
            GeneralParticleHandler.SpawnParticle(waterFlavoredParticle);

            // 2.水沫粒子（WaterFoamParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle waterFoamParticle = new WaterFoamParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                30, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                Color.White // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(waterFoamParticle);

            // 3.水团粒子（WaterGlobParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle waterGlobParticle = new WaterGlobParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                1.0f, // 是否受重力影响
                0.03f, // 生命周期，单位是帧
                40 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(waterGlobParticle);

            //// 4.海沫粒子（SeaFoamParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle seaFoamParticle = new SeaFoamParticle(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    Color.White, // 是否受重力影响
            //    Color.LightBlue, // 生命周期，单位是帧
            //    1.0f, // 缩放大小
            //    0.8f, // 颜色
            //    0.03f // 拉伸或压缩比例
            //);
            //GeneralParticleHandler.SpawnParticle(seaFoamParticle);

            // 5.海棱镜粒子（SeaPrismParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle seaPrismParticle = new SeaPrismParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.1f, // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                0.9f, // 缩放大小
                Color.Cyan, // 颜色
                new Vector2(1.5f, 0.5f), // 拉伸或压缩比例
                true, // 开关参数
                forward.ToRotation(), // 开关参数
                0.95f, // 旋转角度或方向
                false, // 开关参数
                true, // 开关参数
                0.6f // 数值参数
            );
            GeneralParticleHandler.SpawnParticle(seaPrismParticle);

            // 6.普通气泡（GenericBubbleParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle genericBubbleParticle = new GenericBubbleParticle(
                Projectile.Center, // 生成位置
                new Vector2(0f, -1f), // 初始速度
                1.0f, // 是否受重力影响
                0f, // 生命周期，单位是帧
                50 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(genericBubbleParticle);



        }
    }
}
