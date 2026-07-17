using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileI_BloodFleshBiology : ModProjectile
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

            // 1.血液粒子（BloodParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bloodParticle = new BloodParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                30, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                Color.Red // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(bloodParticle);

            // 2.血液粒子二型（BloodParticle2）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bloodParticle2 = new BloodParticle2(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                30, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                Color.DarkRed // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(bloodParticle2);

            // 3.鱼饵骨片（ChumBone）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle chumBone = new ChumBone(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                Color.White, // 是否受重力影响
                Main.rand.NextFloat(MathHelper.TwoPi), // 生命周期，单位是帧
                1.0f, // 缩放大小
                45, // 颜色
                false // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(chumBone);

            // 4.断裂触须（BrokenTendril）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle brokenTendril = new BrokenTendril(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                Main.rand.NextFloat(MathHelper.TwoPi), // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                45 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(brokenTendril);

            // 5.克脑残影（BrainOfCthulhuAfterImage）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            BezierCurve curve = new BezierCurve(new Vector2[] { Projectile.Center, Projectile.Center + new Vector2(20f, -30f), Projectile.Center + forward * 80f }); // 残影曲线路径
            Particle brainOfCthulhuAfterImage = new BrainOfCthulhuAfterImage(
                curve, // 生成位置
                forward.ToRotation(), // 初始速度
                Vector2.One, // 是否受重力影响
                30, // 生命周期，单位是帧
                new Rectangle(0, 0, 64, 64), // 缩放大小
                0.1f // 颜色
            );
            GeneralParticleHandler.SpawnParticle(brainOfCthulhuAfterImage);

            // 6.巨口咬合（Jaws）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle jaws = new Jaws(
                Projectile.Center, // 生成位置
                Vector2.Zero, // 初始速度
                Color.Red, // 是否受重力影响
                Vector2.One, // 生命周期，单位是帧
                forward.ToRotation(), // 缩放大小
                0.1f, // 颜色
                1.0f, // 拉伸或压缩比例
                24 // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(jaws);
        }
    }
}
