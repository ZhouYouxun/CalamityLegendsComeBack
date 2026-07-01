using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    public class DEBullet_CardHeart : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/CardHeart";

        private static readonly Color HeartRed = new(255, 60, 80);
        private static readonly Color HeartPink = new(255, 160, 170);

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
            Projectile.penetrate = 2;
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
                Projectile.alpha = Math.Max(0, Projectile.alpha - 15);

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, 0f, (255 - Projectile.alpha) * 0.15f / 255f);
            Projectile.rotation -= MathHelper.ToRadians(90f) * Projectile.direction;
            Projectile.spriteDirection = Projectile.direction;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, 1, 1, DustID.Web, 0f, 0f, 0, HeartRed, 0.5f);
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, back * 0.8f, false, 5, 0.014f, HeartRed, new Vector2(0.55f, 1.9f)));
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
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, HeartRed, 0.7f, 14));
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi * i / 6f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center + angle.ToRotationVector2() * 5f, angle.ToRotationVector2() * 5f,
                        false, 8, 0.016f, i % 2 == 0 ? HeartRed : HeartPink, new Vector2(0.7f, 0.4f)));
                }
            }
            for (int k = 0; k < 6; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height,
                    DustID.Web, Projectile.oldVelocity.X * 0.15f, Projectile.oldVelocity.Y * 0.15f, 0, HeartRed);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].SpawnLifeStealProjectile(target, Projectile,
                ProjectileID.VampireHeal, Math.Max(1, (int)Math.Round(hit.Damage * 0.02)), 0.4f);
            SpawnHeartImpact(Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }

        private static void SpawnHeartImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, HeartRed, 0.85f, 18));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, HeartRed * 0.65f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.07f, 16, true, 0.8f));
                for (int i = 0; i < 10; i++)
                {
                    float angle = MathHelper.TwoPi * i / 10f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        pos + angle.ToRotationVector2() * 6f, angle.ToRotationVector2() * 7f,
                        false, 9, 0.018f, i % 2 == 0 ? HeartRed : HeartPink, new Vector2(0.85f, 0.42f)));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.Web, Main.rand.NextVector2Circular(6f, 6f), 100, HeartRed, 1.0f);
                d.noGravity = true;
            }
        }
    }
}
