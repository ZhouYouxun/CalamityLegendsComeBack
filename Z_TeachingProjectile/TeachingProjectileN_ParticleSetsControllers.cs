using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileN_ParticleSetsControllers : ModProjectile
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

            // 1.火焰粒子组（FireParticleSet）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            var fireParticleSet = new FireParticleSet(
                90, // 生成位置
                3, // 初始速度
                Color.Orange, // 是否受重力影响
                Color.Red, // 生命周期，单位是帧
                36f, // 缩放大小
                0.9f // 颜色
            );
            fireParticleSet.Update(); // 更新粒子组，实际项目中通常要保存字段并每帧更新

            // 2.蓄力能量粒子组（ChargingEnergyParticleSet）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            var chargingEnergyParticleSet = new ChargingEnergyParticleSet(
                90, // 生成位置
                3, // 初始速度
                Color.Cyan, // 是否受重力影响
                Color.White, // 生命周期，单位是帧
                0.08f, // 缩放大小
                24f // 颜色
            );
            chargingEnergyParticleSet.Update(); // 更新粒子组，实际项目中通常要保存字段并每帧更新

            // 3.阿瑞斯炮蓄力粒子组（AresCannonChargeParticleSet）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            var aresCannonChargeParticleSet = new AresCannonChargeParticleSet(
                90, // 生成位置
                3, // 初始速度
                48f, // 是否受重力影响
                Color.Red // 生命周期，单位是帧
            );
            aresCannonChargeParticleSet.Update(); // 更新粒子组，实际项目中通常要保存字段并每帧更新

            // 4.塔纳托斯烟雾粒子组（ThanatosSmokeParticleSet）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            var thanatosSmokeParticleSet = new ThanatosSmokeParticleSet(
                90, // 生成位置
                4, // 初始速度
                forward.ToRotation(), // 是否受重力影响
                36f, // 生命周期，单位是帧
                0.8f // 缩放大小
            );
            thanatosSmokeParticleSet.Update(); // 更新粒子组，实际项目中通常要保存字段并每帧更新
        }
    }
}
