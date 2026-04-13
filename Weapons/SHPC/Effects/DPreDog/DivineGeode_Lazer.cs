using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class DivineGeode_Lazer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        private int timer;

        public override string Texture => "Terraria/Images/Projectile_0"; // 透明占位

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;

            Projectile.tileCollide = true;
            Projectile.penetrate = 6;

            Projectile.extraUpdates = 10; // 高更新频率核心

            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            timer = 0;
        }

        public override void AI()
        {
            timer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Color c1 = new Color(255, 255, 210);
            Color c2 = new Color(255, 200, 120);

            // 中轴主光：保留 SquishyLightParticle，但大幅削弱亮度、大小、持续时间、刷新频率
            //if (timer % 6 == 0)
            {
                Vector2 squishySpawnPos = Projectile.Center - forward * 2f;
                Vector2 squishyVel = -forward * Main.rand.NextFloat(0.35f, 0.9f);

                SquishyLightParticle coreParticle = new(
                    squishySpawnPos,
                    squishyVel,
                    Main.rand.NextFloat(0.46f, 0.88f), // 大小
                    Color.Lerp(c1, c2, Main.rand.NextFloat()) * 0.82f, // 亮度
                    6 // 原来是 10，这里缩短寿命，减少叠光
                );

                GeneralParticleHandler.SpawnParticle(coreParticle);
            }

            // 锯齿波折线释放：用 BurningRevelationYoyo 那种 ProvidenceMarkParticle 风格
            if (timer % 4 == 0)
            {
                const int zigzagPeriod = 20; // 锯齿波周期，越小越密，越大越舒展
                float zigzagPhase = (timer % zigzagPeriod) / (float)zigzagPeriod;
                float saw = zigzagPhase * 2f - 1f; // -1 到 1 的线性锯齿波

                float zigzagOffset = saw * 12f; // 折线横向振幅
                Vector2 zigzagSpawnPos = Projectile.Center - forward * 4f + right * zigzagOffset;

                Vector2 zigzagVel =
                    (-forward * Main.rand.NextFloat(3.2f, 7.2f) +
                     right * saw * Main.rand.NextFloat(4.5f, 8.5f))
                    .RotatedByRandom(0.12f);

                Particle spark = new CustomSpark(
                    zigzagSpawnPos,
                    zigzagVel,
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    24,
                    Main.rand.NextFloat(0.90f, 1.05f),
                    Main.rand.NextBool(4) ? Color.Khaki : Color.Orange,
                    new Vector2(1.3f, 0.5f),
                    true,
                    false,
                    0,
                    false,
                    false,
                    Main.rand.NextFloat(0.10f, 0.16f)
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 双螺旋快速喷射：位置左右大幅摆动，喷射角度张扬
            if (timer % 3 == 0)
            {
                float helixWave = MathF.Sin(timer * 0.42f);
                float helixRadius = 10f + MathF.Sin(timer * 0.21f) * 2f;
                Vector2 helixOffset = right * helixWave * helixRadius;

                Vector2 spawnA = Projectile.Center - forward * 5f + helixOffset;
                Vector2 spawnB = Projectile.Center - forward * 5f - helixOffset;

                Vector2 baseBack = -forward * Main.rand.NextFloat(5.5f, 10.5f);

                Vector2 velA =
                    baseBack.RotatedBy(0.68f) +
                    right * Main.rand.NextFloat(1.8f, 3.4f);

                Vector2 velB =
                    baseBack.RotatedBy(-0.68f) -
                    right * Main.rand.NextFloat(1.8f, 3.4f);

                Particle spiralSparkA = new SparkParticle(
                    spawnA,
                    velA.RotatedByRandom(0.12f),
                    false,
                    12,
                    Main.rand.NextFloat(0.70f, 0.90f),
                    Main.rand.NextBool(4) ? Color.Khaki : Color.Goldenrod
                );
                GeneralParticleHandler.SpawnParticle(spiralSparkA);

                Particle spiralSparkB = new SparkParticle(
                    spawnB,
                    velB.RotatedByRandom(0.12f),
                    false,
                    12,
                    Main.rand.NextFloat(0.70f, 0.90f),
                    Main.rand.NextBool(4) ? Color.Orange : Color.Goldenrod
                );
                GeneralParticleHandler.SpawnParticle(spiralSparkB);
            }

            // 辅助尾焰：补一点 Holy 系常见的 LightDust，让整体更像神圣尾迹
            if (timer % 5 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * 2f + right * Main.rand.NextFloat(-3f, 3f),
                    ModContent.DustType<LightDust>(),
                    (-forward * Main.rand.NextFloat(1.8f, 4.2f) +
                     right * Main.rand.NextFloat(-1.2f, 1.2f)).RotatedByRandom(0.15f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.55f, 0.80f);
                dust.color = Main.rand.NextBool(4) ? Color.Khaki : Color.Goldenrod;
                dust.noLightEmittence = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 几何反弹（严格轴向反射）
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;

            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;

            return false;
        }



    }
}