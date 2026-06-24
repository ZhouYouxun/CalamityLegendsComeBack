using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.ElephantHunter.Projectiles
{
    internal sealed class ElephantHunterBigGameRound : ModProjectile
    {
        private static readonly Color BrassColor = new(255, 194, 94);

        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.35f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, BrassColor.ToVector3() * 0.5f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, -Projectile.velocity * 0.08f, 105, BrassColor, 1.05f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => SpawnHeavyImpact();
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnHeavyImpact();
            return true;
        }

        private void SpawnHeavyImpact()
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.7f, Pitch = -0.28f }, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.GoldCoin : DustID.Iron, Main.rand.NextVector2Circular(6f, 6f), 100, BrassColor, 1.05f);
                dust.noGravity = true;
            }
        }
    }
}
