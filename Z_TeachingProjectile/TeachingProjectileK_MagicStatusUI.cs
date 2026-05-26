using CalamityMod.DataStructures;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Z_TeachingProjectile
{
    public class TeachingProjectileK_MagicStatusUI : ModProjectile
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

            // 1.治疗十字（HealingPlus）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle healingPlus = new HealingPlus(
                Projectile.Center, // 生成位置
                1.0f, // 初始速度
                new Vector2(0f, -1f), // 是否受重力影响
                Color.Lime, // 生命周期，单位是帧
                Color.Transparent, // 缩放大小
                40 // 颜色
            );
            GeneralParticleHandler.SpawnParticle(healingPlus);

            // 2.属性变化箭头（StatChangeArrow）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle statChangeArrow = new StatChangeArrow(
                Projectile.Center, // 生成位置
                new Vector2(0f, -1f), // 初始速度
                -MathHelper.PiOver2, // 是否受重力影响
                Color.Lime, // 生命周期，单位是帧
                Color.Transparent, // 缩放大小
                1.0f, // 颜色
                40 // 拉伸或压缩比例
            );
            GeneralParticleHandler.SpawnParticle(statChangeArrow);

            // 3.表情粒子（EmoteExpressionParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle emoteExpressionParticle = new EmoteExpressionParticle(
                Projectile.Center, // 生成位置
                new Vector2(0f, -0.5f), // 初始速度
                1.0f, // 是否受重力影响
                Color.White, // 生命周期，单位是帧
                40, // 缩放大小
                EmoteExpressionParticle.EmoteType.Exclamation // 颜色
            );
            GeneralParticleHandler.SpawnParticle(emoteExpressionParticle);

            // 4.钨钢无人机表情（WulfrumDroidEmote）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle wulfrumDroidEmote = new WulfrumDroidEmote(
                Projectile.Center, // 生成位置
                new Vector2(0f, -0.6f), // 初始速度
                45, // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                0 // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(wulfrumDroidEmote);

            // 5.钨钢汗滴表情（WulfrumDroidSweatEmote）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle wulfrumDroidSweatEmote = new WulfrumDroidSweatEmote(
                Projectile.Center, // 生成位置
                new Vector2(0f, -0.6f), // 初始速度
                45, // 是否受重力影响
                1.0f // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(wulfrumDroidSweatEmote);

            // 6.钨钢帽子粒子（WulfrumHatParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle wulfrumHatParticle = new WulfrumHatParticle(
                owner, // 生成位置
                new Vector2(0f, -2f), // 初始速度
                60 // 是否受重力影响
            );
            GeneralParticleHandler.SpawnParticle(wulfrumHatParticle);

            // 7.魔力吸取团（ManaDrainBlob）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle manaDrainBlob = new ManaDrainBlob(
                owner, // 生成位置
                Projectile.Center, // 初始速度
                new Vector2(0f, -1f), // 是否受重力影响
                1.0f, // 生命周期，单位是帧
                Color.Blue // 缩放大小
            );
            GeneralParticleHandler.SpawnParticle(manaDrainBlob);

            //// 8.魔力吸取线（ManaDrainStreak）---------------------------------
            //// 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            //// 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            //Particle manaDrainStreak = new ManaDrainStreak(
            //    owner, // 生成位置
            //    1.0f, // 初始速度
            //    Projectile.Center - owner.Center, // 是否受重力影响
            //    120f, // 生命周期，单位是帧
            //    Color.Blue, // 缩放大小
            //    Color.Cyan, // 颜色
            //    35, // 拉伸或压缩比例
            //    Projectile.Center, // 开关参数
            //    true // 开关参数
            //);
            //GeneralParticleHandler.SpawnParticle(manaDrainStreak);

            // 9.终末百合心形（LiliesOfFinalityHeartParticle）---------------------------------
            // 原灾示范：反编译 CalamityMod.Particles 后按构造函数整理，具体调用点可继续用类名搜索
            // 适用于该类别下的模块化特效；适合按颜色、速度、缩放和生命周期改成自己的武器表现
            Particle liliesOfFinalityHeartParticle = new LiliesOfFinalityHeartParticle(
                Projectile.Center, // 生成位置
                new Vector2(0f, -1f), // 初始速度
                35, // 是否受重力影响
                1.0f // 生命周期，单位是帧
            );
            GeneralParticleHandler.SpawnParticle(liliesOfFinalityHeartParticle);
        }
    }
}
