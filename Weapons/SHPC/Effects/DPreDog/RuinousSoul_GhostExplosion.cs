using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class RuinousSoul_GhostExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 170;
            Projectile.height = 210;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            timer++;
            Vector2 center = Projectile.Center;
            Lighting.AddLight(center, new Color(200, 220, 255).ToVector3() * 0.45f);

            if (timer != 1)
                return;

            for (int i = 0; i < 26; i++)
            {
                float angle = MathHelper.TwoPi * i / 26f;
                float xScale = 1f + 0.22f * (float)System.Math.Sin(angle * 3f);
                Vector2 offset = new Vector2((float)System.Math.Cos(angle) * 54f * xScale, (float)System.Math.Sin(angle) * 76f);
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle particle = new(
                    center + offset,
                    velocity,
                    Main.rand.NextFloat(0.45f, 0.9f),
                    Color.Lerp(new Color(240, 250, 255), new Color(130, 160, 220), Main.rand.NextFloat()),
                    Main.rand.Next(18, 28)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 eyeOffset = new Vector2(i < 5 ? -22f : 22f, -22f) + Main.rand.NextVector2Circular(4f, 4f);
                Dust eye = Dust.NewDustPerfect(
                    center + eyeOffset,
                    DustID.SpectreStaff,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(1f, 1.4f));
                eye.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
