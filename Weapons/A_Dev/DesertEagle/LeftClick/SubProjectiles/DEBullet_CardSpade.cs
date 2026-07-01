using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    public class DEBullet_CardSpade : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/CardSpade";

        private static readonly Color SpadeBlue = new(80, 100, 255);
        private static readonly Color SpadeWhite = new(200, 210, 255);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.light = 0.5f;
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha = System.Math.Max(0, Projectile.alpha - 15);

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.25f / 255f, (255 - Projectile.alpha) * 0.3f / 255f, (255 - Projectile.alpha) * 0.6f / 255f);
            Projectile.rotation -= MathHelper.ToRadians(90f) * Projectile.direction;
            Projectile.spriteDirection = Projectile.direction;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, 1, 1, DustID.TintableDustLighted, 0f, 0f, 0, SpadeBlue, 0.5f);
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, back * 1.0f, false, 5, 0.015f, SpadeWhite, new Vector2(0.4f, 2.6f)));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, SpadeBlue, 0.5f, 10));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, Vector2.Zero, false, 8, 0.02f, SpadeWhite, new Vector2(1.2f, 1.2f)));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, SpadeBlue, 0.6f, 12));
            for (int k = 0; k < 6; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height,
                    DustID.TintableDustLighted, Projectile.oldVelocity.X * 0.15f, Projectile.oldVelocity.Y * 0.15f, 0, SpadeBlue);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(lightColor, SpadeBlue, 0.4f), 1);
            return false;
        }
    }
}
