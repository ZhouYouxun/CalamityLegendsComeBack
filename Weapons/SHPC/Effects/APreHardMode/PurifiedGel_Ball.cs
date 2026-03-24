using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Terraria.Audio;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGel_Ball : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // ===== 自身旋转 =====
            Projectile.rotation += 0.2f;

            // ===== 主题色（粉+蓝）=====
            Color pink = new Color(255, 140, 200);
            Color blue = new Color(120, 200, 255);

            // ===== 双螺旋（核心）=====
            float sine = (float)System.Math.Sin(Projectile.timeLeft * 0.6f);

            Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitX)
                .RotatedBy(MathHelper.PiOver2) * sine * 12f;

            // 正螺旋
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.8f, 2.2f);
                dust.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
                dust.fadeIn = 1.8f;
            }

            // 反螺旋
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - offset,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.8f, 2.2f);
                dust.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
                dust.fadeIn = 1.8f;
            }

            // ===== 辅助光点 =====
            Vector2 randPos = Projectile.Center + Main.rand.NextVector2Circular(20, 20);

            Dust dust2 = Dust.NewDustPerfect(
                randPos,
                DustID.GemDiamond,
                -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.3f)
            );
            dust2.noGravity = true;
            dust2.scale = Main.rand.NextFloat(0.6f, 0.9f);
            dust2.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ===== 小型爆炸特效（参考电凝矛风格）=====

            // 环形电火花（两层）
            int layers = 2;
            float baseRadius = 20f;

            for (int i = 0; i < layers; i++)
            {
                float radius = baseRadius + i * 18f;
                float rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int j = 0; j < 12; j++)
                {
                    float angle = rotation + MathHelper.TwoPi * j / 12f;
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 10f);

                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GemDiamond,
                        velocity,
                        80,
                        Color.Lerp(new Color(255, 140, 200), new Color(120, 200, 255), Main.rand.NextFloat()),
                        Main.rand.NextFloat(1.2f, 1.6f)
                    );
                    dust.noGravity = true;
                }
            }

            // 漩涡型粒子（核心感觉）
            int sparkCount = 16;
            float spiralRadius = 36f;
            float baseRot = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < sparkCount; i++)
            {
                float angle = baseRot + MathHelper.TwoPi * i / sparkCount;

                Vector2 baseVec = angle.ToRotationVector2() * spiralRadius;
                Vector2 perp = baseVec.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY);

                Vector2 velocity = perp * Main.rand.NextFloat(2f, 4f);

                Particle spark = new SparkParticle(
                    Projectile.Center + baseVec,
                    velocity,
                    false,
                    35,
                    1.2f,
                    Color.Lerp(new Color(255, 140, 200), new Color(120, 200, 255), Main.rand.NextFloat())
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 音效
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
        }













    }
}