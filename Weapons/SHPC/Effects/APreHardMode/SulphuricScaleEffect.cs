using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
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
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(projectile.damage * 0.8f),
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = 75;
            proj.height = 75;

            SoundEngine.PlaySound(SoundID.Item107 with { Volume = 0.42f, Pitch = -0.16f }, projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.22f, Pitch = 0.18f }, projectile.Center);

            Particle blastRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(150, 215, 88),
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One * 0.18f,
                Main.rand.NextFloat(-6f, 6f),
                0.03f,
                0.16f,
                20,
                true,
                0.75f
            );
            GeneralParticleHandler.SpawnParticle(blastRing);

            Particle bloom = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(205, 255, 140) * 0.7f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.12f,
                0f,
                0.08f,
                0.24f,
                18,
                true
            );
            GeneralParticleHandler.SpawnParticle(bloom);

            for (int i = 0; i < 16; i++)
            {
                Vector2 smokeVelocity = Main.rand.NextVector2Circular(8f, 8f) * Main.rand.NextFloat(0.18f, 0.52f);
                Color smokeColor = Main.rand.NextBool() ? new Color(126, 196, 74) : new Color(168, 236, 110);
                Particle smoke = new MediumMistParticle(
                    projectile.Center,
                    smokeVelocity,
                    smokeColor,
                    new Color(28, 40, 18),
                    Main.rand.NextFloat(0.7f, 1.45f),
                    Main.rand.Next(80, 120),
                    0.05f
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 bubbleVelocity = Main.rand.NextVector2Circular(7f, 7f);
                Color bubbleColor = Main.rand.NextBool() ? new Color(112, 168, 64) : new Color(82, 142, 52);
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    projectile.Center,
                    bubbleVelocity,
                    bubbleColor,
                    new Vector2(0.7f, 0.95f),
                    0f,
                    0.11f,
                    0f,
                    34
                );
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int i = 0; i < 12; i++)
            {
                Dust splash = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    0,
                    Color.Lerp(new Color(130, 205, 64), new Color(210, 255, 120), Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.NextFloat(0.8f, 1.2f)
                );
                splash.noGravity = true;
            }

            int count = Main.rand.Next(7, 11);

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
