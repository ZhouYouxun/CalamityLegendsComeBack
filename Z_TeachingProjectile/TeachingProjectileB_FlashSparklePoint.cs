using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileB_FlashSparklePoint : ModProjectile
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

            // 1.点刺粒子（PointParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle pointParticle = new PointParticle(
                Projectile.Center, // 生成位置
                -Projectile.velocity * 0.2f, // 初始速度
                false, // 是否受重力影响
                15, // 生命周期，单位是帧
                1.1f, // 缩放大小
                Color.Orange // 颜色
            );
            GeneralParticleHandler.SpawnParticle(pointParticle);

            // 2.通用十字星（GenericSparkle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle genericSparkle = new GenericSparkle(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Gold, // 是否受重力影响
                Color.Cyan, // 生命周期，单位是帧
                1.8f, // 缩放大小
                16, // 颜色
                0.02f, // 拉伸或压缩比例
                1.4f, // 开关参数
                false // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(genericSparkle);

            // 3.暴击小星（CritSpark）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle critSpark = new CritSpark(
                Projectile.Center, // 生成位置
                forward.RotatedByRandom(0.4f) * 4f, // 初始速度
                Color.White, // 是否受重力影响
                Color.LightBlue, // 生命周期，单位是帧
                1f, // 缩放大小
                16, // 颜色
                1f, // 拉伸或压缩比例
                1.2f, // 开关参数
                0f // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(critSpark);

            // 4.闪烁星粒（SparkleParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle sparkleParticle = new SparkleParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                Color.White, // 是否受重力影响
                Color.HotPink, // 生命周期，单位是帧
                1.1f, // 缩放大小
                18, // 颜色
                0.05f, // 拉伸或压缩比例
                1.3f, // 开关参数
                true, // 开关参数
                false // 旋转角度或方向
            );
            GeneralParticleHandler.SpawnParticle(sparkleParticle);

            // 5.大五角星（RoundedStarParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle roundedStarParticle = new RoundedStarParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                Color.LightGoldenrodYellow, // 是否受重力影响
                0.8f, // 生命周期，单位是帧
                24, // 缩放大小
                0.04f, // 颜色
                0.96f, // 拉伸或压缩比例
                false, // 开关参数
                Projectile.Center, // 开关参数
                Projectile.owner // 旋转角度或方向
            );
            GeneralParticleHandler.SpawnParticle(roundedStarParticle);

            // 6.雪花星芒（SnowflakeSparkle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle snowflakeSparkle = new SnowflakeSparkle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1.5f, 1.5f), // 初始速度
                Color.White, // 是否受重力影响
                Color.LightCyan, // 生命周期，单位是帧
                1.1f, // 缩放大小
                24, // 颜色
                0.04f, // 拉伸或压缩比例
                1.2f, // 开关参数
                6 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(snowflakeSparkle);

            // 7.华丽星星（FancyStars）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle fancyStars = new FancyStars(
                Projectile.Center, // 生成位置
                Main.rand.NextFloat(MathHelper.TwoPi), // 初始速度
                0.8f, // 是否受重力影响
                Main.rand.NextVector2Circular(2f, 2f), // 生命周期，单位是帧
                0.03f, // 缩放大小
                30, // 颜色
                Color.Yellow // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(fancyStars);

            // 8.耀斑闪光（FlareShine）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle flareShine = new FlareShine(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.White, // 是否受重力影响
                Color.Orange, // 生命周期，单位是帧
                forward.ToRotation() + MathHelper.PiOver2, // 缩放大小
                new Vector2(0.4f, 1.8f), // 颜色
                new Vector2(0.5f, 6.8f), // 拉伸或压缩比例
                18, // 开关参数
                0.01f, // 开关参数
                1.4f, // 旋转角度或方向
                0f, // 开关参数
                0 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(flareShine);

            // 9.魔力小星（CuteManaStarParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle cuteManaStarParticle = new CuteManaStarParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                1f, // 是否受重力影响
                0.9f, // 生命周期，单位是帧
                24 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(cuteManaStarParticle);

            // 10.珍珠亮粒（PearlParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle pearlParticle = new PearlParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(1.5f, 1.5f), // 初始速度
                false, // 是否受重力影响
                28, // 生命周期，单位是帧
                0.9f, // 缩放大小
                Color.Pink, // 颜色
                0.95f, // 拉伸或压缩比例
                0.03f, // 开关参数
                false // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(pearlParticle);
        }
    }
}
