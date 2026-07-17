using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileL_SlashSmearAfterimage : ModProjectile
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

            // 1.圆形涂抹（CircularSmearVFX）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle circularSmearVFX = new CircularSmearVFX(
                Projectile.Center, // 生成位置
                Color.Orange, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                1.0f // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(circularSmearVFX);

            // 2.烟雾圆形涂抹（CircularSmearSmokeyVFX）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle circularSmearSmokeyVFX = new CircularSmearSmokeyVFX(
                Projectile.Center, // 生成位置
                Color.Gray, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                1.0f // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(circularSmearSmokeyVFX);

            // 3.半圆涂抹（SemiCircularSmearVFX）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle semiCircularSmearVFX = new SemiCircularSmearVFX(
                Projectile.Center, // 生成位置
                Color.Cyan, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                new Vector2(1f, 0.8f), // 缩放大小
                false // 颜色
            );
            GeneralParticleHandler.SpawnParticle(semiCircularSmearVFX);

            // 4.淡出半圆涂抹（SemiCircularSmearFade）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle semiCircularSmearFade = new SemiCircularSmearFade(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.05f, // 初始速度
                Color.LightBlue, // 是否受重力影响
                forward.ToRotation(), // 生命周期，单位是帧
                1.0f, // 缩放大小
                new Vector2(1f, 0.8f), // 颜色
                24, // 拉伸或压缩比例
                false, // 开关参数
                false, // 开关参数
                true, // 旋转角度或方向
                1 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(semiCircularSmearFade);

            // 5.三叉圆形涂抹（TrientCircularSmear）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle trientCircularSmear = new TrientCircularSmear(
                Projectile.Center, // 生成位置
                Color.Yellow, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                1.0f // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(trientCircularSmear);

            // 6.贯穿斩击（SlashThrough）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle slashThrough = new SlashThrough(
                Color.Red, // 生成位置
                Projectile.Center, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                24, // 生命周期，单位是帧
                target // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(slashThrough);

            // 7.螳螂拳影（MantisPunch）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle mantisPunch = new MantisPunch(
                Projectile.Center, // 生成位置
                forward.ToRotation() // 初始速度
            );
            GeneralParticleHandler.SpawnParticle(mantisPunch);
        }
    }
}
