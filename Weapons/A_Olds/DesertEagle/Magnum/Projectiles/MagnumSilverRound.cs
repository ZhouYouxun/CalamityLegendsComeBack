using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.Magnum.Projectiles
{
    internal sealed class MagnumSilverRound : ModProjectile
    {
        private static readonly Color RoundColor = new(214, 224, 236);

        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, RoundColor.ToVector3() * 0.45f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.SilverCoin, -Projectile.velocity * 0.07f, 130, RoundColor, 0.8f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => SpawnImpact();
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpact();
            return true;
        }

        private void SpawnImpact()
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.45f, Pitch = 0.15f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.SilverCoin, Main.rand.NextVector2Circular(4.2f, 4.2f), 110, RoundColor, 0.95f);
                dust.noGravity = true;
            }
        }
    }
}
