using CalamityMod;
using CalamityMod.Utilities;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.Magnum.Projectiles
{
    internal sealed class MagnumLifeRound : ModProjectile
    {
        private static readonly Color LifeColor = new(255, 120, 124);

        public override string Texture => "CalamityMod/Projectiles/Ranged/ShockblastRound";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 480;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            AIType = ProjectileID.Bullet;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, LifeColor.ToVector3() * 0.55f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch, -Projectile.velocity * 0.055f, 125, LifeColor, 0.9f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].SpawnLifeStealProjectile(target, Projectile, ModContent.ProjectileType<TransfusionTrail>(), (int)(damageDone * 0.08f));
            SpawnImpact();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpact();
            return true;
        }

        private void SpawnImpact()
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.55f, Pitch = -0.05f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.RedTorch : DustID.SilverCoin, Main.rand.NextVector2Circular(5.5f, 5.5f), 120, LifeColor, 1.05f);
                dust.noGravity = true;
            }
        }
    }
}
