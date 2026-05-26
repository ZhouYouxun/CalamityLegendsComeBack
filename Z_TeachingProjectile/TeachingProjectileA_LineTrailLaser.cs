using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileA_LineTrailLaser : ModProjectile
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

            // 1.线性粒子（SparkParticle）---------------------------------
            // 原灾示范：CalamityPlayer、CalamityGlobalNPC、CalamityGlobalProjectile
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle sparkParticle = new SparkParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.2f, // 初始速度
                false, // 是否受重力影响
                60, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.Orange // 颜色
            );
            GeneralParticleHandler.SpawnParticle(sparkParticle);

            // 2.细长线性粒子（AltSparkParticle）---------------------------------
            // 原灾示范：ScourgeoftheCosmos、Yharon、AstralShot
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle altSparkParticle = new AltSparkParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.05f, // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                1.2f, // 缩放大小
                Color.Cyan * 0.8f // 颜色
            );
            GeneralParticleHandler.SpawnParticle(altSparkParticle);

            // 3.自定义贴图光线（CustomSpark）---------------------------------
            // 原灾示范：DraedonArsenal、Providence、ExoMechs
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle customSpark = new CustomSpark(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.15f, // 初始速度
                "CalamityMod/Particles/BloomLineSoftEdge", // 是否受重力影响
                false, // 生命周期，单位是帧
                3, // 缩放大小
                0.9f, // 颜色
                Color.Orange * 0.85f, // 拉伸或压缩比例
                new Vector2(2.8f, 0.7f), // 开关参数
                true, // 开关参数
                true, // 旋转角度或方向
                0f, // 开关参数
                false, // 开关参数
                false, // 数值参数
                0.65f, // 数值参数
                1f, // 数值参数
                1f, // 数值参数
                false, // 开关参数
                false, // 开关参数
                0f // 旋转速度
            );
            GeneralParticleHandler.SpawnParticle(customSpark);

            // 4.辉光火花线（GlowSparkParticle）---------------------------------
            // 原灾示范：Astral、ExoMechs、CalamityGlobalProjectile
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle glowSparkParticle = new GlowSparkParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.1f, // 初始速度
                false, // 是否受重力影响
                22, // 生命周期，单位是帧
                0.8f, // 缩放大小
                Color.DeepSkyBlue, // 颜色
                new Vector2(0.01f, 0.05f), // 拉伸或压缩比例
                false, // 开关参数
                true, // 开关参数
                0.8f // 旋转角度或方向
            );
            GeneralParticleHandler.SpawnParticle(glowSparkParticle);

            // 5.彩虹辉光火花（RainbowGlowSparkParticle）---------------------------------
            // 原灾示范：RainbowPartyCannon、Prism、ExoMechs
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle rainbowGlowSparkParticle = new RainbowGlowSparkParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.12f, // 初始速度
                false, // 是否受重力影响
                26, // 生命周期，单位是帧
                0.7f, // 缩放大小
                Color.Magenta, // 颜色
                new Vector2(0.05f, 0.5f), // 拉伸或压缩比例
                false, // 开关参数
                true, // 开关参数
                0.8f, // 旋转角度或方向
                0.03f // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(rainbowGlowSparkParticle);

            // 6.变速光线（VelChangingSpark）---------------------------------
            // 原灾示范：AscendantSpirit_PROJ、SHPCPassiveOrb
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle velChangingSpark = new VelChangingSpark(
                Projectile.Center, // 生成位置
                forward * 6f, // 初始速度
                forward * 0.5f, // 目标减速速度
                "CalamityMod/Particles/BloomLineSoftEdge", // 贴图路径
                28, // 生命周期
                0.7f, // 缩放
                Color.Lime, // 颜色
                new Vector2(0.05f, 0.5f), // 拉伸比例
                true,
                true,
                0f,
                false,
                0.55f,
                0.08f
            );
            GeneralParticleHandler.SpawnParticle(velChangingSpark);

            // 7.虚空火花（VoidSparkParticle）---------------------------------
            // 原灾示范：CeaselessVoid、DarkPlasma、RuinousSoul
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle voidSparkParticle = new VoidSparkParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.08f, // 初始速度
                false, // 是否受重力影响
                24, // 生命周期，单位是帧
                0.9f, // 缩放大小
                Color.Purple, // 颜色
                0.975f // 数字越大越拉的长
            );
            GeneralParticleHandler.SpawnParticle(voidSparkParticle);

            // 8.普通线粒子（LineParticle）---------------------------------
            // 原灾示范：OldDuke、BrinyBaron、Abyss
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle lineParticle = new LineParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.1f, // 初始速度
                false, // 是否受重力影响
                20, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.White // 颜色
            );
            GeneralParticleHandler.SpawnParticle(lineParticle);

            // 9.替代线粒子（AltLineParticle）---------------------------------
            // 原灾示范：Abyss、ExoMechs、Draedon
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle altLineParticle = new AltLineParticle(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.08f, // 初始速度
                false, // 是否受重力影响
                20, // 生命周期，单位是帧
                1.0f, // 缩放大小
                Color.LightBlue // 颜色
            );
            GeneralParticleHandler.SpawnParticle(altLineParticle);

            // 10.辉光线段（BloomLineVFX）---------------------------------
            // 原灾示范：SODLazer、ExoPrism_Lazer、Astral
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle bloomLineVFX = new BloomLineVFX(
                Projectile.Center, // 生成位置
                forward * 240f, // 初始速度
                1.4f, // 是否受重力影响
                Color.Lime, // 生命周期，单位是帧
                40, // 缩放大小
                false, // 颜色
                false // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(bloomLineVFX);

            // 11.静态辉光线（StaticGlowLine）---------------------------------
            // 原灾示范：ExoMechs、Ares、Thanatos
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle staticGlowLine = new StaticGlowLine(
                Projectile.Center, // 生成位置
                Projectile.Center + forward * 180f, // 初始速度
                Vector2.Zero, // 是否受重力影响
                30, // 生命周期，单位是帧
                1.2f, // 缩放大小
                0.03f, // 颜色
                Color.Cyan, // 拉伸或压缩比例
                true // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(staticGlowLine);

            //// 12.蓄力收束线（ChargeUpLineVFX）---------------------------------
            //// 原灾示范：AresCannonChargeParticleSet、Draedon、ExoMechs
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle chargeUpLineVFX = new ChargeUpLineVFX(
            //    Projectile.Center + Main.rand.NextVector2CircularEdge(80f, 80f), // 生成位置
            //    forward.ToRotation(), // 初始速度
            //    0.8f, // 是否受重力影响
            //    Color.Gold, // 生命周期，单位是帧
            //    32, // 缩放大小
            //    0.9f, // 颜色
            //    true, // 拉伸或压缩比例
            //    0.35f, // 开关参数
            //    12f // 开关参数
            //);
            //GeneralParticleHandler.SpawnParticle(chargeUpLineVFX);

            // 13.闪电线（ThunderBoltVFX）---------------------------------
            // 原灾示范：StormWeaver、Lightning、Ares
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle thunderBoltVFX = new ThunderBoltVFX(
                Projectile.Center, // 生成位置
                forward.ToRotation() + MathHelper.PiOver2, // 初始速度
                1.0f, // 是否受重力影响
                Color.Cyan, // 生命周期，单位是帧
                18, // 缩放大小
                5f, // 颜色
                0.9f, // 拉伸或压缩比例
                new Vector2(1f, 1.2f) // 开关参数
            );
            GeneralParticleHandler.SpawnParticle(thunderBoltVFX);

            // 14.跳动电火花（ElectricSpark）---------------------------------
            // 原灾示范：ExoMechs、Draedon、Tesla
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle electricSpark = new ElectricSpark(
                Projectile.Center, // 生成位置
                Projectile.velocity * 0.1f, // 初始速度
                Color.White, // 是否受重力影响
                Color.Cyan, // 生命周期，单位是帧
                0.9f, // 缩放大小
                24, // 颜色
                MathHelper.PiOver4, // 拉伸或压缩比例
                6f, // 开关参数
                1f, // 开关参数
                1.2f // 旋转角度或方向
            );
            GeneralParticleHandler.SpawnParticle(electricSpark);
        }
    }
}
