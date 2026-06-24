using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.LightningEagle.Projectiles
{
    internal sealed class LightningEagleArcRound : ModProjectile
    {
        private static readonly Color ArcColor = new(88, 194, 255);

        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, ArcColor.ToVector3() * 0.9f);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.Electric, -Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(1.4f, 1.4f), 100, ArcColor, 1.15f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 180);
            SpawnArcBurst();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnArcBurst();
            return true;
        }

        private void SpawnArcBurst()
        {
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.6f, Pitch = 0.15f }, Projectile.Center);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(7.5f, 7.5f), 85, ArcColor, 1.3f);
                dust.noGravity = true;
            }
        }
    }
}
