using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Terraria;
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



            // 消亡时释放爆炸特效
            Particle blastRing = new CustomPulse(
                projectile.Center, Vector2.Zero, Color.Khaki,
                "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/IonizingRadiation",
                Vector2.One * 0.33f, Main.rand.NextFloat(-10f, 10f),
                0.07f, 0.33f, 30
            );
            GeneralParticleHandler.SpawnParticle(blastRing);



            int count = Main.rand.Next(9, 16); // 9~15个

            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(2f, 6f);

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
                    (int)(projectile.damage * 0.1f),
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }
    }
}