using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90DefenseRound : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.P90";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.ArmorPenetration = 30;
        }

        public override void AI()
        {
            if (Collision.SolidCollision(Projectile.Center, 5, 5))
                Projectile.Kill();

            if (Projectile.timeLeft == 598)
            {
                for (int i = 0; i <= 3; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool(3) ? 303 : 244,
                        (Projectile.velocity * Main.rand.NextFloat(0.2f, 1.1f)).RotatedByRandom(0.2f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.8f, 1.4f);
                }
            }

            Player owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
            Lighting.AddLight(Projectile.Center, new Vector3(0.19f, 0.17f, 0.08f) * 2f);

            if (targetDist < 1400f && Projectile.timeLeft < 596 && Projectile.timeLeft % 2 == 0)
            {
                int positionVariation = Projectile.timeLeft < 565 ? 25 : Projectile.timeLeft < 585 ? 12 : 5;
                LineParticle spark = new(
                    Projectile.Center - Projectile.velocity * 0.75f + Main.rand.NextVector2Circular(positionVariation, positionVariation),
                    -Projectile.velocity * Main.rand.NextFloat(0.001f, 0.003f),
                    false,
                    4,
                    1.45f,
                    Color.Chocolate);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            GenericSparkle sparker = new(
                Projectile.Center + Projectile.velocity.RotatedByRandom(0.3f),
                Vector2.Zero,
                Color.White,
                Color.Chocolate,
                Main.rand.NextFloat(0.7f, 1.5f),
                Main.rand.Next(9, 17),
                Main.rand.NextFloat(-0.01f, 0.01f),
                2.5f);
            GeneralParticleHandler.SpawnParticle(sparker);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Projectile.velocity * 1.5f + Main.rand.NextVector2Circular(9, 9),
                    Main.rand.NextBool(3) ? 303 : 244,
                    (-Projectile.velocity * Main.rand.NextFloat(0.2f, 3f)).RotatedByRandom(MathHelper.ToRadians(20f)) * Main.rand.NextFloat(0.1f, 0.8f));
                dust.noGravity = true;
                dust.scale = dust.type == 244 ? Main.rand.NextFloat(1.8f, 2.5f) : Main.rand.NextFloat(1.4f, 1.8f);
                dust.fadeIn = dust.type == 244 ? 1.2f : 0f;
            }
        }
    }
}
