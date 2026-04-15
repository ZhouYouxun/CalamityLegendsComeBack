using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class CalamityLegendsComeBack : Mod
	{

	}

    // 03-29-2026：重新编写教学文档
    public class TeachingProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
        }
        public override void AI()
        {
            // 原版Dust效果------------------------------------------------------------------------------------------------------------------------------------
            {
                // 原版 Dust 效果 ----------------------------------------------------------------------------------------------------------------------

                // 方式 1️⃣：使用 Dust.NewDust（带扰动、适合爆炸）
                for (int i = 0; i < 6; i++)
                {
                    int dustIndex = Dust.NewDust(
                        Projectile.Center - new Vector2(4f), // 起始位置（中心偏移）
                        8, 8, // 宽高范围
                        DustID.Torch, // 粒子类型（可以换成你喜欢的）
                        Main.rand.NextFloat(-2f, 2f), // velocityX
                        Main.rand.NextFloat(-2f, 2f), // velocityY
                        0, // alpha
                        Color.Orange, // 粒子颜色（部分类型支持）
                        Main.rand.NextFloat(1f, 1.5f) // scale
                    );
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }

                // 方式 2️⃣：使用 Dust.NewDustPerfect（精准无扰动、适合轨迹/装饰）
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(6f, 6f);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + offset,
                        DustID.GoldFlame, // 另一种粒子类型
                        offset * 0.1f,
                        0,
                        Color.Gold,
                        Main.rand.NextFloat(1f, 1.4f)
                    );
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }

            }



            // 原版Gore效果------------------------------------------------------------------------------------------------------------------------------------
            {
                // 原版 Gore 效果 ----------------------------------------------------------------------------------------------------------------------

                // 示例用途：当弹幕或单位死亡时，喷出碎块血肉残骸

                // 方法：使用 Gore.NewGore 创建一个碎块（支持原版和 ModGore）

                // 原版 Gore 示例（类型：GoreID.Smoke1 ~ Smoke3 是内置灰尘碎块）
                for (int i = 0; i < 3; i++)
                {
                    Vector2 goreVelocity = Projectile.velocity + Main.rand.NextVector2Circular(3f, 3f);
                    int goreType = GoreID.Smoke1 + i; // 示例：烟雾 1~3 号

                    Gore.NewGore(
                        Projectile.GetSource_Death(), // 来源（必须传入）
                        Projectile.Center,            // 生成位置
                        goreVelocity,                 // 初始速度
                        goreType,                     // Gore 类型（原版：GoreID.*，或 ModGore）
                        Projectile.scale              // 缩放
                    );
                }


                // 额外演示：搭配血雾 Dust 效果一起
                for (int i = 0; i < 10; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(5f, 5f));
                    d.noGravity = true;
                    d.scale *= Main.rand.NextFloat(0.8f, 1.3f);
                }
            }


            // Mod粒子效果------------------------------------------------------------------------------------------------------------------------------------
            {
                // 按照个人认为的适用范围，从高到低排布（越是排在越前面的越是我觉得它用处更广泛的）


                // 1.线性粒子---------------------------------
                // 原灾示范：太多了,包括宇宙之火和巨龙之火的debuff效果，日蚀之陨，哈雷彗星炮主体等等
                // 适用于尾迹、火花、能量拖尾，几乎所有都可以用到，它非常广泛
                Particle trail = new SparkParticle(
                    Projectile.Center, // 设定粒子的初始位置，与弹幕中心重合
                    Projectile.velocity * 0.2f, // 设定粒子的运动方向与速度（较慢）
                    false, // ❌ `AffectedByGravity` = false，不受重力影响
                    60, // 粒子的生命周期（帧数，60 帧）
                    1.0f, // 设定粒子的缩放大小
                    Color.Orange // 设定粒子的颜色（橙色）
                );
                // 生成粒子
                GeneralParticleHandler.SpawnParticle(trail);


                // 2.细长线性粒子---------------------------------
                // 原灾示范：NE猎枪
                // 适用于更长的能量流动、更长的光束轨迹、更长的尾迹等效果【适用场景中等偏少】
                AltSparkParticle spark5 = new AltSparkParticle(
                    Projectile.Center - Projectile.velocity * 1.5f, // 生成位置，略微延迟以形成轨迹
                    Projectile.velocity * 0.01f, // 速度极低，几乎静止
                    false, // ❌ 不受重力影响
                    8, // 存活时间（帧数）
                    1.3f, // 设定粒子的缩放大小
                    Color.Cyan * 0.135f // 颜色较淡的效果
                );
                // 生成细长线性粒子
                GeneralParticleHandler.SpawnParticle(spark5);




                // 3.轻型烟雾💨💨💨---------------------------------
                // 原灾示范：余烬箭命中特效
                // 适用于爆炸、蒸汽、灰尘等较轻的烟雾效果
                Particle smokeL = new HeavySmokeParticle(
                    Projectile.Center, // 粒子生成位置，与弹幕中心重合
                    Projectile.velocity * 0.5f, // 烟雾的移动速度，略微跟随弹幕
                    Color.WhiteSmoke, // 烟雾颜色
                    18, // 烟雾的生命周期（帧数，18 帧后消失）
                    Main.rand.NextFloat(0.9f, 1.6f), // 轻型烟雾的缩放大小，随机值
                    0.35f, // 透明度（opacity），较低的值意味着更轻的烟雾
                    Main.rand.NextFloat(-1, 1), // 旋转速度
                    false // ❌ `required = false`，即轻型烟雾
                );
                // 生成烟雾粒子
                GeneralParticleHandler.SpawnParticle(smokeL);

                // 4.重型烟雾🌫️🌫️🌫️---------------------------------
                // 原灾示范：影流喷射器，奥密克戎线枪口火焰，深渊之刃线分裂弹
                // 适用于爆炸核心、强烈冲击、深海水流等浓厚的烟雾
                Particle smokeH = new HeavySmokeParticle(
                    Projectile.Center + new Vector2(0, -10), // 粒子生成位置，略微偏移弹幕中心
                    new Vector2(0, -1) * 5f, // 让烟雾向上飘散
                    Color.Gray, // 烟雾颜色
                    30, // 生命周期（帧数，30 帧后消失）
                    Projectile.scale * Main.rand.NextFloat(0.7f, 1.3f), // 缩放大小，较大范围
                    1.0f, // 透明度（opacity），较高值意味着更浓的烟雾
                    MathHelper.ToRadians(2f), // 旋转速度（更显著）
                    true // ✅ `required = true`，即重型烟雾
                );
                // 生成烟雾粒子
                GeneralParticleHandler.SpawnParticle(smokeH);

                //Opacity（透明度）
                //0.35f → 轻型
                //1.0f → 重型
                //Scale（缩放大小）
                //0.9f - 1.6f → 轻型
                //0.7f - 1.3f → 重型
                //Lifetime（生命周期）
                //15 - 18 → 轻型
                //30 + → 重型



                // 5. 点刺型粒子特效💉💉💉---------------------------------
                // 原灾示范：北辰鹦哥鱼【新版】命中特效
                PointParticle leftSpark = new PointParticle(
                    Projectile.Center, // 设定粒子的初始位置，与弹幕中心重合
                    -Projectile.velocity * 0.5f, // 让粒子朝弹幕运动方向的反方向移动
                    false, // 是否受重力影响 (false = 不受重力影响)
                    15, // 粒子的生命周期（帧数，15 帧）
                    1.1f, // 粒子的缩放大小，数值越大粒子越大
                    Color.Orange // 设定粒子的颜色（橙色）
                );
                // 生成该粒子，使其出现在游戏世界中
                GeneralParticleHandler.SpawnParticle(leftSpark);


                // 5+. 水雾粒子特效💧💧💧---------------------------------
                // 原灾示范：海神之馈赠的小弹幕、极寒的那俩弹药【是新英雄哦】
                // 适用于模拟湿润蒸汽、能量雾化、柔和散逸的效果
                WaterFlavoredParticle mist = new WaterFlavoredParticle(
                    Projectile.Center,                // 初始位置
                    -Projectile.velocity * 0.3f,      // 反向喷出，制造散逸感
                    false,                            // ❌ 不受重力影响
                    Main.rand.Next(18, 26),           // 生命周期（18~26 帧）
                    0.9f + Main.rand.NextFloat(0.3f), // 缩放大小
                    Color.LightBlue * 0.9f            // 柔和蓝色水雾
                );
                // 生成该粒子，使其出现在游戏世界中
                GeneralParticleHandler.SpawnParticle(mist);


                // 6.椭圆形冲击波🔄🔄🔄---------------------------------
                // 原灾示范：影流喷射器，奥密克戎线枪口火焰，深渊之刃线分裂弹
                // 适用于能量爆炸、波动冲击、领域扩散等效果
                Particle pulse = new DirectionalPulseRing(
                    Projectile.Center, // 设定粒子的初始位置，与弹幕中心重合
                    Projectile.velocity * 0.75f, // 设定冲击波的传播方向与速度
                    Color.Green, // 设定粒子的颜色（绿色）
                    new Vector2(1f, 2.5f), // 🔹 `Squish` 参数决定椭圆的长短轴比例 (X 轴 = 1, Y 轴 = 2.5)
                    Projectile.rotation - MathHelper.PiOver4, // 🔄 椭圆的旋转角度（影响椭圆的朝向）
                    0.2f, // 初始缩放大小（`OriginalScale`）
                    0.03f, // 最终缩放大小（`FinalScale`）
                    20 // 粒子存活时间（20 帧）
                );
                // 生成冲击波粒子
                GeneralParticleHandler.SpawnParticle(pulse);


                // 7.十字星---------------------------------
                // 原灾示范：整个方舟和环境之刃系列，p90和金源弹命中特效
                // 适用于华丽的能量闪光、星爆效果、命中反馈等
                GenericSparkle sparker = new GenericSparkle(
                    Projectile.Center, // 设定粒子的初始位置，与弹幕中心重合
                    Vector2.Zero, // 粒子不移动，停留在生成位置
                    Color.Gold, // 设定十字星的主要颜色（金色）
                    Color.Cyan, // 设定十字星的光晕颜色（青色）
                    Main.rand.NextFloat(1.8f, 2.5f), // 设定粒子的缩放大小（随机值）
                    5, // 粒子的生命周期（帧数，5 帧）
                    Main.rand.NextFloat(-0.01f, 0.01f), // 设定旋转速度（正负方向随机）
                    1.68f // 设定光晕的扩散大小
                );
                // 生成十字星粒子
                GeneralParticleHandler.SpawnParticle(sparker);

                // 8.小型十字星【更小、更圆润】---------------------------------
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX); // 使用正前方方向
                Vector2 sparkVelocity = direction.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)) * 6f; // 随机旋转方向
                CritSpark spark = new CritSpark(
                    Projectile.Center, // 粒子生成位置
                    sparkVelocity + Main.player[Projectile.owner].velocity, // 粒子初始速度
                    Color.White, // 粒子起始颜色
                    Color.LightBlue, // 粒子结束颜色
                    1f, // 粒子缩放
                    16 // 粒子寿命
                );
                GeneralParticleHandler.SpawnParticle(spark);

                // 9.EXO之光
                // Squish 拉伸 + 柔光贴图的粒子
                // 非常亮，非要找现实对照物的话，差不多就是镁粉燃烧时的高亮
                // 原灾示范：星流系列武器的纯白量子光束
                SquishyLightParticle exoEnergy = new(
                    Projectile.Center,                                              // 粒子位置
                    -Vector2.UnitY.RotatedByRandom(0.39f) * Main.rand.NextFloat(0.4f, 1.6f), // 初速度
                    0.28f,                                                          // 缩放大小
                    Color.Orange,                                                   // 粒子颜色
                    25,                                                             // 生命周期（帧数）
                    opacity: 1f,                                                    // 不透明度
                    squishStrenght: 1f,                                             // 拉伸强度
                    maxSquish: 3f,                                                  // 最大拉伸倍数
                    hueShift: 0f                                                    // 色相偏移
                );
                GeneralParticleHandler.SpawnParticle(exoEnergy);

                // 10.辉光球
                // 小型闪亮的球非常快速，很清爽
                // 原灾示范：海伯利斯子弹的三色折线
                GlowOrbParticle orb = new GlowOrbParticle(
                    Projectile.Center,         // position：粒子生成位置
                    Vector2.Zero,              // velocity：初速度（通常为零）
                    false,                     // affectedByGravity：是否受重力（true = 掉落）
                    5,                         // lifetime：生命周期（单位：帧）
                    0.9f,                      // scale：缩放大小
                    Color.Red,                 // color：基础颜色
                    true,                      // additiveBlend：是否加法混合（true = 更亮更绚）
                    false,                     // needed：是否重要粒子（一般设 false）
                    true                       // glowCenter：是否在中心叠加额外白色发光（true = 亮度增强）
                );
                GeneralParticleHandler.SpawnParticle(orb);


                // 11.四方粒子🔳---------------------------------
                // 原灾示范：肾上腺素期间，
                // 适用于科技风格武器、能量构造、赛博朋克、数字化特效等
                SquareParticle squareParticle = new SquareParticle(
                    Projectile.Center, // 粒子生成位置
                    Projectile.velocity * 0.5f, // 设定粒子的移动速度
                    false, // ❌ 不受重力影响
                    30, // 粒子的存活时间（帧数）
                    1.7f + Main.rand.NextFloat(0.6f), // 设定粒子的大小，增加随机性
                    Color.Cyan * 1.5f // 粒子的颜色（可调整）
                );
                // 生成四方形粒子
                GeneralParticleHandler.SpawnParticle(squareParticle);



                // 12.裂纹、闪电粒子
                // 用于模拟破裂、冲击爆发时的裂缝特效
                // 当然他更广为人知的用途就是模拟闪电！
                // 注意一下：这个东西的体积较大，调用的时候酌情减少体积
                // 原灾示范：虚空漩涡的闪电球
                CrackParticle crack = new CrackParticle(
                    Projectile.Center,                       // Vector2 position，生成位置
                    new Vector2(0f, -4f),                    // Vector2 velocity，生成时向上轻微漂移
                    Color.DarkOliveGreen,                    // Color color，裂纹颜色
                    new Vector2(1f, 1f),                     // Vector2 squish，缩放比例（1,1为正常）
                    Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi), // float rotation，初始旋转
                    0.8f,                                    // float originalScale，初始缩放
                    2.0f,                                    // float finalScale，最终放大缩放
                    35                                       // int lifeTime，持续时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(crack);

                // 13.骷髅头💀💀💀---------------------------------
                // 原灾示范：重做后的憎恨左键，沙漠巡游者套装
                // 适用于幽灵效果、亡灵能量、恐怖气息等【适用场景非常少】
                Particle smoke = new DesertProwlerSkullParticle(
                    Projectile.Center, // 粒子生成位置，与弹幕中心重合
                    Projectile.velocity * 0.5f, // 让骷髅头缓慢飘动
                    Color.DarkGray * 0.8f, // 初始颜色，较暗的灰色
                    Color.LightGray, // 逐渐变淡至浅灰色
                    Main.rand.NextFloat(0.5f, 1.0f), // 设定骷髅头的缩放大小（随机范围）
                    150 // 骷髅头的初始透明度
                );
                // 生成骷髅头粒子
                GeneralParticleHandler.SpawnParticle(smoke);





                // 这边需要补充的：

                // 阴险穿刺者的弹射特效有一个



                // 元素方舟的多种特效，应该在这补充一下






                // 14.辉光拖尾火花---------------------------------
                // GlowSparkParticle 比普通火花更亮、更细、更适合表现高能尾迹
                // 常用于喷口火花、热束切边、光束拖尾、神圣/魔焰高亮边缘
                GlowSparkParticle glowSpark = new GlowSparkParticle(
                    Projectile.Center,
                    Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 5f),
                    false,
                    14,
                    0.22f,
                    Color.Gold,
                    new Vector2(1.8f, 0.45f),
                    true,
                    false
                );
                GeneralParticleHandler.SpawnParticle(glowSpark);


                // 15.自定义光线火花---------------------------------
                // CustomSpark 是“吃贴图的线状粒子”，适合喷枪、能量划痕、刀气切线、激光边缘
                // 对新手来说最重要的就是 3 个参数：贴图路径、scale、stretch
                Particle customLine = new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * 0.15f,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    20,
                    0.9f,
                    Color.Orange * 0.85f,
                    new Vector2(2.8f, 0.7f),
                    shrinkSpeed: 0.65f
                );
                GeneralParticleHandler.SpawnParticle(customLine);


                // 16.柔雾粒子---------------------------------
                // MediumMistParticle 比 HeavySmokeParticle 更轻、更柔和，适合寒气、薄雾、液氮云层、魔焰尾烟
                // 想要“大块柔雾”就优先用它；想要“厚重烟团”再用 HeavySmokeParticle
                Particle mediumMist = new MediumMistParticle(
                    Projectile.Center,
                    new Vector2(
                        Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(-4f, -1.5f)
                    ),
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.7f, 1.1f),
                    Main.rand.NextFloat(180f, 240f)
                );
                GeneralParticleHandler.SpawnParticle(mediumMist);


                // 17.小型烟团---------------------------------
                // SmallSmokeParticle 是一个短命、小体积的烟雾粒子
                // 适合灰烬、喷焰尾气、爆点边缘、燃烧残影
                SmallSmokeParticle smallSmoke = new SmallSmokeParticle(
                    Projectile.Center,
                    Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(1.1f, 1.1f),
                    Color.DarkGray,
                    Color.Black,
                    Main.rand.NextFloat(0.45f, 0.75f),
                    Main.rand.Next(30, 50)
                );
                GeneralParticleHandler.SpawnParticle(smallSmoke);


                // 18.闪电裂片---------------------------------
                // BoltParticle 比 CrackParticle 更像“飞出去的一小段电弧”
                // 适合电击、击穿、装甲裂闪、短促放电
                BoltParticle bolt = new BoltParticle(
                    Projectile.Center,
                    Projectile.velocity.SafeNormalize(Vector2.UnitX) * 3f,
                    false,
                    12,
                    0.55f,
                    Color.Yellow,
                    new Vector2(0.45f, 0.85f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);






            }





            // 11.刀盘刀光[仅适用于旋转类的武器]---------------------------------------------------------------------------------------------------------------------------------------------------------------------
            {
                // 原灾示范：宇宙暗流前奏，海神之馈赠线，环境之刃转盘模式
                // 仅适用于快速旋转的近战武器、回旋镖残影等，不能应用于不可旋转的武器，建议在释放的时候搭配对应的音效
                // 🟠 **4/5圆的刀光**
                CircularSmearVFX fullSmear = new CircularSmearVFX(
                    Projectile.Center, // 生成位置
                    Color.OrangeRed * Main.rand.NextFloat(0.78f, 0.85f), // 颜色
                    Main.rand.NextFloat(-8, 8), // 旋转角度
                    Main.rand.NextFloat(1.2f, 1.3f) * 2.1f // 2.1 倍缩放
                );
                GeneralParticleHandler.SpawnParticle(fullSmear);
                // 🟡 **1/2圆的刀光**
                SemiCircularSmearVFX semiSmear = new SemiCircularSmearVFX(
                    Projectile.Center, // 生成位置
                    Color.Yellow * Main.rand.NextFloat(0.78f, 0.85f), // 颜色
                    Main.rand.NextFloat(-8, 8), // 旋转角度
                    Main.rand.NextFloat(1.2f, 1.3f) * 2.1f, // 2.1 倍缩放
                    new Vector2(1f, 0.8f) // 半圆的扁平变形比例
                );
                GeneralParticleHandler.SpawnParticle(semiSmear);
                // 🟢 **1/3圆的刀光**
                TrientCircularSmear trientSmear = new TrientCircularSmear(
                    Projectile.Center, // 生成位置
                    Color.Lime * Main.rand.NextFloat(0.78f, 0.85f), // 颜色
                    Main.rand.NextFloat(-8, 8), // 旋转角度
                    Main.rand.NextFloat(1.2f, 1.3f) * 2.1f // 2.1 倍缩放
                );
                GeneralParticleHandler.SpawnParticle(trientSmear);

                // 烟雾刀光[1/2圆]
                // 与刀光类似，但为柔和烟雾半透明刀光，适合暗影或灵息效果
                CircularSmearSmokeyVFX smokeySmear = new CircularSmearSmokeyVFX(
                    Projectile.Center,                                     // Vector2 position，生成位置
                    Color.ForestGreen * Main.rand.NextFloat(0.5f, 0.8f),   // Color color，柔和绿色烟雾刀光
                    Main.rand.NextFloat(-8, 8),                            // float rotation，旋转角度
                    Main.rand.NextFloat(1.0f, 1.5f) * 2.0f                 // float scale，缩放大小
                );
                GeneralParticleHandler.SpawnParticle(smokeySmear);
            }




            // 冲击波/震荡波/扩散类型---------------------------------------------------------------------------------------------------------------------------------------------------------------------
            {

                // 8.圆形冲击波🌊🌊🌊---------------------------------
                // 原灾示范：钛晶磁轨炮，蜂巢发射器，天蝎座，哈雷彗星
                // 往内收缩
                Particle shrinkingpulse = new DirectionalPulseRing(
                    Projectile.Center, // 粒子生成位置，与弹幕中心重合
                    Vector2.Zero, // 粒子静止不动
                    Color.Purple, // 冲击波的颜色（紫色）
                    new Vector2(1f, 1f), // 冲击波的初始形状（圆形）
                    Main.rand.NextFloat(6f, 10f), // 初始缩放大小
                    0.15f, // 最终缩放大小（收缩到非常小）
                    3f, // 设定扩散范围
                    10 // 粒子的存活时间（10 帧）
                );
                // 生成收缩冲击波粒子
                GeneralParticleHandler.SpawnParticle(shrinkingpulse);

                // 往外扩张
                Particle expandingPulse = new DirectionalPulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    Color.Cyan,
                    new Vector2(1.2f, 1.2f),
                    0f, // 设定旋转角度
                    0.5f, // 🔹 初始尺寸较小
                    6.0f, // 🔹 让冲击波变大
                    20 // 存活时间更长
                );
                // 生成扩张冲击波粒子
                GeneralParticleHandler.SpawnParticle(expandingPulse);


                // 9.自定义冲击波【需要自备纹理】---------------------------------
                // 原灾示范：诺法雷爆炸
                // 适用于特定能量场、放射性爆炸、魔法冲击等特效
                Particle bolt = new CustomPulse(
                    Projectile.Center, // 粒子生成位置，与弹幕中心重合
                    Vector2.Zero, // 粒子静止不动
                    Color.Aqua, // 设定冲击波颜色（青色）
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge", // 设定粒子使用的贴图【可以随意修改】
                    Vector2.One * (Projectile.ai[0] == 1 ? 1.5f : 1f), // 冲击波的椭圆变形比例
                    Main.rand.NextFloat(-10f, 10f), // 设定旋转角度
                    0.03f, // 初始缩放大小
                    0.16f, // 最终缩放大小（逐渐变大）【也可以是逐渐变小】
                    16 // 粒子的存活时间（16 帧）
                );
                // 生成自定义冲击波粒子
                GeneralParticleHandler.SpawnParticle(bolt);

                // 10.细节爆炸💥---------------------------------
                // 原灾示范：圣火弹爆炸
                // 适用于高质量爆炸、能量释放、冲击波特效
                Particle explosion = new DetailedExplosion(
                    Projectile.Center, // 爆炸中心位置
                    Vector2.Zero, // 爆炸粒子不会移动
                    Color.OrangeRed * 0.9f, // 设定爆炸的颜色
                    Vector2.One, // 爆炸的形状比例（1,1 代表正圆）
                    Main.rand.NextFloat(-5, 5), // 设定随机旋转角度
                    0f, // 初始缩放大小
                    0.28f, // 最终缩放大小，逐渐变大
                    10 // 爆炸持续时间（帧数）
                );
                // 生成细节爆炸粒子
                GeneralParticleHandler.SpawnParticle(explosion);


                // 星座环光圈
                // 在圆环光圈【血炎系列同款】周围漂浮旋转多颗星点，领域视觉特效
                // 原灾示范：“银河”的北极星攻击
                // 适用于跟星空魔法能量相关的爆炸类
                ConstellationRingVFX constellationRing = new ConstellationRingVFX(
                    Projectile.Center,                       // Vector2 position，生成位置
                    Color.GreenYellow * 0.8f,                // Color color，光圈颜色（可调亮度）
                    Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi), // float rotation，初始旋转角度
                    1.2f,                                    // float scale，缩放大小
                    new Vector2(1f, 1f),                     // Vector2 squish，圆环扁平比例
                    0.9f,                                    // float opacity，透明度（0~1）
                    5,                                       // int starAmount，星点数量
                    1.5f,                                    // float starScale，星点缩放
                    0.06f,                                   // float spinSpeed，旋转速度
                    false                                    // bool important，是否优先渲染
                );
                GeneralParticleHandler.SpawnParticle(constellationRing);
            }




            // Metaball---------------------------------------------------------------------------------------------------------------------------------------------------------------------
            {
                // 12.熔岩 Metaball 🌋---------------------------------
                // 原灾示范：怨戾激光喷射后留下的橙色熔岩滞留光效
                // 适用于持续领域伤害、能量残留、地面烙印等
                RancorLavaMetaball.SpawnParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(32f, 32f), // 粒子生成位置（弹幕中心 + 偏移）
                    Main.rand.NextFloat(60f, 100f) // 粒子大小（半径）
                );

                // 13.宇宙 Metaball 🌌---------------------------------
                // 原灾示范：宇宙暗流三叉戟命中后的紫色烟雾
                // 适用于宇宙能量残留、黑洞边缘、深渊气场
                // 14.灾厄魔焰 Metaball---------------------------------
                // 这是 AshesofCalamity_Soul 里那种“像活着一样往前推的红色火团”
                // 推荐写法是每帧少量生成，让它自己融合成一团持续前冲的能量体
                // 新手记忆法：
                // 1. 生成位置稍微放在弹幕前方
                // 2. 初速度只给很小的扰动
                // 3. 再叠一点 GlowOrbParticle，观感会非常漂亮
                CalamitasMetaball.SpawnParticle(
                    Projectile.Center + Projectile.velocity,
                    Main.rand.NextVector2Circular(2f, 2f),
                    64f
                );

                StreamGougeMetaball.SpawnParticle(
                    Projectile.Center, // 粒子生成位置（弹幕中心）
                    Vector2.Zero, // 粒子速度（静止）
                    30f // 粒子大小（半径）
                );

                // 还有别的几个Metaball我就不单独举例了，反正原理都一样
            }




            // Bloom系列 [非常有亮度]---------------------------------------------------------------------------------------------------------------------------------------------------------------------
            {
                // 【StrongBloom】超级光晕
                // 【非常非常强力的光圈，内部极亮【是真的非常非常非常亮，使用时务必控制数量】，带外侧光晕】
                // 【原灾示范：奈落瓮的水母靠近敌人释放的蓝色光圈】
                StrongBloom strongBloom = new StrongBloom(
                    Projectile.Center,          // Vector2 position，粒子生成位置
                    Projectile.velocity * 0.1f, // Vector2 velocity，粒子初速度，轻微扩散或静止
                    Color.LimeGreen,            // Color color，光晕颜色
                    2.0f,                       // float scale，光晕整体缩放
                    50                          // int lifetime，粒子存活时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(strongBloom);


                // 【GenericBloom】普通光晕
                // 【与超级光晕相比，去掉内部极亮部分，仅保留外侧柔和光晕】
                // 【原灾示范：巨龙之息命中敌人时光球、天蝎座大型火箭死亡时】
                GenericBloom genericBloom = new GenericBloom(
                    Projectile.Center,          // Vector2 position，粒子生成位置
                    Vector2.Zero,               // Vector2 velocity，粒子速度（通常设为零）
                    Color.GreenYellow,          // Color color，光晕颜色
                    1.5f,                       // float scale，光晕缩放
                    40                          // int lifetime，粒子存活时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(genericBloom);


                // 【BloomParticle】绽放粒子
                // 【与超级光晕类似，但光晕较窄，用于柔和脉冲扩散效果】
                // 【原灾示范：至尊灾厄战中各种弹幕光晕】
                BloomParticle bloomParticle = new BloomParticle(
                    Projectile.Center,          // Vector2 position，粒子生成位置
                    Vector2.Zero,               // Vector2 velocity，粒子初速度（可轻微漂移）
                    Color.LightGreen,           // Color color，光晕颜色
                    0.8f,                       // float originalScale，初始缩放
                    2.5f,                       // float finalScale，最终缩放
                    60                          // int lifetime，粒子存活时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(bloomParticle);


                // 【BloomRing】光晕环
                // 【类似超级光晕，但中间掏空，仅保留环状光晕】
                // 【原灾示范：暗黑♂领主右键召唤分身时的光圈】
                BloomRing bloomRing = new BloomRing(
                    Projectile.Center,          // Vector2 position，粒子生成位置
                    Vector2.Zero,               // Vector2 velocity，粒子速度（通常静止或轻微漂移）
                    Color.ForestGreen,          // Color color，光晕颜色
                    1.8f,                       // float scale，光晕环大小
                    45                          // int lifetime，粒子存活时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(bloomRing);


                // 【BloomLineVFX】光晕线
                // 【光晕系列的线段版本，可用于激光、预警线等视觉效果】
                // 【原灾示范：“银河”右键切换星系时出现的星系图能量线】
                BloomLineVFX bloomLine = new BloomLineVFX(
                    Projectile.Center,                          // Vector2 startPoint，起点位置
                    Projectile.velocity.SafeNormalize(Vector2.UnitY) * 240f, // Vector2 lineVector，线段方向与长度
                    1.4f,                                       // float thickness，线条粗细
                    Color.Lime,                                 // Color color，颜色
                    40                                          // int lifetime，粒子存活时间（帧数）
                );
                GeneralParticleHandler.SpawnParticle(bloomLine);

            }













            // X.粒子轨迹修改------------------------------------------------------------------------------------------------------------------------------------
            // 必须先创建引用，也就是在下面类字段区域
            // === 1️⃣ 正常调用 SparkParticle （不需要动粒子类本体） ===
            Particle trailChange = new SparkParticle(
                Projectile.Center, // 初始位置
                Projectile.velocity * 0.2f, // 初速度（较慢）
                false, // 不受重力影响
                60, // 生命周期（60 帧）
                1.0f, // 缩放
                Color.Orange // 颜色
            );
            GeneralParticleHandler.SpawnParticle(trailChange);

            // 记录引用到我们自己的列表中，便于后续修改它的飞行轨迹
            ownedSparkParticles.Add((SparkParticle)trailChange);

            // === 2️⃣ 在弹幕的 AI 内，手动修改自己记录的 SparkParticle 的轨迹 ===
            for (int i = ownedSparkParticles.Count - 1; i >= 0; i--)
            {
                SparkParticle p = ownedSparkParticles[i];

                // 检查是否超时销毁，避免一直占用内存
                if (p.Time >= p.Lifetime)
                {
                    ownedSparkParticles.RemoveAt(i);
                    continue;
                }

                // 🚩 举例：让粒子以“蛇形步”运动
                // 每帧旋转 3°，连续左转 5 帧，再右转 5 帧，循环
                int cycle = 10; // 总周期：5 左 + 5 右
                int phase = p.Time % cycle;

                float rotateAmount = MathHelper.ToRadians(3f); // 每帧旋转角度
                if (phase < 5)
                {
                    // 前 5 帧左转
                    p.Velocity = p.Velocity.RotatedBy(-rotateAmount);
                }
                else
                {
                    // 后 5 帧右转
                    p.Velocity = p.Velocity.RotatedBy(rotateAmount);
                }

                // （可选）演示如何让粒子加速：
                // p.Velocity *= 1.01f;

                // （可选）演示如何让粒子沿螺旋扩散：
                // p.Position += p.Velocity.RotatedBy(Main.GameUpdateCount * 0.05f) * 0.1f;
            }





        }

        // 在类字段区域（用于存储自己生成的 SparkParticle 引用）
        private List<SparkParticle> ownedSparkParticles = new();


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);

            // 原版内置了很多很多的特效【它们可以通过 ParticleOrchestrator.RequestParticleSpawn() 进行调用】，你可以直接检查相关文件来获得相关信息，包括但不限于：
            // 可以看一下这一段：https://gist.github.com/Rijam/971b5252707860b65b582093580aa49c
            // 想去学习的话可以去这里：namespace Terraria.GameContent.Drawing; public class ParticleOrchestrator

            // Keybrand 圣钥剑特效 三棱刺
            // NightsEdge 永夜之刃特效 对于后者只剩下紫色
            // TrueNightsEdge 真·永夜之刃特效 绿色和紫色的双倾斜交叉
            // Excalibur 圣剑特效 光芒四射的剑气粒子
            // TrueExcalibur 真·圣剑特效 更强的光剑爆炸效果，黄色和粉色混合
            // TerraBlade 泰拉之刃特效 十字星但是会与玩家的方向相关
            // PaladinsHammer 圣骑士战锤特效 生成圣光粒子，带有锤子旋转轨迹
            // SlapHand 巨手特效 产生击打音效和轻微冲击波
            // WaffleIron 华夫饼熨斗特效 生成华夫饼的各种黄色粒子特效
            // FlyMeal 飞蝇拍特效 生成苍蝇飞散粒子

            // RainbowRodHit 彩虹魔杖击中特效 各种彩虹色的十字星往周围集体扩散，慢慢消失
            // SilverBulletSparkle 银弹火花 银色十字闪光
            // ShimmerArrow 微光箭特效 闪耀粒子和光点，带有魔法波动
            // StellarTune 星律魔杖特效 生成星云色光斑，带有音乐符号
            // StardustPunch 星尘拳特效 星尘颜色的能量波动，类似星尘龙冲击
            // ShimmerBlock 闪耀方块特效 生成闪烁的光点，类似幻象
            // BlackLightningHit 黑色闪电击中特效 黑紫色闪电冲击，带有能量环
            // BlackLightningSmall 小型黑色闪电 类似 BlackLightningHit，但规模更小

            // ChlorophyteLeafCrystalPassive 叶晶体被动特效 绿色能量粒子，类似叶晶体的能量涌动
            // ChlorophyteLeafCrystalShot 叶晶体射击特效 绿色光束，类似叶晶体发射的激光
            // AshTreeShake 火山灰树震动 产生火山灰尘粒子，模拟树木震落灰烬

            // FlameWaders 火焰行者特效 在地面留下火焰轨迹，类似熔岩靴效果
            // WallOfFleshGoatMountFlames 血肉墙山羊坐骑火焰 生成恶魔山羊燃烧特效

            // PetExchange 宠物交换特效 产生宠物交换时的金色光芒
            // GasTrap 毒气陷阱特效 释放绿色毒雾粒子，扩散状
            // Digestion 消化特效 产生食物消化时的光点和颗粒效果
            // LoadoutChange 装备切换特效 角色切换装备时释放闪光和粒子
            // ItemTransfer 物品传送特效 物品传送时的轨迹粒子，类似魔法传输
            // ShimmerTownNPC 闪耀城镇 NPC 变化 NPC 受到 Shimmer 影响时的变形特效
            // ShimmerTownNPCSend 闪耀城镇 NPC 传送 城镇 NPC 传送时的光斑粒子

            // TownSlimeTransform 城镇史莱姆变形 史莱姆变形时的颜色变换
            // PooFly 苍蝇飞舞特效 生成随机飞舞的小苍蝇粒子

            //ParticleOrchestrator.RequestParticleSpawn(
            //    clientOnly: false,
            //    ParticleOrchestraType.TerraBlade,
            //    new ParticleOrchestraSettings { PositionInWorld = explosionPosition + offset },
            //    Projectile.owner
            //);


        }


        /*
         
         
         
         
         
         
         
         
         
         
         
         
         
         */




    }










}
