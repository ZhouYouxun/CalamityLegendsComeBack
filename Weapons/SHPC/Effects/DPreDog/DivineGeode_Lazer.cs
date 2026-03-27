using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class DivineGeode_Lazer : ModProjectile
    {
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

            Color c1 = new Color(255, 255, 210);
            Color c2 = new Color(255, 200, 120);

            // Dust几何线
            for (int i = 0; i < 1; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(2f, 2f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.8f, 2.2f);
                dust.color = Color.Lerp(c1, c2, Main.rand.NextFloat());
                dust.fadeIn = 1.8f;
            }

            // 光粒子线
            Vector2 vel = -Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.2f, 0.8f);

            SquishyLightParticle p = new(
                Projectile.Center,
                vel,
                Main.rand.NextFloat(0.4f, 0.9f),
                Color.Lerp(c1, c2, Main.rand.NextFloat()),
                10
            );

            GeneralParticleHandler.SpawnParticle(p);
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