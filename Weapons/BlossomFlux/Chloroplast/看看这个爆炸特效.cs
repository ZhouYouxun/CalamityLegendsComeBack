//using CalamityMod.Particles;
//using Microsoft.Xna.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Terraria;

//namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
//{
//    internal class 看看这个爆炸特效
//    {
//    }
//}

//{
//    // ======================== 🌿 追加大自然美感特效 ========================

//    // 🌿 高复杂度 Spark 调用
//    int sparkCount = 20;
//    for (int i = 0; i < sparkCount; i++)
//    {
//        float angle = MathHelper.TwoPi / sparkCount * i;
//        Vector2 initialVelocity = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);

//        Particle spark = new SparkParticle(
//            Projectile.Center,
//            initialVelocity,
//            false,
//            60 + Main.rand.Next(0, 20), // 寿命随机波动
//            Main.rand.NextFloat(0.8f, 1.4f),
//            Color.LimeGreen
//        );
//        GeneralParticleHandler.SpawnParticle(spark);
//        ownedSparkParticles.Add((SparkParticle)spark);
//    }

//    // 🌿 猛烈的森林灵息 Dust 爆发
//    int dustAmount = 100;
//    float maxRadius = 48f;
//    for (int i = 0; i < dustAmount; i++)
//    {
//        Vector2 spawnOffset = Main.rand.NextVector2CircularEdge(maxRadius, maxRadius); // 保证分布在圆环上，避免中心堆积
//        Vector2 spawnPos = Projectile.Center + spawnOffset;

//        Vector2 velocity = spawnOffset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 6f);
//        Dust brightDust = Dust.NewDustPerfect(
//            spawnPos,
//            Main.rand.NextBool() ? 107 : 110, // 绿叶系 Dust 混合
//            velocity,
//            120,
//            Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen,
//            Main.rand.NextFloat(1.0f, 2.2f)
//        );

//        brightDust.noGravity = true;
//        brightDust.fadeIn = Main.rand.NextFloat(0.8f, 1.3f);
//    }


//    // 3) 小而精致的图形 - 使用 PointParticle 拼出【自然之花】五角星花瓣
//    int petals = 5;
//    float petalRadius = 18f;
//    for (int j = 0; j < petals; j++)
//    {
//        float angle = MathHelper.TwoPi / petals * j;
//        Vector2 offset = angle.ToRotationVector2() * petalRadius;
//        Particle point = new PointParticle(
//            Projectile.Center + offset,
//            offset.SafeNormalize(Vector2.Zero) * 0.5f,
//            false,
//            16,
//            1.1f,
//            Color.LawnGreen
//        );
//        GeneralParticleHandler.SpawnParticle(point);
//    }



//    // 生成3个方向不同的翠绿色圆圈粒子特效
//    for (int i = -1; i <= 1; i++) // 三个不同方向，i = -1, 0, 1
//    {
//        // 设置每个粒子的方向，偏移角度根据 i 的值 (-15度, 0度, +15度)
//        Vector2 scatterDirection = Projectile.velocity.RotatedBy(MathHelper.ToRadians(15 * i)) * 0.55f; // 沿着前方偏移 -15, 0, +15 度

//        // 定义一个逐渐扩散的圆圈粒子，调整旋转方向使圆圈摆正
//        Particle pulse = new DirectionalPulseRing(
//            Projectile.Center,
//            scatterDirection,
//            Color.LimeGreen,
//            new Vector2(1f, 2.5f), // 取消旋转比例，使用默认形状
//            Projectile.rotation - MathHelper.PiOver4, // 调整旋转角度，使粒子摆正
//            0.2f, // 粒子透明度衰减
//            0.1f, // 粒子每帧的扩展速度
//            30); // 粒子的存活时间 (帧数)

//        // 生成粒子
//        GeneralParticleHandler.SpawnParticle(pulse);
//    }












//}

