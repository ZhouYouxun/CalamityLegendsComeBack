using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class SulphuricScaleEffect : DefaultEffect
    {
        public override int EffectID => 3;
        public override int AmmoType => ModContent.ItemType<SulphuricScale>();


        // 硫磺黄色（从图中提取的偏绿黄）
        public override Color ThemeColor => new Color(200, 255, 80);
        public override Color StartColor => new Color(230, 255, 120);
        public override Color EndColor => new Color(120, 180, 40);
        public override float ExplosionPulseFactor => 0f;
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 模拟重力 =====
            float gravity = 0.18f;
            float maxFallSpeed = 16f;

            projectile.velocity.Y += gravity;

            if (projectile.velocity.Y > maxFallSpeed)
                projectile.velocity.Y = maxFallSpeed;

            // 抵消默认减速
            projectile.velocity *= 1.020408f;
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 保留原本的爆炸本体 =====
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(projectile.damage * 0.8f),
                projectile.knockBack,
                projectile.owner
            );

            if (Main.projectile.IndexInRange(projIndex))
            {
                Projectile spawnedExplosion = Main.projectile[projIndex];
                spawnedExplosion.width = 75;
                spawnedExplosion.height = 75;
            }

            // ===== 音效：主体爆破 + 次级化学爆鸣 =====
            SoundEngine.PlaySound(SoundID.Item107 with { Volume = 0.52f, Pitch = -0.18f }, projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.34f, Pitch = 0.12f }, projectile.Center);

            // ===== IonizingRadiation：严格原封不动保留 =====
            Particle blastRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(150, 215, 88),
                "CalamityLegendsComeBack/Texture/Myown/IonizingRadiation",
                Vector2.One * 0.18f,
                Main.rand.NextFloat(-6f, 6f),
                0.03f,
                0.16f,
                20,
                true,
                0.75f
            );
            GeneralParticleHandler.SpawnParticle(blastRing);

            // ===== 第一层：主核白热闪 + 外层毒光扩张 =====
            Particle coreFlash = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(245, 255, 185) * 0.95f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.18f,
                0f,
                0.13f,
                0.34f,
                16,
                true
            );
            GeneralParticleHandler.SpawnParticle(coreFlash);

            Particle outerBloom = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(182, 255, 96) * 0.82f,
                "CalamityMod/Particles/LargeBloom",
                new Vector2(0.26f, 0.26f),
                Main.rand.NextFloat(-0.08f, 0.08f),
                0.095f,
                0.20f,
                24,
                true
            );
            GeneralParticleHandler.SpawnParticle(outerBloom);

            Particle acidHalo = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(118, 168, 42) * 0.78f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.12f,
                0f,
                0.11f,
                0.10f,
                22,
                true
            );
            GeneralParticleHandler.SpawnParticle(acidHalo);

            // ===== 第二层：有秩序的主爆环，先做出“宏伟感”骨架 =====
            int orderedRingCount = 10;
            float orderedBaseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < orderedRingCount; i++)
            {
                float angle = orderedBaseAngle + MathHelper.TwoPi * i / orderedRingCount;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 ringVelocity = direction * Main.rand.NextFloat(1.2f, 4.8f);

                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    projectile.Center + direction * Main.rand.NextFloat(4f, 12f),
                    ringVelocity,
                    Color.Lerp(new Color(138, 210, 68), new Color(220, 255, 128), Main.rand.NextFloat(0.15f, 0.75f)),
                    new Vector2(Main.rand.NextFloat(0.95f, 1.28f), Main.rand.NextFloat(1.10f, 1.75f)),
                    angle,
                    Main.rand.NextFloat(0.12f, 0.18f),
                    0f,
                    Main.rand.Next(30, 42)
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // ===== 第三层：中心毒烟团，厚而不死黑，负责“体积感” =====
            for (int i = 0; i < 24; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 smokeVelocity =
                    direction * Main.rand.NextFloat(1.2f, 4.8f) +
                    new Vector2(0f, Main.rand.NextFloat(-1.6f, -0.35f));

                Color smokeColor = Main.rand.NextBool()
                    ? new Color(126, 196, 74)
                    : new Color(186, 245, 110);

                Particle smoke = new MediumMistParticle(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    smokeVelocity,
                    smokeColor,
                    new Color(24, 34, 12),
                    Main.rand.NextFloat(1.05f, 1.85f),
                    Main.rand.Next(95, 145),
                    0.045f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ===== 第四层：外圈抬升毒雾，制造大范围污染扩散 =====
            for (int i = 0; i < 18; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 mistVelocity =
                    direction * Main.rand.NextFloat(3.4f, 7.2f) +
                    new Vector2(0f, Main.rand.NextFloat(-2.8f, -0.8f));

                Particle mist = new MediumMistParticle(
                    projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    mistVelocity,
                    Color.Lerp(new Color(168, 236, 110), new Color(112, 168, 64), Main.rand.NextFloat(0.15f, 0.8f)),
                    new Color(20, 26, 10),
                    Main.rand.NextFloat(0.82f, 1.45f),
                    Main.rand.Next(120, 180),
                    0.035f
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }

            // ===== 第五层：黄金角碎屑螺旋，给“优雅美感” =====
            float goldenAngle = MathHelper.TwoPi * 0.38196601125f;
            for (int i = 0; i < 34; i++)
            {
                float t = i + 1f;
                float angle = goldenAngle * t + Main.rand.NextFloat(-0.08f, 0.08f);
                float speed = 1.8f + 0.42f * t;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangential = outward.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.2f, 1.2f);

                Dust shard = Dust.NewDustPerfect(
                    projectile.Center + outward * Main.rand.NextFloat(3f, 14f),
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.FireworksRGB,
                    outward * speed + tangential,
                    0,
                    Color.Lerp(new Color(130, 205, 64), new Color(235, 255, 140), Main.rand.NextFloat(0.15f, 0.85f)),
                    Main.rand.NextFloat(0.95f, 1.45f)
                );
                shard.noGravity = true;
            }

            // ===== 第六层：近距离高能溅射，负责“爆破感”和混乱 =====
            for (int i = 0; i < 26; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity =
                    direction * Main.rand.NextFloat(4.2f, 10.6f) +
                    new Vector2(0f, Main.rand.NextFloat(-1.2f, 1.0f));

                Dust splash = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(2) ? DustID.FireworksRGB : DustID.Smoke,
                    velocity,
                    0,
                    Color.Lerp(new Color(140, 220, 74), new Color(220, 255, 128), Main.rand.NextFloat(0.1f, 0.9f)),
                    Main.rand.NextFloat(1.05f, 1.7f)
                );
                splash.noGravity = true;
            }

            // ===== 第七层：收尾白核，避免中心被大雾吃掉 =====
            for (int i = 0; i < 12; i++)
            {
                Dust coreDust = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.FireworksRGB,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 4.6f),
                    0,
                    new Color(240, 255, 190),
                    Main.rand.NextFloat(1f, 1.3f)
                );
                coreDust.noGravity = true;
            }

            // ===== 第八层：真正的持续污染物，数量和速度都拉高 =====
            int count = Main.rand.Next(14, 19);
            float cloudBaseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                float angle;

                // 一半按有秩序环形放出，另一半随机，兼顾“优雅”和“混乱”
                if (i < count / 2)
                    angle = cloudBaseAngle + MathHelper.TwoPi * i / (count / 2f);
                else
                    angle = Main.rand.NextFloat(MathHelper.TwoPi);

                float speed = i < count / 2
                    ? Main.rand.NextFloat(3.6f, 6.8f)
                    : Main.rand.NextFloat(2.4f, 7.8f);

                Vector2 velocity = angle.ToRotationVector2() * speed + new Vector2(0f, Main.rand.NextFloat(-0.8f, 0.6f));

                int projType;
                int rand = Main.rand.Next(3);
                if (rand == 0)
                    projType = ProjectileID.ToxicCloud;
                else if (rand == 1)
                    projType = ProjectileID.ToxicCloud2;
                else
                    projType = ProjectileID.ToxicCloud3;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    velocity,
                    projType,
                    (int)(projectile.damage * 0.24f),
                    projectile.knockBack,
                    projectile.owner
                );
            }













            count = Main.rand.Next(7, 11);
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(1.6f, 4.2f);

                Vector2 velocity = angle.ToRotationVector2() * speed;

                int projType;

                // 三种毒云随机
                int rand = Main.rand.Next(3);
                if (rand == 0)
                    projType = ProjectileID.ToxicCloud;
                else if (rand == 1)
                    projType = ProjectileID.ToxicCloud2;
                else
                    projType = ProjectileID.ToxicCloud3;



                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    projType,
                    (int)(projectile.damage * 0.24f),
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }
    }
}
