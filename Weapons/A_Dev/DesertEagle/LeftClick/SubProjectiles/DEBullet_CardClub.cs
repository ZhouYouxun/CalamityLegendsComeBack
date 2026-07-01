using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    public class DEBullet_CardClub : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/CardClub";

        private static readonly Color ClubPurple = new(160, 60, 255);
        private static readonly Color ClubLight = new(210, 140, 255);

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

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.35f / 255f, 0f, (255 - Projectile.alpha) * 0.55f / 255f);
            Projectile.rotation -= MathHelper.ToRadians(90f) * Projectile.direction;
            Projectile.spriteDirection = Projectile.direction;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, 1, 1, DustID.Shadowflame, 0f, 0f, 0, ClubPurple, 0.5f);
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, back * 0.8f, false, 5, 0.014f, ClubPurple, new Vector2(0.5f, 1.7f)));
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
            int splitDmg = (int)(Projectile.damage * 0.55f);
            float baseAngle = Projectile.velocity.ToRotation();
            float splitSpeed = Projectile.velocity.Length();
            int[] offsets = { -8, 0, 8 };
            foreach (int deg in offsets)
            {
                Vector2 splitVel = (baseAngle + MathHelper.ToRadians(deg)).ToRotationVector2() * splitSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y,
                    splitVel.X, splitVel.Y,
                    ModContent.ProjectileType<DEBullet_CardSplit>(),
                    splitDmg, 0f, Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item110, Projectile.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, ClubPurple, 0.75f, 16));
                for (int i = 0; i < 8; i++)
                {
                    float angle = MathHelper.TwoPi * i / 8f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center + angle.ToRotationVector2() * 5f, angle.ToRotationVector2() * 6f,
                        false, 8, 0.017f, i % 2 == 0 ? ClubPurple : ClubLight, new Vector2(0.65f, 0.4f)));
                }
            }
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    Main.rand.NextVector2Circular(5f, 5f), 100, ClubPurple, 0.9f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
