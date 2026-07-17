using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileD_PulseRingField : ModProjectile
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

            // 1.方向冲击环（DirectionalPulseRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle directionalPulseRing = new DirectionalPulseRing(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.1f, // 初始速度
                Color.Green, // 是否受重力影响
                new Vector2(1f, 2.5f), // 生命周期，单位是帧
                forward.ToRotation(), // 缩放大小
                0.2f, // 颜色
                0.03f, // 拉伸或压缩比例
                20 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(directionalPulseRing);

            // 2.自定义贴图脉冲（CustomPulse）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle customPulse = new CustomPulse(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Aqua, // 是否受重力影响
                "CalamityMod/Particles/HighResFoggyCircleHardEdge", // 生命周期，单位是帧
                Vector2.One, // 缩放大小
                Main.rand.NextFloat(-1f, 1f), // 颜色
                0.03f, // 拉伸或压缩比例
                0.16f, // 开关参数
                16, // 开关参数
                true, // 旋转角度或方向
                1f, // 开关参数
                true, // 开关参数
                1f, // 数值参数
                SpriteEffects.None // 数值参数
            );
            GeneralParticleHandler.SpawnParticle(customPulse);

            // 3.普通脉冲环（PulseRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle pulseRing = new PulseRing(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Cyan, // 是否受重力影响
                0.1f, // 生命周期，单位是帧
                1.0f, // 缩放大小
                24 // 颜色
            );
            GeneralParticleHandler.SpawnParticle(pulseRing);

            // 4.静态脉冲环（StaticPulseRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle staticPulseRing = new StaticPulseRing(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Gold, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                0f, // 缩放大小
                0.1f, // 颜色
                1.2f, // 拉伸或压缩比例
                26 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(staticPulseRing);

            // 5.贴附NPC光环（AuraPulseRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle auraPulseRing = new AuraPulseRing(
                Color.Violet, // 生成位置
                Vector2.One * 0.2f, // 初始速度
                Vector2.One * 1.4f, // 是否受重力影响
                30, // 生命周期，单位是帧
                target // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(auraPulseRing);

            // 6.玩家中心脉冲环（PlayerCenteredPulseRing）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle playerCenteredPulseRing = new PlayerCenteredPulseRing(
                owner, // 生成位置
                Vector2.Zero, // 初始速度
                Color.LightBlue, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                0f, // 缩放大小
                0.1f, // 颜色
                1.1f, // 拉伸或压缩比例
                24 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(playerCenteredPulseRing);

            // 7.星座光环（ConstellationRingVFX）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle constellationRingVFX = new ConstellationRingVFX(
                Projectile.Center, // 生成位置
                Color.GreenYellow * 0.8f, // 初始速度
                Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi), // 是否受重力影响
                1.2f, // 生命周期，单位是帧
                Vector2.One, // 缩放大小
                0.9f, // 颜色
                5, // 拉伸或压缩比例
                1.5f, // 开关参数
                0.06f, // 开关参数
                false // 旋转角度或方向
            );
            GeneralParticleHandler.SpawnParticle(constellationRingVFX);
        }
    }
}
