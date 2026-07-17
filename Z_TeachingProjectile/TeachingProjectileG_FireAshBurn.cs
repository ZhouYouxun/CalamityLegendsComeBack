using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileG_FireAshBurn : ModProjectile
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

            //// 1.火焰粒子（FireParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle fireParticle = new FireParticle(
            //    Projectile.Center, // 生成位置
            //    28, // 初始速度
            //    1.0f, // 是否受重力影响
            //    0.9f, // 生命周期，单位是帧
            //    Color.Orange, // 缩放大小
            //    Color.Red // 颜色
            //);
            //GeneralParticleHandler.SpawnParticle(fireParticle);

            // 2.火苗粒子（FlameParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle flameParticle = new FlameParticle(
                Projectile.Center, // 生成位置
                28, // 初始速度
                1.0f, // 是否受重力影响
                0.9f, // 生命周期，单位是帧
                Color.Yellow, // 缩放大小
                Color.OrangeRed // 颜色
            );
            GeneralParticleHandler.SpawnParticle(flameParticle);

            //// 3.死亡灰烬（DeathAshParticle）---------------------------------
            //// 原灾示范：Yharon、SupremeCatastrophe、SupremeCataclysm
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //DeathAshParticle deathAshParticle = new DeathAshParticle(
            //    30, // 生成位置
            //    0.8f, // 初始速度
            //    Projectile.Center // 是否受重力影响
            //);
            //DeathAshParticle.Ashes.Add(deathAshParticle); // 加入死亡灰烬系统，注意它不是普通 Particle

            //// 4.方形灰烬（SquareAshParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle squareAshParticle = new SquareAshParticle(
            //    Projectile.Center, // 生成位置
            //    Main.rand.NextVector2Circular(1f, 1f), // 初始速度
            //    32, // 是否受重力影响
            //    0.9f, // 生命周期，单位是帧
            //    Color.OrangeRed // 缩放大小
            //);
            //GeneralParticleHandler.SpawnParticle(squareAshParticle);

            //// 5.附魔火粒（EnchantedParticle）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle enchantedParticle = new EnchantedParticle(
            //    Projectile.Center, // 生成位置
            //    35, // 初始速度
            //    1.0f, // 是否受重力影响
            //    Color.DeepSkyBlue, // 生命周期，单位是帧
            //    Color.White, // 缩放大小
            //    0.08f, // 颜色
            //    8f // 拉伸或压缩比例
            //);
            //GeneralParticleHandler.SpawnParticle(enchantedParticle);
        }
    }
}
