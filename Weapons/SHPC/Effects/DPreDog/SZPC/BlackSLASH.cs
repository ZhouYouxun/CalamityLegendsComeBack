using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNether_BlackSLASH : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/SZPC/BlackSLASH";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 35;
            Projectile.MaxUpdates = 2;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 18;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Projectile.timeLeft / 35f;

            if (Projectile.timeLeft >= 24 && Main.rand.NextBool(2))
            {
                Particle spark = new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.05f, 0.18f),
                    false,
                    12,
                    Main.rand.NextFloat(0.03f, 0.05f),
                    Color.Lerp(new Color(135, 75, 210), Color.Black, Main.rand.NextFloat(0.15f, 0.55f)),
                    new Vector2(2f, 0.5f),
                    true);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    Projectile.velocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.15f, 0.4f),
                    0,
                    Color.Lerp(new Color(145, 90, 220), Color.Black, Main.rand.NextFloat(0.1f, 0.45f)),
                    Main.rand.NextFloat(0.95f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(new Color(135, 75, 210), Color.Black, 0.55f) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
