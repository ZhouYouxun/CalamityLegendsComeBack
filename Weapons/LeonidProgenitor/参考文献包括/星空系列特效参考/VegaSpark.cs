
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Rogue
{
    public class VegaSpark : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Rogue";
        public static int lifetime = 150;

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = lifetime;
            Projectile.DamageType = RogueDamageClass.Instance;
            Projectile.localAI[0] = 20f;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.MaxUpdates = 2;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.direction * Projectile.ai[0];

            if (Projectile.timeLeft < (lifetime - Projectile.ai[1]) && Projectile.localAI[0] >= 0)
            {
                Projectile.velocity.Normalize();
                Projectile.velocity *= Projectile.localAI[0];
                Projectile.localAI[0]--;
            }

            if (Projectile.localAI[0] == 0)
            {
                Projectile.Kill();
            }
            GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center,Projectile.velocity* 0.001f,false,10,1,new Color(69,69,200)));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustToUse = Main.rand.Next(0, 3);
                int dustType = 0;
                switch (dustToUse)
                {
                    case 0:
                        dustType = 109;
                        break;
                    case 1:
                        dustType = 111;
                        break;
                    case 2:
                        dustType = 132;
                        break;
                }

                int dust = Dust.NewDust(Projectile.Center, 1, 1, dustType, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.75f;
            }
        }
    }
}
