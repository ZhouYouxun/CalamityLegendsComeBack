using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileJ_DebrisMechanicalParts : ModProjectile
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

            // 1.石块碎片（StoneDebrisParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle stoneDebrisParticle = new StoneDebrisParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                Color.Gray, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                50, // 缩放大小
                0.05f // 颜色
            );
            GeneralParticleHandler.SpawnParticle(stoneDebrisParticle);

            // 2.钛金轨道炮弹壳（TitaniumRailgunShell）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle titaniumRailgunShell = new TitaniumRailgunShell(
                Projectile.Center, // 生成位置
                Projectile.Center.ToTileCoordinates(), // 初始速度
                forward.ToRotation(), // 是否受重力影响
                Color.Cyan, // 生命周期，单位是帧
                80 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(titaniumRailgunShell);

            // 3.钨钢堡垒部件（WulfrumBastionPartsParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle wulfrumBastionPartsParticle = new WulfrumBastionPartsParticle(
                owner, // 生成位置
                0, // 初始速度
                60 // 是否受重力影响
            );
            GeneralParticleHandler.SpawnParticle(wulfrumBastionPartsParticle);

            // 4.阿瑞斯召唤箱（AresSummonCrateParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle aresSummonCrateParticle = new AresSummonCrateParticle(
                owner, // 生成位置
                new Vector2(0f, -2f), // 初始速度
                60 // 是否受重力影响
            );
            GeneralParticleHandler.SpawnParticle(aresSummonCrateParticle);

            // 5.海胆尖刺（UrchinSpikeParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle urchinSpikeParticle = new UrchinSpikeParticle(
                Projectile.Center, // 生成位置
                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                forward.ToRotation(), // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                0.9f, // 缩放大小
                30 // 颜色
            );
            GeneralParticleHandler.SpawnParticle(urchinSpikeParticle);
        }
    }
}
