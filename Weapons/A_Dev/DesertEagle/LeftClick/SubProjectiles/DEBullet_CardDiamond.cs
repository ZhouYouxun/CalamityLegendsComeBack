using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    public class DEBullet_CardDiamond : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/CardDiamond";

        private static readonly Color DiamondOrange = new(255, 165, 30);
        private static readonly Color DiamondYellow = new(255, 230, 100);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 0.45f;
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha = System.Math.Max(0, Projectile.alpha - 15);

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.4f / 255f, 0f);
            Projectile.rotation -= MathHelper.ToRadians(90f) * Projectile.direction;
            Projectile.spriteDirection = Projectile.direction;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, 1, 1, DustID.GoldCoin, 0f, 0f, 0, DiamondOrange, 0.5f);
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, back * 0.8f, false, 5, 0.014f, DiamondOrange, new Vector2(0.5f, 1.8f)));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;

            Projectile.position = center;
            Projectile.width = Projectile.height = 50;
            Projectile.Center = center;
            Projectile.maxPenetrate = Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item14, center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, DiamondOrange, 1.1f, 24));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, DiamondOrange * 0.75f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.09f, 20, true, 0.85f));
                for (int i = 0; i < 10; i++)
                {
                    float angle = MathHelper.TwoPi * i / 10f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        center + angle.ToRotationVector2() * 8f, angle.ToRotationVector2() * 10f,
                        false, 12, 0.022f, i % 2 == 0 ? DiamondOrange : DiamondYellow, new Vector2(0.7f, 0.4f)));
                }
            }

            for (int d = 0; d < 25; d++)
            {
                int dustIdx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Web, 0f, 0f, 100, DiamondOrange, 2f);
                Main.dust[dustIdx].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[dustIdx].scale = 0.5f;
                    Main.dust[dustIdx].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int d = 0; d < 15; d++)
            {
                int dustIdx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Web, 0f, 0f, 100, DiamondYellow, 3f);
                Main.dust[dustIdx].noGravity = true;
                Main.dust[dustIdx].velocity *= 5f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
